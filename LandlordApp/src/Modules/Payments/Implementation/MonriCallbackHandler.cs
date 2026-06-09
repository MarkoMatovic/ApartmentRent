using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lander.Helpers;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Payments.Models;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Payments.Implementation;

public class MonriCallbackHandler : IMonriCallbackHandler
{
    private readonly string _merchantKey;
    private readonly IUserInterface _userService;
    private readonly PaymentsContext _paymentsContext;
    private readonly UsersContext _usersContext;
    private readonly ListingsContext _listingsContext;
    private readonly RoommatesContext _roommatesContext;
    private readonly ILogger<MonriCallbackHandler> _logger;
    private readonly TimeProvider _timeProvider;

    // Planovi koji dodaju tokene na balans — ključ = planId, vrijednost = broj tokena
    private static readonly IReadOnlyDictionary<string, int> TokenPlans = new Dictionary<string, int>
    {
        ["tokens-10"]  = 10,
        ["tokens-50"]  = 50,
        ["tokens-150"] = 150,
    };

    // Planovi koji dodaju listing kredite — ključ = planId, vrijednost = broj kredita
    private static readonly IReadOnlyDictionary<string, int> ListingCreditPlans = new Dictionary<string, int>
    {
        ["listing-1"] = 1,
        ["listing-3"] = 3,
        ["listing-5"] = 5,
    };

    // Trajanje featured planova u danima
    private static readonly IReadOnlyDictionary<string, int> FeaturedDays = new Dictionary<string, int>
    {
        ["featured-7"]  = 7,
        ["featured-30"] = 30,
    };

    public MonriCallbackHandler(
        IConfiguration configuration,
        IUserInterface userService,
        PaymentsContext paymentsContext,
        UsersContext usersContext,
        ListingsContext listingsContext,
        RoommatesContext roommatesContext,
        ILogger<MonriCallbackHandler> logger,
        TimeProvider timeProvider)
    {
        _merchantKey = configuration["Monri:MerchantKey"]
            ?? throw new InvalidOperationException("Monri:MerchantKey is not configured");
        _userService = userService;
        _paymentsContext = paymentsContext;
        _usersContext = usersContext;
        _listingsContext = listingsContext;
        _roommatesContext = roommatesContext;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task HandleCallbackAsync(string json)
    {
        _logger.LogInformation("Monri callback received");
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monri callback contained invalid JSON");
            throw new ArgumentException("Invalid callback JSON");
        }

        var root = doc.RootElement;
        var eventType = root.TryGetProperty("event", out var ev) ? ev.GetString() : null;
        var hasPayload = root.TryGetProperty("payload", out var payload);

        if (eventType == "transaction:approved" && hasPayload)
        {
            if (!ValidateCallbackDigest(payload))
            {
                _logger.LogWarning("Monri callback HMAC digest validation failed — payload rejected");
                throw new InvalidOperationException("Monri callback digest mismatch.");
            }
            await ProcessApprovedTransaction(payload);
        }
        else if (root.TryGetProperty("pgw_response_code", out var pgwCode) && pgwCode.GetString() == "0000")
        {
            if (!ValidateCallbackDigest(root))
            {
                _logger.LogWarning("Monri form callback HMAC digest validation failed — payload rejected");
                throw new InvalidOperationException("Monri callback digest mismatch.");
            }
            await ProcessApprovedTransaction(root);
        }
    }

    /// <summary>
    /// Validates Monri callback HMAC:
    /// SHA512(merchant_key + order_number + pgw_transaction_id + pgw_response_code
    ///        + pgw_amount + pgw_outgoing_amount + pgw_currency + pgw_outgoing_currency
    ///        + pgw_approval_code + pgw_response_message)
    /// </summary>
    private bool ValidateCallbackDigest(JsonElement data)
    {
        try
        {
            var receivedDigest = data.TryGetProperty("digest", out var d) ? d.GetString() : null;
            if (string.IsNullOrEmpty(receivedDigest))
            {
                _logger.LogWarning("Monri callback missing digest field");
                return false;
            }

            string Get(string key) => data.TryGetProperty(key, out var v) ? v.GetString() ?? "" : "";

            var raw = string.Concat(
                _merchantKey,
                Get("order_number"),
                Get("pgw_transaction_id"),
                Get("pgw_response_code"),
                Get("pgw_amount"),
                Get("pgw_outgoing_amount"),
                Get("pgw_currency"),
                Get("pgw_outgoing_currency"),
                Get("pgw_approval_code"),
                Get("pgw_response_message")
            );

            var expected = Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes(raw))).ToLower();
            // Constant-time comparison prevents timing-oracle attacks.
            var match = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(receivedDigest.ToLower()));
            if (!match)
                _logger.LogWarning("Monri digest mismatch. Expected prefix: {Expected}…",
                    expected.Length >= 16 ? expected[..16] : expected);
            return match;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Monri digest validation");
            return false;
        }
    }

    private async Task ProcessApprovedTransaction(JsonElement data)
    {
        var orderNumber = data.TryGetProperty("order_number", out var on) ? on.GetString() : null;
        if (string.IsNullOrEmpty(orderNumber)) return;

        // DB-backed idempotency — safe across multiple instances
        var alreadyProcessed = await _paymentsContext.ProcessedMonriOrders
            .AnyAsync(o => o.OrderNumber == orderNumber);
        if (alreadyProcessed)
        {
            _logger.LogWarning("Monri duplicate callback for order {OrderNumber} — ignored", orderNumber);
            return;
        }

        // Order number formats:
        //   featured plans:  "{userId}_{planId}_{apartmentId}_{timestamp}"  (4 parts)
        //   all other plans: "{userId}_{planId}_{timestamp}"                (3 parts)
        var parts = orderNumber.Split('_');
        if (parts.Length < 3 || !int.TryParse(parts[0], out var userId)) return;

        var planId = parts[1];
        int? apartmentId = null;

        if (planId.StartsWith("featured", StringComparison.OrdinalIgnoreCase) && parts.Length >= 4)
        {
            if (int.TryParse(parts[2], out var aptId))
                apartmentId = aptId;
        }

        _logger.LogInformation("Monri payment approved — userId={UserId}, planId={PlanId}, order={Order}",
            userId, planId, orderNumber);

        // ── Route to the correct fulfillment handler based on planId ─────────
        if (TokenPlans.TryGetValue(planId, out var tokensToAdd))
            await FulfillTokenPurchaseAsync(userId, planId, tokensToAdd);
        else if (planId.StartsWith("analytics", StringComparison.OrdinalIgnoreCase))
            await FulfillAnalyticsSubscriptionAsync(userId, planId);
        else if (FeaturedDays.TryGetValue(planId, out var days))
            await FulfillFeaturedListingAsync(userId, planId, apartmentId, days);
        else if (ListingCreditPlans.TryGetValue(planId, out var credits))
            await FulfillListingCreditsAsync(userId, planId, credits);
        else if (planId == "boost-7")
            await FulfillBoostProfileAsync(userId, planId, boostDays: 7);
        else if (planId == "priority-30")
            await FulfillPriorityInboxAsync(userId, planId, days: 30);
        else
            _logger.LogWarning(
                "Monri order {Order}: unknown planId '{PlanId}' — recording as processed without action.",
                orderNumber, planId);

        // Mark as processed AFTER fulfillment succeeds
        try
        {
            _paymentsContext.ProcessedMonriOrders.Add(new ProcessedMonriOrder
            {
                OrderNumber = orderNumber,
                ProcessedAt = _timeProvider.GetUtcNow()
            });
            await _paymentsContext.SaveEntitiesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique constraint race — another instance processed concurrently; action already done
            _logger.LogWarning("Monri concurrent duplicate for order {OrderNumber} — fulfillment already applied", orderNumber);
        }
    }

    // ── Fulfillment handlers ─────────────────────────────────────────────────

    private async Task FulfillAnalyticsSubscriptionAsync(int userId, string planId)
    {
        var userProfile = await _userService.GetUserProfileAsync(userId);
        if (userProfile == null) { _logger.LogWarning("Analytics fulfillment: user {UserId} not found", userId); return; }

        var targetRoleName = userProfile.RoleName switch
        {
            RoleConstants.Tenant         => RoleConstants.PremiumTenant,
            RoleConstants.TenantLandlord => RoleConstants.PremiumLandlord,
            _                            => RoleConstants.PremiumLandlord
        };
        await _userService.UpgradeUserRoleAsync(userId, targetRoleName);

        var updateDto = new UserProfileUpdateInputDto
        {
            FirstName = userProfile.FirstName, LastName = userProfile.LastName,
            Email = userProfile.Email, PhoneNumber = userProfile.PhoneNumber,
            DateOfBirth = userProfile.DateOfBirth, HasPersonalAnalytics = true
        };
        await _userService.UpdateUserProfileAsync(userId, updateDto);

        _logger.LogInformation("Analytics fulfilled — userId={UserId}, plan={Plan}, role={Role}",
            userId, planId, targetRoleName);
    }

    private async Task FulfillTokenPurchaseAsync(int userId, string planId, int tokensToAdd)
    {
        var affected = await _usersContext.Database.ExecuteSqlRawAsync(
            "UPDATE [UsersRoles].[Users] SET TokenBalance = TokenBalance + {0} WHERE UserId = {1}",
            tokensToAdd, userId);

        if (affected == 0)
            _logger.LogWarning("Token fulfillment: user {UserId} not found — plan={Plan}", userId, planId);
        else
            _logger.LogInformation("Tokens fulfilled — userId={UserId}, plan={Plan}, added={Count}",
                userId, planId, tokensToAdd);
    }

    private async Task FulfillFeaturedListingAsync(int userId, string planId, int? apartmentId, int days)
    {
        if (apartmentId is null or <= 0)
        {
            _logger.LogWarning("Featured fulfillment: missing apartmentId for plan={Plan}, userId={UserId}", planId, userId);
            return;
        }

        // Verify the apartment belongs to this user before featuring it
        var apartment = await _listingsContext.Apartments
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId.Value && a.LandlordId == userId && !a.IsDeleted);
        if (apartment == null)
        {
            _logger.LogWarning("Featured fulfillment: apartment {AptId} not found or not owned by user {UserId}",
                apartmentId, userId);
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        // Extend if already featured (stack on top of remaining time)
        apartment.IsFeatured = true;
        apartment.FeaturedUntil = apartment.FeaturedUntil.HasValue && apartment.FeaturedUntil > now
            ? apartment.FeaturedUntil.Value.AddDays(days)
            : now.AddDays(days);
        apartment.ModifiedDate = now;
        await _listingsContext.SaveChangesAsync();

        _logger.LogInformation("Featured fulfilled — apartmentId={AptId}, userId={UserId}, plan={Plan}, until={Until}",
            apartmentId, userId, planId, apartment.FeaturedUntil);
    }

    private async Task FulfillListingCreditsAsync(int userId, string planId, int credits)
    {
        var affected = await _usersContext.Database.ExecuteSqlRawAsync(
            "UPDATE [UsersRoles].[Users] SET ListingCredits = ListingCredits + {0} WHERE UserId = {1}",
            credits, userId);

        if (affected == 0)
            _logger.LogWarning("Listing credits fulfillment: user {UserId} not found — plan={Plan}", userId, planId);
        else
            _logger.LogInformation("Listing credits fulfilled — userId={UserId}, plan={Plan}, added={Credits}",
                userId, planId, credits);
    }

    private async Task FulfillBoostProfileAsync(int userId, string planId, int boostDays)
    {
        var roommate = await _roommatesContext.Roommates
            .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);
        if (roommate == null)
        {
            _logger.LogWarning("Boost fulfillment: no active roommate profile for user {UserId}", userId);
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        // Stack on existing boost if still active
        roommate.BoostedUntil = roommate.BoostedUntil.HasValue && roommate.BoostedUntil > now
            ? roommate.BoostedUntil.Value.AddDays(boostDays)
            : now.AddDays(boostDays);
        roommate.ModifiedDate = now;
        await _roommatesContext.SaveChangesAsync();

        _logger.LogInformation("Boost fulfilled — userId={UserId}, plan={Plan}, until={Until}",
            userId, planId, roommate.BoostedUntil);
    }

    private async Task FulfillPriorityInboxAsync(int userId, string planId, int days)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var affected = await _usersContext.Database.ExecuteSqlRawAsync(
            @"UPDATE [UsersRoles].[Users]
              SET PriorityInboxUntil = CASE
                WHEN PriorityInboxUntil IS NOT NULL AND PriorityInboxUntil > GETUTCDATE()
                THEN DATEADD(day, {0}, PriorityInboxUntil)
                ELSE DATEADD(day, {0}, GETUTCDATE())
              END
              WHERE UserId = {1}",
            days, userId);

        if (affected == 0)
            _logger.LogWarning("Priority Inbox fulfillment: user {UserId} not found — plan={Plan}", userId, planId);
        else
            _logger.LogInformation("Priority Inbox fulfilled — userId={UserId}, plan={Plan}, days={Days}",
                userId, planId, days);
    }
}
