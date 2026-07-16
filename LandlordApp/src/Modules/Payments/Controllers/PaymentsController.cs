using Lander.src.Common;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Payments.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentsController : ApiControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IUserInterface userService,
        ILogger<PaymentsController> logger) : base(userService)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet("plans")]
    public IActionResult GetSubscriptionPlans()
        => Ok(_paymentService.GetPlans());

    // ─────────────────────────────────────────────────────────────────────────
    // Charge initiation + provider callback.
    //
    // The Monri provider was removed (cost). The frontend payment pages stay; a new
    // provider will be plugged in here. When that happens, the provider implementation
    // confirms the charge and calls IPaymentFulfillmentService.FulfillAsync(...) to grant
    // the purchase. Until then these endpoints return 503 so the UI can show a clear
    // "payments temporarily unavailable" state instead of failing opaquely.
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("create-payment")]
    [Authorize]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        _logger.LogInformation(
            "create-payment requested by user {UserId} for plan {PlanId} but no payment provider is configured.",
            user.UserId, request.PlanId);

        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { message = "Plaćanje trenutno nije dostupno. Pokušajte kasnije." });
    }

    /// <summary>Returns the current active premium feature state for the authenticated user.</summary>
    [HttpGet("my-status")]
    [Authorize]
    public async Task<IActionResult> GetMyStatus()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();
        return Ok(await _paymentService.GetUserStatusAsync(user.UserId));
    }

    /// <summary>Returns the authenticated user's processed payment order history, newest first.</summary>
    [HttpGet("my-orders")]
    [Authorize]
    public async Task<IActionResult> GetMyOrders()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();
        return Ok(await _paymentService.GetUserOrdersAsync(user.UserId));
    }

    /// <summary>
    /// Deactivates analytics subscription and downgrades premium role.
    /// Since all payments are one-time (no auto-renewal), this is a manual feature deactivation.
    /// </summary>
    [HttpPost("cancel-analytics")]
    [Authorize]
    public async Task<IActionResult> CancelAnalytics()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();
        await _paymentService.CancelAnalyticsAsync(user.UserId);
        return Ok(new { message = "Analitika je deaktivirana. Vaš nalog je vraćen na osnovni plan." });
    }

    [HttpPost("callback")]
    [AllowAnonymous]
    public IActionResult Callback()
    {
        // No payment provider is currently configured; reject callbacks explicitly.
        _logger.LogWarning("Payment callback received but no payment provider is configured.");
        return StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}

public class CreatePaymentRequest
{
    public string PlanId { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string FailureUrl { get; set; } = string.Empty;
    /// <summary>
    /// Required for featured-* plans — identifies which apartment to feature.
    /// Ignored for all other plan types.
    /// </summary>
    public int? ApartmentId { get; set; }
}
