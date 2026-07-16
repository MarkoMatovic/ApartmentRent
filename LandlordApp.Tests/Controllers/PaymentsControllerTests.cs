using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Lander.src.Modules.Payments.Controllers;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.Extensions.Logging;

namespace LandlordApp.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IPaymentService> _mockPayments;
    private readonly Mock<IUserInterface> _mockUserService;
    private readonly PaymentsController _controller;

    private static readonly Guid TestGuid = Guid.NewGuid();
    private static readonly User TestUser = new()
    {
        UserId = 1, FirstName = "A", LastName = "B",
        Email = "a@b.com", Password = "h", UserGuid = TestGuid
    };

    public PaymentsControllerTests()
    {
        _mockPayments = new Mock<IPaymentService>();
        _mockUserService = new Mock<IUserInterface>();

        _controller = new PaymentsController(
            _mockPayments.Object, _mockUserService.Object,
            new Mock<ILogger<PaymentsController>>().Object);
        _controller.ControllerContext = MakeAuthContext(TestGuid);
    }

    // ─── GetSubscriptionPlans ─────────────────────────────────────────────────

    [Fact]
    public void GetSubscriptionPlans_ReturnsOkWithPlans()
    {
        _mockPayments.Setup(s => s.GetPlans()).Returns(new List<SubscriptionPlanDto>
        {
            new() { PlanId = "basic", Name = "Basic" }
        });

        var result = _controller.GetSubscriptionPlans();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var plans = ok.Value.Should().BeAssignableTo<IEnumerable<SubscriptionPlanDto>>().Subject;
        plans.Should().HaveCount(1);
        plans.First().PlanId.Should().Be("basic");
    }

    // ─── CreatePayment (no provider configured → 503) ─────────────────────────

    [Fact]
    public async Task CreatePayment_Authenticated_ReturnsServiceUnavailable()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);

        var result = await _controller.CreatePayment(new CreatePaymentRequest
        {
            PlanId = "basic",
            SuccessUrl = "https://s.com",
            FailureUrl = "https://f.com"
        });

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task CreatePayment_UserNotFound_ReturnsUnauthorized()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync((User?)null);

        var result = await _controller.CreatePayment(new CreatePaymentRequest { PlanId = "basic" });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreatePayment_NoSubClaim_ReturnsUnauthorized()
    {
        var controller = new PaymentsController(
            _mockPayments.Object, _mockUserService.Object,
            new Mock<ILogger<PaymentsController>>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity())
            }
        };

        var result = await controller.CreatePayment(new CreatePaymentRequest { PlanId = "basic" });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ─── Callback (no provider configured → 503) ──────────────────────────────

    [Fact]
    public void Callback_ReturnsServiceUnavailable()
    {
        var result = _controller.Callback();

        var obj = result.Should().BeOfType<StatusCodeResult>().Subject;
        obj.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    // ─── my-status / my-orders ────────────────────────────────────────────────

    [Fact]
    public async Task GetMyStatus_Authenticated_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockPayments.Setup(s => s.GetUserStatusAsync(TestUser.UserId))
            .ReturnsAsync(new UserSubscriptionStatusDto { TokenBalance = 5 });

        var result = await _controller.GetMyStatus();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyOrders_Authenticated_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockPayments.Setup(s => s.GetUserOrdersAsync(TestUser.UserId))
            .ReturnsAsync(new List<PaymentOrderDto>());

        var result = await _controller.GetMyOrders();

        result.Should().BeOfType<OkObjectResult>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(Guid userGuid)
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new("sub", userGuid.ToString()),
            new("userId", "1")
        };
        var httpContext = new DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(claims, "Test"))
        };
        return new ControllerContext { HttpContext = httpContext };
    }
}
