using Lander.src.Modules.Listings.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// Provides the minimal user information that the Listings bounded context
/// needs, without the rest of the Listings module ever touching UsersContext.
/// </summary>
public class ListingsUserLookup : IListingsUserLookup
{
    private readonly UsersContext _usersContext;

    public ListingsUserLookup(UsersContext usersContext)
        => _usersContext = usersContext;

    public async Task<int?> GetUserIdByGuidAsync(Guid userGuid)
    {
        var user = await _usersContext.Users
            .AsNoTracking()
            .Where(u => u.UserGuid == userGuid)
            .Select(u => new { u.UserId })
            .FirstOrDefaultAsync();

        return user?.UserId;
    }

    public async Task<LandlordBrief?> GetLandlordBriefAsync(int userId)
    {
        var user = await _usersContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync();

        return user is null ? null : new LandlordBrief(user.FirstName, user.LastName, user.Email);
    }

    public async Task<(string? RoleName, int ListingCredits)> GetUserListingContextAsync(int userId)
    {
        var data = await _usersContext.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new
            {
                u.ListingCredits,
                RoleName = u.UserRole != null ? u.UserRole.RoleName : null
            })
            .FirstOrDefaultAsync();

        return data is null ? (null, 0) : (data.RoleName, data.ListingCredits);
    }

    public async Task<bool> TryDeductListingCreditAsync(int userId)
    {
        // Atomic conditional decrement — only executes when balance > 0 to prevent going negative.
        var affected = await _usersContext.Database.ExecuteSqlRawAsync(
            "UPDATE [UsersRoles].[Users] SET ListingCredits = ListingCredits - 1 WHERE UserId = {0} AND ListingCredits > 0",
            userId);
        return affected > 0;
    }
}
