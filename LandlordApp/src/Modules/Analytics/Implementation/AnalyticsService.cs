using System.Text.Json;
using Lander.src.Modules.Analytics.Dtos.Dto;
using Lander.src.Modules.Analytics.Interfaces;
using Lander.src.Modules.Analytics.Models;
using Microsoft.EntityFrameworkCore;
namespace Lander.src.Modules.Analytics.Implementation;
public class AnalyticsService : IAnalyticsService
{
    private readonly AnalyticsContext _context;
    public AnalyticsService(AnalyticsContext context)
    {
        _context = context;
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
}
