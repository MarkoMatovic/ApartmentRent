using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;

namespace Lander.src.Modules.ApartmentApplications.Models;

public partial class ApartmentApplication
{
    public int ApplicationId { get; set; }

    public int? UserId { get; set; }

    public int? ApartmentId { get; set; }

    public DateTime? ApplicationDate { get; set; }

    public string? Status { get; set; }

    public Guid? CreatedByGuid { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? ModifiedByGuid { get; set; }

    public DateTime? ModifiedDate { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public virtual Apartment? Apartment { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public virtual User? User { get; set; }
}
