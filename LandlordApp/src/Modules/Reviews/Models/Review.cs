using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
namespace Lander.src.Modules.Reviews.Modules;
public class Review
{
    public int ReviewId { get; set; }
    public int? TenantId { get; set; }
    public int? LandlordId { get; set; }
    public int? ApartmentId { get; set; }
    public int? Rating { get; set; }
    public string? ReviewText { get; set; }
    public bool IsAnonymous { get; set; } = false;
    public bool IsPublic { get; set; } = true;
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public virtual User? Landlord { get; set; }
    public virtual User? Tenant { get; set; }
}
