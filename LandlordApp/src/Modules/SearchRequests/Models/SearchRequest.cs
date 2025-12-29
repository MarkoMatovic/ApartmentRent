namespace Lander.src.Modules.SearchRequests.Models;

public enum SearchRequestType
{
    LookingForWGRoom = 0,
    LookingForApartment = 1,
    LookingForRoommate = 2,
    LookingForHouse = 3
}

public class SearchRequest
{
    public int SearchRequestId { get; set; }
    public int UserId { get; set; }
    
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
    
    public bool IsActive { get; set; } = true;
    
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public virtual Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate.User? User { get; set; }
}

