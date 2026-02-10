using Lander.src.Modules.Listings.Models;
using Microsoft.AspNetCore.Mvc;
namespace Lander.src.Modules.Listings.Dtos.InputDto;
public class ApartmentFilterDto
{
    [FromQuery(Name = "listingType")]
    public ListingType? ListingType { get; set; }
    [FromQuery(Name = "city")]
    public string? City { get; set; }
    [FromQuery(Name = "minRent")]
    public decimal? MinRent { get; set; }
    [FromQuery(Name = "maxRent")]
    public decimal? MaxRent { get; set; }
    [FromQuery(Name = "numberOfRooms")]
    public int? NumberOfRooms { get; set; }
    [FromQuery(Name = "apartmentType")]
    public ApartmentType? ApartmentType { get; set; }
    [FromQuery(Name = "isFurnished")]
    public bool? IsFurnished { get; set; }
    [FromQuery(Name = "isPetFriendly")]
    public bool? IsPetFriendly { get; set; }
    [FromQuery(Name = "isSmokingAllowed")]
    public bool? IsSmokingAllowed { get; set; }
    [FromQuery(Name = "hasParking")]
    public bool? HasParking { get; set; }
    [FromQuery(Name = "hasBalcony")]
    public bool? HasBalcony { get; set; }
    [FromQuery(Name = "isImmediatelyAvailable")]
    public bool? IsImmediatelyAvailable { get; set; }
    [FromQuery(Name = "availableFrom")]
    public DateOnly? AvailableFrom { get; set; }
    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; set; } = 20;
    [FromQuery(Name = "sortBy")]
    public string? SortBy { get; set; }
    [FromQuery(Name = "sortOrder")]
    public string? SortOrder { get; set; }
}
