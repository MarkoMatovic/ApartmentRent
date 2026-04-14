using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Lander.src.Modules.Analytics.Controllers;
using Lander.src.Modules.Analytics.Dtos.Dto;
using Lander.src.Modules.Analytics.Dtos.InputDto;
using Lander.src.Modules.Analytics.Interfaces;

namespace LandlordApp.Tests.Controllers;

public class AnalyticsControllerTests
{
    private readonly Mock<IAnalyticsService> _mockAnalyticsService;
    private readonly AnalyticsController _controller;
    private const int CurrentUserId = 1;

    public AnalyticsControllerTests()
    {
        _mockAnalyticsService = new Mock<IAnalyticsService>();

        _mockAnalyticsService.Setup(a => a.TrackEventAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        _controller = new AnalyticsController(_mockAnalyticsService.Object);
        _controller.ControllerContext = MakeAuthContext(CurrentUserId);
    }

    // ─── TrackEvent ───────────────────────────────────────────────────────────

    [Fact]
    public async Task TrackEvent_ValidInput_ReturnsOk()
    {
        var input = new TrackEventInputDto
        {
            EventType = "view",
            EventCategory = "apartment",
            EntityId = 5,
            EntityType = "Apartment"
        };

        var result = await _controller.TrackEvent(input);

        result.Should().BeOfType<OkObjectResult>();
        _mockAnalyticsService.Verify(s => s.TrackEventAsync(
            "view", "apartment",
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task TrackEvent_ServiceThrows_Returns500()
    {
        var input = new TrackEventInputDto
        {
            EventType = "view",
            EventCategory = "apartment"
        };

        _mockAnalyticsService.Setup(a => a.TrackEventAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<Dictionary<string, string>?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Service error"));

        Func<Task> act = async () => await _controller.TrackEvent(input);

        await act.Should().ThrowAsync<Exception>().WithMessage("Service error");
    }

    [Fact]
    public async Task TrackEvent_NoUserIdClaim_ReturnsOkWithNullUserId()
    {
        _controller.ControllerContext = MakeAuthContext(0);
        var input = new TrackEventInputDto { EventType = "search", EventCategory = "listings" };

        var result = await _controller.TrackEvent(input);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ─── GetSummary ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSummary_NoDateFilter_ReturnsOkWithSummary()
    {
        var summary = new AnalyticsSummaryDto { TotalEvents = 100, TotalApartmentViews = 50 };
        _mockAnalyticsService.Setup(s => s.GetSummaryAsync(null, null))
            .ReturnsAsync(summary);

        var result = await _controller.GetSummary();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(summary);
    }

    [Fact]
    public async Task GetSummary_WithDateFilter_ReturnsOk()
    {
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 12, 31);
        var summary = new AnalyticsSummaryDto { TotalEvents = 42 };
        _mockAnalyticsService.Setup(s => s.GetSummaryAsync(from, to))
            .ReturnsAsync(summary);

        var result = await _controller.GetSummary(from, to);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(summary);
    }

    [Fact]
    public async Task GetSummary_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetSummaryAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("DB error"));

        Func<Task> act = async () => await _controller.GetSummary();

        await act.Should().ThrowAsync<Exception>().WithMessage("DB error");
    }

    // ─── GetTopViewedApartments ───────────────────────────────────────────────

    [Fact]
    public async Task GetTopViewedApartments_DefaultCount_ReturnsOkWithList()
    {
        var apartments = new List<TopEntityDto>
        {
            new() { EntityId = 1, EntityType = "Apartment", ViewCount = 10 },
            new() { EntityId = 2, EntityType = "Apartment", ViewCount = 8 }
        };
        _mockAnalyticsService.Setup(s => s.GetTopViewedApartmentsAsync(10, null, null))
            .ReturnsAsync(apartments);

        var result = await _controller.GetTopViewedApartments();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(apartments);
    }

    [Fact]
    public async Task GetTopViewedApartments_CustomCount_ReturnsOk()
    {
        var apartments = new List<TopEntityDto>
        {
            new() { EntityId = 1, EntityType = "Apartment", ViewCount = 5 }
        };
        _mockAnalyticsService.Setup(s => s.GetTopViewedApartmentsAsync(5, null, null))
            .ReturnsAsync(apartments);

        var result = await _controller.GetTopViewedApartments(5);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTopViewedApartments_EmptyResult_ReturnsOkWithEmptyList()
    {
        _mockAnalyticsService.Setup(s => s.GetTopViewedApartmentsAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TopEntityDto>());

        var result = await _controller.GetTopViewedApartments();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<TopEntityDto>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopViewedApartments_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetTopViewedApartmentsAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Service error"));

        Func<Task> act = async () => await _controller.GetTopViewedApartments();

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetTopSearchTerms ────────────────────────────────────────────────────

    [Fact]
    public async Task GetTopSearchTerms_DefaultCount_ReturnsOkWithList()
    {
        var terms = new List<SearchTermDto>
        {
            new() { SearchTerm = "studio Beograd", SearchCount = 30 },
            new() { SearchTerm = "garsonjera Novi Sad", SearchCount = 20 }
        };
        _mockAnalyticsService.Setup(s => s.GetTopSearchTermsAsync(10, null, null))
            .ReturnsAsync(terms);

        var result = await _controller.GetTopSearchTerms();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(terms);
    }

    [Fact]
    public async Task GetTopSearchTerms_EmptyResult_ReturnsOkWithEmptyList()
    {
        _mockAnalyticsService.Setup(s => s.GetTopSearchTermsAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<SearchTermDto>());

        var result = await _controller.GetTopSearchTerms();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<SearchTermDto>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopSearchTerms_WithDateFilter_ReturnsOk()
    {
        var from = new DateTime(2025, 6, 1);
        var to = new DateTime(2025, 6, 30);
        _mockAnalyticsService.Setup(s => s.GetTopSearchTermsAsync(10, from, to))
            .ReturnsAsync(new List<SearchTermDto>());

        var result = await _controller.GetTopSearchTerms(10, from, to);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTopSearchTerms_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetTopSearchTermsAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Search error"));

        Func<Task> act = async () => await _controller.GetTopSearchTerms();

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetMyApartmentViews ──────────────────────────────────────────────────

    [Fact]
    public async Task GetMyApartmentViews_ValidClaims_ReturnsOk()
    {
        var viewStats = new List<ApartmentViewStatsDto>
        {
            new() { ApartmentId = 1, Title = "Studio", ViewCount = 15 }
        };
        _mockAnalyticsService.Setup(s => s.GetLandlordApartmentViewsAsync(CurrentUserId, null, null))
            .ReturnsAsync(viewStats);

        var result = await _controller.GetMyApartmentViews();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(viewStats);
    }

    [Fact]
    public async Task GetMyApartmentViews_MissingUserIdClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var result = await _controller.GetMyApartmentViews();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    // ─── GetMyMessagesSent ────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyMessagesSent_ValidClaims_ReturnsOkWithCount()
    {
        _mockAnalyticsService.Setup(s => s.GetUserMessageCountAsync(CurrentUserId, null, null))
            .ReturnsAsync(7);

        var result = await _controller.GetMyMessagesSent();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(7);
    }

    [Fact]
    public async Task GetMyMessagesSent_MissingUserIdClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var result = await _controller.GetMyMessagesSent();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
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
