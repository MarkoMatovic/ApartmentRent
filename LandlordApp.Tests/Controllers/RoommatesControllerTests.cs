using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Modules.Analytics.Interfaces;
using Lander.src.Modules.Roommates.Controllers;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Modules.Roommates.Dtos.InputDto;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace LandlordApp.Tests.Controllers;

public class RoommatesControllerTests
{
    private readonly Mock<IRoommateService> _mockRoommateService;
    private readonly Mock<IUserInterface> _mockUserService;
    private readonly Mock<IAnalyticsService> _mockAnalytics;
    private readonly RoommatesController _controller;

    private static readonly Guid TestGuid = Guid.NewGuid();
    private static readonly User TestUser = new()
    {
        UserId = 3, FirstName = "T", LastName = "U",
        Email = "t@u.com", Password = "h", UserGuid = TestGuid, IsActive = true
    };
    private static readonly RoommateDto SampleRoommate = new() { RoommateId = 1, UserId = 3 };

    public RoommatesControllerTests()
    {
        _mockRoommateService = new Mock<IRoommateService>();
        _mockUserService = new Mock<IUserInterface>();
        _mockAnalytics = new Mock<IAnalyticsService>();

        _mockAnalytics.Setup(a => a.TrackEventAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _controller = new RoommatesController(
            _mockRoommateService.Object, _mockAnalytics.Object, _mockUserService.Object);
        _controller.ControllerContext = MakeAuthContext(TestGuid);
    }

    // ─── GetAllRoommates ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllRoommates_NoParams_ReturnsOkList()
    {
        _mockRoommateService.Setup(s => s.GetAllRoommatesAsync(
            null, null, null, null, null, null, null, null, null, null))
            .ReturnsAsync(new List<RoommateDto> { SampleRoommate });

        var result = await _controller.GetAllRoommates();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllRoommates_WithFilters_TracksSearch()
    {
        _mockRoommateService.Setup(s => s.GetAllRoommatesAsync(
            It.IsAny<string?>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(),
            It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<DateOnly?>(), It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(new List<RoommateDto>());

        await _controller.GetAllRoommates(location: "Sarajevo", lifestyle: "active");

        _mockAnalytics.Verify(a => a.TrackEventAsync(
            "RoommateSearch", It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task GetAllRoommates_WithPageAndPageSize_ReturnsPaged()
    {
        var paged = new Lander.src.Common.PagedResult<RoommateDto>
        {
            Items = new List<RoommateDto>(), TotalCount = 0
        };
        _mockRoommateService.Setup(s => s.GetAllRoommatesAsync(
            null, null, null, null, null, null, null, null, null, null, 1, 10))
            .ReturnsAsync(paged);

        var result = await _controller.GetAllRoommates(page: 1, pageSize: 10);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(paged);
    }

    // ─── GetRoommate ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoommate_Found_ReturnsOk()
    {
        _mockRoommateService.Setup(s => s.GetRoommateByIdAsync(1)).ReturnsAsync(SampleRoommate);

        var result = await _controller.GetRoommate(1);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(SampleRoommate);
    }

    [Fact]
    public async Task GetRoommate_NotFound_ReturnsNotFound()
    {
        _mockRoommateService.Setup(s => s.GetRoommateByIdAsync(99)).ReturnsAsync((RoommateDto?)null);

        var result = await _controller.GetRoommate(99);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ─── GetRoommateByUserId ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRoommateByUserId_Found_ReturnsOk()
    {
        _mockRoommateService.Setup(s => s.GetRoommateByUserIdAsync(3)).ReturnsAsync(SampleRoommate);

        var result = await _controller.GetRoommateByUserId(3);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(SampleRoommate);
    }

    [Fact]
    public async Task GetRoommateByUserId_NotFound_ReturnsNotFound()
    {
        _mockRoommateService.Setup(s => s.GetRoommateByUserIdAsync(99)).ReturnsAsync((RoommateDto?)null);

        var result = await _controller.GetRoommateByUserId(99);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ─── CreateRoommate ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRoommate_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        var input = new RoommateInputDto();
        _mockRoommateService.Setup(s => s.CreateRoommateAsync(3, input)).ReturnsAsync(SampleRoommate);

        var result = await _controller.CreateRoommate(input);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(SampleRoommate);
    }

    [Fact]
    public async Task CreateRoommate_UserNotFound_ReturnsUnauthorized()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync((User?)null);

        var result = await _controller.CreateRoommate(new RoommateInputDto());

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreateRoommate_InactiveUser_ReturnsUnauthorized()
    {
        var inactiveUser = new User
        {
            UserId = 3, UserGuid = TestGuid, IsActive = false, FirstName = "T", LastName = "U",
            Email = "t@u.com", Password = "h"
        };
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(inactiveUser);

        var result = await _controller.CreateRoommate(new RoommateInputDto());

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ─── UpdateRoommate ───────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateRoommate_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        var input = new RoommateInputDto();
        _mockRoommateService.Setup(s => s.UpdateRoommateAsync(1, 3, input)).ReturnsAsync(SampleRoommate);

        var result = await _controller.UpdateRoommate(1, input);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(SampleRoommate);
    }

    [Fact]
    public async Task UpdateRoommate_ServiceThrows_ReturnsBadRequest()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockRoommateService.Setup(s => s.UpdateRoommateAsync(1, 3, It.IsAny<RoommateInputDto>()))
            .ThrowsAsync(new Exception("Not found"));

        var result = await _controller.UpdateRoommate(1, new RoommateInputDto());

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── DeleteRoommate ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRoommate_ReturnsOk()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockRoommateService.Setup(s => s.DeleteRoommateAsync(1, 3)).ReturnsAsync(true);

        var result = await _controller.DeleteRoommate(1);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task DeleteRoommate_NotFound_ReturnsNotFound()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync(TestUser);
        _mockRoommateService.Setup(s => s.DeleteRoommateAsync(99, 3)).ReturnsAsync(false);

        var result = await _controller.DeleteRoommate(99);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteRoommate_NoUser_ReturnsUnauthorized()
    {
        _mockUserService.Setup(s => s.GetUserByGuidAsync(TestGuid)).ReturnsAsync((User?)null);

        var result = await _controller.DeleteRoommate(1);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(Guid userGuid)
    {
        var claims = new List<Claim>
        {
            new("sub", userGuid.ToString()),
            new("userId", "3")
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        return new ControllerContext { HttpContext = httpContext };
    }
}
