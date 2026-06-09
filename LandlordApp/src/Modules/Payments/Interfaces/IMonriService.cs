using Lander.src.Modules.Payments.Dtos;

namespace Lander.src.Modules.Payments.Interfaces;

public interface IMonriService
{
    MonriPaymentFormDto CreatePaymentForm(string planId, string successUrl, string failureUrl, int userId, string buyerEmail, string buyerName);
    MonriPaymentFormDto CreatePaymentForm(string planId, string successUrl, string failureUrl, int userId, string buyerEmail, string buyerName, int? apartmentId);

    /// <summary>
    /// Full orchestration: checks idempotency, fetches buyer profile, builds the payment form.
    /// Returns null when the request is a duplicate (idempotency key already seen).
    /// </summary>
    /// <param name="apartmentId">Required for featured-* plans; ignored otherwise.</param>
    Task<MonriPaymentFormDto?> CreatePaymentAsync(int userId, string planId, string successUrl, string failureUrl, string? idempotencyKey = null, int? apartmentId = null);

    IEnumerable<SubscriptionPlanDto> GetPlans();

    Task HandleCallbackAsync(string json);

    /// <summary>Returns the current active premium feature state for a user.</summary>
    Task<UserSubscriptionStatusDto> GetUserStatusAsync(int userId);

    /// <summary>Returns the user's processed payment order history, newest first.</summary>
    Task<List<PaymentOrderDto>> GetUserOrdersAsync(int userId);

    /// <summary>
    /// Deactivates analytics and downgrades premium role.
    /// Note: since all purchases are one-time (no auto-renewal), this is a manual downgrade.
    /// </summary>
    Task CancelAnalyticsAsync(int userId);
}
