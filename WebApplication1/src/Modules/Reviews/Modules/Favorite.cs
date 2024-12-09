using System;
using System.Collections.Generic;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Users.Models;

namespace Lander.src.Modules.Reviews.Modules;

public partial class Favorite
{
    public int FavoriteId { get; set; }

    public int? UserId { get; set; }

    public int? ApartmentId { get; set; }

    public Guid? CreatedByGuid { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? ModifiedByGuid { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual Apartment? Apartment { get; set; }

    public virtual User? User { get; set; }
}
