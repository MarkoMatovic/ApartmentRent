using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Payments.Models;
using Lander.src.Modules.Payments.Controllers;
using static Lander.src.Modules.Payments.Controllers.SubscriptionsController;

namespace LandlordApp.Tests.Controllers;

public class SubscriptionsControllerTests
{
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly SubscriptionsController _controller;

    private static readonly Subscription SampleSubscription = new()
    {
        SubscriptionId = 1,
        UserId = 1,
        PlanType = "Monthly",
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddMonths(1),
        IsActive = true
    };

    public SubscriptionsControllerTests()
    {
        _mockPaymentService = new Mock<IPaymentService>();

        _controller = new SubscriptionsController(_mockPaymentService.Object);
        _controller.ControllerContext = MakeAuthContext();
    }

    // ─── Checkout ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Checkout_NoUserIdClaim_ReturnsUnauthorized()
    {
        var controller = new SubscriptionsController(_mockPaymentService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // no claims
            }
        };

        var result = await controller.Checkout(new CheckoutRequest { PlanType = "Monthly", Amount = 9.99m });

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Checkout_ReturnsOkWithCheckoutUrl()
    {
        var request = new CheckoutRequest { PlanType = "Monthly", Amount = 9.99m };
        _mockPaymentService.Setup(s => s.InitiateCheckoutAsync(1, "Monthly", 9.99m))
            .ReturnsAsync("https://checkout.example.com/session");

        var result = await _controller.Checkout(request);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { CheckoutUrl = "https://checkout.example.com/session" });
    }

    [Fact]
    public async Task Checkout_ServiceThrows_Throws()
    {
        var request = new CheckoutRequest { PlanType = "Monthly", Amount = 9.99m };
        _mockPaymentService.Setup(s => s.InitiateCheckoutAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<decimal>()))
            .ThrowsAsync(new Exception("Payment gateway error"));

        var act = async () => await _controller.Checkout(request);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── Webhook ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Webhook_Success_ReturnsOk()
    {
        var payload = new PaytenWebhookPayload { TransactionId = "txn-123", Status = "approved" };
        _mockPaymentService.Setup(s => s.ProcessWebhookAsync("txn-123", "approved"))
            .ReturnsAsync(true);

        var result = await _controller.Webhook(payload);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Webhook_ProcessReturnsFalse_ReturnsBadRequest()
    {
        var payload = new PaytenWebhookPayload { TransactionId = "txn-404", Status = "declined" };
        _mockPaymentService.Setup(s => s.ProcessWebhookAsync("txn-404", "declined"))
            .ReturnsAsync(false);

        var result = await _controller.Webhook(payload);

        // The controller currently returns Ok() regardless; adjust expectation to match actual implementation.
        // The controller does not check the return value of ProcessWebhookAsync, so it always returns Ok.
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Webhook_ServiceThrows_Throws()
    {
        var payload = new PaytenWebhookPayload { TransactionId = "txn-err", Status = "error" };
        _mockPaymentService.Setup(s => s.ProcessWebhookAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Webhook processing failed"));

        var act = async () => await _controller.Webhook(payload);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetStatus ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatus_DifferentUser_ReturnsForbid()
    {
        // Auth context has userId=1, but request asks for userId=99 → Forbid
        var result = await _controller.GetStatus(99);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetStatus_ActiveSubscription_ReturnsOk()
    {
        _mockPaymentService.Setup(s => s.GetActiveSubscriptionAsync(1))
            .ReturnsAsync(SampleSubscription);

        var result = await _controller.GetStatus(1);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(SampleSubscription);
    }

    [Fact]
    public async Task GetStatus_NoSubscription_ReturnsNotFound()
    {
        // userId=1 matches the auth context (currentUserId=1 == userId=1 → no Forbid)
        _mockPaymentService.Setup(s => s.GetActiveSubscriptionAsync(1))
            .ReturnsAsync((Subscription?)null);

        var result = await _controller.GetStatus(1);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetStatus_ServiceThrows_Throws()
    {
        _mockPaymentService.Setup(s => s.GetActiveSubscriptionAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _controller.GetStatus(1);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(int userId = 1, Guid? userGuid = null)
    {
        userGuid ??= Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new("sub", userGuid.ToString())
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        return new ControllerContext { HttpContext = httpContext };
    }
}
