namespace Lander.src.Modules.Listings.Interfaces;

/// <summary>
/// Provides the minimal user data that the Listings bounded context needs without
/// directly depending on the Users DbContext.
/// </summary>
public interface IListingsUserLookup
{
    /// <summary>
    /// Returns the integer UserId for the given user GUID, or null if not found.
    /// Used to resolve the current caller's identity from the JWT sub claim.
    /// </summary>
    Task<int?> GetUserIdByGuidAsync(Guid userGuid);

    /// <summary>
    /// Returns brief landlord info needed for the apartment detail page.
    /// Returns null when the user does not exist.
    /// </summary>
    Task<LandlordBrief?> GetLandlordBriefAsync(int userId);

    /// <summary>
    /// Returns (roleName, listingCredits) for the given user in a single DB round-trip.
    /// Used by CreateApartmentAsync to decide whether to deduct a listing credit.
    /// </summary>
    Task<(string? RoleName, int ListingCredits)> GetUserListingContextAsync(int userId);

    /// <summary>
    /// Atomically decrements ListingCredits by 1 (only when balance > 0).
    /// Returns true if a credit was consumed; false if balance was already 0.
    /// </summary>
    Task<bool> TryDeductListingCreditAsync(int userId);
}

/// <param name="FirstName">Landlord first name.</param>
/// <param name="LastName">Landlord last name.</param>
/// <param name="Email">Landlord email.</param>
public record LandlordBrief(string? FirstName, string? LastName, string? Email);
