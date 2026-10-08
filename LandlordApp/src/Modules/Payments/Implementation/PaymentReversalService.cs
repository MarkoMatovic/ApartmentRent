using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Notifications.Dtos.InputDto;
using Lander.src.Notifications.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Payments.Implementation;

/// <summary>
/// Reverses a fulfilled purchase after a full refund or chargeback. Mirrors the grants in
/// <see cref="PaymentFulfillmentService"/>: consumables (tokens, listing credits) are taken
/// back but never below zero, time-based benefits (analytics, featured, boost, priority inbox)
/// have the purchased duration subtracted and are switched off if that leaves no time.
/// </summary>
public class PaymentReversalService : IPaymentReversalService
{
    private readonly PaymentsContext _paymentsContext;
    private readonly UsersContext _usersContext;
    private readonly ListingsContext _listingsContext;
    private readonly RoommatesContext _roommatesContext;
    private readonly IPaymentService _paymentService;
    private readonly INotificationService _notifications;
    private readonly ILogger<PaymentReversalService> _logger;
    private readonly TimeProvider _timeProvider;

    public PaymentReversalService(
        PaymentsContext paymentsContext,
        UsersContext usersContext,
        ListingsContext listingsContext,
        RoommatesContext roommatesContext,
        IPaymentService paymentService,
        INotificationService notifications,
        ILogger<PaymentReversalService> logger,
        TimeProvider timeProvider)
    {
        _paymentsContext = paymentsContext;
        _usersContext = usersContext;
        _listingsContext = listingsContext;
        _roommatesContext = roommatesContext;
        _paymentService = paymentService;
        _notifications = notifications;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<ReversalOutcome> ReverseAsync(string transactionId, string adjustmentId, bool isChargeback)
    {
        var order = await _paymentsContext.ProcessedOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == transactionId);

        if (order is null)
        {
            _logger.LogWarning(
                "Reversal {Adjustment}: no fulfilled order for transaction {Txn} — nothing to take back.",
                adjustmentId, transactionId);
            return ReversalOutcome.OrderNotFound;
        }

        if (order.UserId is null || string.IsNullOrEmpty(order.PlanId))
        {
            _logger.LogWarning(
                "Reversal {Adjustment}: order {Txn} has no user/plan recorded (legacy row) — reverse it manually.",
                adjustmentId, transactionId);
            return ReversalOutcome.NotReversible;
        }

        // Claim the reversal atomically: only the caller that flips ReversedAt from NULL proceeds,
        // so a duplicate or retried event can never subtract the benefits twice.
        var now = _timeProvider.GetUtcNow();
        var claimed = await _paymentsContext.ProcessedOrders
            .Where(o => o.Id == order.Id && o.ReversedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.ReversedAt, now)
                .SetProperty(o => o.ReversalReference, adjustmentId));

        if (claimed == 0)
        {
            _logger.LogInformation("Reversal {Adjustment}: transaction {Txn} was already reversed.", adjustmentId, transactionId);
            return ReversalOutcome.AlreadyReversed;
        }

        try
        {
            await TakeBackAsync(order.UserId.Value, order.PlanId, order.ApartmentId);
        }
        catch
        {
            // Release the claim so the provider's retry can finish the job.
            await _paymentsContext.ProcessedOrders
                .Where(o => o.Id == order.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.ReversedAt, (DateTimeOffset?)null)
                    .SetProperty(o => o.ReversalReference, (string?)null));
            throw;
        }

        _logger.LogWarning(
            "Purchase reversed ({Kind}) — txn={Txn}, adjustment={Adjustment}, userId={UserId}, plan={Plan}",
            isChargeback ? "chargeback" : "refund", transactionId, adjustmentId, order.UserId, order.PlanId);

        await NotifyAsync(order.UserId.Value, isChargeback);
        return ReversalOutcome.Reversed;
    }

    private async Task TakeBackAsync(int userId, string planId, int? apartmentId)
    {
        if (PaymentFulfillmentService.TokenPlans.TryGetValue(planId, out var tokens))
        {
            // Never below zero: tokens may already have been spent.
            await _usersContext.Database.ExecuteSqlRawAsync(
                "UPDATE [UsersRoles].[Users] SET TokenBalance = CASE WHEN TokenBalance >= {0} THEN TokenBalance - {0} ELSE 0 END WHERE UserId = {1}",
                tokens, userId);
        }
        else if (PaymentFulfillmentService.ListingCreditPlans.TryGetValue(planId, out var credits))
        {
            await _usersContext.Database.ExecuteSqlRawAsync(
                "UPDATE [UsersRoles].[Users] SET ListingCredits = CASE WHEN ListingCredits >= {0} THEN ListingCredits - {0} ELSE 0 END WHERE UserId = {1}",
                credits, userId);
        }
        else if (planId.StartsWith("analytics", StringComparison.OrdinalIgnoreCase))
        {
            var days = planId.Contains("yearly", StringComparison.OrdinalIgnoreCase) ? 365 : 30;
            await _usersContext.Database.ExecuteSqlRawAsync(
                "UPDATE [UsersRoles].[Users] SET AnalyticsUntil = DATEADD(day, -{0}, AnalyticsUntil) WHERE UserId = {1} AND AnalyticsUntil IS NOT NULL",
                days, userId);

            var until = await _usersContext.Users.AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.AnalyticsUntil)
                .FirstOrDefaultAsync();

            // No paid time left → same switch-off as the user cancelling analytics.
            if (until is null || until <= _timeProvider.GetUtcNow().UtcDateTime)
                await _paymentService.CancelAnalyticsAsync(userId);
        }
        else if (PaymentFulfillmentService.FeaturedDays.TryGetValue(planId, out var featuredDays))
        {
            await TakeBackFeaturedAsync(userId, apartmentId, featuredDays);
        }
        else if (planId == "boost-7")
        {
            await TakeBackBoostAsync(userId, 7);
        }
        else if (planId == "priority-30")
        {
            await _usersContext.Database.ExecuteSqlRawAsync(
                @"UPDATE [UsersRoles].[Users]
                  SET PriorityInboxUntil = CASE
                    WHEN DATEADD(day, -{0}, PriorityInboxUntil) > GETUTCDATE()
                    THEN DATEADD(day, -{0}, PriorityInboxUntil)
                    ELSE NULL
                  END
                  WHERE UserId = {1} AND PriorityInboxUntil IS NOT NULL",
                30, userId);
        }
        else
        {
            _logger.LogWarning("Reversal: unknown plan '{Plan}' for user {UserId} — nothing to take back.", planId, userId);
        }
    }

    private async Task TakeBackFeaturedAsync(int userId, int? apartmentId, int days)
    {
        if (apartmentId is null or <= 0) return; // was never granted without an apartment

        var apartment = await _listingsContext.Apartments
            .FirstOrDefaultAsync(a => a.ApartmentId == apartmentId.Value && a.LandlordId == userId && !a.IsDeleted);
        if (apartment is null || !apartment.FeaturedUntil.HasValue) return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var remaining = apartment.FeaturedUntil.Value.AddDays(-days);
        apartment.IsFeatured = remaining > now;
        apartment.FeaturedUntil = remaining > now ? remaining : null;
        apartment.ModifiedDate = now;
        await _listingsContext.SaveChangesAsync();
    }

    private async Task TakeBackBoostAsync(int userId, int days)
    {
        var roommate = await _roommatesContext.Roommates
            .FirstOrDefaultAsync(r => r.UserId == userId && r.IsActive);
        if (roommate is null || !roommate.BoostedUntil.HasValue) return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var remaining = roommate.BoostedUntil.Value.AddDays(-days);
        roommate.BoostedUntil = remaining > now ? remaining : null;
        roommate.ModifiedDate = now;
        await _roommatesContext.SaveChangesAsync();
    }

    private async Task NotifyAsync(int userId, bool isChargeback)
    {
        try
        {
            await _notifications.SendNotificationAsync(new CreateNotificationInputDto
            {
                Title           = isChargeback ? "Kupovina poništena" : "Povraćaj novca obrađen",
                Message         = isChargeback
                    ? "Uplata je osporena kod banke, pa su pogodnosti iz te kupovine uklonjene sa vašeg naloga."
                    : "Povraćaj novca za vašu kupovinu je obrađen, pa su pogodnosti iz te kupovine uklonjene sa vašeg naloga.",
                ActionType      = "purchase_reversed",
                ActionTarget    = "/istorija-placanja",
                CreatedByGuid   = Guid.Empty,
                SenderUserId    = 0,
                RecipientUserId = userId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create reversal notification for user {UserId}.", userId);
        }
    }
}
