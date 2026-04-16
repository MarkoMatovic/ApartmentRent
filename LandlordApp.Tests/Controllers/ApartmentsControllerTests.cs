using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Security.Claims;
using Lander.src.Modules.Analytics.Interfaces;
using Lander.src.Modules.Listings.Controllers;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.MachineLearning.Services;
using Lander.src.Common;

namespace LandlordApp.Tests.Controllers;

public class ApartmentsControllerTests
{
    private readonly Mock<IApartmentService> _mockApartmentService;
    private readonly Mock<IAnalyticsService> _mockAnalytics;
    private readonly ApartmentsController _controller;

    private static readonly ApartmentDto SampleApartment = new()
    {
        Title = "Test Apt", Address = "Addr", City = "City"
    };

    public ApartmentsControllerTests()
    {
        _mockApartmentService = new Mock<IApartmentService>();
        _mockAnalytics = new Mock<IAnalyticsService>();

        _mockAnalytics.Setup(a => a.TrackEventAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _controller = new ApartmentsController(
            _mockApartmentService.Object,
            _mockAnalytics.Object,
            new SimpleEmbeddingService());

        _controller.ControllerContext = MakeAuthContext(1, Guid.NewGuid(), withOutputCache: true);
    }

    // ─── CreateApartment ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateApartment_ReturnsOk()
    {
        var input = new ApartmentInputDto { Title = "T", Address = "A", City = "C" };
        _mockApartmentService.Setup(s => s.CreateApartmentAsync(input)).ReturnsAsync(SampleApartment);

        var result = await _controller.CreateApartment(input);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(SampleApartment);
    }

    [Fact]
    public async Task CreateApartment_ServiceThrows_PropagatesException()
    {
        var input = new ApartmentInputDto { Title = "T", Address = "A", City = "C" };
        _mockApartmentService.Setup(s => s.CreateApartmentAsync(input))
            .ThrowsAsync(new Exception("DB error"));

        Func<Task> act = async () => await _controller.CreateApartment(input);

        await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
    }

    // ─── GetAllApartments ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllApartments_NoFilters_ReturnsOk()
    {
        var pagedResult = new PagedResult<ApartmentDto> { Items = new List<ApartmentDto> { SampleApartment }, TotalCount = 1 };
        _mockApartmentService.Setup(s => s.GetAllApartmentsAsync(It.IsAny<ApartmentFilterDto>()))
            .ReturnsAsync(pagedResult);

        var result = await _controller.GetAllApartments(null);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(pagedResult);
    }

    [Fact]
    public async Task GetAllApartments_WithCityFilter_TracksSearch()
    {
        var filter = new ApartmentFilterDto { City = "Sarajevo" };
        _mockApartmentService.Setup(s => s.GetAllApartmentsAsync(filter))
            .ReturnsAsync(new PagedResult<ApartmentDto> { Items = new List<ApartmentDto>(), TotalCount = 0 });

        await _controller.GetAllApartments(filter);

        _mockAnalytics.Verify(a => a.TrackEventAsync(
            "ApartmentSearch", It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task GetAllApartments_ServiceThrows_PropagatesException()
    {
        _mockApartmentService.Setup(s => s.GetAllApartmentsAsync(It.IsAny<ApartmentFilterDto>()))
            .ThrowsAsync(new Exception("fail"));

        Func<Task> act = async () => await _controller.GetAllApartments(null);

        await act.Should().ThrowAsync<Exception>().WithMessage("fail");
    }

    // ─── GetMyApartments ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyApartments_ReturnsOk()
    {
        var paged = new PagedResult<ApartmentDto> { Items = new List<ApartmentDto>(), TotalCount = 0 };
        _mockApartmentService.Setup(s => s.GetMyApartmentsAsync()).ReturnsAsync(paged);

        var result = await _controller.GetMyApartments();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMyApartments_ServiceThrows_PropagatesException()
    {
        _mockApartmentService.Setup(s => s.GetMyApartmentsAsync())
            .ThrowsAsync(new Exception("DB error"));

        Func<Task> act = async () => await _controller.GetMyApartments();

        await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
    }

    // ─── GetApartment ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetApartment_Found_ReturnsOk()
    {
        var detail = new GetApartmentDto();
        _mockApartmentService.Setup(s => s.GetApartmentByIdAsync(5)).ReturnsAsync(detail);

        var result = await _controller.GetApartment(5);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(detail);
    }

    [Fact]
    public async Task GetApartment_ServiceThrows_PropagatesException()
    {
        _mockApartmentService.Setup(s => s.GetApartmentByIdAsync(99))
            .ThrowsAsync(new Exception("Not found"));

        Func<Task> act = async () => await _controller.GetApartment(99);

        await act.Should().ThrowAsync<Exception>().WithMessage("Not found");
    }

    // ─── UpdateApartment ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateApartment_ReturnsOk()
    {
        var dto = new ApartmentUpdateInputDto();
        _mockApartmentService.Setup(s => s.UpdateApartmentAsync(3, dto)).ReturnsAsync(SampleApartment);

        var result = await _controller.UpdateApartment(3, dto);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(SampleApartment);
    }

    [Fact]
    public async Task UpdateApartment_ServiceThrows_PropagatesException()
    {
        var dto = new ApartmentUpdateInputDto();
        _mockApartmentService.Setup(s => s.UpdateApartmentAsync(3, dto))
            .ThrowsAsync(new Exception("Update failed"));

        Func<Task> act = async () => await _controller.UpdateApartment(3, dto);

        await act.Should().ThrowAsync<Exception>().WithMessage("Update failed");
    }

    // ─── DeleteApartment ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteApartment_ReturnsOk()
    {
        _mockApartmentService.Setup(s => s.DeleteApartmentAsync(7)).ReturnsAsync(true);

        var result = await _controller.DeleteApartment(7);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task DeleteApartment_ReturnsFalse_ReturnsOkFalse()
    {
        _mockApartmentService.Setup(s => s.DeleteApartmentAsync(7)).ReturnsAsync(false);

        var result = await _controller.DeleteApartment(7);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(false);
    }

    [Fact]
    public async Task DeleteApartment_ServiceThrows_PropagatesException()
    {
        _mockApartmentService.Setup(s => s.DeleteApartmentAsync(7))
            .ThrowsAsync(new Exception("Delete failed"));

        Func<Task> act = async () => await _controller.DeleteApartment(7);

        await act.Should().ThrowAsync<Exception>().WithMessage("Delete failed");
    }

    // ─── ActivateApartment ────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateApartment_ReturnsOk()
    {
        _mockApartmentService.Setup(s => s.ActivateApartmentAsync(8)).ReturnsAsync(true);

        var result = await _controller.ActivateApartment(8);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    [Fact]
    public async Task ActivateApartment_ReturnsFalse_ReturnsOkFalse()
    {
        _mockApartmentService.Setup(s => s.ActivateApartmentAsync(8)).ReturnsAsync(false);

        var result = await _controller.ActivateApartment(8);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(false);
    }

    [Fact]
    public async Task ActivateApartment_ServiceThrows_PropagatesException()
    {
        _mockApartmentService.Setup(s => s.ActivateApartmentAsync(8))
            .ThrowsAsync(new Exception("Activate failed"));

        Func<Task> act = async () => await _controller.ActivateApartment(8);

        await act.Should().ThrowAsync<Exception>().WithMessage("Activate failed");
    }

    // ─── GetAllApartmentsCursor ───────────────────────────────────────────────

    [Fact]
    public async Task GetAllApartmentsCursor_NoFilters_ReturnsOkWithFirstPage()
    {
        var cursorResult = new CursorPagedResult<ApartmentDto>
        {
            Items = new List<ApartmentDto> { SampleApartment },
            NextCursor = "1"
        };
        _mockApartmentService.Setup(s => s.GetAllApartmentsCursorAsync(
                It.IsAny<ApartmentFilterDto>(), null, 20))
            .ReturnsAsync(cursorResult);

        var result = await _controller.GetAllApartmentsCursor(null, null, 20);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(cursorResult);
    }

    [Fact]
    public async Task GetAllApartmentsCursor_WithAfterId_PassesCursorToService()
    {
        var cursorResult = new CursorPagedResult<ApartmentDto>
        {
            Items = new List<ApartmentDto>(),
            NextCursor = null
        };
        _mockApartmentService.Setup(s => s.GetAllApartmentsCursorAsync(
                It.IsAny<ApartmentFilterDto>(), 10, 20))
            .ReturnsAsync(cursorResult);

        var result = await _controller.GetAllApartmentsCursor(null, afterId: 10, pageSize: 20);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mockApartmentService.Verify(s => s.GetAllApartmentsCursorAsync(
            It.IsAny<ApartmentFilterDto>(), 10, 20), Times.Once);
    }

    [Fact]
    public async Task GetAllApartmentsCursor_LastPage_HasMoreIsFalse()
    {
        var cursorResult = new CursorPagedResult<ApartmentDto>
        {
            Items = new List<ApartmentDto> { SampleApartment },
            NextCursor = null   // no next page
        };
        _mockApartmentService.Setup(s => s.GetAllApartmentsCursorAsync(
                It.IsAny<ApartmentFilterDto>(), It.IsAny<int?>(), It.IsAny<int>()))
            .ReturnsAsync(cursorResult);

        var result = await _controller.GetAllApartmentsCursor(null, null, 20);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = ok.Value.Should().BeOfType<CursorPagedResult<ApartmentDto>>().Subject;
        returned.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllApartmentsCursor_ServiceThrows_PropagatesException()
    {
        _mockApartmentService.Setup(s => s.GetAllApartmentsCursorAsync(
                It.IsAny<ApartmentFilterDto>(), It.IsAny<int?>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("DB error"));

        Func<Task> act = async () => await _controller.GetAllApartmentsCursor(null, null, 20);

        await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(int userId, Guid userGuid, bool withOutputCache = false)
    {
        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new("sub", userGuid.ToString())
        };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };

        if (withOutputCache)
        {
            var mockCacheStore = new Mock<IOutputCacheStore>();
            mockCacheStore.Setup(s => s.EvictByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
            var mockServices = new Mock<IServiceProvider>();
            mockServices.Setup(s => s.GetService(typeof(IOutputCacheStore)))
                .Returns(mockCacheStore.Object);
            httpContext.RequestServices = mockServices.Object;
        }

        return new ControllerContext { HttpContext = httpContext };
    }
}
