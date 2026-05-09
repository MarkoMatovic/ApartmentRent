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
}

/// <param name="FirstName">Landlord first name.</param>
/// <param name="LastName">Landlord last name.</param>
/// <param name="Email">Landlord email.</param>
public record LandlordBrief(string? FirstName, string? LastName, string? Email);
