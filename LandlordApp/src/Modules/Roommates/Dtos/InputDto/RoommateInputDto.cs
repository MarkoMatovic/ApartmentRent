namespace Lander.src.Modules.Roommates.Dtos.InputDto;

public class RoommateInputDto
{
    public string? Bio { get; set; }
    public string? Hobbies { get; set; }
    public string? Profession { get; set; }
    
    public bool? SmokingAllowed { get; set; }
    public bool? PetFriendly { get; set; }
    public string? Lifestyle { get; set; }
    public string? Cleanliness { get; set; }
    public bool? GuestsAllowed { get; set; }
    
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? BudgetIncludes { get; set; }
    
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableUntil { get; set; }
    public int? MinimumStayMonths { get; set; }
    public int? MaximumStayMonths { get; set; }
    
    public string? LookingForRoomType { get; set; }
    public string? LookingForApartmentType { get; set; }
    public string? PreferredLocation { get; set; }
    public int? LookingForApartmentId { get; set; }
}

