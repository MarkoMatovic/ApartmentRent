using Lander.src.Modules.Payments.Dtos;

namespace Lander.src.Modules.Payments.Interfaces;

public interface IMonriService
{
    MonriPaymentFormDto CreatePaymentForm(string planId, string successUrl, string failureUrl, int userId, string buyerEmail, string buyerName);

    /// <summary>
    /// Full orchestration: checks idempotency, fetches buyer profile, builds the payment form.
    /// Returns null when the request is a duplicate (idempotency key already seen).
    /// </summary>
    Task<MonriPaymentFormDto?> CreatePaymentAsync(int userId, string planId, string successUrl, string failureUrl, string? idempotencyKey = null);

    IEnumerable<SubscriptionPlanDto> GetPlans();

    Task HandleCallbackAsync(string json);
}
