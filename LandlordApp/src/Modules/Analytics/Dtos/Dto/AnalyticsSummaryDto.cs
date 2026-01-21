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

// User-specific analytics DTOs
public class UserRoommateAnalyticsSummaryDto
{
    public int RoommateViews { get; set; }
    public int MessagesSent { get; set; }
    public int ApplicationsSent { get; set; }
    public int Searches { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class UserRoommateTrendsDto
{
    public List<PopularCityDto> PopularCities { get; set; } = new();
    public List<AveragePriceDto> AveragePrices { get; set; } = new();
}

public class PopularCityDto
{
    public string City { get; set; } = null!;
    public int ViewCount { get; set; }
}

public class AveragePriceDto
{
    public string City { get; set; } = null!;
    public decimal AveragePrice { get; set; }
}