using System.Security.Cryptography;
using System.Text;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Lander.src.Modules.Payments.Controllers;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Payments.Paddle;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.Extensions.Logging;

namespace LandlordApp.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IPaymentService> _mockPayments = new();
    private readonly Mock<IPaymentFulfillmentService> _mockFulfillment = new();
    private readonly Mock<IPaymentReversalService> _mockReversal = new();
    private readonly Mock<IPaddleClient> _mockPaddle = new();
    private readonly Mock<IUserInterface> _mockUserService = new();

    private static readonly Guid TestGuid = Guid.NewGuid();
    private static readonly User TestUser = new()
    {
        UserId = 1, FirstName = "A", LastName = "B",
        Email = "a@b.com", Password = "h", UserGuid = TestGuid
    };

    private const string WebhookSecret = "pdl_ntfset_test_secret_123456";

    private PaymentsController BuildController(PaddleOptions options, bool authenticated = true)
    {
        var controller = new PaymentsController(
            _mockPayments.Object,
            _mockFulfillment.Object,
            _mockReversal.Object,
            _mockPaddle.Object,
            new PaddleSignatureVerifier(),
            Options.Create(options),
            _mockUserService.Object,
            new Mock<ILogger<PaymentsController>>().Object);

        controller.ControllerContext = new ControllerContext { HttpContext = MakeHttpContext(authenticated) };
        return controller;
    }

    private static PaddleOptions ConfiguredOptions() => new()
    {
        Environment = "sandbox",
        ApiKey = "pdl_test_key",
        ClientToken = "test_token",
        WebhookSecret = WebhookSecret,
        PriceIds = new Dictionary<string, string> { ["tokens-50"] = "pri_tokens50" }
    };

    // ─── GetSubscriptionPlans ─────────────────────────────────────────────────

    [Fact]
    public void GetSubscriptionPlans_ReturnsOkWithPlans()
    {
        _mockPayments.Setup(s => s.GetPlans()).Returns(new List<SubscriptionPlanDto>
        {
            new() { PlanId = "tokens-50", Name = "Tokens" }
        });

        var controller = BuildController(new PaddleOptions());
        var result = controller.GetSubscriptionPlans();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<SubscriptionPlanDto>>()
            .Which.Should().ContainSingle(p => p.PlanId == "tokens-50");
    }

    // ─── CreatePayment ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePayment_PaddleNotConfigured_ReturnsServiceUnavailable()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);

        var controller = BuildController(new PaddleOptions()); // empty → not configured
        var result = await controller.CreatePayment(new CreatePaymentRequest { PlanId = "tokens-50" });

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task CreatePayment_ValidPlan_CreatesTransactionAndReturnsId()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockPayments.Setup(s => s.GetPlans()).Returns(new List<SubscriptionPlanDto>
        {
            new() { PlanId = "tokens-50", Name = "Tokens" }
        });
        _mockPaddle
            .Setup(p => p.CreateTransactionAsync("pri_tokens50", It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("txn_created");

        var controller = BuildController(ConfiguredOptions());
        var result = await controller.CreatePayment(new CreatePaymentRequest { PlanId = "tokens-50" });

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value!.ToString().Should().Contain("txn_created");

        // custom_data must carry the authenticated user's id (server-side, untamperable).
        _mockPaddle.Verify(p => p.CreateTransactionAsync(
            "pri_tokens50",
            It.Is<IReadOnlyDictionary<string, string>>(d => d["userId"] == "1" && d["planId"] == "tokens-50"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePayment_UnknownPlan_ReturnsBadRequest()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockPayments.Setup(s => s.GetPlans()).Returns(new List<SubscriptionPlanDto>());

        var controller = BuildController(ConfiguredOptions());
        var result = await controller.CreatePayment(new CreatePaymentRequest { PlanId = "does-not-exist" });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreatePayment_UserNotFound_ReturnsUnauthorized()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync((User?)null);

        var controller = BuildController(ConfiguredOptions());
        var result = await controller.CreatePayment(new CreatePaymentRequest { PlanId = "tokens-50" });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ─── Webhook ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task PaddleWebhook_ValidSignatureAndCompletedTransaction_FulfillsOrder()
    {
        var body = """
        {"event_type":"transaction.completed","data":{"id":"txn_ABC",
        "custom_data":{"userId":"1","planId":"tokens-50"},
        "items":[{"price":{"id":"pri_tokens50"}}]}}
        """;

        var controller = BuildController(ConfiguredOptions());
        SetSignedBody(controller, body, WebhookSecret);

        var result = await controller.PaddleWebhook();

        result.Should().BeOfType<OkResult>();
        // planId is derived from the purchased price, and orderReference is the txn id.
        _mockFulfillment.Verify(f => f.FulfillAsync("txn_ABC", 1, "tokens-50", null), Times.Once);
    }

    [Fact]
    public async Task PaddleWebhook_InvalidSignature_ReturnsUnauthorizedAndDoesNotFulfil()
    {
        var body = """{"event_type":"transaction.completed","data":{"id":"txn_X"}}""";

        var controller = BuildController(ConfiguredOptions());
        // Sign with the WRONG secret → verification must fail.
        SetSignedBody(controller, body, "wrong-secret");

        var result = await controller.PaddleWebhook();

        result.Should().BeOfType<UnauthorizedResult>();
        _mockFulfillment.Verify(f => f.FulfillAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task PaddleWebhook_NonCompletedEvent_IsAcknowledgedWithoutFulfilling()
    {
        var body = """{"event_type":"transaction.created","data":{"id":"txn_Y"}}""";

        var controller = BuildController(ConfiguredOptions());
        SetSignedBody(controller, body, WebhookSecret);

        var result = await controller.PaddleWebhook();

        result.Should().BeOfType<OkResult>();
        _mockFulfillment.Verify(f => f.FulfillAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    // ─── Webhook: refunds / chargebacks (adjustment.*) ─────────────────────────

    private static string AdjustmentBody(string action, string status, string type, string txn = "txn_REF") =>
        "{\"event_type\":\"adjustment.updated\",\"data\":{\"id\":\"adj_1\",\"action\":\"" + action +
        "\",\"status\":\"" + status + "\",\"type\":\"" + type + "\",\"transaction_id\":\"" + txn + "\"}}";

    [Fact]
    public async Task PaddleWebhook_ApprovedFullRefund_ReversesThePurchase()
    {
        var controller = BuildController(ConfiguredOptions());
        SetSignedBody(controller, AdjustmentBody("refund", "approved", "full"), WebhookSecret);

        var result = await controller.PaddleWebhook();

        result.Should().BeOfType<OkResult>();
        _mockReversal.Verify(r => r.ReverseAsync("txn_REF", "adj_1", false), Times.Once);
    }

    [Fact]
    public async Task PaddleWebhook_ApprovedChargeback_ReversesAsChargeback()
    {
        var controller = BuildController(ConfiguredOptions());
        SetSignedBody(controller, AdjustmentBody("chargeback", "approved", "full"), WebhookSecret);

        await controller.PaddleWebhook();

        _mockReversal.Verify(r => r.ReverseAsync("txn_REF", "adj_1", true), Times.Once);
    }

    [Theory]
    [InlineData("refund", "pending_approval", "full")]   // not approved yet
    [InlineData("refund", "rejected", "full")]            // refund was declined
    [InlineData("refund", "approved", "partial")]         // partial: flagged for manual review
    [InlineData("credit", "approved", "full")]            // credit note, not a refund
    [InlineData("chargeback_warning", "approved", "full")] // early warning only
    public async Task PaddleWebhook_NonActionableAdjustment_IsAcknowledgedWithoutReversing(
        string action, string status, string type)
    {
        var controller = BuildController(ConfiguredOptions());
        SetSignedBody(controller, AdjustmentBody(action, status, type), WebhookSecret);

        var result = await controller.PaddleWebhook();

        result.Should().BeOfType<OkResult>();
        _mockReversal.Verify(r => r.ReverseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task PaddleWebhook_AdjustmentWithInvalidSignature_DoesNotReverse()
    {
        var controller = BuildController(ConfiguredOptions());
        SetSignedBody(controller, AdjustmentBody("refund", "approved", "full"), "wrong-secret");

        var result = await controller.PaddleWebhook();

        result.Should().BeOfType<UnauthorizedResult>();
        _mockReversal.Verify(r => r.ReverseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    // ─── my-status / my-orders ────────────────────────────────────────────────

    [Fact]
    public async Task GetMyStatus_Authenticated_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockPayments.Setup(s => s.GetUserStatusAsync(TestUser.UserId))
            .ReturnsAsync(new UserSubscriptionStatusDto { TokenBalance = 5 });

        var controller = BuildController(new PaddleOptions());
        var result = await controller.GetMyStatus();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyOrders_Authenticated_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockPayments.Setup(s => s.GetUserOrdersAsync(TestUser.UserId))
            .ReturnsAsync(new List<PaymentOrderDto>());

        var controller = BuildController(new PaddleOptions());
        var result = await controller.GetMyOrders();

        result.Should().BeOfType<OkObjectResult>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static DefaultHttpContext MakeHttpContext(bool authenticated)
    {
        var identity = authenticated
            ? new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("sub", TestGuid.ToString()),
                new System.Security.Claims.Claim("userId", "1")
            }, "Test")
            : new System.Security.Claims.ClaimsIdentity();

        return new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) };
    }

    private static void SetSignedBody(PaymentsController controller, string body, string secret)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var h1 = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{ts}:{body}"))).ToLowerInvariant();

        var http = controller.ControllerContext.HttpContext;
        http.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        http.Request.Headers["Paddle-Signature"] = $"ts={ts};h1={h1}";
    }
}
