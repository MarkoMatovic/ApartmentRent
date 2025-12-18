using Lander.src.Modules.Listings.Models;

namespace Lander.src.Modules.Listings.Dtos.Dto;

public class ApartmentDto
{
    public int ApartmentId { get; set; }
    public string Title { get; set; }
    public decimal Rent { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    // Extended fields (optional for backward compatibility)
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? SizeSquareMeters { get; set; }
    public ApartmentType? ApartmentType { get; set; }
    public bool? IsFurnished { get; set; }
    public bool? IsImmediatelyAvailable { get; set; }
}
public class GetApartmentDto
{
    public int ApartmentId { get; set; }
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
    // Location (for maps & search)
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    // Apartment characteristics (filters)
    public int? SizeSquareMeters { get; set; }
    public ApartmentType ApartmentType { get; set; }
    // Furnishing & amenities
    public bool IsFurnished { get; set; }
    public bool HasBalcony { get; set; }
    public bool HasElevator { get; set; }
    public bool HasParking { get; set; }
    public bool HasInternet { get; set; }
    public bool HasAirCondition { get; set; }
    // Rules
    public bool IsPetFriendly { get; set; }
    public bool IsSmokingAllowed { get; set; }
    // Availability & rental terms
    public decimal? DepositAmount { get; set; }
    public int? MinimumStayMonths { get; set; }
    public int? MaximumStayMonths { get; set; }
    public bool IsImmediatelyAvailable { get; set; }
}