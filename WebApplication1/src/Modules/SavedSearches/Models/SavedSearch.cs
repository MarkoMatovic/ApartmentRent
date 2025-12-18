namespace Lander.src.Modules.SavedSearches.Models;

public class SavedSearch
{
    public int SavedSearchId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = null!; // User-defined name for the search
    
    // Search filters (stored as JSON or individual fields)
    public string SearchType { get; set; } = null!; // 'apartments', 'roommates', 'search-requests'
    public string? FiltersJson { get; set; } // JSON string with all filter criteria
    
    // Notification settings
    public bool EmailNotificationsEnabled { get; set; } = true;
    public DateTime? LastNotificationSent { get; set; }
    
    // Status
    public bool IsActive { get; set; } = true;
    
    // Audit
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

