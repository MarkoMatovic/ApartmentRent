using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.Reviews.Modules;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
namespace Lander.src.Modules.Listings.Models;
public partial class Apartment
{
    public int ApartmentId { get; set; }
    public int? LandlordId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Rent { get; set; }
    public decimal? Price { get; set; }
    public string Address { get; set; } = null!;
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableUntil { get; set; }
    public int? NumberOfRooms { get; set; }
    public bool? RentIncludeUtilities { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? SizeSquareMeters { get; set; }
    public ApartmentType ApartmentType { get; set; } = ApartmentType.Studio;
    public ListingType ListingType { get; set; } = ListingType.Rent;
    public ApartmentFeatures Features { get; set; } = new();
    
   
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsFurnished 
    { 
        get => Features.IsFurnished; 
        set => Features.IsFurnished = value; 
    }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool HasBalcony 
    { 
        get => Features.HasBalcony; 
        set => Features.HasBalcony = value; 
    }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool HasElevator 
    { 
        get => Features.HasElevator; 
        set => Features.HasElevator = value; 
    }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool HasParking 
    { 
        get => Features.HasParking; 
        set => Features.HasParking = value; 
    }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool HasInternet 
    { 
        get => Features.HasInternet; 
        set => Features.HasInternet = value; 
    }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool HasAirCondition 
    { 
        get => Features.HasAirCondition; 
        set => Features.HasAirCondition = value; 
    }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsPetFriendly 
    { 
        get => Features.IsPetFriendly; 
        set => Features.IsPetFriendly = value; 
    }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsSmokingAllowed 
    { 
        get => Features.IsSmokingAllowed; 
        set => Features.IsSmokingAllowed = value; 
    }
    
    public decimal? DepositAmount { get; set; }
    public int? MinimumStayMonths { get; set; }
    public int? MaximumStayMonths { get; set; }
    public bool IsImmediatelyAvailable { get; set; } = false;
    public bool IsLookingForRoommate { get; set; } = false;
    public string? ContactPhone { get; set; }
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public virtual ICollection<ApartmentImage> ApartmentImages { get; set; } = new List<ApartmentImage>();
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public virtual User? Landlord { get; set; }
    public string? DescriptionEmbedding { get; set; }  
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; }
}
