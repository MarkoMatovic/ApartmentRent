using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// Fetches neighbourhood livability data for an apartment:
///   • Walk Score / Transit Score / Bike Score  — walkscore.com API (optional, needs API key)
///   • Nearby POIs (schools, parks, markets…)   — OpenStreetMap Overpass API (free, no key)
///
/// Results are cached via HybridCache for 24 hours (scores rarely change).
///
/// Config keys (appsettings.json → "WalkScore"):
///   ApiKey  — Walk Score API key.  Leave empty to skip scores and return POIs only.
/// </summary>
public sealed class NeighbourhoodService : INeighbourhoodService
{
    private readonly ListingsContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HybridCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NeighbourhoodService> _logger;

    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    // Max POIs returned per category so the response stays tidy.
    private const int MaxPerCategory = 5;

    public NeighbourhoodService(
        ListingsContext db,
        IHttpClientFactory httpClientFactory,
        HybridCache cache,
        IConfiguration configuration,
        ILogger<NeighbourhoodService> logger)
    {
        _db                = db;
        _httpClientFactory = httpClientFactory;
        _cache             = cache;
        _configuration     = configuration;
        _logger            = logger;
    }

    public async Task<NeighbourhoodInsightsDto?> GetInsightsAsync(
        int apartmentId, CancellationToken ct = default)
    {
        // ── Pull coordinates from DB ─────────────────────────────────────────────
        var coords = await _db.Apartments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.ApartmentId == apartmentId)
            .Select(a => new { a.Latitude, a.Longitude, a.Address, a.City })
            .FirstOrDefaultAsync(ct);

        if (coords is null || !coords.Latitude.HasValue || !coords.Longitude.HasValue)
            return null;

        double lat = (double)coords.Latitude.Value;
        double lon = (double)coords.Longitude.Value;
        string address = $"{coords.Address ?? ""}, {coords.City ?? ""}".Trim(',', ' ');

        // ── HybridCache (L1 in-process + L2 Redis) — 24 h TTL ───────────────────
        var cacheKey = $"neighbourhood:{apartmentId}";
        return await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel => await FetchInsightsAsync(lat, lon, address, cancel),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(24) },
            cancellationToken: ct);
    }

    // ────────────────────────────────────────────────────────────────────────────

    private async Task<NeighbourhoodInsightsDto> FetchInsightsAsync(
        double lat, double lon, string address, CancellationToken ct)
    {
        var result = new NeighbourhoodInsightsDto { FetchedAt = DateTime.UtcNow };

        // Run both external calls in parallel; each is fire-and-handle (errors logged, not thrown).
        await Task.WhenAll(
            FetchWalkScoreAsync(lat, lon, address, result, ct),
            FetchOverpassPoiAsync(lat, lon, result, ct));

        return result;
    }

    // ── Walk Score API ───────────────────────────────────────────────────────────

    private async Task FetchWalkScoreAsync(
        double lat, double lon, string address,
        NeighbourhoodInsightsDto target, CancellationToken ct)
    {
        var apiKey = _configuration["WalkScore:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return; // key not configured — silently skip

        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"https://api.walkscore.com/score" +
                      $"?format=json&transit=1&bike=1" +
                      $"&lat={lat:F6}&lon={lon:F6}" +
                      $"&address={encodedAddress}" +
                      $"&wsapikey={apiKey}";

            var client = _httpClientFactory.CreateClient("WalkScore");
            using var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(ct);
            var ws   = JsonSerializer.Deserialize<WalkScoreApiResponse>(body, _jsonOpts);

            if (ws is null || ws.Status != 1) return;

            target.WalkScore             = ws.Walkscore;
            target.WalkScoreDescription  = ws.Description;
            target.TransitScore          = ws.Transit?.Score;
            target.TransitScoreDescription = ws.Transit?.Description;
            target.BikeScore             = ws.Bike?.Score;
            target.BikeScoreDescription  = ws.Bike?.Description;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Walk Score API call failed for ({Lat},{Lon})", lat, lon);
        }
    }

    // ── OpenStreetMap Overpass API ───────────────────────────────────────────────

    private async Task FetchOverpassPoiAsync(
        double lat, double lon,
        NeighbourhoodInsightsDto target, CancellationToken ct)
    {
        try
        {
            // Overpass QL: nodes/ways within radius for each amenity group.
            // Separate radii: transit 800 m, others 600 m.
            var query = $@"[out:json][timeout:12];
(
  node[""amenity""~""school|kindergarten|university|college""](around:600,{lat:F6},{lon:F6});
  node[""shop""~""supermarket|convenience|grocery""](around:600,{lat:F6},{lon:F6});
  node[""amenity""~""restaurant|cafe|fast_food""](around:600,{lat:F6},{lon:F6});
  node[""leisure""~""park|garden|nature_reserve""](around:600,{lat:F6},{lon:F6});
  node[""amenity""~""hospital|clinic|doctors""](around:600,{lat:F6},{lon:F6});
  node[""amenity""=""pharmacy""](around:600,{lat:F6},{lon:F6});
  node[""highway""=""bus_stop""](around:400,{lat:F6},{lon:F6});
  node[""railway""~""station|halt|tram_stop|subway_entrance""](around:800,{lat:F6},{lon:F6});
  node[""station""=""subway""](around:800,{lat:F6},{lon:F6});
);
out center 80;";

            var client  = _httpClientFactory.CreateClient("Overpass");
            var content = new StringContent($"data={Uri.EscapeDataString(query)}", Encoding.UTF8,
                                            "application/x-www-form-urlencoded");

            using var response = await client.PostAsync("https://overpass-api.de/api/interpreter", content, ct);
            response.EnsureSuccessStatusCode();

            var body    = await response.Content.ReadAsStringAsync(ct);
            var overpass = JsonSerializer.Deserialize<OverpassResponse>(body, _jsonOpts);

            if (overpass?.Elements is null) return;

            // ── Categorise + deduplicate + limit per category ────────────────────
            var byCategory = new Dictionary<string, List<PointOfInterestDto>>(StringComparer.Ordinal);

            foreach (var el in overpass.Elements)
            {
                if (el.Tags is null) continue;

                double elLat = el.Type == "way" ? (el.Center?.Lat ?? el.Lat) : el.Lat;
                double elLon = el.Type == "way" ? (el.Center?.Lon ?? el.Lon) : el.Lon;

                var (category, icon) = ClassifyTags(el.Tags);
                if (category == "other") continue;

                if (!byCategory.TryGetValue(category, out var list))
                    byCategory[category] = list = new List<PointOfInterestDto>();

                if (list.Count >= MaxPerCategory) continue;

                var name = el.Tags.TryGetValue("name", out var n) && !string.IsNullOrWhiteSpace(n)
                    ? n
                    : FriendlyFallbackName(category);

                list.Add(new PointOfInterestDto
                {
                    Name           = name,
                    Category       = category,
                    Icon           = icon,
                    DistanceMeters = (int)Math.Round(HaversineMeters(lat, lon, elLat, elLon)),
                    Latitude       = elLat,
                    Longitude      = elLon
                });
            }

            // Flatten, sort by distance within each category, then globally.
            target.NearbyPlaces = byCategory.Values
                .SelectMany(l => l)
                .OrderBy(p => p.DistanceMeters)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Overpass API call failed for ({Lat},{Lon})", lat, lon);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static (string Category, string Icon) ClassifyTags(Dictionary<string, string> tags)
    {
        if (tags.TryGetValue("amenity", out var amenity))
        {
            return amenity switch
            {
                "school" or "kindergarten" or "university" or "college"
                    => ("school",      "🏫"),
                "restaurant" or "cafe" or "fast_food" or "food_court"
                    => ("restaurant",  "🍽️"),
                "hospital" or "clinic" or "doctors"
                    => ("healthcare",  "🏥"),
                "pharmacy"
                    => ("pharmacy",    "💊"),
                "bus_station"
                    => ("transit",     "🚌"),
                "marketplace"
                    => ("supermarket", "🛒"),
                _ => ("other", "📍")
            };
        }

        if (tags.TryGetValue("shop", out var shop))
        {
            return shop switch
            {
                "supermarket" or "convenience" or "grocery"
                    => ("supermarket", "🛒"),
                _ => ("other", "📍")
            };
        }

        if (tags.TryGetValue("leisure", out var leisure))
        {
            return leisure switch
            {
                "park" or "garden" or "nature_reserve"
                    => ("park",  "🌳"),
                "playground"
                    => ("park",  "🎠"),
                _ => ("other", "📍")
            };
        }

        if (tags.TryGetValue("highway", out var highway) && highway == "bus_stop")
            return ("transit", "🚌");

        if (tags.TryGetValue("railway", out var railway))
        {
            return railway switch
            {
                "station" or "halt"      => ("transit", "🚉"),
                "tram_stop"              => ("transit", "🚊"),
                "subway_entrance"        => ("transit", "🚇"),
                _ => ("other", "📍")
            };
        }

        if (tags.TryGetValue("station", out var station) && station == "subway")
            return ("transit", "🚇");

        return ("other", "📍");
    }

    private static string FriendlyFallbackName(string category) => category switch
    {
        "school"      => "Škola",
        "supermarket" => "Supermarket",
        "restaurant"  => "Restoran",
        "park"        => "Park",
        "transit"     => "Stanica",
        "healthcare"  => "Zdravstvena ustanova",
        "pharmacy"    => "Apoteka",
        _ => "Objekat"
    };

    /// <summary>Haversine great-circle distance in metres.</summary>
    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6_371_000;
        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δφ = (lat2 - lat1) * Math.PI / 180;
        var Δλ = (lon2 - lon1) * Math.PI / 180;
        var a  = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2)
               + Math.Cos(φ1) * Math.Cos(φ2)
               * Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // ── Walk Score API response models ───────────────────────────────────────────

    private sealed class WalkScoreApiResponse
    {
        public int     Status      { get; set; }
        public int?    Walkscore   { get; set; }
        public string? Description { get; set; }
        public WsTransit? Transit  { get; set; }
        public WsBike?    Bike     { get; set; }
    }
    private sealed class WsTransit
    {
        public int     Score       { get; set; }
        public string? Description { get; set; }
    }
    private sealed class WsBike
    {
        public int     Score       { get; set; }
        public string? Description { get; set; }
    }

    // ── Overpass API response models ─────────────────────────────────────────────

    private sealed class OverpassResponse
    {
        public List<OverpassElement>? Elements { get; set; }
    }
    private sealed class OverpassElement
    {
        public string  Type   { get; set; } = string.Empty;
        public long    Id     { get; set; }
        public double  Lat    { get; set; }
        public double  Lon    { get; set; }
        [JsonPropertyName("tags")]
        public Dictionary<string, string>? Tags   { get; set; }
        public OverpassCenter?             Center { get; set; }
    }
    private sealed class OverpassCenter
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
    }
}
