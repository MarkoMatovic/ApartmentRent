namespace Lander.src.Modules.Analytics.Dtos.Dto;
public class AnalyticsSummaryDto
{
    public int TotalEvents { get; set; }
    public int TotalApartmentViews { get; set; }
    public int TotalRoommateViews { get; set; }
    public int TotalSearches { get; set; }
    public int TotalContactClicks { get; set; }
    public Dictionary<string, int> EventsByCategory { get; set; } = new();
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
public class TopEntityDto
{
    public int EntityId { get; set; }
    public string EntityType { get; set; } = null!;
    public int ViewCount { get; set; }
    public string? EntityTitle { get; set; }
    public string? EntityDetails { get; set; }
}
public class SearchTermDto
{
    public string SearchTerm { get; set; } = null!;
    public int SearchCount { get; set; }
    public DateTime? LastSearched { get; set; }
}
public class EventTrendDto
{
    public DateTime Date { get; set; }
    public string EventType { get; set; } = null!;
    public int Count { get; set; }
}
