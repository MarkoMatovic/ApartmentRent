using Lander.Helpers;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Payments.Models;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Payments.Implementation;

/// <summary>
/// Provider-agnostic order fulfillment. Whichever payment provider is plugged in
/// only needs to confirm the charge and call <see cref="FulfillAsync"/> — all the
/// business effects (tokens, credits, featured, boost, priority, analytics) and
/// idempotency live here.
/// </summary>
public class PaymentFulfillmentService : IPaymentFulfillmentService
{
    private readonly IUserInterface _userService;
    private readonly PaymentsContext _paymentsContext;
    private readonly UsersContext _usersContext;
    private readonly ListingsContext _listingsContext;
    private readonly RoommatesContext _roommatesContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentFulfillmentService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _configuration;

    // Token plans — planId => number of tokens granted
    private static readonly IReadOnlyDictionary<string, int> TokenPlans = new Dictionary<string, int>
    {
        ["tokens-10"]  = 10,
        ["tokens-50"]  = 50,
        ["tokens-150"] = 150,
    };

    // Listing-credit plans — planId => number of credits granted
    private static readonly IReadOnlyDictionary<string, int> ListingCreditPlans = new Dictionary<string, int>
    {
        ["listing-1"] = 1,
        ["listing-3"] = 3,
        ["listing-5"] = 5,
    };

    // Featured plans — planId => duration in days
    private static readonly IReadOnlyDictionary<string, int> FeaturedDays = new Dictionary<string, int>
    {
        ["featured-7"]  = 7,
        ["featured-30"] = 30,
    };

    public PaymentFulfillmentService(
        IUserInterface userService,
        PaymentsContext paymentsContext,
        UsersContext usersContext,
        ListingsContext listingsContext,
        RoommatesContext roommatesContext,
        IEmailService emailService,
        ILogger<PaymentFulfillmentService> logger,
        TimeProvider timeProvider,
        IConfiguration configuration)
    {
        _userService = userService;
        _paymentsContext = paymentsContext;
        _usersContext = usersContext;
        _listingsContext = listingsContext;
        _roommatesContext = roommatesContext;
        _emailService = emailService;
        _logger = logger;
        _timeProvider = timeProvider;
        _configuration = configuration;
    }

    public async Task<bool> FulfillAsync(string orderReference, int userId, string planId, int? apartmentId = null)
    {
        if (string.IsNullOrWhiteSpace(orderReference))
            throw new ArgumentException("orderReference is required.", nameof(orderReference));

        // DB-backed idempotency — safe across multiple instances.
        var alreadyProcessed = await _paymentsContext.ProcessedOrders
            .AnyAsync(o => o.OrderNumber == orderReference);
        if (alreadyProcessed)
        {
            _logger.LogWarning("Payment order {Order} already fulfilled — ignored.", orderReference);
            return true;
        }

        _logger.LogInformation("Fulfilling payment order {Order} — userId={UserId}, planId={PlanId}",
            orderReference, userId, planId);

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
                "Payment order {Order}: unknown planId '{PlanId}' — recording as processed without action.",
                orderReference, planId);

        // Mark as processed AFTER fulfillment succeeds.
        try
        {
            _paymentsContext.ProcessedOrders.Add(new ProcessedOrder
            {
                OrderNumber = orderReference,
                ProcessedAt = _timeProvider.GetUtcNow()
            });
            await _paymentsContext.SaveEntitiesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique-constraint race — another instance fulfilled concurrently; effect already applied.
            _logger.LogWarning("Concurrent duplicate for order {Order} — fulfillment already applied.", orderReference);
        }

        // Send order confirmation email (best-effort — never fail the fulfillment).
        _ = SendConfirmationEmailAsync(userId, planId, orderReference);

        return true;
    }

    private async Task SendConfirmationEmailAsync(int userId, string planId, string orderReference)
    {
        try
        {
            var userProfile = await _userService.GetUserProfileAsync(userId);
            if (userProfile is null) return;

            var planSection = _configuration.GetSection($"Payments:Plans:{planId}");
            var planName = planSection["Name"] ?? planId;
            var amountCents = planSection["Amount"];
            var amountEur = decimal.TryParse(amountCents, out var c) ? c / 100m : 0m;

            await _emailService.SendOrderConfirmationEmailAsync(
                userProfile.Email, $"{userProfile.FirstName} {userProfile.LastName}",
                planName, orderReference, amountEur);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Order confirmation email failed for order {Order}", orderReference);
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
            PhoneNumber = userProfile.PhoneNumber,
            DateOfBirth = userProfile.DateOfBirth, HasPersonalAnalytics = true
        };
        await _userService.UpdateUserProfileAsync(userId, updateDto);

        // Set AnalyticsUntil with stacking: monthly = +30 days, yearly = +365 days.
        var days = planId.Contains("yearly", StringComparison.OrdinalIgnoreCase) ? 365 : 30;
        await _usersContext.Database.ExecuteSqlRawAsync(
            @"UPDATE [UsersRoles].[Users]
              SET AnalyticsUntil = CASE
                WHEN AnalyticsUntil IS NOT NULL AND AnalyticsUntil > GETUTCDATE()
                THEN DATEADD(day, {0}, AnalyticsUntil)
                ELSE DATEADD(day, {0}, GETUTCDATE())
              END
              WHERE UserId = {1}",
            days, userId);

        _logger.LogInformation("Analytics fulfilled — userId={UserId}, plan={Plan}, role={Role}, days={Days}",
            userId, planId, targetRoleName, days);
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

        // Verify the apartment belongs to this user before featuring it.
        var apartment = await _listingsContext.Apartments
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId.Value && a.LandlordId == userId && !a.IsDeleted);
        if (apartment == null)
        {
            _logger.LogWarning("Featured fulfillment: apartment {AptId} not found or not owned by user {UserId}",
                apartmentId, userId);
            return;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
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
