namespace Lander.src.Modules.Roommates.Models;

public class Roommate
{
    public int RoommateId { get; set; }
    public int UserId { get; set; }
    
    // Basic Info
    public string? Bio { get; set; }
    public string? Hobbies { get; set; }
    public string? Profession { get; set; }
    
    // Preferences
    public bool? SmokingAllowed { get; set; }
    public bool? PetFriendly { get; set; }
    public string? Lifestyle { get; set; } // 'quiet', 'social', 'mixed'
    public string? Cleanliness { get; set; } // 'very clean', 'clean', 'moderate'
    public bool? GuestsAllowed { get; set; }
    
    // Budget
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? BudgetIncludes { get; set; } // What's included in budget
    
    // Availability
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableUntil { get; set; }
    public int? MinimumStayMonths { get; set; }
    public int? MaximumStayMonths { get; set; }
    
    // What I'm looking for
    public string? LookingForRoomType { get; set; } // 'single', 'shared'
    public string? LookingForApartmentType { get; set; }
    public string? PreferredLocation { get; set; }
    public int? LookingForApartmentId { get; set; } // Link to specific apartment
    
    // Status
    public bool IsActive { get; set; } = true;
    
    // Audit
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    // Navigation (NotMapped - cross context)
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public virtual Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate.User? User { get; set; }
}

