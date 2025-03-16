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

    public string Address { get; set; } = null!;

    public string? City { get; set; }

    public string? PostalCode { get; set; }

    public DateOnly? AvailableFrom { get; set; }

    public DateOnly? AvailableUntil { get; set; }

    public int? NumberOfRooms { get; set; }

    public bool? RentIncludeUtilities { get; set; }

    public Guid? CreatedByGuid { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? ModifiedByGuid { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<ApartmentImage> ApartmentImages { get; set; } = new List<ApartmentImage>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual User? Landlord { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; }
}
