using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Modules.SavedSearches.Controllers;
using Lander.src.Modules.SavedSearches.Dtos.Dto;
using Lander.src.Modules.SavedSearches.Dtos.InputDto;
using Lander.src.Modules.SavedSearches.Interfaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace LandlordApp.Tests.Controllers;

public class SavedSearchesControllerTests
{
    private readonly Mock<ISavedSearchService> _mockService;
    private readonly Mock<IUserInterface> _mockUser;
    private readonly SavedSearchesController _controller;

    private static readonly SavedSearchDto SampleDto = new()
    {
        SavedSearchId = 1,
        UserId = 1,
        Name = "Test Search",
        SearchType = "Apartment",
        IsActive = true
    };

    private static readonly SavedSearchInputDto SampleInput = new()
    {
        Name = "Test Search",
        SearchType = "Apartment",
        EmailNotificationsEnabled = true
    };

    public SavedSearchesControllerTests()
    {
        _mockService = new Mock<ISavedSearchService>();
        _mockUser = new Mock<IUserInterface>();

        _mockUser.Setup(u => u.GetUserByGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User { UserId = 1 });

        _controller = new SavedSearchesController(_mockService.Object, _mockUser.Object);
        _controller.ControllerContext = MakeAuthContext();
    }

    // ─── GetSavedSearchesByUserId ─────────────────────────────────────────────

    [Fact]
    public async Task GetSavedSearchesByUserId_ReturnsOk()
    {
        var list = new List<SavedSearchDto> { SampleDto };
        _mockService.Setup(s => s.GetSavedSearchesByUserIdAsync(1)).ReturnsAsync(list);

        var result = await _controller.GetSavedSearchesByUserId(1);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(list);
    }

    [Fact]
    public async Task GetSavedSearchesByUserId_MissingSubClaim_ReturnsUnauthorized()
    {
        // Controller without "sub" claim → Unauthorized
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity()) // no claims
            }
        };

        var result = await _controller.GetSavedSearchesByUserId(1);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetSavedSearchesByUserId_DifferentUser_ReturnsForbid()
    {
        // GetUserByGuidAsync returns user with UserId=1, but request asks for userId=99 → Forbid
        var result = await _controller.GetSavedSearchesByUserId(99);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetSavedSearchesByUserId_ServiceThrows_Returns500()
    {
        _mockService.Setup(s => s.GetSavedSearchesByUserIdAsync(1))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _controller.GetSavedSearchesByUserId(1);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetSavedSearch ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetSavedSearch_Found_ReturnsOk()
    {
        _mockService.Setup(s => s.GetSavedSearchByIdAsync(1)).ReturnsAsync(SampleDto);

        var result = await _controller.GetSavedSearch(1);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(SampleDto);
    }

    [Fact]
    public async Task GetSavedSearch_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetSavedSearchByIdAsync(99)).ReturnsAsync((SavedSearchDto?)null);

        var result = await _controller.GetSavedSearch(99);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSavedSearch_ServiceThrows_Throws()
    {
        _mockService.Setup(s => s.GetSavedSearchByIdAsync(1))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _controller.GetSavedSearch(1);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── CreateSavedSearch ────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSavedSearch_ReturnsOk()
    {
        _mockService.Setup(s => s.CreateSavedSearchAsync(1, SampleInput)).ReturnsAsync(SampleDto);

        var result = await _controller.CreateSavedSearch(SampleInput);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(SampleDto);
    }

    [Fact]
    public async Task CreateSavedSearch_UserNotFound_ReturnsUnauthorized()
    {
        _mockUser.Setup(u => u.GetUserByGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.CreateSavedSearch(SampleInput);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreateSavedSearch_ServiceThrows_Throws()
    {
        _mockService.Setup(s => s.CreateSavedSearchAsync(1, SampleInput))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _controller.CreateSavedSearch(SampleInput);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── UpdateSavedSearch ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSavedSearch_ReturnsOk()
    {
        _mockService.Setup(s => s.UpdateSavedSearchAsync(1, 1, SampleInput)).ReturnsAsync(SampleDto);

        var result = await _controller.UpdateSavedSearch(1, SampleInput);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(SampleDto);
    }

    [Fact]
    public async Task UpdateSavedSearch_UserNotFound_ReturnsUnauthorized()
    {
        _mockUser.Setup(u => u.GetUserByGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.UpdateSavedSearch(1, SampleInput);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task UpdateSavedSearch_ServiceThrows_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdateSavedSearchAsync(1, 1, SampleInput))
            .ThrowsAsync(new Exception("not found"));

        var result = await _controller.UpdateSavedSearch(1, SampleInput);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── DeleteSavedSearch ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSavedSearch_ReturnsOk()
    {
        _mockService.Setup(s => s.DeleteSavedSearchAsync(1, 1)).ReturnsAsync(true);

        var result = await _controller.DeleteSavedSearch(1);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task DeleteSavedSearch_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteSavedSearchAsync(99, 1)).ReturnsAsync(false);

        var result = await _controller.DeleteSavedSearch(99);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteSavedSearch_UserNotFound_ReturnsUnauthorized()
    {
        _mockUser.Setup(u => u.GetUserByGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.DeleteSavedSearch(1);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task DeleteSavedSearch_ServiceThrows_Throws()
    {
        _mockService.Setup(s => s.DeleteSavedSearchAsync(1, 1))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _controller.DeleteSavedSearch(1);

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
