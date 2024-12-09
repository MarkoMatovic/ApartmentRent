using System;
using System.Collections.Generic;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.Communication.Models;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Reviews.Modules;

namespace Lander.src.Modules.Users.Models;

public partial class User
{
    public int UserId { get; set; }

    public Guid UserGuid { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ProfilePicture { get; set; }

    public int? UserRoleId { get; set; }

    public Guid? CreatedByGuid { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? ModifiedByGuid { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<ApartmentApplication> ApartmentApplications { get; set; } = new List<ApartmentApplication>();

    public virtual ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<Message> MessageReceivers { get; set; } = new List<Message>();

    public virtual ICollection<Message> MessageSenders { get; set; } = new List<Message>();

    public virtual ICollection<Review> ReviewLandlords { get; set; } = new List<Review>();

    public virtual ICollection<Review> ReviewTenants { get; set; } = new List<Review>();

    public virtual ICollection<SearchPreference> SearchPreferences { get; set; } = new List<SearchPreference>();

    public virtual Role? UserRole { get; set; }
}
