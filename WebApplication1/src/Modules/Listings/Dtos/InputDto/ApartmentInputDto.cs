using Lander.src.Modules.Listings.Models;

namespace Lander.src.Modules.Listings.Dtos.InputDto;

public class ApartmentInputDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Rent { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
    public DateOnly AvailableFrom { get; set; }
    public DateOnly AvailableUntil { get; set; }
    public int NumberOfRooms { get; set; }
    public bool RentIncludeUtilities { get; set; }
    public List<string> ImageUrls { get; set; }
    // Location (for maps & search) - optional
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    // Apartment characteristics (filters) - optional
    public int? SizeSquareMeters { get; set; }
    public ApartmentType? ApartmentType { get; set; }
    // Furnishing & amenities - optional, defaults to false
    public bool? IsFurnished { get; set; }
    public bool? HasBalcony { get; set; }
    public bool? HasElevator { get; set; }
    public bool? HasParking { get; set; }
    public bool? HasInternet { get; set; }
    public bool? HasAirCondition { get; set; }
    // Rules - optional, defaults to false
    public bool? IsPetFriendly { get; set; }
    public bool? IsSmokingAllowed { get; set; }
    // Availability & rental terms - optional
    public decimal? DepositAmount { get; set; }
    public int? MinimumStayMonths { get; set; }
    public int? MaximumStayMonths { get; set; }
    public bool? IsImmediatelyAvailable { get; set; }
}

public class ApartmentImageInputDto
{
    public string ImageUrl { get; set; }
}