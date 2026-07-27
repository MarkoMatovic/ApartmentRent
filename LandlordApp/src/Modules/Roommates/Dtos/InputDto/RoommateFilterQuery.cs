using Lander.src.Modules.Roommates.Models;

namespace Lander.src.Modules.Roommates.Dtos.InputDto;

/// <summary>
/// Filter, sort and pagination options for the roommate listing endpoint.
/// Bound from the query string; every filter is optional, Page/PageSize default to 1/20.
/// </summary>
public class RoommateFilterQuery
{
    public string? Location { get; set; }
    public decimal? MinBudget { get; set; }
    public decimal? MaxBudget { get; set; }
    public bool? SmokingAllowed { get; set; }
    public bool? PetFriendly { get; set; }
    public string? Lifestyle { get; set; }
    public string? Profession { get; set; }
    public DateOnly? AvailableFrom { get; set; }
    public int? StayDuration { get; set; }
    public int? ApartmentId { get; set; }
    public RoommateGender? Gender { get; set; }
    public WorkSchedule? WorkSchedule { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
