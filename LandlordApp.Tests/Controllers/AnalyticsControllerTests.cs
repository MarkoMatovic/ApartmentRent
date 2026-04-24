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
using Lander.src.Modules.Users.Interfaces.UserInterface;

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

        _controller = new AnalyticsController(_mockAnalyticsService.Object, new Mock<IUserInterface>().Object);
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

    // ─── GetTopViewedRoommates ────────────────────────────────────────────────

    [Fact]
    public async Task GetTopViewedRoommates_DefaultCount_ReturnsOkWithList()
    {
        var roommates = new List<TopEntityDto>
        {
            new() { EntityId = 1, EntityType = "Roommate", ViewCount = 7 },
            new() { EntityId = 2, EntityType = "Roommate", ViewCount = 4 }
        };
        _mockAnalyticsService.Setup(s => s.GetTopViewedRoommatesAsync(10, null, null))
            .ReturnsAsync(roommates);

        var result = await _controller.GetTopViewedRoommates();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(roommates);
    }

    [Fact]
    public async Task GetTopViewedRoommates_CustomCount_ReturnsOk()
    {
        _mockAnalyticsService.Setup(s => s.GetTopViewedRoommatesAsync(5, null, null))
            .ReturnsAsync(new List<TopEntityDto> { new() { EntityId = 3, EntityType = "Roommate", ViewCount = 2 } });

        var result = await _controller.GetTopViewedRoommates(5);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTopViewedRoommates_EmptyResult_ReturnsOkWithEmptyList()
    {
        _mockAnalyticsService.Setup(s => s.GetTopViewedRoommatesAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TopEntityDto>());

        var result = await _controller.GetTopViewedRoommates();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<TopEntityDto>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTopViewedRoommates_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetTopViewedRoommatesAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Service error"));

        Func<Task> act = async () => await _controller.GetTopViewedRoommates();

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetEventTrends ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetEventTrends_WithRequiredDates_ReturnsOkWithTrends()
    {
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 1, 31);
        var trends = new List<EventTrendDto>
        {
            new() { Date = new DateTime(2025, 1, 10), EventType = "view", Count = 15 }
        };
        _mockAnalyticsService.Setup(s => s.GetEventTrendsAsync(from, to, null))
            .ReturnsAsync(trends);

        var result = await _controller.GetEventTrends(from, to);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(trends);
    }

    [Fact]
    public async Task GetEventTrends_WithEventTypeFilter_PassesFilterToService()
    {
        var from = new DateTime(2025, 3, 1);
        var to = new DateTime(2025, 3, 31);
        _mockAnalyticsService.Setup(s => s.GetEventTrendsAsync(from, to, "search"))
            .ReturnsAsync(new List<EventTrendDto>());

        var result = await _controller.GetEventTrends(from, to, "search");

        result.Result.Should().BeOfType<OkObjectResult>();
        _mockAnalyticsService.Verify(s => s.GetEventTrendsAsync(from, to, "search"), Times.Once);
    }

    [Fact]
    public async Task GetEventTrends_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetEventTrendsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string?>()))
            .ThrowsAsync(new Exception("Trend error"));

        Func<Task> act = async () => await _controller.GetEventTrends(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        await act.Should().ThrowAsync<Exception>().WithMessage("Trend error");
    }

    // ─── GetUserRoommateSummary ───────────────────────────────────────────────

    [Fact]
    public async Task GetUserRoommateSummary_ValidUserId_ReturnsOk()
    {
        var summary = new UserRoommateAnalyticsSummaryDto
        {
            RoommateViews = 5, MessagesSent = 3, ApplicationsSent = 1, Searches = 10
        };
        _mockAnalyticsService.Setup(s => s.GetUserRoommateSummaryAsync(CurrentUserId, null, null))
            .ReturnsAsync(summary);

        var result = await _controller.GetUserRoommateSummary(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(summary);
    }

    [Fact]
    public async Task GetUserRoommateSummary_WithDateFilter_ReturnsOk()
    {
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 6, 30);
        var summary = new UserRoommateAnalyticsSummaryDto { Searches = 8 };
        _mockAnalyticsService.Setup(s => s.GetUserRoommateSummaryAsync(CurrentUserId, from, to))
            .ReturnsAsync(summary);

        var result = await _controller.GetUserRoommateSummary(CurrentUserId, from, to);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(summary);
    }

    [Fact]
    public async Task GetUserRoommateSummary_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetUserRoommateSummaryAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Summary error"));

        Func<Task> act = async () => await _controller.GetUserRoommateSummary(CurrentUserId);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetUserTopRoommates ──────────────────────────────────────────────────

    [Fact]
    public async Task GetUserTopRoommates_ValidUserId_ReturnsOkWithList()
    {
        var roommates = new List<TopEntityDto>
        {
            new() { EntityId = 10, EntityType = "Roommate", ViewCount = 6 }
        };
        _mockAnalyticsService.Setup(s => s.GetUserTopRoommatesAsync(CurrentUserId, 10, null, null))
            .ReturnsAsync(roommates);

        var result = await _controller.GetUserTopRoommates(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(roommates);
    }

    [Fact]
    public async Task GetUserTopRoommates_EmptyResult_ReturnsOkWithEmptyList()
    {
        _mockAnalyticsService.Setup(s => s.GetUserTopRoommatesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TopEntityDto>());

        var result = await _controller.GetUserTopRoommates(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<TopEntityDto>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserTopRoommates_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetUserTopRoommatesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Roommate error"));

        Func<Task> act = async () => await _controller.GetUserTopRoommates(CurrentUserId);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetUserSearches ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserSearches_ValidUserId_ReturnsOkWithList()
    {
        var searches = new List<SearchTermDto>
        {
            new() { SearchTerm = "garsonjera Sarajevo", SearchCount = 4 }
        };
        _mockAnalyticsService.Setup(s => s.GetUserSearchesAsync(CurrentUserId, 10, null, null))
            .ReturnsAsync(searches);

        var result = await _controller.GetUserSearches(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(searches);
    }

    [Fact]
    public async Task GetUserSearches_EmptyResult_ReturnsOkWithEmptyList()
    {
        _mockAnalyticsService.Setup(s => s.GetUserSearchesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<SearchTermDto>());

        var result = await _controller.GetUserSearches(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<SearchTermDto>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserSearches_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetUserSearchesAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Search error"));

        Func<Task> act = async () => await _controller.GetUserSearches(CurrentUserId);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetUserRoommateTrends ────────────────────────────────────────────────

    [Fact]
    public async Task GetUserRoommateTrends_ValidUserId_ReturnsOk()
    {
        var trends = new UserRoommateTrendsDto
        {
            PopularCities = new List<PopularCityDto> { new() { City = "Sarajevo", ViewCount = 12 } },
            AveragePrices = new List<AveragePriceDto> { new() { City = "Sarajevo", AveragePrice = 450m } }
        };
        _mockAnalyticsService.Setup(s => s.GetUserRoommateTrendsAsync(CurrentUserId, null, null))
            .ReturnsAsync(trends);

        var result = await _controller.GetUserRoommateTrends(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(trends);
    }

    [Fact]
    public async Task GetUserRoommateTrends_WithDateFilter_ReturnsOk()
    {
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 12, 31);
        _mockAnalyticsService.Setup(s => s.GetUserRoommateTrendsAsync(CurrentUserId, from, to))
            .ReturnsAsync(new UserRoommateTrendsDto());

        var result = await _controller.GetUserRoommateTrends(CurrentUserId, from, to);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserRoommateTrends_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetUserRoommateTrendsAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Trends error"));

        Func<Task> act = async () => await _controller.GetUserRoommateTrends(CurrentUserId);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetUserTopApartments ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUserTopApartments_ValidUserId_ReturnsOkWithList()
    {
        var apartments = new List<TopEntityDto>
        {
            new() { EntityId = 5, EntityType = "Apartment", ViewCount = 9 }
        };
        _mockAnalyticsService.Setup(s => s.GetUserTopApartmentsAsync(CurrentUserId, 10, null, null))
            .ReturnsAsync(apartments);

        var result = await _controller.GetUserTopApartments(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(apartments);
    }

    [Fact]
    public async Task GetUserTopApartments_EmptyResult_ReturnsOkWithEmptyList()
    {
        _mockAnalyticsService.Setup(s => s.GetUserTopApartmentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<TopEntityDto>());

        var result = await _controller.GetUserTopApartments(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<TopEntityDto>>()
            .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserTopApartments_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetUserTopApartmentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Apartment error"));

        Func<Task> act = async () => await _controller.GetUserTopApartments(CurrentUserId);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetUserCompleteAnalytics ─────────────────────────────────────────────

    [Fact]
    public async Task GetUserCompleteAnalytics_ValidUserId_ReturnsOk()
    {
        var summary = new AnalyticsSummaryDto { TotalEvents = 42, TotalApartmentViews = 18 };
        _mockAnalyticsService.Setup(s => s.GetUserCompleteAnalyticsAsync(CurrentUserId, null, null))
            .ReturnsAsync(summary);

        var result = await _controller.GetUserCompleteAnalytics(CurrentUserId);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(summary);
    }

    [Fact]
    public async Task GetUserCompleteAnalytics_WithDateFilter_ReturnsOk()
    {
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 6, 30);
        _mockAnalyticsService.Setup(s => s.GetUserCompleteAnalyticsAsync(CurrentUserId, from, to))
            .ReturnsAsync(new AnalyticsSummaryDto { TotalEvents = 15 });

        var result = await _controller.GetUserCompleteAnalytics(CurrentUserId, from, to);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserCompleteAnalytics_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetUserCompleteAnalyticsAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Analytics error"));

        Func<Task> act = async () => await _controller.GetUserCompleteAnalytics(CurrentUserId);

        await act.Should().ThrowAsync<Exception>();
    }

    // ─── GetMyViewedApartments ────────────────────────────────────────────────

    [Fact]
    public async Task GetMyViewedApartments_ValidClaims_ReturnsOk()
    {
        var apartments = new List<TopEntityDto>
        {
            new() { EntityId = 3, EntityType = "Apartment", ViewCount = 2 }
        };
        _mockAnalyticsService.Setup(s => s.GetUserTopApartmentsAsync(CurrentUserId, 10, null, null))
            .ReturnsAsync(apartments);

        var result = await _controller.GetMyViewedApartments();

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(apartments);
    }

    [Fact]
    public async Task GetMyViewedApartments_MissingUserIdClaim_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var result = await _controller.GetMyViewedApartments();

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetMyViewedApartments_WithCustomCount_PassesCountToService()
    {
        _mockAnalyticsService.Setup(s => s.GetUserTopApartmentsAsync(CurrentUserId, 5, null, null))
            .ReturnsAsync(new List<TopEntityDto>());

        var result = await _controller.GetMyViewedApartments(5);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mockAnalyticsService.Verify(s => s.GetUserTopApartmentsAsync(CurrentUserId, 5, null, null), Times.Once);
    }

    // ─── TrackEvent — response body & metadata ────────────────────────────────

    [Fact]
    public async Task TrackEvent_ResponseBodyContainsSuccessTrue()
    {
        var input = new TrackEventInputDto { EventType = "view", EventCategory = "apartment" };

        var result = await _controller.TrackEvent(input);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { success = true });
    }

    [Fact]
    public async Task TrackEvent_WithMetadataAndSearchQuery_PassesAllFieldsToService()
    {
        var metadata = new Dictionary<string, string> { { "source", "homepage" } };
        var input = new TrackEventInputDto
        {
            EventType = "search",
            EventCategory = "listings",
            SearchQuery = "garsonjera Beograd",
            EntityId = 42,
            EntityType = "Apartment",
            Metadata = metadata
        };

        var result = await _controller.TrackEvent(input);

        result.Should().BeOfType<OkObjectResult>();
        _mockAnalyticsService.Verify(s => s.TrackEventAsync(
            "search", "listings",
            42, "Apartment", "garsonjera Beograd",
            metadata,
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
    }

    // ─── GetMyApartmentViews — date filter & error ────────────────────────────

    [Fact]
    public async Task GetMyApartmentViews_WithDateFilter_ReturnsOk()
    {
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 6, 30);
        var stats = new List<ApartmentViewStatsDto>
        {
            new() { ApartmentId = 2, Title = "Dvosoban stan", ViewCount = 30 }
        };
        _mockAnalyticsService.Setup(s => s.GetLandlordApartmentViewsAsync(CurrentUserId, from, to))
            .ReturnsAsync(stats);

        var result = await _controller.GetMyApartmentViews(from, to);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(stats);
    }

    [Fact]
    public async Task GetMyApartmentViews_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetLandlordApartmentViewsAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("DB unavailable"));

        Func<Task> act = async () => await _controller.GetMyApartmentViews();

        await act.Should().ThrowAsync<Exception>().WithMessage("DB unavailable");
    }

    // ─── GetMyMessagesSent — date filter & error ──────────────────────────────

    [Fact]
    public async Task GetMyMessagesSent_WithDateFilter_ReturnsOk()
    {
        var from = new DateTime(2025, 3, 1);
        var to = new DateTime(2025, 3, 31);
        _mockAnalyticsService.Setup(s => s.GetUserMessageCountAsync(CurrentUserId, from, to))
            .ReturnsAsync(12);

        var result = await _controller.GetMyMessagesSent(from, to);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(12);
    }

    [Fact]
    public async Task GetMyMessagesSent_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetUserMessageCountAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("Count failed"));

        Func<Task> act = async () => await _controller.GetMyMessagesSent();

        await act.Should().ThrowAsync<Exception>().WithMessage("Count failed");
    }

    // ─── GetMyViewedApartments — error propagation ────────────────────────────

    [Fact]
    public async Task GetMyViewedApartments_ServiceThrows_PropagatesException()
    {
        _mockAnalyticsService.Setup(s => s.GetUserTopApartmentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ThrowsAsync(new Exception("View fetch failed"));

        Func<Task> act = async () => await _controller.GetMyViewedApartments();

        await act.Should().ThrowAsync<Exception>().WithMessage("View fetch failed");
    }

    // ─── GetTopViewedApartments — date filter ─────────────────────────────────

    [Fact]
    public async Task GetTopViewedApartments_WithDateFilter_ReturnsOk()
    {
        var from = new DateTime(2025, 1, 1);
        var to = new DateTime(2025, 1, 31);
        var apartments = new List<TopEntityDto>
        {
            new() { EntityId = 7, EntityType = "Apartment", ViewCount = 22 }
        };
        _mockAnalyticsService.Setup(s => s.GetTopViewedApartmentsAsync(10, from, to))
            .ReturnsAsync(apartments);

        var result = await _controller.GetTopViewedApartments(10, from, to);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(apartments);
    }

    // ─── GetTopViewedRoommates — date filter ──────────────────────────────────

    [Fact]
    public async Task GetTopViewedRoommates_WithDateFilter_ReturnsOk()
    {
        var from = new DateTime(2025, 4, 1);
        var to = new DateTime(2025, 4, 30);
        _mockAnalyticsService.Setup(s => s.GetTopViewedRoommatesAsync(10, from, to))
            .ReturnsAsync(new List<TopEntityDto>());

        var result = await _controller.GetTopViewedRoommates(10, from, to);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ─── GetUserTopApartments — custom count ──────────────────────────────────

    [Fact]
    public async Task GetUserTopApartments_CustomCount_PassesCountToService()
    {
        _mockAnalyticsService.Setup(s => s.GetUserTopApartmentsAsync(CurrentUserId, 5, null, null))
            .ReturnsAsync(new List<TopEntityDto>());

        var result = await _controller.GetUserTopApartments(CurrentUserId, 5);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mockAnalyticsService.Verify(s => s.GetUserTopApartmentsAsync(CurrentUserId, 5, null, null), Times.Once);
    }

    // ─── GetUserSearches — custom count ───────────────────────────────────────

    [Fact]
    public async Task GetUserSearches_CustomCount_PassesCountToService()
    {
        _mockAnalyticsService.Setup(s => s.GetUserSearchesAsync(CurrentUserId, 3, null, null))
            .ReturnsAsync(new List<SearchTermDto>());

        var result = await _controller.GetUserSearches(CurrentUserId, 3);

        result.Result.Should().BeOfType<OkObjectResult>();
        _mockAnalyticsService.Verify(s => s.GetUserSearchesAsync(CurrentUserId, 3, null, null), Times.Once);
    }

    // ─── GetUserTopRoommates — custom count & date filter ────────────────────

    [Fact]
    public async Task GetUserTopRoommates_WithDateFilter_ReturnsOk()
    {
        var from = new DateTime(2025, 5, 1);
        var to = new DateTime(2025, 5, 31);
        _mockAnalyticsService.Setup(s => s.GetUserTopRoommatesAsync(CurrentUserId, 10, from, to))
            .ReturnsAsync(new List<TopEntityDto>());

        var result = await _controller.GetUserTopRoommates(CurrentUserId, 10, from, to);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ─── GetSummary — verifies DTO fields ────────────────────────────────────

    [Fact]
    public async Task GetSummary_ReturnsDtoWithAllFields()
    {
        var summary = new AnalyticsSummaryDto
        {
            TotalEvents = 200,
            TotalApartmentViews = 80,
            TotalRoommateViews = 40,
            TotalSearches = 60,
            TotalContactClicks = 20,
            EventsByCategory = new Dictionary<string, int> { { "listing", 80 }, { "message", 60 } }
        };
        _mockAnalyticsService.Setup(s => s.GetSummaryAsync(null, null))
            .ReturnsAsync(summary);

        var result = await _controller.GetSummary();

        var value = result.Result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<AnalyticsSummaryDto>().Subject;

        value.TotalEvents.Should().Be(200);
        value.TotalApartmentViews.Should().Be(80);
        value.TotalRoommateViews.Should().Be(40);
        value.TotalSearches.Should().Be(60);
        value.TotalContactClicks.Should().Be(20);
        value.EventsByCategory.Should().ContainKey("listing").And.ContainKey("message");
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
