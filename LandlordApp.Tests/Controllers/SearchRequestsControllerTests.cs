using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Common;
using Lander.src.Modules.SearchRequests.Controllers;
using Lander.src.Modules.SearchRequests.Dtos.Dto;
using Lander.src.Modules.SearchRequests.Dtos.InputDto;
using Lander.src.Modules.SearchRequests.Interfaces;
using Lander.src.Modules.SearchRequests.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Interfaces.UserInterface;

namespace LandlordApp.Tests.Controllers;

public class SearchRequestsControllerTests
{
    private readonly Mock<ISearchRequestService> _mockService;
    private readonly Mock<IUserInterface> _mockUser;
    private readonly SearchRequestsController _controller;

    private static readonly SearchRequestDto SampleDto = new()
    {
        SearchRequestId = 1,
        UserId = 1,
        Title = "Looking for apartment",
        RequestType = SearchRequestType.LookingForApartment,
        FirstName = "John",
        LastName = "Doe",
        IsActive = true
    };

    private static readonly SearchRequestInputDto SampleInput = new()
    {
        RequestType = SearchRequestType.LookingForApartment,
        Title = "Looking for apartment",
        City = "Sarajevo"
    };

    public SearchRequestsControllerTests()
    {
        _mockService = new Mock<ISearchRequestService>();
        _mockUser = new Mock<IUserInterface>();

        _mockUser.Setup(u => u.GetUserByGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User { UserId = 1 });

        _controller = new SearchRequestsController(_mockService.Object, _mockUser.Object);
        _controller.ControllerContext = MakeAuthContext();
    }

    // ─── GetAllSearchRequests ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllSearchRequests_NoPagination_ReturnsOk()
    {
        var paged = PagedResult<SearchRequestDto>.Create(
            new List<SearchRequestDto> { SampleDto }, 1, 1, 20);
        _mockService.Setup(s => s.GetAllSearchRequestsAsync(null, null, null, null, 1, 20))
            .ReturnsAsync(paged);

        var result = await _controller.GetAllSearchRequests();

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(paged);
    }

    [Fact]
    public async Task GetAllSearchRequests_WithPagination_ReturnsPagedResult()
    {
        var paged = PagedResult<SearchRequestDto>.Create(
            new List<SearchRequestDto> { SampleDto }, 1, 1, 10);
        _mockService.Setup(s => s.GetAllSearchRequestsAsync(null, null, null, null, 1, 10))
            .ReturnsAsync(paged);

        var result = await _controller.GetAllSearchRequests(page: 1, pageSize: 10);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(paged);
    }

    [Fact]
    public async Task GetAllSearchRequests_WithFilters_ReturnsOk()
    {
        var list = new List<SearchRequestDto> { SampleDto };
        _mockService.Setup(s => s.GetAllSearchRequestsAsync(
                SearchRequestType.LookingForApartment, "Sarajevo", 500m, 1500m))
            .ReturnsAsync(list);

        var result = await _controller.GetAllSearchRequests(
            requestType: SearchRequestType.LookingForApartment,
            city: "Sarajevo",
            minBudget: 500m,
            maxBudget: 1500m);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllSearchRequests_ServiceThrows_Throws()
    {
        _mockService.Setup(s => s.GetAllSearchRequestsAsync(null, null, null, null, 1, 20))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _controller.GetAllSearchRequests();

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetSearchRequest ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSearchRequest_Found_ReturnsOk()
    {
        _mockService.Setup(s => s.GetSearchRequestByIdAsync(1)).ReturnsAsync(SampleDto);

        var result = await _controller.GetSearchRequest(1);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(SampleDto);
    }

    [Fact]
    public async Task GetSearchRequest_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetSearchRequestByIdAsync(99)).ReturnsAsync((SearchRequestDto?)null);

        var result = await _controller.GetSearchRequest(99);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSearchRequest_ServiceThrows_Throws()
    {
        _mockService.Setup(s => s.GetSearchRequestByIdAsync(1))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _controller.GetSearchRequest(1);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetSearchRequestsByUserId ────────────────────────────────────────────

    [Fact]
    public async Task GetSearchRequestsByUserId_ReturnsOk()
    {
        var list = new List<SearchRequestDto> { SampleDto };
        _mockService.Setup(s => s.GetSearchRequestsByUserIdAsync(1)).ReturnsAsync(list);

        var result = await _controller.GetSearchRequestsByUserId(1);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(list);
    }

    [Fact]
    public async Task GetSearchRequestsByUserId_ServiceThrows_Throws()
    {
        _mockService.Setup(s => s.GetSearchRequestsByUserIdAsync(1))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _controller.GetSearchRequestsByUserId(1);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── CreateSearchRequest ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateSearchRequest_ReturnsOk()
    {
        _mockService.Setup(s => s.CreateSearchRequestAsync(1, SampleInput)).ReturnsAsync(SampleDto);

        var result = await _controller.CreateSearchRequest(SampleInput);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(SampleDto);
    }

    [Fact]
    public async Task CreateSearchRequest_UserNotFound_ReturnsUnauthorized()
    {
        _mockUser.Setup(u => u.GetUserByGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.CreateSearchRequest(SampleInput);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreateSearchRequest_ServiceThrows_Throws()
    {
        _mockService.Setup(s => s.CreateSearchRequestAsync(1, SampleInput))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _controller.CreateSearchRequest(SampleInput);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── UpdateSearchRequest ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSearchRequest_ReturnsOk()
    {
        _mockService.Setup(s => s.UpdateSearchRequestAsync(1, 1, SampleInput)).ReturnsAsync(SampleDto);

        var result = await _controller.UpdateSearchRequest(1, SampleInput);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(SampleDto);
    }

    [Fact]
    public async Task UpdateSearchRequest_UserNotFound_ReturnsUnauthorized()
    {
        _mockUser.Setup(u => u.GetUserByGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.UpdateSearchRequest(1, SampleInput);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task UpdateSearchRequest_ServiceThrows_ReturnsBadRequest()
    {
        _mockService.Setup(s => s.UpdateSearchRequestAsync(1, 1, SampleInput))
            .ThrowsAsync(new Exception("not found"));

        var result = await _controller.UpdateSearchRequest(1, SampleInput);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── DeleteSearchRequest ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteSearchRequest_ReturnsOk()
    {
        _mockService.Setup(s => s.DeleteSearchRequestAsync(1, 1)).ReturnsAsync(true);

        var result = await _controller.DeleteSearchRequest(1);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task DeleteSearchRequest_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DeleteSearchRequestAsync(99, 1)).ReturnsAsync(false);

        var result = await _controller.DeleteSearchRequest(99);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteSearchRequest_UserNotFound_ReturnsUnauthorized()
    {
        _mockUser.Setup(u => u.GetUserByGuidAsync(It.IsAny<Guid>()))
            .ReturnsAsync((User?)null);

        var result = await _controller.DeleteSearchRequest(1);

        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task DeleteSearchRequest_ServiceThrows_Throws()
    {
        _mockService.Setup(s => s.DeleteSearchRequestAsync(1, 1))
            .ThrowsAsync(new Exception("DB error"));

        var act = async () => await _controller.DeleteSearchRequest(1);

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
