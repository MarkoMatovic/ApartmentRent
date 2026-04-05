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
    public async Task CreateApartment_ServiceThrows_Returns500()
    {
        var input = new ApartmentInputDto { Title = "T", Address = "A", City = "C" };
        _mockApartmentService.Setup(s => s.CreateApartmentAsync(input))
            .ThrowsAsync(new Exception("DB error"));

        var result = await _controller.CreateApartment(input);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
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
    public async Task GetAllApartments_ServiceThrows_Returns500()
    {
        _mockApartmentService.Setup(s => s.GetAllApartmentsAsync(It.IsAny<ApartmentFilterDto>()))
            .ThrowsAsync(new Exception("fail"));

        var result = await _controller.GetAllApartments(null);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
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

    // ─── GetApartment ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetApartment_Found_ReturnsOk()
    {
        var detail = new GetApartmentDto();
        _mockApartmentService.Setup(s => s.GetApartmentByIdAsync(5)).ReturnsAsync(detail);

        var result = await _controller.GetApartment(5);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(detail);
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

    // ─── DeleteApartment ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteApartment_ReturnsOk()
    {
        _mockApartmentService.Setup(s => s.DeleteApartmentAsync(7)).ReturnsAsync(true);

        var result = await _controller.DeleteApartment(7);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
    }

    // ─── ActivateApartment ────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateApartment_ReturnsOk()
    {
        _mockApartmentService.Setup(s => s.ActivateApartmentAsync(8)).ReturnsAsync(true);

        var result = await _controller.ActivateApartment(8);

        result.Result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(true);
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
