namespace Lander.src.Modules.Analytics.Models;
public class AnalyticsEvent
{
    public int EventId { get; set; }
    public string EventType { get; set; } = null!;
    public string EventCategory { get; set; } = null!;
    public int? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? SearchQuery { get; set; }
    public string? MetadataJson { get; set; }
    public int? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid? CreatedByGuid { get; set; }
}
