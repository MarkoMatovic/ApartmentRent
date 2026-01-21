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
    [Authorize]  // Changed from AllowAnonymous - need user to be authenticated
    public async Task<IActionResult> TrackEvent([FromBody] TrackEventInputDto input)
    {
        // DEBUG: Log everything about the request
        Console.WriteLine("========== TRACK EVENT DEBUG ==========");
        Console.WriteLine($"Event Type: {input.EventType}");
        Console.WriteLine($"Event Category: {input.EventCategory}");
        Console.WriteLine($"Entity ID: {input.EntityId}");
        Console.WriteLine($"Entity Type: {input.EntityType}");
        Console.WriteLine($"User.Identity.IsAuthenticated: {User?.Identity?.IsAuthenticated}");
        Console.WriteLine($"User.Identity.Name: {User?.Identity?.Name}");
        
        // Log all claims
        if (User?.Claims != null)
        {
            Console.WriteLine($"Total claims: {User.Claims.Count()}");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"  Claim: {claim.Type} = {claim.Value}");
            }
        }
        else
        {
            Console.WriteLine("No claims found!");
        }
        
        // Extract userId from claims if user is authenticated
        int? userId = null;
        
        // Try multiple possible claim names
        var userIdClaim = User?.FindFirst("userId") 
            ?? User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? User?.FindFirst("sub");
            
        if (userIdClaim != null)
        {
            Console.WriteLine($"Found userId claim: {userIdClaim.Type} = {userIdClaim.Value}");
            // Try to parse as int directly
            if (int.TryParse(userIdClaim.Value, out var parsedUserId))
            {
                userId = parsedUserId;
                Console.WriteLine($"✅ Parsed userId: {userId}");
            }
            else
            {
                Console.WriteLine($"❌ Failed to parse userId from value: {userIdClaim.Value}");
            }
        }
        else
        {
            Console.WriteLine("❌ No userId claim found!");
        }
        
        Console.WriteLine($"Final userId being saved: {userId?.ToString() ?? "NULL"}");
        Console.WriteLine("=====================================");

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

        return Ok(new { success = true, userId = userId }); // Return userId for debugging
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

    // User-specific analytics endpoints
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

    // Complete user analytics
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

    // Personal analytics endpoints - New
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
