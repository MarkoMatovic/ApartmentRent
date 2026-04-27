using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Modules.SearchRequests.Dtos.Dto;
using Lander.src.Modules.SearchRequests.Dtos.InputDto;
using Lander.src.Modules.SearchRequests.Interfaces;
using Lander.src.Modules.SearchRequests.Models;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.SearchRequests.Controllers;

[Route(ApiActionsV1.SearchRequests)]
[ApiController]
public class SearchRequestsController : ApiControllerBase
{
    private readonly ISearchRequestService _searchRequestService;

    public SearchRequestsController(
        ISearchRequestService searchRequestService,
        IUserInterface userService) : base(userService)
    {
        _searchRequestService = searchRequestService;
    }

    [HttpGet(ApiActionsV1.GetAllSearchRequests, Name = nameof(ApiActionsV1.GetAllSearchRequests))]
    [AllowAnonymous]
    public async Task<ActionResult> GetAllSearchRequests(
        [FromQuery] SearchRequestType? requestType = null,
        [FromQuery] string? city = null,
        [FromQuery] decimal? minBudget = null,
        [FromQuery] decimal? maxBudget = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var pagedResult = await _searchRequestService.GetAllSearchRequestsAsync(
            requestType, city, minBudget, maxBudget, page, pageSize);
        return Ok(pagedResult);
    }

    [HttpGet(ApiActionsV1.GetSearchRequest, Name = nameof(ApiActionsV1.GetSearchRequest))]
    [AllowAnonymous]
    public async Task<ActionResult<SearchRequestDto>> GetSearchRequest([FromQuery] int id)
    {
        var searchRequest = await _searchRequestService.GetSearchRequestByIdAsync(id);
        if (searchRequest == null) return NotFound();
        return Ok(searchRequest);
    }

    [HttpGet(ApiActionsV1.GetSearchRequestsByUserId, Name = nameof(ApiActionsV1.GetSearchRequestsByUserId))]
    [Authorize]
    public async Task<ActionResult<IEnumerable<SearchRequestDto>>> GetSearchRequestsByUserId([FromQuery] int userId)
    {
        var searchRequests = await _searchRequestService.GetSearchRequestsByUserIdAsync(userId);
        return Ok(searchRequests);
    }

    [HttpPost(ApiActionsV1.CreateSearchRequest, Name = nameof(ApiActionsV1.CreateSearchRequest))]
    [Authorize]
    public async Task<ActionResult<SearchRequestDto>> CreateSearchRequest([FromBody] SearchRequestInputDto input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var searchRequest = await _searchRequestService.CreateSearchRequestAsync(user.UserId, input);
        return Ok(searchRequest);
    }

    [HttpPut(ApiActionsV1.UpdateSearchRequest, Name = nameof(ApiActionsV1.UpdateSearchRequest))]
    [Authorize]
    public async Task<ActionResult<SearchRequestDto>> UpdateSearchRequest([FromRoute] int id, [FromBody] SearchRequestInputDto input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var searchRequest = await _searchRequestService.UpdateSearchRequestAsync(id, user.UserId, input);
        return Ok(searchRequest);
    }

    [HttpDelete(ApiActionsV1.DeleteSearchRequest, Name = nameof(ApiActionsV1.DeleteSearchRequest))]
    [Authorize]
    public async Task<ActionResult<bool>> DeleteSearchRequest([FromRoute] int id)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var result = await _searchRequestService.DeleteSearchRequestAsync(id, user.UserId);
        if (!result) return NotFound();
        return Ok(result);
    }
}
