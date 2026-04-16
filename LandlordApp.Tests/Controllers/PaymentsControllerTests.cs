using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text;
using Lander.src.Modules.Payments.Controllers;
using Lander.src.Modules.Payments.Dtos;
using Lander.src.Modules.Payments.Interfaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Lander.Helpers;

namespace LandlordApp.Tests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IMonriService> _mockMonri;
    private readonly Mock<IUserInterface> _mockUserService;
    private readonly IConfiguration _config;
    private readonly PaymentsController _controller;

    private static readonly Guid TestGuid = Guid.NewGuid();
    private static readonly User TestUser = new()
    {
        UserId = 1, FirstName = "A", LastName = "B",
        Email = "a@b.com", Password = "h", UserGuid = TestGuid
    };
    private static readonly UserProfileDto TestProfile = new()
    {
        UserId = 1, FirstName = "A", LastName = "B", Email = "a@b.com", RoleName = "Tenant"
    };

    public PaymentsControllerTests()
    {
        _mockMonri = new Mock<IMonriService>();
        _mockUserService = new Mock<IUserInterface>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Monri:Plans:basic:Name"] = "Basic",
                ["Monri:Plans:basic:Amount"] = "999",
                ["Monri:Plans:basic:Currency"] = "EUR",
                ["Monri:Plans:basic:Description"] = "Basic plan",
                ["Monri:Plans:basic:Interval"] = "month",
            })
            .Build();

        _controller = new PaymentsController(_mockMonri.Object, _mockUserService.Object, _config,
            new IdempotencyService(new Mock<IDistributedCache>().Object),
            new Mock<ILogger<PaymentsController>>().Object);
        _controller.ControllerContext = MakeAuthContext(TestGuid);
    }

    // ─── GetSubscriptionPlans ─────────────────────────────────────────────────

    [Fact]
    public void GetSubscriptionPlans_ReturnsOkWithPlans()
    {
        var result = _controller.GetSubscriptionPlans();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var plans = ok.Value.Should().BeAssignableTo<IEnumerable<SubscriptionPlanDto>>().Subject;
        plans.Should().HaveCount(1);
        plans.First().PlanId.Should().Be("basic");
    }

    // ─── CreatePayment ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePayment_Authenticated_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockUserService.Setup(s => s.GetUserProfileAsync(1)).ReturnsAsync(TestProfile);
        _mockMonri.Setup(s => s.CreatePaymentForm(
            "basic", "https://s.com", "https://f.com",
            1, "a@b.com", "A B"))
            .Returns(new MonriPaymentFormDto { OrderNumber = "1_basic_20260101" });

        var request = new CreateMonriPaymentRequest
        {
            PlanId = "basic",
            SuccessUrl = "https://s.com",
            FailureUrl = "https://f.com"
        };

        var result = await _controller.CreatePayment(request);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreatePayment_UserNotFound_ReturnsUnauthorized()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync((User?)null);

        var result = await _controller.CreatePayment(new CreateMonriPaymentRequest { PlanId = "basic" });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreatePayment_ProfileNotFound_ReturnsUnauthorized()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockUserService.Setup(s => s.GetUserProfileAsync(1)).ReturnsAsync((UserProfileDto?)null);

        var result = await _controller.CreatePayment(new CreateMonriPaymentRequest { PlanId = "basic" });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreatePayment_NoSubClaim_ReturnsUnauthorized()
    {
        var controller = new PaymentsController(_mockMonri.Object, _mockUserService.Object, _config,
            new IdempotencyService(new Mock<IDistributedCache>().Object),
            new Mock<ILogger<PaymentsController>>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                // No claims at all
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity())
            }
        };

        var result = await controller.CreatePayment(new CreateMonriPaymentRequest { PlanId = "basic" });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ─── Callback ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Callback_ValidJson_ReturnsOk()
    {
        _mockMonri.Setup(s => s.HandleCallbackAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        SetupRequestBody("{\"event\":\"transaction:approved\"}");

        var result = await _controller.Callback();

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Callback_InvalidJson_ReturnsBadRequest()
    {
        _mockMonri.Setup(s => s.HandleCallbackAsync(It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("invalid json"));

        SetupRequestBody("not json");

        var result = await _controller.Callback();

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Callback_OtherException_ReturnsOk()
    {
        // Always return 200 to Monri to prevent retries
        _mockMonri.Setup(s => s.HandleCallbackAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("digest mismatch"));

        SetupRequestBody("{\"event\":\"x\"}");

        var result = await _controller.Callback();

        result.Should().BeOfType<OkResult>();
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

    private void SetupRequestBody(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        _controller.ControllerContext.HttpContext.Request.Body = new MemoryStream(bytes);
    }
}
