using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Payments.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(IPaymentService paymentService, IConfiguration configuration, ILogger<SubscriptionsController> logger)
    {
        _paymentService = paymentService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto request)
    {
        var userIdClaim = User.FindFirstValue("userId");
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "User ID not found in token." });

        var checkoutUrl = await _paymentService.InitiateCheckoutAsync(userId, request.PlanType, request.Amount);
        return Ok(new { CheckoutUrl = checkoutUrl });
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        // Read raw body so we can verify HMAC before deserializing
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var webhookSecret = _configuration["Payten:WebhookSecret"];
        if (!string.IsNullOrEmpty(webhookSecret))
        {
            var receivedSig = Request.Headers["X-Payten-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(receivedSig))
            {
                _logger.LogWarning("Payten webhook received without signature header");
                return Unauthorized(new { message = "Missing webhook signature" });
            }

            var keyBytes = Encoding.UTF8.GetBytes(webhookSecret);
            var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
            var expectedSig = Convert.ToHexString(HMACSHA256.HashData(keyBytes, bodyBytes)).ToLowerInvariant();

            if (!string.Equals(expectedSig, receivedSig, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Payten webhook HMAC mismatch — payload rejected");
                return Unauthorized(new { message = "Invalid webhook signature" });
            }
        }
        else
        {
            _logger.LogWarning("Payten:WebhookSecret not configured — skipping signature check");
        }

        PaytenWebhookPayloadDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<PaytenWebhookPayloadDto>(rawBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return BadRequest(new { message = "Invalid webhook payload" });
        }

        if (payload == null || string.IsNullOrEmpty(payload.TransactionId))
            return BadRequest(new { message = "Missing transactionId" });

        await _paymentService.ProcessWebhookAsync(payload.TransactionId, payload.Status);
        return Ok();
    }

    [HttpGet("status/{userId}")]
    public async Task<IActionResult> GetStatus(int userId)
    {
        var currentUserIdClaim = User.FindFirstValue("userId");
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out var currentUserId))
            return Unauthorized();
        if (currentUserId != userId)
            return Forbid();

        var sub = await _paymentService.GetActiveSubscriptionAsync(userId);
        if (sub == null) return NotFound(new { message = "No active subscription" });
        return Ok(sub);
    }
}
