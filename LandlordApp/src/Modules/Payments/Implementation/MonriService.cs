using Lander.Helpers;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace Lander.src.Modules.Payments.Implementation;

/// <summary>
/// Facade that keeps IMonriService intact for controllers while delegating to focused sub-services.
/// </summary>
public class MonriService : IMonriService
{
    private readonly IMonriPaymentFormService _formService;
    private readonly IMonriCallbackHandler _callbackHandler;
    private readonly IUserInterface _userService;
    private readonly IdempotencyService _idempotencyService;
    private readonly IConfiguration _configuration;

    public MonriService(
        IMonriPaymentFormService formService,
        IMonriCallbackHandler callbackHandler,
        IUserInterface userService,
        IdempotencyService idempotencyService,
        IConfiguration configuration)
    {
        _formService = formService;
        _callbackHandler = callbackHandler;
        _userService = userService;
        _idempotencyService = idempotencyService;
        _configuration = configuration;
    }

    public MonriPaymentFormDto CreatePaymentForm(
        string planId, string successUrl, string failureUrl,
        int userId, string buyerEmail, string buyerName)
        => _formService.CreatePaymentForm(planId, successUrl, failureUrl, userId, buyerEmail, buyerName);

    public async Task<MonriPaymentFormDto?> CreatePaymentAsync(
        int userId, string planId, string successUrl, string failureUrl, string? idempotencyKey = null)
    {
        if (idempotencyKey is not null &&
            await _idempotencyService.IsDuplicateAsync($"payment:{userId}:{idempotencyKey}"))
            return null;

        var userProfile = userId > 0 ? await _userService.GetUserProfileAsync(userId) : null;
        var buyerEmail = userProfile?.Email ?? "k6-test@landlord-test.local";
        var buyerName  = userProfile != null
            ? $"{userProfile.FirstName} {userProfile.LastName}".Trim()
            : "K6 Test";

        return _formService.CreatePaymentForm(planId, successUrl, failureUrl, userId, buyerEmail, buyerName);
    }

    public IEnumerable<SubscriptionPlanDto> GetPlans()
        => _configuration.GetSection("Monri:Plans").GetChildren()
            .Select(s => new SubscriptionPlanDto
            {
                Name        = s["Name"] ?? s.Key,
                Description = s["Description"] ?? string.Empty,
                Price       = decimal.Parse(s["Amount"] ?? "999") / 100,
                Currency    = s["Currency"] ?? "EUR",
                PlanId      = s.Key,
                Interval    = s["Interval"] ?? "month"
            });

    public Task HandleCallbackAsync(string json)
        => _callbackHandler.HandleCallbackAsync(json);
}
