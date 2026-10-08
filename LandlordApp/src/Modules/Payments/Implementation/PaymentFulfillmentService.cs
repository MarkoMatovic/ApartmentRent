using Lander.Helpers;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Payments.Models;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
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
    private readonly INotificationService _notifications;
    private readonly ILogger<PaymentFulfillmentService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly IConfiguration _configuration;

    // Token plans — planId => number of tokens granted
    internal static readonly IReadOnlyDictionary<string, int> TokenPlans = new Dictionary<string, int>
    {
        ["tokens-10"]  = 10,
        ["tokens-50"]  = 50,
        ["tokens-150"] = 150,
    };

    // Listing-credit plans — planId => number of credits granted
    internal static readonly IReadOnlyDictionary<string, int> ListingCreditPlans = new Dictionary<string, int>
    {
        ["listing-1"] = 1,
        ["listing-3"] = 3,
        ["listing-5"] = 5,
    };

    // Featured plans — planId => duration in days
    internal static readonly IReadOnlyDictionary<string, int> FeaturedDays = new Dictionary<string, int>
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
        INotificationService notifications,
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
        _notifications = notifications;
        _logger = logger;
        _timeProvider = timeProvider;
        _configuration = configuration;
    }

    public async Task<bool> FulfillAsync(string orderReference, int userId, string planId, int? apartmentId = null)
    {
        if (string.IsNullOrWhiteSpace(orderReference))
            throw new ArgumentException("orderReference is required.", nameof(orderReference));

        // Fast path for sequential duplicates (webhook retries, paid → completed pair).
        if (await _paymentsContext.ProcessedOrders.AnyAsync(o => o.OrderNumber == orderReference))
        {
            _logger.LogWarning("Payment order {Order} already fulfilled — ignored.", orderReference);
            return true;
        }

        // ── Claim the order BEFORE granting anything ─────────────────────────
        // The unique index on OrderNumber makes the claim atomic across threads and instances.
        // Previously the grant ran first and the row was written afterwards, so two concurrent
        // webhooks for the same transaction (Paddle sends transaction.paid and
        // transaction.completed back to back) both passed the check above and both granted;
        // the loser's unique-key failure was swallowed as "already applied". Claiming first
        // means exactly one caller proceeds to the grant.
        var claim = new ProcessedOrder
        {
            OrderNumber = orderReference,
            ProcessedAt = _timeProvider.GetUtcNow(),
            UserId      = userId,
            PlanId      = planId,
            ApartmentId = apartmentId
        };
        _paymentsContext.ProcessedOrders.Add(claim);
        try
        {
            await _paymentsContext.SaveEntitiesAsync();
        }
        catch (DbUpdateException)
        {
            _paymentsContext.Entry(claim).State = EntityState.Detached;
            if (await _paymentsContext.ProcessedOrders.AnyAsync(o => o.OrderNumber == orderReference))
            {
                _logger.LogWarning("Payment order {Order} claimed by a concurrent request — ignored.", orderReference);
                return true;
            }
            throw; // not a duplicate: a genuine database failure
        }

        _logger.LogInformation("Fulfilling payment order {Order} — userId={UserId}, planId={PlanId}",
            orderReference, userId, planId);

        try
        {
            await GrantAsync(orderReference, userId, planId, apartmentId);
        }
        catch
        {
            // Grant failed: release the claim so the provider's webhook retry can fulfil it,
            // instead of leaving a "processed" row for an order that was never delivered.
            try
            {
                _paymentsContext.ProcessedOrders.Remove(claim);
                await _paymentsContext.SaveEntitiesAsync();
            }
            catch (Exception releaseEx)
            {
                _logger.LogError(releaseEx,
                    "Failed to release claim for order {Order} after a failed grant — manual check needed.", orderReference);
            }
            throw;
        }

        // In-app notification so the purchase is visible immediately in the bell menu,
        // independent of email (which needs Brevo configured). Best-effort — never fails fulfillment.
        await SendPurchaseNotificationAsync(userId, planId);

        // Send order confirmation email (best-effort — never fail the fulfillment).
        _ = SendConfirmationEmailAsync(userId, planId, orderReference);

        return true;
    }

    // ── Route to the correct fulfillment handler based on planId ─────────────
    private async Task GrantAsync(string orderReference, int userId, string planId, int? apartmentId)
    {
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
                "Payment order {Order}: unknown planId '{PlanId}' — recorded as processed without action.",
                orderReference, planId);
    }

    /// <summary>Creates an in-app notification describing what the purchase granted.</summary>
    private async Task SendPurchaseNotificationAsync(int userId, string planId)
    {
        try
        {
            await _notifications.SendNotificationAsync(new CreateNotificationInputDto
            {
                Title           = "Uspešna kupovina",
                Message         = BuildPurchaseMessage(planId),
                ActionType      = "purchase",
                ActionTarget    = "/istorija-placanja",
                CreatedByGuid   = Guid.Empty,
                SenderUserId    = 0,
                RecipientUserId = userId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create purchase notification for user {UserId}, plan {PlanId}.", userId, planId);
        }
    }

    private string BuildPurchaseMessage(string planId)
    {
        if (TokenPlans.TryGetValue(planId, out var tokens))
            return $"Uspešno ste kupili {tokens} tokena — dodati su na vaš nalog.";
        if (planId.StartsWith("analytics", StringComparison.OrdinalIgnoreCase))
            return "Napredna analitika je aktivirana na vašem nalogu.";
        if (FeaturedDays.TryGetValue(planId, out var featuredDays))
            return $"Vaš oglas je istaknut na vrhu pretrage narednih {featuredDays} dana.";
        if (ListingCreditPlans.TryGetValue(planId, out var credits))
            return $"Dodato je {credits} kredita za objavu oglasa.";
        if (planId == "boost-7")
            return "Boost profila cimera je aktiviran na 7 dana.";
        if (planId == "priority-30")
            return "Priority Inbox je aktiviran na 30 dana.";
        return "Vaša kupovina je uspešno obrađena.";
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
