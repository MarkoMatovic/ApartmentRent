using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Modules.Analytics.Dtos.Dto;
using Lander.src.Modules.Analytics.Dtos.InputDto;
using Lander.src.Modules.Analytics.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lander.src.Modules.Analytics.Controllers;

[Route(ApiActionsV1.Analytics)]
[ApiController]
[Authorize]
public class AnalyticsController : ApiControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    // Only these event types may be recorded. Without a whitelist a bot can pump the
    // analytics table with arbitrary event types (storage cost + skewing "top viewed").
    private static readonly HashSet<string> AllowedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ApartmentView", "RoommateView", "ApartmentSearch", "RoommateSearch",
        "ContactClick", "MessageSent",
    };

    public AnalyticsController(
        IAnalyticsService analyticsService,
        IUserInterface userService) : base(userService)
    {
        _analyticsService = analyticsService;
    }

    [HttpPost(ApiActionsV1.TrackEvent, Name = nameof(ApiActionsV1.TrackEvent))]
    [AllowAnonymous] // Views must be tracked for both logged-in and anonymous visitors
    [EnableRateLimiting("analytics-track")]
    public async Task<IActionResult> TrackEvent([FromBody] TrackEventInputDto input)
    {
        if (string.IsNullOrWhiteSpace(input.EventType) || !AllowedEventTypes.Contains(input.EventType))
            return BadRequest(new { error = "Unsupported event type." });

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
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary([FromQuery] DateRangeQuery range)
    {
        var summary = await _analyticsService.GetSummaryAsync(range.From, range.To);
        return Ok(summary);
    }

    [HttpGet(ApiActionsV1.GetTopViewedApartments, Name = nameof(ApiActionsV1.GetTopViewedApartments))]
    public async Task<ActionResult<List<TopEntityDto>>> GetTopViewedApartments([FromQuery] TopEntityQuery query)
    {
        return Ok(await _analyticsService.GetTopViewedApartmentsAsync(query.Count, query.From, query.To));
    }

    [HttpGet(ApiActionsV1.GetTopViewedRoommates, Name = nameof(ApiActionsV1.GetTopViewedRoommates))]
    public async Task<ActionResult<List<TopEntityDto>>> GetTopViewedRoommates([FromQuery] TopEntityQuery query)
    {
        return Ok(await _analyticsService.GetTopViewedRoommatesAsync(query.Count, query.From, query.To));
    }

    [HttpGet(ApiActionsV1.GetTopSearchTerms, Name = nameof(ApiActionsV1.GetTopSearchTerms))]
    public async Task<ActionResult<List<SearchTermDto>>> GetTopSearchTerms([FromQuery] TopEntityQuery query)
    {
        return Ok(await _analyticsService.GetTopSearchTermsAsync(query.Count, query.From, query.To));
    }

    [HttpGet(ApiActionsV1.GetEventTrends, Name = nameof(ApiActionsV1.GetEventTrends))]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<EventTrendDto>>> GetEventTrends(
        [FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] string? eventType = null)
    {
        if ((to - from).TotalDays > 366)
            return BadRequest(new { message = "Date range cannot exceed 1 year." });
        return Ok(await _analyticsService.GetEventTrendsAsync(from, to, eventType));
    }

    [HttpGet(ApiActionsV1.GetUserRoommateSummary, Name = nameof(ApiActionsV1.GetUserRoommateSummary))]
    public async Task<ActionResult<UserRoommateAnalyticsSummaryDto>> GetUserRoommateSummary(
        [FromQuery] int userId, [FromQuery] DateRangeQuery range)
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();
        if (callerId.Value != userId && !User.IsInRole("Admin")) return Forbid();
        return Ok(await _analyticsService.GetUserRoommateSummaryAsync(userId, range.From, range.To));
    }

    [HttpGet(ApiActionsV1.GetUserTopRoommates, Name = nameof(ApiActionsV1.GetUserTopRoommates))]
    public async Task<ActionResult<List<TopEntityDto>>> GetUserTopRoommates(
        [FromQuery] int userId, [FromQuery] TopEntityQuery query)
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();
        if (callerId.Value != userId && !User.IsInRole("Admin")) return Forbid();
        return Ok(await _analyticsService.GetUserTopRoommatesAsync(userId, query.Count, query.From, query.To));
    }

    [HttpGet(ApiActionsV1.GetUserSearches, Name = nameof(ApiActionsV1.GetUserSearches))]
    public async Task<ActionResult<List<SearchTermDto>>> GetUserSearches(
        [FromQuery] int userId, [FromQuery] TopEntityQuery query)
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();
        if (callerId.Value != userId && !User.IsInRole("Admin")) return Forbid();
        return Ok(await _analyticsService.GetUserSearchesAsync(userId, query.Count, query.From, query.To));
    }

    [HttpGet(ApiActionsV1.GetUserRoommateTrends, Name = nameof(ApiActionsV1.GetUserRoommateTrends))]
    public async Task<ActionResult<UserRoommateTrendsDto>> GetUserRoommateTrends(
        [FromQuery] int userId, [FromQuery] DateRangeQuery range)
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();
        if (callerId.Value != userId && !User.IsInRole("Admin")) return Forbid();
        return Ok(await _analyticsService.GetUserRoommateTrendsAsync(userId, range.From, range.To));
    }

    [HttpGet(ApiActionsV1.GetUserTopApartments, Name = nameof(ApiActionsV1.GetUserTopApartments))]
    public async Task<ActionResult<List<TopEntityDto>>> GetUserTopApartments(
        [FromQuery] int userId, [FromQuery] TopEntityQuery query)
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();
        if (callerId.Value != userId && !User.IsInRole("Admin")) return Forbid();
        return Ok(await _analyticsService.GetUserTopApartmentsAsync(userId, query.Count, query.From, query.To));
    }

    [HttpGet(ApiActionsV1.GetUserCompleteAnalytics, Name = nameof(ApiActionsV1.GetUserCompleteAnalytics))]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetUserCompleteAnalytics(
        [FromQuery] int userId, [FromQuery] DateRangeQuery range)
    {
        var callerId = TryGetCurrentUserId();
        if (callerId is null) return Unauthorized();
        if (callerId.Value != userId && !User.IsInRole("Admin")) return Forbid();
        return Ok(await _analyticsService.GetUserCompleteAnalyticsAsync(userId, range.From, range.To));
    }

    [HttpGet(ApiActionsV1.GetMyViewedApartments, Name = nameof(ApiActionsV1.GetMyViewedApartments))]
    public async Task<ActionResult<List<TopEntityDto>>> GetMyViewedApartments([FromQuery] TopEntityQuery query)
    {
        var userId = TryGetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "User ID not found in token" });

        return Ok(await _analyticsService.GetUserTopApartmentsAsync(userId.Value, query.Count, query.From, query.To));
    }

    [HttpGet(ApiActionsV1.GetMyApartmentViews, Name = nameof(ApiActionsV1.GetMyApartmentViews))]
    public async Task<ActionResult<List<ApartmentViewStatsDto>>> GetMyApartmentViews([FromQuery] DateRangeQuery range)
    {
        var userId = TryGetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "User ID not found in token" });

        return Ok(await _analyticsService.GetLandlordApartmentViewsAsync(userId.Value, range.From, range.To));
    }

    [HttpGet(ApiActionsV1.GetMyMessagesSent, Name = nameof(ApiActionsV1.GetMyMessagesSent))]
    public async Task<ActionResult<int>> GetMyMessagesSent([FromQuery] DateRangeQuery range)
    {
        var userId = TryGetCurrentUserId();
        if (userId is null) return Unauthorized(new { message = "User ID not found in token" });

        return Ok(await _analyticsService.GetUserMessageCountAsync(userId.Value, range.From, range.To));
    }
}
