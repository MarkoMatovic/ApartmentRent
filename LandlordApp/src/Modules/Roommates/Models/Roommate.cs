namespace Lander.src.Modules.Roommates.Models;

/// <summary>How the user identifies themselves — used for search filtering.</summary>
public enum RoommateGender { PreferNotToSay = 0, Male = 1, Female = 2, NonBinary = 3, Other = 4 }

/// <summary>Daily rhythm — affects compatibility matching.</summary>
public enum WorkSchedule { Flexible = 0, Morning = 1, Evening = 2, Night = 3 }

public class Roommate
{
    public int RoommateId { get; set; }
    public int UserId { get; set; }
    public string? Bio { get; set; }
    public string? Hobbies { get; set; }
    public string? Profession { get; set; }
    // New fields for better matching and filtering
    public RoommateGender Gender { get; set; } = RoommateGender.PreferNotToSay;
    /// <summary>Comma-separated list, e.g. "Serbian,English,German".</summary>
    public string? Languages { get; set; }
    public WorkSchedule WorkSchedule { get; set; } = WorkSchedule.Flexible;
    /// <summary>Whether the person is ok with music/instruments at home.</summary>
    public bool? MusicFriendly { get; set; }
    public bool? SmokingAllowed { get; set; }
    public bool? PetFriendly { get; set; }
    public string? Lifestyle { get; set; }
    public string? Cleanliness { get; set; }
    public bool? GuestsAllowed { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? BudgetIncludes { get; set; }
    public DateOnly? AvailableFrom { get; set; }
    public DateOnly? AvailableUntil { get; set; }
    public int? MinimumStayMonths { get; set; }
    public int? MaximumStayMonths { get; set; }
    public string? LookingForRoomType { get; set; }
    public string? LookingForApartmentType { get; set; }
    public string? PreferredLocation { get; set; }
    public int? LookingForApartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Until when this roommate profile is boosted (shown first in search results).</summary>
    public DateTime? BoostedUntil { get; set; }
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public virtual Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate.User? User { get; set; }
}
