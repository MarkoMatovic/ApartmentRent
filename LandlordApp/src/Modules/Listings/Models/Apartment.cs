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
    public bool IsFurnished { get; set; } = false;
    public bool HasBalcony { get; set; } = false;
    public bool HasElevator { get; set; } = false;
    public bool HasParking { get; set; } = false;
    public bool HasInternet { get; set; } = false;
    public bool HasAirCondition { get; set; } = false;
    public bool IsPetFriendly { get; set; } = false;
    public bool IsSmokingAllowed { get; set; } = false;
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
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; }
}
