using Lander.src.Modules.Listings.Models;

namespace Lander.src.Modules.Listings.Dtos.InputDto;

public class ApartmentFilterDto
{
    public string? City { get; set; }
    public decimal? MinRent { get; set; }
    public decimal? MaxRent { get; set; }
    public int? NumberOfRooms { get; set; }
    public ApartmentType? ApartmentType { get; set; }
    public bool? IsFurnished { get; set; }
    public bool? IsPetFriendly { get; set; }
    public bool? IsSmokingAllowed { get; set; }
    public bool? HasParking { get; set; }
    public bool? HasBalcony { get; set; }
    public bool? IsImmediatelyAvailable { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
