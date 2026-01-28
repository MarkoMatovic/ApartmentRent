namespace Lander.src.Modules.Analytics.Dtos.InputDto;
public class TrackEventInputDto
{
    public string EventType { get; set; } = null!;
    public string EventCategory { get; set; } = null!;
    public int? EntityId { get; set; }
    public string? EntityType { get; set; }
    public string? SearchQuery { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
