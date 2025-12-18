namespace Lander.src.Modules.SavedSearches.Dtos.Dto;

public class SavedSearchDto
{
    public int SavedSearchId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public string SearchType { get; set; } = null!;
    public string? FiltersJson { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public DateTime? LastNotificationSent { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
}

