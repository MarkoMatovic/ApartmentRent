using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lander.src.Modules.Listings.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// Geocoding through the public OpenStreetMap Nominatim API (free, no key).
///
/// Nominatim's usage policy (https://operations.osmfoundation.org/policies/nominatim/) is respected:
///   • at most one request per second  → every call waits for a process-wide gate;
///   • an identifying User-Agent       → "Geocoding:UserAgent" (set a contact there in production);
///   • no repeated lookups             → answers are cached (found: 30 days, not found: 1 hour).
///
/// Config (appsettings.json → "Geocoding"):
///   Enabled        – turn geocoding off entirely                      (default true)
///   UserAgent      – identifies the app to OpenStreetMap              (default "TuRentaj/1.0")
///   CountryCodes   – optional comma list ("rs,ba,hr") to narrow hits  (default none)
///   MinIntervalMs  – gap between requests                             (default 1100)
/// </summary>
public sealed partial class NominatimGeocodingService : IGeocodingService
{
    public const string HttpClientName = "Nominatim";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTime _lastRequestUtc = DateTime.MinValue;

    private static readonly TimeSpan FoundTtl = TimeSpan.FromDays(30);
    private static readonly TimeSpan NotFoundTtl = TimeSpan.FromHours(1);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<NominatimGeocodingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GeoPoint?> GeocodeAsync(string? address, string? city, string? postalCode, CancellationToken ct = default)
    {
        if (!_configuration.GetValue("Geocoding:Enabled", true))
            return null;

        var query = BuildQuery(address, city, postalCode);
        if (query is null)
            return null;

        var cacheKey = "geocode:" + query.ToLowerInvariant();
        if (_cache.TryGetValue(cacheKey, out CachedLookup? cached) && cached is not null)
            return cached.Point;

        try
        {
            var point = await LookUpAsync(query, ct);
            _cache.Set(cacheKey, new CachedLookup(point), point is null ? NotFoundTtl : FoundTtl);
            return point;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Unreachable / timeout / unexpected payload: do not cache, a retry later may work.
            _logger.LogWarning(ex, "Geocoding failed for '{Query}'", query);
            return null;
        }
    }

    /// <summary>"Valjevska , Beograd" → "Valjevska, Beograd". Needs at least a street and a city.</summary>
    internal static string? BuildQuery(string? address, string? city, string? postalCode)
    {
        var street = Clean(address);
        var town = Clean(city);
        if (street.Length == 0 || town.Length == 0)
            return null;

        var postal = Clean(postalCode);
        return postal.Length == 0 ? $"{street}, {town}" : $"{street}, {postal} {town}";
    }

    private static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        // collapse runs of whitespace and drop spaces/commas around the edges
        return WhitespaceRun().Replace(value, " ").Trim(' ', ',');
    }

    private async Task<GeoPoint?> LookUpAsync(string query, CancellationToken ct)
    {
        var url = new StringBuilder("search?format=jsonv2&limit=1&q=").Append(Uri.EscapeDataString(query));
        var countries = _configuration["Geocoding:CountryCodes"];
        if (!string.IsNullOrWhiteSpace(countries))
            url.Append("&countrycodes=").Append(Uri.EscapeDataString(countries.Replace(" ", "")));

        await Gate.WaitAsync(ct);
        try
        {
            var minInterval = TimeSpan.FromMilliseconds(_configuration.GetValue("Geocoding:MinIntervalMs", 1100));
            var wait = _lastRequestUtc + minInterval - DateTime.UtcNow;
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, ct);

            var client = _httpClientFactory.CreateClient(HttpClientName);
            try
            {
                using var response = await client.GetAsync(url.ToString(), ct);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(ct);
                return Parse(json);
            }
            finally
            {
                _lastRequestUtc = DateTime.UtcNow;
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    internal static GeoPoint? Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            return null;

        var first = root[0];
        if (!first.TryGetProperty("lat", out var latEl) || !first.TryGetProperty("lon", out var lonEl))
            return null;

        if (!double.TryParse(latEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) ||
            !double.TryParse(lonEl.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            return null;

        if (lat is < -90 or > 90 || lon is < -180 or > 180)
            return null;

        return new GeoPoint(Math.Round((decimal)lat, 6), Math.Round((decimal)lon, 6));
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();

    private sealed record CachedLookup(GeoPoint? Point);
}
