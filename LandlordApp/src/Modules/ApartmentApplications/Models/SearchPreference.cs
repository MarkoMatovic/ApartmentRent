using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
namespace Lander.src.Modules.ApartmentApplications.Models;
public partial class SearchPreference
{
    public int PreferenceId { get; set; }
    public int? UserId { get; set; }
    public decimal? MaxRent { get; set; }
    public int? MinRooms { get; set; }
    public string? City { get; set; }
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableUntil { get; set; }
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public virtual User? User { get; set; }
}
