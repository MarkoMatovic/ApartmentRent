using Lander.src.Modules.Listings.Models;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// Snapshot of a listing's matchable fields, passed to the saved-search alert pipeline.
/// Avoids re-loading the Apartment entity inside the notification service.
/// </summary>
public sealed record ListingAlertContext(
    int    ApartmentId,
    string Title,
    string? City,
    decimal Rent,
    int?   NumberOfRooms,
    ApartmentType ApartmentType,
    ListingType   ListingType,
    bool IsFurnished,
    bool IsPetFriendly,
    bool IsSmokingAllowed,
    bool HasParking,
    bool HasBalcony,
    bool IsImmediatelyAvailable,
    int? LandlordId
);
