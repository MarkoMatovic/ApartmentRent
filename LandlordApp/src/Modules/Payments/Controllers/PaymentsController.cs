using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Lander.Helpers;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace Lander.src.Modules.Payments.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IMonriService _monriService;
    private readonly IUserInterface _userService;
    private readonly IConfiguration _configuration;
    private readonly IdempotencyService _idempotencyService;

    public PaymentsController(IMonriService monriService, IUserInterface userService, IConfiguration configuration, IdempotencyService idempotencyService)
    {
        _monriService = monriService;
        _userService = userService;
        _configuration = configuration;
        _idempotencyService = idempotencyService;
    }

    [HttpGet("plans")]
    public IActionResult GetSubscriptionPlans()
    {
        var plans = _configuration.GetSection("Monri:Plans").GetChildren()
            .Select(s => new SubscriptionPlanDto
            {
                Name = s["Name"] ?? s.Key,
                Description = s["Description"] ?? string.Empty,
                Price = decimal.Parse(s["Amount"] ?? "999") / 100,
                Currency = s["Currency"] ?? "EUR",
                PlanId = s.Key,
                Interval = s["Interval"] ?? "month"
            })
            .ToList();

        return Ok(plans);
    }

    [HttpPost("create-payment")]
    [Authorize]
    [EnableRateLimiting("mutating")]
    public async Task<IActionResult> CreatePayment([FromBody] CreateMonriPaymentRequest request)
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid)) return Unauthorized();

        var user = await _userService.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null) return Unauthorized();

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(idempotencyKey) && _idempotencyService.IsDuplicate($"payment:{user.UserId}:{idempotencyKey}"))
            return Conflict(new { message = "Duplicate request — this payment was already initiated." });

        var userProfile = await _userService.GetUserProfileAsync(user.UserId);
        if (userProfile == null) return Unauthorized();

        var formDto = _monriService.CreatePaymentForm(
            request.PlanId,
            request.SuccessUrl,
            request.FailureUrl,
            user.UserId,
            userProfile.Email ?? string.Empty,
            $"{userProfile.FirstName} {userProfile.LastName}".Trim()
        );

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
        catch
        {
            // Always return 200 to Monri to prevent retries on internal errors
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
