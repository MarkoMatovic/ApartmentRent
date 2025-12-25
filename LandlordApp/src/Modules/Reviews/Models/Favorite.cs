using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;

namespace Lander.src.Modules.Reviews.Modules;

public class Favorite
{
    public int FavoriteId { get; set; }

    public int? UserId { get; set; }

    public int? ApartmentId { get; set; }
    public int? RoommateId { get; set; }
    public int? SearchRequestId { get; set; }

    public Guid? CreatedByGuid { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? ModifiedByGuid { get; set; }

    public DateTime? ModifiedDate { get; set; }
}
public class CreateFavoriteInput
{

    public int? UserId { get; set; }

    public int? ApartmentId { get; set; }

    public Guid? CreatedByGuid { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? ModifiedByGuid { get; set; }

    public DateTime? ModifiedDate { get; set; }

    
}
