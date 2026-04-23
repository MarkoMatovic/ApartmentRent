using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Modules.Analytics.Dtos.Dto;
using Lander.src.Modules.Analytics.Dtos.InputDto;
using Lander.src.Modules.Analytics.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Analytics.Controllers;

[Route(ApiActionsV1.Analytics)]
[ApiController]
[Authorize]
public class AnalyticsController : ApiControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        IUserInterface userService) : base(userService)
    {
        _analyticsService = analyticsService;
    }

    [HttpPost(ApiActionsV1.TrackEvent, Name = nameof(ApiActionsV1.TrackEvent))]
    public async Task<IActionResult> TrackEvent([FromBody] TrackEventInputDto input)
    {
        await _analyticsService.TrackEventAsync(
            input.EventType, input.EventCategory,
            input.EntityId, input.EntityType,
            input.SearchQuery, input.Metadata,
            userId: TryGetCurrentUserId(),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(new { success = true });
    }

    [HttpGet(ApiActionsV1.GetAnalyticsSummary, Name = nameof(ApiActionsV1.GetAnalyticsSummary))]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary(
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var summary = await _analyticsService.GetSummaryAsync(from, to);
        return Ok(summary);
    }

    [HttpGet(ApiActionsV1.GetTopViewedApartments, Name = nameof(ApiActionsV1.GetTopViewedApartments))]
    public async Task<ActionResult<List<TopEntityDto>>> GetTopViewedApartments(
        [FromQuery] int count = 10, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        return Ok(await _analyticsService.GetTopViewedApartmentsAsync(count, from, to));
    }

    [HttpGet(ApiActionsV1.GetTopViewedRoommates, Name = nameof(ApiActionsV1.GetTopViewedRoommates))]
    public async Task<ActionResult<List<TopEntityDto>>> GetTopViewedRoommates(
        [FromQuery] int count = 10, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        return Ok(await _analyticsService.GetTopViewedRoommatesAsync(count, from, to));
    }

    [HttpGet(ApiActionsV1.GetTopSearchTerms, Name = nameof(ApiActionsV1.GetTopSearchTerms))]
    public async Task<ActionResult<List<SearchTermDto>>> GetTopSearchTerms(
        [FromQuery] int count = 10, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        return Ok(await _analyticsService.GetTopSearchTermsAsync(count, from, to));
    }

    [HttpGet(ApiActionsV1.GetEventTrends, Name = nameof(ApiActionsV1.GetEventTrends))]
    public async Task<ActionResult<List<EventTrendDto>>> GetEventTrends(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string? eventType = null)
    {
        return Ok(await _analyticsService.GetEventTrendsAsync(from, to, eventType));
    }

    [HttpGet(ApiActionsV1.GetUserRoommateSummary, Name = nameof(ApiActionsV1.GetUserRoommateSummary))]
    public async Task<ActionResult<UserRoommateAnalyticsSummaryDto>> GetUserRoommateSummary(
        [FromQuery] int userId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        return Ok(await _analyticsService.GetUserRoommateSummaryAsync(userId, from, to));
    }

    [HttpGet(ApiActionsV1.GetUserTopRoommates, Name = nameof(ApiActionsV1.GetUserTopRoommates))]
    public async Task<ActionResult<List<TopEntityDto>>> GetUserTopRoommates(
        [FromQuery] int userId, [FromQuery] int count = 10,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        return Ok(await _analyticsService.GetUserTopRoommatesAsync(userId, count, from, to));
    }

    [HttpGet(ApiActionsV1.GetUserSearches, Name = nameof(ApiActionsV1.GetUserSearches))]
    public async Task<ActionResult<List<SearchTermDto>>> GetUserSearches(
        [FromQuery] int userId, [FromQuery] int count = 10,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        return Ok(await _analyticsService.GetUserSearchesAsync(userId, count, from, to));
    }

    [HttpGet(ApiActionsV1.GetUserRoommateTrends, Name = nameof(ApiActionsV1.GetUserRoommateTrends))]
    public async Task<ActionResult<UserRoommateTrendsDto>> GetUserRoommateTrends(
        [FromQuery] int userId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        return Ok(await _analyticsService.GetUserRoommateTrendsAsync(userId, from, to));
    }

    [HttpGet(ApiActionsV1.GetUserTopApartments, Name = nameof(ApiActionsV1.GetUserTopApartments))]
    public async Task<ActionResult<List<TopEntityDto>>> GetUserTopApartments(
        [FromQuery] int userId, [FromQuery] int count = 10,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        return Ok(await _analyticsService.GetUserTopApartmentsAsync(userId, count, from, to));
    }

    [HttpGet(ApiActionsV1.GetUserCompleteAnalytics, Name = nameof(ApiActionsV1.GetUserCompleteAnalytics))]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetUserCompleteAnalytics(
        [FromQuery] int userId, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        return Ok(await _analyticsService.GetUserCompleteAnalyticsAsync(userId, from, to));
    }

    [HttpGet("my-viewed-apartments", Name = "GetMyViewedApartments")]
    public async Task<ActionResult<List<TopEntityDto>>> GetMyViewedApartments(
        [FromQuery] int count = 10, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var userId = TryGetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "User ID not found in token" });

        return Ok(await _analyticsService.GetUserTopApartmentsAsync(userId.Value, count, from, to));
    }

    [HttpGet("my-apartment-views", Name = "GetMyApartmentViews")]
    public async Task<ActionResult<List<ApartmentViewStatsDto>>> GetMyApartmentViews(
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var userId = TryGetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "User ID not found in token" });

        return Ok(await _analyticsService.GetLandlordApartmentViewsAsync(userId.Value, from, to));
    }

    [HttpGet("my-messages-sent", Name = "GetMyMessagesSent")]
    public async Task<ActionResult<int>> GetMyMessagesSent(
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var userId = TryGetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "User ID not found in token" });

        return Ok(await _analyticsService.GetUserMessageCountAsync(userId.Value, from, to));
    }
}
