using System.Text.Json;
using Lander;
using Lander.src.Modules.Analytics.Dtos.Dto;
using Lander.src.Modules.Analytics.Interfaces;
using Lander.src.Modules.Analytics.Models;
using Microsoft.EntityFrameworkCore;
namespace Lander.src.Modules.Analytics.Implementation;
public class AnalyticsService : IAnalyticsService
{
    private readonly AnalyticsContext _context;
    private readonly ListingsContext _listingsContext;
    private readonly RoommatesContext _roommatesContext;
    public AnalyticsService(AnalyticsContext context, ListingsContext listingsContext, RoommatesContext roommatesContext)
    {
        _context = context;
        _listingsContext = listingsContext;
        _roommatesContext = roommatesContext;
    }
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
    public async Task<AnalyticsSummaryDto> GetSummaryAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AnalyticsEvents.AsQueryable();
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
        var totalEvents = await query.CountAsync();
        var apartmentViews = await query.CountAsync(e => e.EventType == "ApartmentView");
        var roommateViews = await query.CountAsync(e => e.EventType == "RoommateView");
        var searches = await query.CountAsync(e => e.EventType.Contains("Search"));
        var contactClicks = await query.CountAsync(e => e.EventType == "ContactClick");
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
    public async Task<List<TopEntityDto>> GetTopViewedApartmentsAsync(int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AnalyticsEvents
            .Where(e => e.EventType == "ApartmentView" && e.EntityId.HasValue);
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
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
        var query = _context.AnalyticsEvents
            .Where(e => e.EventType == "RoommateView" && e.EntityId.HasValue);
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
        var topRoommates = await query
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
        return topRoommates;
    }
    public async Task<List<SearchTermDto>> GetTopSearchTermsAsync(int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AnalyticsEvents
            .Where(e => e.EventType.Contains("Search") && !string.IsNullOrEmpty(e.SearchQuery));
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
        var topSearches = await query
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
        return topSearches;
    }
    public async Task<List<EventTrendDto>> GetEventTrendsAsync(DateTime from, DateTime to, string? eventType = null)
    {
        var query = _context.AnalyticsEvents
            .Where(e => e.CreatedDate >= from && e.CreatedDate <= to);
        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(e => e.EventType == eventType);
        var trends = await query
            .GroupBy(e => new { Date = e.CreatedDate.Date, e.EventType })
            .Select(g => new EventTrendDto
            {
                Date = g.Key.Date,
                EventType = g.Key.EventType,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
        return trends;
    }
    public async Task<UserRoommateAnalyticsSummaryDto> GetUserRoommateSummaryAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AnalyticsEvents.Where(e => e.UserId == userId);
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
        var roommateViews = await query.CountAsync(e => e.EventType == "RoommateView");
        var messagesSent = await query.CountAsync(e => e.EventType == "MessageSent");
        var applicationsSent = await query.CountAsync(e => e.EventType == "ApplicationSent");
        var searches = await query.CountAsync(e => e.EventType.Contains("Search"));
        return new UserRoommateAnalyticsSummaryDto
        {
            RoommateViews = roommateViews,
            MessagesSent = messagesSent,
            ApplicationsSent = applicationsSent,
            Searches = searches,
            FromDate = from,
            ToDate = to
        };
    }
    public async Task<List<TopEntityDto>> GetUserTopRoommatesAsync(int userId, int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AnalyticsEvents
            .Where(e => e.EventType == "RoommateView" && e.EntityId.HasValue && e.UserId == userId);
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
        var topRoommates = await query
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
        return topRoommates;
    }
    public async Task<List<SearchTermDto>> GetUserSearchesAsync(int userId, int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AnalyticsEvents
            .Where(e => e.EventType.Contains("Search") && !string.IsNullOrEmpty(e.SearchQuery) && e.UserId == userId);
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
        var searches = await query
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
        return searches;
    }
    public async Task<UserRoommateTrendsDto> GetUserRoommateTrendsAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AnalyticsEvents
            .Where(e => e.EventType == "RoommateView" && e.EntityId.HasValue && e.UserId == userId);
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
        var events = await query.ToListAsync();
        var roommateIds = events.Select(e => e.EntityId!.Value).Distinct().ToList();
        var roommates = await _roommatesContext.Roommates
            .Where(r => roommateIds.Contains(r.RoommateId) && r.IsActive)
            .Select(r => new { r.RoommateId, r.PreferredLocation, r.BudgetMax })
            .ToListAsync();
        var popularCities = roommates
            .Where(r => !string.IsNullOrEmpty(r.PreferredLocation))
            .GroupBy(r => r.PreferredLocation!)
            .Select(g => new PopularCityDto
            {
                City = g.Key,
                ViewCount = g.Count()
            })
            .OrderByDescending(x => x.ViewCount)
            .Take(5)
            .ToList();
        var averagePrices = roommates
            .Where(r => !string.IsNullOrEmpty(r.PreferredLocation) && r.BudgetMax.HasValue && r.BudgetMax.Value > 0)
            .GroupBy(r => r.PreferredLocation!)
            .Select(g => new AveragePriceDto
            {
                City = g.Key,
                AveragePrice = g.Average(r => r.BudgetMax!.Value)
            })
            .OrderBy(x => x.City)
            .ToList();
        return new UserRoommateTrendsDto
        {
            PopularCities = popularCities,
            AveragePrices = averagePrices
        };
    }
    public async Task<List<TopEntityDto>> GetUserTopApartmentsAsync(int userId, int count = 10, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AnalyticsEvents
            .Where(e => e.EventType == "ApartmentView" && e.EntityId.HasValue && e.UserId == userId);
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
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
        var query = _context.AnalyticsEvents.Where(e => e.UserId == userId);
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
        var totalEvents = await query.CountAsync();
        var apartmentViews = await query.CountAsync(e => e.EventType == "ApartmentView");
        var roommateViews = await query.CountAsync(e => e.EventType == "RoommateView");
        var searches = await query.CountAsync(e => e.EventType.Contains("Search"));
        var contactClicks = await query.CountAsync(e => e.EventType == "ContactClick");
        var messagesSent = await query.CountAsync(e => e.EventType == "MessageSent");
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
    public async Task<List<ApartmentViewStatsDto>> GetLandlordApartmentViewsAsync(int landlordUserId, DateTime? from = null, DateTime? to = null)
    {
        var myApartments = await _listingsContext.Apartments
            .Where(a => a.LandlordId == landlordUserId && !a.IsDeleted)
            .Select(a => new { a.ApartmentId, a.Title, a.City, a.Rent })
            .ToListAsync();
        if (!myApartments.Any())
        {
            return new List<ApartmentViewStatsDto>();
        }
        var apartmentIds = myApartments.Select(a => a.ApartmentId).ToList();
        var query = _context.AnalyticsEvents
            .Where(e => e.EventType == "ApartmentView" && 
                        e.EntityId.HasValue && 
                        apartmentIds.Contains(e.EntityId.Value));
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
        var viewStats = await query
            .GroupBy(e => e.EntityId.Value)
            .Select(g => new 
            { 
                ApartmentId = g.Key, 
                ViewCount = g.Count(),
                LastViewed = g.Max(e => e.CreatedDate)
            })
            .ToListAsync();
        var result = myApartments
            .Select(a =>
            {
                var stats = viewStats.FirstOrDefault(v => v.ApartmentId == a.ApartmentId);
                return new ApartmentViewStatsDto
                {
                    ApartmentId = a.ApartmentId,
                    Title = a.Title,
                    City = a.City,
                    Rent = a.Rent,
                    ViewCount = stats?.ViewCount ?? 0,
                    LastViewed = stats?.LastViewed
                };
            })
            .OrderByDescending(a => a.ViewCount)
            .ToList();
        return result;
    }
    public async Task<int> GetUserMessageCountAsync(int userId, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AnalyticsEvents
            .Where(e => e.EventType == "MessageSent" && e.UserId == userId);
        if (from.HasValue)
            query = query.Where(e => e.CreatedDate >= from.Value);
        if (to.HasValue)
            query = query.Where(e => e.CreatedDate <= to.Value);
        return await query.CountAsync();
    }
}
