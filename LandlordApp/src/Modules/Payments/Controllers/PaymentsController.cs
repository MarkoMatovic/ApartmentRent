using Lander.src.Common;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lander.src.Modules.Payments.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentsController : ApiControllerBase
{
    private readonly IMonriService _monriService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IMonriService monriService,
        IUserInterface userService,
        ILogger<PaymentsController> logger) : base(userService)
    {
        _monriService = monriService;
        _logger = logger;
    }

    [HttpGet("plans")]
    public IActionResult GetSubscriptionPlans()
        => Ok(_monriService.GetPlans());

    [HttpPost("create-payment")]
    [AllowAnonymous] // TEMP: k6 testing
    [DisableRateLimiting] // TEMP: k6 testing
    public async Task<IActionResult> CreatePayment([FromBody] CreateMonriPaymentRequest request)
    {
        var user = await GetCurrentUserAsync();
        var userId = user?.UserId ?? 0;

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var formDto = await _monriService.CreatePaymentAsync(userId, request.PlanId, request.SuccessUrl, request.FailureUrl, idempotencyKey);

        if (formDto is null)
            return Conflict(new { message = "Duplicate request — this payment was already initiated." });

        return Ok(formDto);
    }

    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        try
        {
            await _monriService.HandleCallbackAsync(json);
            return Ok();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
        catch (Exception ex)
        {
            // Always return 200 to Monri to prevent retries on internal errors
            _logger.LogError(ex, "Unhandled error processing Monri payment callback.");
            return Ok();
        }
    }
}

public class CreateMonriPaymentRequest
{
    public string PlanId { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string FailureUrl { get; set; } = string.Empty;
}
