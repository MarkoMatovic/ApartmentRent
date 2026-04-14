using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using Lander.src.Modules.Payments.Interfaces;

namespace Lander.src.Modules.Payments.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubscriptionsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public SubscriptionsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            var userIdClaim = User.FindFirstValue("userId");
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "User ID not found in token." });

            var checkoutUrl = await _paymentService.InitiateCheckoutAsync(userId, request.PlanType, request.Amount);
            return Ok(new { CheckoutUrl = checkoutUrl });
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook([FromBody] PaytenWebhookPayload payload)
        {
            await _paymentService.ProcessWebhookAsync(payload.TransactionId, payload.Status);
            return Ok();
        }

        [HttpGet("status/{userId}")]
        public async Task<IActionResult> GetStatus(int userId)
        {
            // Verify requesting user matches the userId in route
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

    public class CheckoutRequest
    {
        public string PlanType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class PaytenWebhookPayload
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
