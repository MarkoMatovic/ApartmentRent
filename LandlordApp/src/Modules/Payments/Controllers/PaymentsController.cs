using System.Text.Json;
using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Payments.Paddle;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Lander.src.Modules.Payments.Controllers;

[Route(ApiActionsV1.Payments)]
[ApiController]
public class PaymentsController : ApiControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IPaymentFulfillmentService _fulfillment;
    private readonly IPaymentReversalService _reversal;
    private readonly IPaddleClient _paddle;
    private readonly PaddleSignatureVerifier _signatureVerifier;
    private readonly PaddleOptions _paddleOptions;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        IPaymentFulfillmentService fulfillment,
        IPaymentReversalService reversal,
        IPaddleClient paddle,
        PaddleSignatureVerifier signatureVerifier,
        IOptions<PaddleOptions> paddleOptions,
        IUserInterface userService,
        ILogger<PaymentsController> logger) : base(userService)
    {
        _paymentService = paymentService;
        _fulfillment = fulfillment;
        _reversal = reversal;
        _paddle = paddle;
        _signatureVerifier = signatureVerifier;
        _paddleOptions = paddleOptions.Value;
        _logger = logger;
    }

    [HttpGet(ApiActionsV1.GetSubscriptionPlans, Name = nameof(ApiActionsV1.GetSubscriptionPlans))]
    public IActionResult GetSubscriptionPlans()
        => Ok(_paymentService.GetPlans());

    /// <summary>
    /// Public Paddle.js bootstrap config. The client token is public by design; the
    /// browser needs it plus the environment to initialise the inline checkout.
    /// </summary>
    [HttpGet(ApiActionsV1.PaddleConfig, Name = nameof(ApiActionsV1.PaddleConfig))]
    [AllowAnonymous]
    public IActionResult GetPaddleConfig()
        => Ok(new
        {
            environment = _paddleOptions.Environment,
            clientToken = _paddleOptions.ClientToken,
            enabled = _paddleOptions.IsConfigured
        });

    /// <summary>
    /// Creates a Paddle transaction for the chosen plan and returns its id. The frontend
    /// opens the Paddle inline checkout with that id. custom_data (userId, planId,
    /// apartmentId) is attached here — server-side — so the browser can never tamper with it.
    /// </summary>
    [HttpPost(ApiActionsV1.CreatePayment, Name = nameof(ApiActionsV1.CreatePayment))]
    [Authorize]
    [EnableRateLimiting("create-payment")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        if (!_paddleOptions.IsConfigured)
        {
            _logger.LogWarning("create-payment called but Paddle is not configured.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Plaćanje trenutno nije dostupno. Pokušajte kasnije." });
        }

        if (string.IsNullOrWhiteSpace(request.PlanId))
            return BadRequest(new { message = "planId je obavezan." });

        // The plan must exist in our catalogue AND have a Paddle price mapped.
        var planExists = _paymentService.GetPlans().Any(p => p.PlanId == request.PlanId);
        if (!planExists)
            return BadRequest(new { message = "Nepoznat plan." });

        if (!_paddleOptions.PriceIds.TryGetValue(request.PlanId, out var priceId) ||
            string.IsNullOrWhiteSpace(priceId))
        {
            _logger.LogError("No Paddle Price ID mapped for plan {PlanId}.", request.PlanId);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Ovaj plan trenutno nije dostupan za kupovinu." });
        }

        // featured-* plans require the apartment being promoted.
        if (request.PlanId.StartsWith("featured", StringComparison.OrdinalIgnoreCase) && request.ApartmentId is null)
            return BadRequest(new { message = "Za istaknuti oglas je obavezan apartmentId." });

        var customData = new Dictionary<string, string>
        {
            ["userId"] = user.UserId.ToString(),
            ["planId"] = request.PlanId,
        };
        if (request.ApartmentId.HasValue)
            customData["apartmentId"] = request.ApartmentId.Value.ToString();

        var transactionId = await _paddle.CreateTransactionAsync(priceId, customData);
        return Ok(new { transactionId });
    }

    /// <summary>Returns the current active premium feature state for the authenticated user.</summary>
    [HttpGet(ApiActionsV1.GetMyPaymentStatus, Name = nameof(ApiActionsV1.GetMyPaymentStatus))]
    [Authorize]
    public async Task<IActionResult> GetMyStatus()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();
        return Ok(await _paymentService.GetUserStatusAsync(user.UserId));
    }

    /// <summary>Returns the authenticated user's processed payment order history, newest first.</summary>
    [HttpGet(ApiActionsV1.GetMyOrders, Name = nameof(ApiActionsV1.GetMyOrders))]
    [Authorize]
    public async Task<IActionResult> GetMyOrders()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();
        return Ok(await _paymentService.GetUserOrdersAsync(user.UserId));
    }

    /// <summary>
    /// Deactivates analytics and downgrades premium role. All payments are one-time
    /// (no auto-renewal), so this is a manual feature deactivation.
    /// </summary>
    [HttpPost(ApiActionsV1.CancelAnalytics, Name = nameof(ApiActionsV1.CancelAnalytics))]
    [Authorize]
    public async Task<IActionResult> CancelAnalytics()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();
        await _paymentService.CancelAnalyticsAsync(user.UserId);
        return Ok(new { message = "Analitika je deaktivirana. Vaš nalog je vraćen na osnovni plan." });
    }

    /// <summary>
    /// Paddle webhook. Verifies the signature against the raw body, then fulfils the order
    /// on <c>transaction.paid</c> or <c>transaction.completed</c>, whichever arrives first
    /// (idempotent on the transaction id). The plan is derived from the purchased Paddle price
    /// (authoritative), never from client-supplied data. Idempotency lives in FulfillAsync.
    /// </summary>
    [HttpPost(ApiActionsV1.PaddleWebhook, Name = nameof(ApiActionsV1.PaddleWebhook))]
    [AllowAnonymous]
    public async Task<IActionResult> PaddleWebhook()
    {
        // Read the EXACT raw bytes — the signature is computed over them; parsing first breaks it.
        string rawBody;
        using (var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8))
            rawBody = await reader.ReadToEndAsync();

        var signature = Request.Headers["Paddle-Signature"].FirstOrDefault();
        if (!_signatureVerifier.Verify(signature, rawBody, _paddleOptions.WebhookSecret, _paddleOptions.MaxWebhookAgeSeconds))
        {
            _logger.LogWarning("Paddle webhook rejected — invalid or missing signature.");
            return Unauthorized();
        }

        string eventType;
        try
        {
            eventType = PeekEventType(rawBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paddle webhook: failed to parse payload.");
            return BadRequest();
        }

        // Refunds and chargebacks arrive as adjustment.* events.
        if (eventType.StartsWith("adjustment.", StringComparison.Ordinal))
            return await HandleAdjustmentAsync(rawBody);

        PaddleWebhookData parsed;
        try
        {
            parsed = ParseWebhook(rawBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paddle webhook: failed to parse payload.");
            return BadRequest();
        }

        // Grant on the first of transaction.paid / transaction.completed to arrive: paid fires as
        // soon as the payment is captured, completed follows once Paddle finishes post-payment
        // processing, so granting on paid avoids waiting for the later event. Both carry the same
        // transaction id and FulfillAsync is idempotent on it, so whichever fires first grants and
        // the other is a no-op. Everything else is acknowledged with 200 so Paddle does not retry
        // events we intentionally ignore.
        if (parsed.EventType != "transaction.completed" && parsed.EventType != "transaction.paid")
            return Ok();

        if (parsed.UserId is not int userId)
        {
            _logger.LogWarning("Paddle webhook {Txn}: missing/invalid userId in custom_data — cannot fulfil.", parsed.TransactionId);
            return Ok(); // acknowledge; nothing actionable, don't trigger retries
        }

        // Authoritative plan = the one mapped to the actually-purchased price.
        var planId = parsed.PriceId is not null && _paddleOptions.PlanIdByPriceId.TryGetValue(parsed.PriceId, out var mapped)
            ? mapped
            : parsed.PlanId;

        if (string.IsNullOrWhiteSpace(planId))
        {
            _logger.LogWarning("Paddle webhook {Txn}: could not resolve planId (price {Price}).", parsed.TransactionId, parsed.PriceId);
            return Ok();
        }

        if (parsed.PlanId is not null && !string.Equals(parsed.PlanId, planId, StringComparison.Ordinal))
            _logger.LogWarning("Paddle webhook {Txn}: custom_data planId '{Claimed}' != price-derived planId '{Actual}' — using price-derived.",
                parsed.TransactionId, parsed.PlanId, planId);

        await _fulfillment.FulfillAsync(parsed.TransactionId, userId, planId, parsed.ApartmentId);
        return Ok();
    }

    /// <summary>
    /// Takes back a purchase after Paddle approves a FULL refund or a chargeback. Pending or
    /// rejected adjustments, partial refunds, credits and chargeback warnings are acknowledged
    /// and logged only — a partial refund has no automatic equivalent (what share of 50 tokens is
    /// a 30% refund?), so it is flagged for manual review. Exceptions bubble up as a 500 so Paddle
    /// retries; the reversal itself is idempotent per transaction.
    /// </summary>
    private async Task<IActionResult> HandleAdjustmentAsync(string rawBody)
    {
        PaddleAdjustmentData adj;
        try
        {
            adj = ParseAdjustment(rawBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Paddle webhook: failed to parse adjustment payload.");
            return BadRequest();
        }

        var isChargeback = adj.Action == "chargeback";
        if (adj.Action != "refund" && !isChargeback)
            return Ok(); // credit, chargeback_warning, chargeback_reverse, ... — nothing to take back

        if (adj.Status != "approved")
        {
            _logger.LogInformation("Paddle adjustment {Adj} ({Action}) is '{Status}' — not applied yet.", adj.Id, adj.Action, adj.Status);
            return Ok();
        }

        if (adj.Type != "full")
        {
            _logger.LogWarning(
                "Paddle adjustment {Adj} is a PARTIAL {Action} on {Txn} — benefits NOT reversed automatically; review manually.",
                adj.Id, adj.Action, adj.TransactionId);
            return Ok();
        }

        if (string.IsNullOrWhiteSpace(adj.TransactionId))
        {
            _logger.LogWarning("Paddle adjustment {Adj} has no transaction_id — cannot reverse.", adj.Id);
            return Ok();
        }

        await _reversal.ReverseAsync(adj.TransactionId, adj.Id, isChargeback);
        return Ok();
    }

    private static string PeekEventType(string rawBody)
    {
        using var doc = JsonDocument.Parse(rawBody);
        return doc.RootElement.TryGetProperty("event_type", out var et) ? et.GetString() ?? "" : "";
    }

    private static PaddleAdjustmentData ParseAdjustment(string rawBody)
    {
        using var doc = JsonDocument.Parse(rawBody);
        var data = doc.RootElement.GetProperty("data");
        string S(string name) => data.TryGetProperty(name, out var v) ? v.GetString() ?? "" : "";
        return new PaddleAdjustmentData(S("id"), S("action"), S("status"), S("type"), S("transaction_id"));
    }

    // ── Webhook parsing ──────────────────────────────────────────────────────
    private static PaddleWebhookData ParseWebhook(string rawBody)
    {
        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var eventType = root.TryGetProperty("event_type", out var et) ? et.GetString() ?? "" : "";
        var data = root.GetProperty("data");

        var txnId = data.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

        int? userId = null, apartmentId = null;
        string? planId = null;
        if (data.TryGetProperty("custom_data", out var cd) && cd.ValueKind == JsonValueKind.Object)
        {
            if (cd.TryGetProperty("userId", out var u) && int.TryParse(u.GetString(), out var uid)) userId = uid;
            if (cd.TryGetProperty("planId", out var p)) planId = p.GetString();
            if (cd.TryGetProperty("apartmentId", out var a) && int.TryParse(a.GetString(), out var aid)) apartmentId = aid;
        }

        // First line item's price id → authoritative plan lookup.
        string? priceId = null;
        if (data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("price", out var price) && price.TryGetProperty("id", out var pid))
                {
                    priceId = pid.GetString();
                    break;
                }
            }
        }

        return new PaddleWebhookData(eventType, txnId, userId, planId, apartmentId, priceId);
    }

    private sealed record PaddleAdjustmentData(
        string Id, string Action, string Status, string Type, string TransactionId);

    private sealed record PaddleWebhookData(
        string EventType, string TransactionId, int? UserId, string? PlanId, int? ApartmentId, string? PriceId);
}

public class CreatePaymentRequest
{
    public string PlanId { get; set; } = string.Empty;

    /// <summary>Required for featured-* plans — identifies which apartment to feature. Ignored otherwise.</summary>
    public int? ApartmentId { get; set; }
}
