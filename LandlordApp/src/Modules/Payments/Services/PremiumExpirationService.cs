using Lander.Helpers;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Payments.Services;

/// <summary>
/// Hangfire recurring job (daily 01:00 UTC) that expires time-limited premium features:
///   • Featured listings  (Apartments.IsFeatured / FeaturedUntil)
///   • Boosted roommate profiles (Roommates.BoostedUntil)
///   • Priority Inbox     (Users.PriorityInboxUntil)
///   • Analytics subscription (Users.AnalyticsUntil + role downgrade)
/// </summary>
public sealed class PremiumExpirationService
{
    private readonly ListingsContext _listings;
    private readonly RoommatesContext _roommates;
    private readonly UsersContext _users;
    private readonly IUserInterface _userSvc;
    private readonly ILogger<PremiumExpirationService> _logger;

    public PremiumExpirationService(
        ListingsContext listings,
        RoommatesContext roommates,
        UsersContext users,
        IUserInterface userSvc,
        ILogger<PremiumExpirationService> logger)
    {
        _listings = listings;
        _roommates = roommates;
        _users    = users;
        _userSvc  = userSvc;
        _logger   = logger;
    }

    public async Task RunAsync()
    {
        // ── 1. Featured listings ────────────────────────────────────────────────
        var featuredExpired = await _listings.Database.ExecuteSqlRawAsync(
            "UPDATE [Listings].[Apartments] SET IsFeatured = 0 " +
            "WHERE IsFeatured = 1 AND FeaturedUntil IS NOT NULL AND FeaturedUntil < GETUTCDATE()");

        if (featuredExpired > 0)
            _logger.LogInformation("PremiumExpiration: expired {N} featured listing(s).", featuredExpired);

        // ── 2. Boosted roommate profiles ────────────────────────────────────────
        var boostExpired = await _roommates.Database.ExecuteSqlRawAsync(
            "UPDATE [Roommates].[Roommates] SET BoostedUntil = NULL " +
            "WHERE BoostedUntil IS NOT NULL AND BoostedUntil < GETUTCDATE()");

        if (boostExpired > 0)
            _logger.LogInformation("PremiumExpiration: expired {N} boosted profile(s).", boostExpired);

        // ── 3. Priority Inbox ──────────────────────────────────────────────────
        var priorityExpired = await _users.Database.ExecuteSqlRawAsync(
            "UPDATE [UsersRoles].[Users] SET PriorityInboxUntil = NULL " +
            "WHERE PriorityInboxUntil IS NOT NULL AND PriorityInboxUntil < GETUTCDATE()");

        if (priorityExpired > 0)
            _logger.LogInformation("PremiumExpiration: expired {N} priority inbox(es).", priorityExpired);

        // ── 4. Analytics subscriptions ─────────────────────────────────────────
        var expiredAnalyticsUsers = await _users.Users
            .Where(u => u.AnalyticsUntil != null && u.AnalyticsUntil < DateTime.UtcNow
                     && (u.HasPersonalAnalytics || u.HasLandlordAnalytics))
            .Select(u => new { u.UserId, u.UserRole!.RoleName })
            .ToListAsync();

        foreach (var u in expiredAnalyticsUsers)
        {
            try
            {
                var downgraded = u.RoleName switch
                {
                    RoleConstants.PremiumTenant   => RoleConstants.Tenant,
                    RoleConstants.PremiumLandlord => RoleConstants.Landlord,
                    _ => null
                };
                if (downgraded != null)
                    await _userSvc.UpgradeUserRoleAsync(u.UserId, downgraded);

                await _users.Database.ExecuteSqlRawAsync(
                    "UPDATE [UsersRoles].[Users] " +
                    "SET HasPersonalAnalytics = 0, HasLandlordAnalytics = 0, AnalyticsUntil = NULL " +
                    "WHERE UserId = {0}", u.UserId);

                _logger.LogInformation(
                    "PremiumExpiration: analytics expired for userId={UserId}, downgraded to {Role}.",
                    u.UserId, downgraded ?? u.RoleName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PremiumExpiration: analytics downgrade failed for userId={UserId}.", u.UserId);
            }
        }

        _logger.LogInformation(
            "PremiumExpiration run complete — featured={F}, boosts={B}, priority={P}, analytics={A}.",
            featuredExpired, boostExpired, priorityExpired, expiredAnalyticsUsers.Count);
    }
}
