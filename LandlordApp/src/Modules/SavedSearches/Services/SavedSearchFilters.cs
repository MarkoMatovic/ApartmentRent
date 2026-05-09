using Lander.src.Modules.Listings.Models;

namespace Lander.src.Modules.SavedSearches.Services;

/// <summary>
/// Mirrors the filter fields stored as JSON in <see cref="SavedSearch.FiltersJson"/>.
/// Keys are camelCase to match the frontend serialization convention.
/// </summary>
public sealed class SavedSearchFilters
{
    public ListingType?   ListingType            { get; set; }
    public ApartmentType? ApartmentType          { get; set; }
    public string?        City                   { get; set; }
    public decimal?       MinRent                { get; set; }
    public decimal?       MaxRent                { get; set; }
    public int?           NumberOfRooms          { get; set; }
    public bool?          IsFurnished            { get; set; }
    public bool?          IsPetFriendly          { get; set; }
    public bool?          IsSmokingAllowed       { get; set; }
    public bool?          HasParking             { get; set; }
    public bool?          HasBalcony             { get; set; }
    public bool?          IsImmediatelyAvailable { get; set; }
}
