using Lander.src.Modules.Payments.Dtos;

namespace Lander.src.Modules.Payments.Interfaces;

public interface IMonriService
{
    MonriPaymentFormDto CreatePaymentForm(string planId, string successUrl, string failureUrl, int userId, string buyerEmail, string buyerName);
    Task HandleCallbackAsync(string json);
}
