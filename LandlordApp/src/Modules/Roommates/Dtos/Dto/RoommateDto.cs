namespace Lander.src.Modules.Roommates.Dtos.Dto;
public class RoommateDto
{
    public int RoommateId { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? ProfilePicture { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? PhoneNumber { get; set; }
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
    public bool IsActive { get; set; }
}
