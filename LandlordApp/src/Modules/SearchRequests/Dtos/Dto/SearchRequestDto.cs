using Lander.src.Modules.SearchRequests.Models;

namespace Lander.src.Modules.SearchRequests.Dtos.Dto;

public class SearchRequestDto
{
    public int SearchRequestId { get; set; }
    public int UserId { get; set; }
    
    // User Info
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? ProfilePicture { get; set; }
    
    public SearchRequestType RequestType { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    
    // Location
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? PreferredLocation { get; set; }
    
    // Budget
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    
    // Requirements
    public int? NumberOfRooms { get; set; }
    public int? SizeSquareMeters { get; set; }
    public bool? IsFurnished { get; set; }
    public bool? HasParking { get; set; }
    public bool? HasBalcony { get; set; }
    public bool? PetFriendly { get; set; }
    public bool? SmokingAllowed { get; set; }
    
    // Availability
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableUntil { get; set; }
    
    // For roommate searches
    public bool? LookingForSmokingAllowed { get; set; }
    public bool? LookingForPetFriendly { get; set; }
    public string? PreferredLifestyle { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
}

