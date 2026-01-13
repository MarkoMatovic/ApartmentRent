namespace Lander.src.Modules.Analytics.Models;

public class AnalyticsEvent
{
    public int EventId { get; set; }
    
    /// <summary>
    /// Type of event: "ApartmentView", "RoommateView", "ApartmentSearch", "RoommateSearch", "ContactClick"
    /// </summary>
    public string EventType { get; set; } = null!;
    
    /// <summary>
    /// Category: "Listings", "Roommates", "Search", "Communication"
    /// </summary>
    public string EventCategory { get; set; } = null!;
    
    /// <summary>
    /// ID of the entity being tracked (ApartmentId, RoommateId, etc.)
    /// </summary>
    public int? EntityId { get; set; }
    
    /// <summary>
    /// Type of entity: "Apartment", "Roommate"
    /// </summary>
    public string? EntityType { get; set; }
    
    /// <summary>
    /// For search events, stores the search query or filters
    /// </summary>
    public string? SearchQuery { get; set; }
    
    /// <summary>
    /// JSON string for flexible metadata storage
    /// </summary>
    public string? MetadataJson { get; set; }
    
    /// <summary>
    /// User ID if the user is logged in
    /// </summary>
    public int? UserId { get; set; }
    
    /// <summary>
    /// IP address of the user (for anonymous tracking)
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }
    
    public DateTime CreatedDate { get; set; }
    
    public Guid? CreatedByGuid { get; set; }
}
