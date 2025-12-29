using Lander.src.Modules.SearchRequests.Models;

namespace Lander.src.Modules.SearchRequests.Dtos.InputDto;

public class SearchRequestInputDto
{
    public SearchRequestType RequestType { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? PreferredLocation { get; set; }
    
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    
    public int? NumberOfRooms { get; set; }
    public int? SizeSquareMeters { get; set; }
    public bool? IsFurnished { get; set; }
    public bool? HasParking { get; set; }
    public bool? HasBalcony { get; set; }
    public bool? PetFriendly { get; set; }
    public bool? SmokingAllowed { get; set; }
    
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableUntil { get; set; }
    
    public bool? LookingForSmokingAllowed { get; set; }
    public bool? LookingForPetFriendly { get; set; }
    public string? PreferredLifestyle { get; set; }
}

