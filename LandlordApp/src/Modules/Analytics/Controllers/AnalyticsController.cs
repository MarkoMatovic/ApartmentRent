using Lander.Helpers;
using Lander.src.Modules.Analytics.Dtos.Dto;
using Lander.src.Modules.Analytics.Dtos.InputDto;
using Lander.src.Modules.Analytics.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Lander.src.Modules.Analytics.Controllers;
[Route(ApiActionsV1.Analytics)]
[ApiController]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }
    [HttpPost(ApiActionsV1.TrackEvent, Name = nameof(ApiActionsV1.TrackEvent))]
    [Authorize]
    public async Task<IActionResult> TrackEvent([FromBody] TrackEventInputDto input)
    {
        int? userId = null;
        var userIdClaim = User?.FindFirst("userId") 
            ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User?.FindFirst("sub");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedUserId))
        {
            userId = parsedUserId;
        }
        await _analyticsService.TrackEventAsync(
            input.EventType,
            input.EventCategory,
            input.EntityId,
            input.EntityType,
            input.SearchQuery,
            input.Metadata,
            userId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            HttpContext.Request.Headers["User-Agent"].ToString()
        );
        return Ok(new { success = true });
    }
    [HttpGet(ApiActionsV1.GetAnalyticsSummary, Name = nameof(ApiActionsV1.GetAnalyticsSummary))]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetSummary(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var summary = await _analyticsService.GetSummaryAsync(from, to);
        return Ok(summary);
    }
    [HttpGet(ApiActionsV1.GetTopViewedApartments, Name = nameof(ApiActionsV1.GetTopViewedApartments))]
    public async Task<ActionResult<List<TopEntityDto>>> GetTopViewedApartments(
        [FromQuery] int count = 10,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var topApartments = await _analyticsService.GetTopViewedApartmentsAsync(count, from, to);
        return Ok(topApartments);
    }
    [HttpGet(ApiActionsV1.GetTopViewedRoommates, Name = nameof(ApiActionsV1.GetTopViewedRoommates))]
    public async Task<ActionResult<List<TopEntityDto>>> GetTopViewedRoommates(
        [FromQuery] int count = 10,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var topRoommates = await _analyticsService.GetTopViewedRoommatesAsync(count, from, to);
        return Ok(topRoommates);
    }
    [HttpGet(ApiActionsV1.GetTopSearchTerms, Name = nameof(ApiActionsV1.GetTopSearchTerms))]
    public async Task<ActionResult<List<SearchTermDto>>> GetTopSearchTerms(
        [FromQuery] int count = 10,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var topSearches = await _analyticsService.GetTopSearchTermsAsync(count, from, to);
        return Ok(topSearches);
    }
    [HttpGet(ApiActionsV1.GetEventTrends, Name = nameof(ApiActionsV1.GetEventTrends))]
    public async Task<ActionResult<List<EventTrendDto>>> GetEventTrends(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string? eventType = null)
    {
        var trends = await _analyticsService.GetEventTrendsAsync(from, to, eventType);
        return Ok(trends);
    }
    [HttpGet(ApiActionsV1.GetUserRoommateSummary, Name = nameof(ApiActionsV1.GetUserRoommateSummary))]
    public async Task<ActionResult<UserRoommateAnalyticsSummaryDto>> GetUserRoommateSummary(
        [FromQuery] int userId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var summary = await _analyticsService.GetUserRoommateSummaryAsync(userId, from, to);
        return Ok(summary);
    }
    [HttpGet(ApiActionsV1.GetUserTopRoommates, Name = nameof(ApiActionsV1.GetUserTopRoommates))]
    public async Task<ActionResult<List<TopEntityDto>>> GetUserTopRoommates(
        [FromQuery] int userId,
        [FromQuery] int count = 10,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var topRoommates = await _analyticsService.GetUserTopRoommatesAsync(userId, count, from, to);
        return Ok(topRoommates);
    }
    [HttpGet(ApiActionsV1.GetUserSearches, Name = nameof(ApiActionsV1.GetUserSearches))]
    public async Task<ActionResult<List<SearchTermDto>>> GetUserSearches(
        [FromQuery] int userId,
        [FromQuery] int count = 10,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var searches = await _analyticsService.GetUserSearchesAsync(userId, count, from, to);
        return Ok(searches);
    }
    [HttpGet(ApiActionsV1.GetUserRoommateTrends, Name = nameof(ApiActionsV1.GetUserRoommateTrends))]
    public async Task<ActionResult<UserRoommateTrendsDto>> GetUserRoommateTrends(
        [FromQuery] int userId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var trends = await _analyticsService.GetUserRoommateTrendsAsync(userId, from, to);
        return Ok(trends);
    }
    [HttpGet(ApiActionsV1.GetUserTopApartments, Name = nameof(ApiActionsV1.GetUserTopApartments))]
    public async Task<ActionResult<List<TopEntityDto>>> GetUserTopApartments(
        [FromQuery] int userId,
        [FromQuery] int count = 10,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var topApartments = await _analyticsService.GetUserTopApartmentsAsync(userId, count, from, to);
        return Ok(topApartments);
    }
    [HttpGet(ApiActionsV1.GetUserCompleteAnalytics, Name = nameof(ApiActionsV1.GetUserCompleteAnalytics))]
    public async Task<ActionResult<AnalyticsSummaryDto>> GetUserCompleteAnalytics(
        [FromQuery] int userId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var summary = await _analyticsService.GetUserCompleteAnalyticsAsync(userId, from, to);
        return Ok(summary);
    }
    [HttpGet("my-viewed-apartments", Name = "GetMyViewedApartments")]
    [Authorize]
    public async Task<ActionResult<List<TopEntityDto>>> GetMyViewedApartments(
        [FromQuery] int count = 10,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userIdClaim = User.FindFirst("userId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "User ID not found in token" });
        }
        var topApartments = await _analyticsService.GetUserTopApartmentsAsync(userId, count, from, to);
        return Ok(topApartments);
    }
    [HttpGet("my-apartment-views", Name = "GetMyApartmentViews")]
    [Authorize]
    public async Task<ActionResult<List<ApartmentViewStatsDto>>> GetMyApartmentViews(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userIdClaim = User.FindFirst("userId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "User ID not found in token" });
        }
        var viewStats = await _analyticsService.GetLandlordApartmentViewsAsync(userId, from, to);
        return Ok(viewStats);
    }
    [HttpGet("my-messages-sent", Name = "GetMyMessagesSent")]
    [Authorize]
    public async Task<ActionResult<int>> GetMyMessagesSent(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var userIdClaim = User.FindFirst("userId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new { message = "User ID not found in token" });
        }
        var count = await _analyticsService.GetUserMessageCountAsync(userId, from, to);
        return Ok(count);
    }
}
