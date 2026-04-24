using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Modules.ApartmentApplications.Controllers;
using Lander.src.Modules.ApartmentApplications.Dtos.Dto;
using Lander.src.Modules.ApartmentApplications.Dtos.InputDto;
using Lander.src.Modules.ApartmentApplications.Interfaces;
using Lander.src.Modules.ApartmentApplications.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace LandlordApp.Tests.Controllers;

public class ApartmentApplicationsControllerTests
{
    private readonly Mock<IApartmentApplicationService> _mockAppService;
    private readonly Mock<IUserInterface> _mockUserService;
    private readonly Mock<IApplicationApprovalService> _mockApprovalService;
    private readonly ApartmentApplicationsController _controller;

    private static readonly Guid TestGuid = Guid.NewGuid();
    private static readonly User TestUser = new()
    {
        UserId = 5, FirstName = "T", LastName = "U",
        Email = "t@u.com", Password = "h", UserGuid = TestGuid, IsActive = true
    };

    public ApartmentApplicationsControllerTests()
    {
        _mockAppService = new Mock<IApartmentApplicationService>();
        _mockUserService = new Mock<IUserInterface>();
        _mockApprovalService = new Mock<IApplicationApprovalService>();

        _controller = new ApartmentApplicationsController(
            _mockAppService.Object, _mockApprovalService.Object, _mockUserService.Object);
        _controller.ControllerContext = MakeAuthContext(TestGuid);
    }

    // ─── ApplyForApartment ────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyForApartment_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        var app = new ApartmentApplication { ApplicationId = 1, UserId = 5, ApartmentId = 10 };
        _mockAppService.Setup(s => s.ApplyForApartmentAsync(5, 10, false)).ReturnsAsync(app);

        var result = await _controller.ApplyForApartment(new CreateApplicationInputDto { ApartmentId = 10 });

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(app);
    }

    [Fact]
    public async Task ApplyForApartment_NullResult_ReturnsBadRequest()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockAppService.Setup(s => s.ApplyForApartmentAsync(5, 10, false))
            .ReturnsAsync((ApartmentApplication?)null);

        var result = await _controller.ApplyForApartment(new CreateApplicationInputDto { ApartmentId = 10 });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ApplyForApartment_NoUser_ReturnsUnauthorized()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync((User?)null);

        var result = await _controller.ApplyForApartment(new CreateApplicationInputDto { ApartmentId = 10 });

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ─── GetLandlordApplications ──────────────────────────────────────────────

    [Fact]
    public async Task GetLandlordApplications_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockAppService.Setup(s => s.GetLandlordApplicationsAsync(5))
            .ReturnsAsync(new List<ApartmentApplicationDto>());

        var result = await _controller.GetLandlordApplications();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetLandlordApplications_NoUser_ReturnsUnauthorized()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync((User?)null);

        var result = await _controller.GetLandlordApplications();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ─── GetTenantApplications ────────────────────────────────────────────────

    [Fact]
    public async Task GetTenantApplications_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockAppService.Setup(s => s.GetTenantApplicationsAsync(5))
            .ReturnsAsync(new List<ApartmentApplicationDto>());

        var result = await _controller.GetTenantApplications();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTenantApplications_NoUser_ReturnsUnauthorized()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync((User?)null);

        var result = await _controller.GetTenantApplications();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ─── UpdateStatus ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_Found_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        var app = new ApartmentApplication { ApplicationId = 1 };
        _mockAppService.Setup(s => s.UpdateApplicationStatusAsync(1, "Approved", 5)).ReturnsAsync(app);

        var result = await _controller.UpdateStatus(1, new UpdateApplicationStatusInputDto { Status = "Approved" });

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(app);
    }

    [Fact]
    public async Task UpdateStatus_NotFound_ReturnsNotFound()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockAppService.Setup(s => s.UpdateApplicationStatusAsync(99, "Approved", 5))
            .ReturnsAsync((ApartmentApplication?)null);

        var result = await _controller.UpdateStatus(99, new UpdateApplicationStatusInputDto { Status = "Approved" });

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateStatus_UnauthorizedAccess_ReturnsForbid()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockAppService.Setup(s => s.UpdateApplicationStatusAsync(1, "Approved", 5))
            .ThrowsAsync(new UnauthorizedAccessException("not your application"));

        var result = await _controller.UpdateStatus(1, new UpdateApplicationStatusInputDto { Status = "Approved" });

        result.Should().BeOfType<ForbidResult>();
    }

    // ─── CheckApprovalStatus ─────────────────────────────────────────────────

    [Fact]
    public async Task CheckApprovalStatus_ReturnsOkWithStatus()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockApprovalService.Setup(s => s.GetApprovalStatusAsync(5, 10))
            .ReturnsAsync(new Lander.src.Modules.ApartmentApplications.Interfaces.ApprovalStatusResult(true, "Approved", 1));

        var result = await _controller.CheckApprovalStatus(10);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CheckApprovalStatus_NoApplicationFound_ReturnsOkWithFalse()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockApprovalService.Setup(s => s.GetApprovalStatusAsync(5, 10))
            .ReturnsAsync(new Lander.src.Modules.ApartmentApplications.Interfaces.ApprovalStatusResult(false, null, null));

        var result = await _controller.CheckApprovalStatus(10);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(Guid userGuid)
    {
        var claims = new List<Claim>
        {
            new("sub", userGuid.ToString()),
            new("userId", "5")
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        return new ControllerContext { HttpContext = httpContext };
    }
}
