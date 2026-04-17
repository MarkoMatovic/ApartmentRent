using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;

namespace Lander.src.Modules.Payments.Implementation;

/// <summary>
/// Facade that keeps IMonriService intact for controllers while delegating to focused sub-services.
/// </summary>
public class MonriService : IMonriService
{
    private readonly IMonriPaymentFormService _formService;
    private readonly IMonriCallbackHandler _callbackHandler;

    public MonriService(IMonriPaymentFormService formService, IMonriCallbackHandler callbackHandler)
    {
        _formService = formService;
        _callbackHandler = callbackHandler;
    }

    public MonriPaymentFormDto CreatePaymentForm(
        string planId, string successUrl, string failureUrl,
        int userId, string buyerEmail, string buyerName)
        => _formService.CreatePaymentForm(planId, successUrl, failureUrl, userId, buyerEmail, buyerName);

    public Task HandleCallbackAsync(string json)
        => _callbackHandler.HandleCallbackAsync(json);
}
