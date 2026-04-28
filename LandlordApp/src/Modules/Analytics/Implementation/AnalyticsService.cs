using System.Text.Json;
using Lander;
using Lander.src.Modules.Analytics.Dtos.Dto;
using Lander.src.Modules.Analytics.Interfaces;
using Lander.src.Modules.Analytics.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lander.src.Modules.Analytics.Implementation;

public class AnalyticsService : IAnalyticsService
{
    private readonly AnalyticsContext _context;
    private readonly ListingsContext _listingsContext;
    private readonly RoommatesContext _roommatesContext;
    private readonly UsersContext _usersContext;
    private readonly ILogger<AnalyticsService> _logger;

    // Known bot/crawler User-Agent substrings — extend as needed
    private static readonly string[] BotSignatures =
    [
        "bot", "crawler", "spider", "slurp", "bingpreview",
        "facebookexternalhit", "twitterbot", "linkedinbot",
        "whatsapp", "telegrambot", "curl", "wget", "python-requests"
    ];

    public AnalyticsService(
        AnalyticsContext context,
        ListingsContext listingsContext,
        RoommatesContext roommatesContext,
        UsersContext usersContext,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _listingsContext = listingsContext;
        _roommatesContext = roommatesContext;
        _usersContext = usersContext;
        _logger = logger;
    }

    // -----------------------------------------------------------------------
    // Date helpers
    // -----------------------------------------------------------------------

    // When the caller sends a date-only value (e.g. "2026-04-28"), ASP.NET Core
    // parses it as 2026-04-28T00:00:00, which excludes all events from that day.
    // Bump it to 2026-04-28T23:59:59.9999999 so the full day is included.
    private static DateTime NormalizeToDate(DateTime to)
        => to.TimeOfDay == TimeSpan.Zero ? to.AddDays(1).AddTicks(-1) : to;

    private IQueryable<AnalyticsEvent> ApplyDateRange(
        IQueryable<AnalyticsEvent> query, DateTime? from, DateTime? to)
    {
        if (from.HasValue) query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)   query = query.Where(e => e.CreatedDate <= NormalizeToDate(to.Value));
        return query;
    }

    // -----------------------------------------------------------------------
    // Track event
    // -----------------------------------------------------------------------

    public async Task TrackEventAsync(
        string eventType,
        string category,
        int? entityId = null,
        string? entityType = null,
        string? searchQuery = null,
        Dictionary<string, string>? metadata = null,
        int? userId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            // 1. Drop bot/crawler traffic — keeps view counts clean
            if (!string.IsNullOrEmpty(userAgent))
            {
                var ua = userAgent.ToLowerInvariant();
                if (BotSignatures.Any(sig => ua.Contains(sig)))
                    return;
            }

            // 2. Incognito opt-out — single query using AsNoTracking to avoid EF overhead
            if (userId.HasValue)
            {
                var isIncognito = await _usersContext.Users
                    .AsNoTracking()
                    .Where(u => u.UserId == userId.Value)
                    .Select(u => u.IsIncognito)
                    .FirstOrDefaultAsync();
                if (isIncognito) return;
            }

            // 3. Deduplicate view events: same user + same entity + same event within 30 min
            //    counts as one visit, not multiple refreshes.
            //    For anonymous traffic (no userId) we skip dedup — the bot filter above
            //    handles crawlers; occasional anonymous re-loads are acceptable noise.
            if (entityId.HasValue && userId.HasValue &&
                (eventType == "ApartmentView" || eventType == "RoommateView"))
            {
                var window = DateTime.UtcNow.AddMinutes(-30);
                var alreadyTracked = await _context.AnalyticsEvents
                    .AsNoTracking()
                    .AnyAsync(e =>
                        e.UserId == userId &&
                        e.EntityId == entityId &&
                        e.EventType == eventType &&
                        e.CreatedDate >= window);
                if (alreadyTracked) return;
            }

            // 4. Skip self-views: landlord viewing own apartment, user viewing own roommate profile
            if (entityId.HasValue && userId.HasValue)
            {
                if (eventType == "ApartmentView")
                {
                    var isOwner = await _listingsContext.Apartments
                        .AsNoTracking()
                        .AnyAsync(a => a.ApartmentId == entityId && a.LandlordId == userId);
                    if (isOwner) return;
                }
                else if (eventType == "RoommateView")
                {
                    var isOwner = await _roommatesContext.Roommates
                        .AsNoTracking()
                        .AnyAsync(r => r.RoommateId == entityId && r.UserId == userId);
                    if (isOwner) return;
                }
            }

            var analyticsEvent = new AnalyticsEvent
            {
                EventType = eventType,
                EventCategory = category,
                EntityId = entityId,
                EntityType = entityType,
                SearchQuery = searchQuery,
                MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedDate = DateTime.UtcNow
            };
            _context.AnalyticsEvents.Add(analyticsEvent);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Analytics must never break the main request — log and continue
            _logger.LogWarning(ex, "Failed to track analytics event {EventType} for entity {EntityId}", eventType, entityId);
        }
    }

    // -----------------------------------------------------------------------
    // Summary — single GROUP BY query instead of 5 separate COUNTs
    // -----------------------------------------------------------------------

    public async Task<AnalyticsSummaryDto> GetSummaryAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(_context.AnalyticsEvents.AsNoTracking(), from, to);

        // One round-trip: group by EventType, count per type
        var countsByType = await query
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToListAsync();

        var byType = countsByType.ToDictionary(x => x.EventType, x => x.Count);
        var totalEvents = byType.Values.Sum();
        var apartmentViews = byType.GetValueOrDefault("ApartmentView");
        var roommateViews  = byType.GetValueOrDefault("RoommateView");
        var contactClicks  = byType.GetValueOrDefault("ContactClick");
        var searches = byType.Where(kv => kv.Key.Contains("Search")).Sum(kv => kv.Value);

        var eventsByCategory = await query
            .GroupBy(e => e.EventCategory)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count);

        return new AnalyticsSummaryDto
        {
            TotalEvents = totalEvents,
            TotalApartmentViews = apartmentViews,
            TotalRoommateViews = roommateViews,
            TotalSearches = searches,
            TotalContactClicks = contactClicks,
            EventsByCategory = eventsByCategory,
            FromDate = from,
            ToDate = to
        };
    }

    // -----------------------------------------------------------------------
    // Top lists
    // -----------------------------------------------------------------------

    public async Task<List<TopEntityDto>> GetTopViewedApartmentsAsync(int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking()
                .Where(e => e.EventType == "ApartmentView" && e.EntityId.HasValue),
            from, to);

        var topApartments = await query
            .GroupBy(e => e.EntityId!.Value)
            .Select(g => new TopEntityDto
            {
                EntityId = g.Key,
                EntityType = "Apartment",
                ViewCount = g.Count()
            })
            .OrderByDescending(x => x.ViewCount)
            .Take(count)
            .ToListAsync();

        var apartmentIds = topApartments.Select(a => a.EntityId).ToList();
        var apartments = await _listingsContext.Apartments
            .AsNoTracking()
            .Where(a => apartmentIds.Contains(a.ApartmentId) && !a.IsDeleted)
            .Select(a => new { a.ApartmentId, a.Title, a.City, a.Rent, a.Address })
            .ToListAsync();

        var apartmentDict = apartments.ToDictionary(a => a.ApartmentId);
        foreach (var apartment in topApartments)
        {
            if (apartmentDict.TryGetValue(apartment.EntityId, out var apt))
            {
                apartment.EntityTitle = apt.Title;
                apartment.EntityDetails = $"{apt.City ?? "N/A"} - {apt.Rent:C}/month";
            }
        }
        return topApartments;
    }

    public async Task<List<TopEntityDto>> GetTopViewedRoommatesAsync(int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking()
                .Where(e => e.EventType == "RoommateView" && e.EntityId.HasValue),
            from, to);

        return await query
            .GroupBy(e => e.EntityId!.Value)
            .Select(g => new TopEntityDto
            {
                EntityId = g.Key,
                EntityType = "Roommate",
                ViewCount = g.Count()
            })
            .OrderByDescending(x => x.ViewCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<SearchTermDto>> GetTopSearchTermsAsync(int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking()
                .Where(e => e.EventType.Contains("Search") && !string.IsNullOrEmpty(e.SearchQuery)),
            from, to);

        return await query
            .GroupBy(e => e.SearchQuery!)
            .Select(g => new SearchTermDto
            {
                SearchTerm = g.Key,
                SearchCount = g.Count(),
                LastSearched = g.Max(e => e.CreatedDate)
            })
            .OrderByDescending(x => x.SearchCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<EventTrendDto>> GetEventTrendsAsync(DateTime from, DateTime to, string? eventType = null)
    {
        var normalizedTo = NormalizeToDate(to);
        var query = _context.AnalyticsEvents.AsNoTracking()
            .Where(e => e.CreatedDate >= from && e.CreatedDate <= normalizedTo);

        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(e => e.EventType == eventType);

        return await query
            .GroupBy(e => new { Date = e.CreatedDate.Date, e.EventType })
            .Select(g => new EventTrendDto
            {
                Date = g.Key.Date,
                EventType = g.Key.EventType,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    // -----------------------------------------------------------------------
    // User-specific analytics
    // -----------------------------------------------------------------------

    public async Task<UserRoommateAnalyticsSummaryDto> GetUserRoommateSummaryAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking().Where(e => e.UserId == userId),
            from, to);

        // Single round-trip
        var countsByType = await query
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToListAsync();

        var byType = countsByType.ToDictionary(x => x.EventType, x => x.Count);

        return new UserRoommateAnalyticsSummaryDto
        {
            RoommateViews    = byType.GetValueOrDefault("RoommateView"),
            MessagesSent     = byType.GetValueOrDefault("MessageSent"),
            ApplicationsSent = byType.GetValueOrDefault("ApplicationSent"),
            Searches         = byType.Where(kv => kv.Key.Contains("Search")).Sum(kv => kv.Value),
            FromDate = from,
            ToDate   = to
        };
    }

    public async Task<List<TopEntityDto>> GetUserTopRoommatesAsync(int userId, int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking()
                .Where(e => e.EventType == "RoommateView" && e.EntityId.HasValue && e.UserId == userId),
            from, to);

        return await query
            .GroupBy(e => e.EntityId!.Value)
            .Select(g => new TopEntityDto
            {
                EntityId = g.Key,
                EntityType = "Roommate",
                ViewCount = g.Count()
            })
            .OrderByDescending(x => x.ViewCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<SearchTermDto>> GetUserSearchesAsync(int userId, int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking()
                .Where(e => e.EventType.Contains("Search") && !string.IsNullOrEmpty(e.SearchQuery) && e.UserId == userId),
            from, to);

        return await query
            .GroupBy(e => e.SearchQuery!)
            .Select(g => new SearchTermDto
            {
                SearchTerm = g.Key,
                SearchCount = g.Count(),
                LastSearched = g.Max(e => e.CreatedDate)
            })
            .OrderByDescending(x => x.SearchCount)
            .Take(count)
            .ToListAsync();
    }

    public async Task<UserRoommateTrendsDto> GetUserRoommateTrendsAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking()
                .Where(e => e.EventType == "RoommateView" && e.EntityId.HasValue && e.UserId == userId),
            from, to);

        var events = await query.ToListAsync();
        var roommateIds = events.Select(e => e.EntityId!.Value).Distinct().ToList();

        var roommates = await _roommatesContext.Roommates
            .AsNoTracking()
            .Where(r => roommateIds.Contains(r.RoommateId) && r.IsActive)
            .Select(r => new { r.RoommateId, r.PreferredLocation, r.BudgetMax })
            .ToListAsync();

        var popularCities = roommates
            .Where(r => !string.IsNullOrEmpty(r.PreferredLocation))
            .GroupBy(r => r.PreferredLocation!)
            .Select(g => new PopularCityDto { City = g.Key, ViewCount = g.Count() })
            .OrderByDescending(x => x.ViewCount)
            .Take(5)
            .ToList();

        var averagePrices = roommates
            .Where(r => !string.IsNullOrEmpty(r.PreferredLocation) && r.BudgetMax.HasValue && r.BudgetMax.Value > 0)
            .GroupBy(r => r.PreferredLocation!)
            .Select(g => new AveragePriceDto { City = g.Key, AveragePrice = g.Average(r => r.BudgetMax!.Value) })
            .OrderBy(x => x.City)
            .ToList();

        return new UserRoommateTrendsDto { PopularCities = popularCities, AveragePrices = averagePrices };
    }

    public async Task<List<TopEntityDto>> GetUserTopApartmentsAsync(int userId, int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking()
                .Where(e => e.EventType == "ApartmentView" && e.EntityId.HasValue && e.UserId == userId),
            from, to);

        var topApartments = await query
            .GroupBy(e => e.EntityId!.Value)
            .Select(g => new TopEntityDto
            {
                EntityId = g.Key,
                EntityType = "Apartment",
                ViewCount = g.Count()
            })
            .OrderByDescending(x => x.ViewCount)
            .Take(count)
            .ToListAsync();

        var apartmentIds = topApartments.Select(a => a.EntityId).ToList();
        var apartments = await _listingsContext.Apartments
            .AsNoTracking()
            .Where(a => apartmentIds.Contains(a.ApartmentId) && !a.IsDeleted)
            .Select(a => new { a.ApartmentId, a.Title, a.City, a.Rent })
            .ToListAsync();

        var apartmentDict = apartments.ToDictionary(a => a.ApartmentId);
        foreach (var apartment in topApartments)
        {
            if (apartmentDict.TryGetValue(apartment.EntityId, out var apt))
            {
                apartment.EntityTitle = apt.Title;
                apartment.EntityDetails = $"{apt.City ?? "N/A"} - €{apt.Rent}/month";
            }
        }
        return topApartments;
    }

    public async Task<AnalyticsSummaryDto> GetUserCompleteAnalyticsAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking().Where(e => e.UserId == userId),
            from, to);

        // Single round-trip
        var countsByType = await query
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .ToListAsync();

        var byType = countsByType.ToDictionary(x => x.EventType, x => x.Count);
        var totalEvents = byType.Values.Sum();

        var eventsByCategory = await query
            .GroupBy(e => e.EventCategory)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count);

        return new AnalyticsSummaryDto
        {
            TotalEvents         = totalEvents,
            TotalApartmentViews = byType.GetValueOrDefault("ApartmentView"),
            TotalRoommateViews  = byType.GetValueOrDefault("RoommateView"),
            TotalSearches       = byType.Where(kv => kv.Key.Contains("Search")).Sum(kv => kv.Value),
            TotalContactClicks  = byType.GetValueOrDefault("ContactClick"),
            EventsByCategory    = eventsByCategory,
            FromDate = from,
            ToDate   = to
        };
    }

    public async Task<List<ApartmentViewStatsDto>> GetLandlordApartmentViewsAsync(int landlordUserId, DateTime? from = null, DateTime? to = null)
    {
        var myApartments = await _listingsContext.Apartments
            .AsNoTracking()
            .Where(a => a.LandlordId == landlordUserId && !a.IsDeleted)
            .Select(a => new { a.ApartmentId, a.Title, a.City, a.Rent })
            .ToListAsync();

        if (!myApartments.Any())
            return new List<ApartmentViewStatsDto>();

        var apartmentIds = myApartments.Select(a => a.ApartmentId).ToList();
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking()
                .Where(e => e.EventType == "ApartmentView" &&
                            e.EntityId.HasValue &&
                            apartmentIds.Contains(e.EntityId.Value)),
            from, to);

        var viewStats = await query
            .GroupBy(e => e.EntityId!.Value)
            .Select(g => new
            {
                ApartmentId = g.Key,
                ViewCount   = g.Count(),
                LastViewed  = g.Max(e => e.CreatedDate)
            })
            .ToListAsync();

        return myApartments
            .Select(a =>
            {
                var stats = viewStats.FirstOrDefault(v => v.ApartmentId == a.ApartmentId);
                return new ApartmentViewStatsDto
                {
                    ApartmentId = a.ApartmentId,
                    Title       = a.Title,
                    City        = a.City,
                    Rent        = a.Rent,
                    ViewCount   = stats?.ViewCount ?? 0,
                    LastViewed  = stats?.LastViewed
                };
            })
            .OrderByDescending(a => a.ViewCount)
            .ToList();
    }

    public async Task<int> GetUserMessageCountAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = ApplyDateRange(
            _context.AnalyticsEvents.AsNoTracking()
                .Where(e => e.EventType == "MessageSent" && e.UserId == userId),
            from, to);

        return await query.CountAsync();
    }
}
