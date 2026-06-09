using Lander.src.Modules.Payments.Dtos;

namespace Lander.src.Modules.Payments.Interfaces;

public interface IMonriPaymentFormService
{
    /// <param name="apartmentId">Required for featured-* plans; null for all others.</param>
    MonriPaymentFormDto CreatePaymentForm(
        string planId, string successUrl, string failureUrl,
        int userId, string buyerEmail, string buyerName,
        int? apartmentId = null);
}
