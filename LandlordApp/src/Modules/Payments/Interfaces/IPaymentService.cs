using Lander.src.Modules.Payments.Dtos;

namespace Lander.src.Modules.Payments.Interfaces;

/// <summary>
/// Provider-agnostic payment facade. Holds everything that does not depend on a specific
/// payment provider: the catalogue of plans, the user's purchased-feature status, order
/// history and manual analytics cancellation. Initiating a charge and handling provider
/// callbacks are intentionally NOT here — those belong to whichever provider we plug in
/// next, which then calls <see cref="IPaymentFulfillmentService"/> to grant the purchase.
/// </summary>
public interface IPaymentService
{
    IEnumerable<SubscriptionPlanDto> GetPlans();

    /// <summary>Returns the current active premium feature state for a user.</summary>
    Task<UserSubscriptionStatusDto> GetUserStatusAsync(int userId);

    /// <summary>Returns the user's processed payment order history, newest first.</summary>
    Task<List<PaymentOrderDto>> GetUserOrdersAsync(int userId);

    /// <summary>
    /// Deactivates analytics and downgrades the premium role.
    /// Note: purchases are one-time (no auto-renewal), so this is a manual downgrade.
    /// </summary>
    Task CancelAnalyticsAsync(int userId);
}
