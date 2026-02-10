using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace Lander.src.Modules.Payments.Controllers;

[Route("api/payments")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly IUserInterface _userService;
    private readonly IConfiguration _configuration;

    public PaymentsController(IStripeService stripeService, IUserInterface userService, IConfiguration configuration)
    {
        _stripeService = stripeService;
        _userService = userService;
        _configuration = configuration;
    }

    [HttpGet("plans")]
    public IActionResult GetSubscriptionPlans()
    {
        var premiumPlan = new Lander.src.Modules.Payments.Dtos.SubscriptionPlanDto
        {
            Name = _configuration["Stripe:PremiumPlan:Name"] ?? "Premium Analytics",
            Description = "Get access to advanced analytics, insights, and ML-powered features",
            Price = decimal.Parse(_configuration["Stripe:PremiumPlan:PriceEUR"] ?? "1.99"),
            Currency = _configuration["Stripe:PremiumPlan:Currency"] ?? "EUR",
            StripePriceId = _configuration["Stripe:PremiumPlan:PriceId"] ?? "",
            Interval = "month"
        };

        return Ok(new[] { premiumPlan });
    }

    [HttpPost("create-checkout-session")]
    [Authorize]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid)) return Unauthorized();

        var user = await _userService.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null) return Unauthorized();

        try
        {
            var session = await _stripeService.CreateCheckoutSessionAsync(
                request.PriceId,
                request.SuccessUrl,
                request.CancelUrl,
                user.UserId
            );

            return Ok(new { url = session.Url });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("webhook")]
    [AllowAnonymous] // Webhooks come from Stripe, not authenticated users
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        try
        {
            await _stripeService.HandleWebhookAsync(json, signature);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message }); // Stripe expects 400 if signature is invalid
        }
    }
}

public class CreateCheckoutSessionRequest
{
    public string PriceId { get; set; }
    public string SuccessUrl { get; set; }
    public string CancelUrl { get; set; }
}
