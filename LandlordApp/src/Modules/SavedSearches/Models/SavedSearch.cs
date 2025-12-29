namespace Lander.src.Modules.SavedSearches.Models;

public class SavedSearch
{
    public int SavedSearchId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    
    public string SearchType { get; set; } = null!;
    public string? FiltersJson { get; set; }
    
    public bool EmailNotificationsEnabled { get; set; } = true;
    public DateTime? LastNotificationSent { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

