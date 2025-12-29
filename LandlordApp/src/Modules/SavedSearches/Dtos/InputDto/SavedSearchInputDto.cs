namespace Lander.src.Modules.SavedSearches.Dtos.InputDto;

public class SavedSearchInputDto
{
    public string Name { get; set; } = null!;
    public string SearchType { get; set; } = null!;
    public string? FiltersJson { get; set; }
    public bool EmailNotificationsEnabled { get; set; } = true;
}

