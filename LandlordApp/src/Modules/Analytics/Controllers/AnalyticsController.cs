using Lander.Helpers;
using Lander.src.Modules.Analytics.Dtos.Dto;
using Lander.src.Modules.Analytics.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Analytics.Controllers;

[Route(ApiActionsV1.Analytics)]
[ApiController]
[Authorize(Policy = "AdminPolicy")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
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
}
