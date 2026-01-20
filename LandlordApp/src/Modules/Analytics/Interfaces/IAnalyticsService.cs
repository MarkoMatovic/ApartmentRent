using Lander.src.Modules.Analytics.Dtos.Dto;
namespace Lander.src.Modules.Analytics.Interfaces;
public interface IAnalyticsService
{
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
    Task<AnalyticsSummaryDto> GetSummaryAsync(DateTime? from = null, DateTime? to = null);
    Task<List<TopEntityDto>> GetTopViewedApartmentsAsync(int count = 10, DateTime? from = null, DateTime? to = null);
    Task<List<TopEntityDto>> GetTopViewedRoommatesAsync(int count = 10, DateTime? from = null, DateTime? to = null);
    Task<List<SearchTermDto>> GetTopSearchTermsAsync(int count = 10, DateTime? from = null, DateTime? to = null);
    Task<List<EventTrendDto>> GetEventTrendsAsync(DateTime from, DateTime to, string? eventType = null);
}
