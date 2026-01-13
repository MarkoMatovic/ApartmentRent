using Lander.src.Modules.Analytics.Dtos.Dto;

namespace Lander.src.Modules.Analytics.Interfaces;

public interface IAnalyticsService
{
    /// <summary>
    /// Track a user event
    /// </summary>
    Task TrackEventAsync(
        string eventType, 
        string category, 
        int? entityId = null, 
        string? entityType = null, 
        string? searchQuery = null,
        Dictionary<string, string>? metadata = null,
        int? userId = null,
        string? ipAddress = null,
        string? userAgent = null);
    
    /// <summary>
    /// Get analytics summary for a date range
    /// </summary>
    Task<AnalyticsSummaryDto> GetSummaryAsync(DateTime? from = null, DateTime? to = null);
    
    /// <summary>
    /// Get top viewed apartments
    /// </summary>
    Task<List<TopEntityDto>> GetTopViewedApartmentsAsync(int count = 10, DateTime? from = null, DateTime? to = null);
    
    /// <summary>
    /// Get top viewed roommates
    /// </summary>
    Task<List<TopEntityDto>> GetTopViewedRoommatesAsync(int count = 10, DateTime? from = null, DateTime? to = null);
    
    /// <summary>
    /// Get most popular search terms
    /// </summary>
    Task<List<SearchTermDto>> GetTopSearchTermsAsync(int count = 10, DateTime? from = null, DateTime? to = null);
    
    /// <summary>
    /// Get event trends over time (daily aggregation)
    /// </summary>
    Task<List<EventTrendDto>> GetEventTrendsAsync(DateTime from, DateTime to, string? eventType = null);
}
