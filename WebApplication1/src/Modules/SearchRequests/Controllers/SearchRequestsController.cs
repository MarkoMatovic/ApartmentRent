using Lander.Helpers;
using Lander.src.Modules.SearchRequests.Dtos.Dto;
using Lander.src.Modules.SearchRequests.Dtos.InputDto;
using Lander.src.Modules.SearchRequests.Interfaces;
using Lander.src.Modules.SearchRequests.Models;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lander.src.Modules.SearchRequests.Controllers;

[Route(ApiActionsV1.SearchRequests)]
[ApiController]
public class SearchRequestsController : ControllerBase
{
    private readonly ISearchRequestService _searchRequestService;
    private readonly IUserInterface _userInterface;

    public SearchRequestsController(ISearchRequestService searchRequestService, IUserInterface userInterface)
    {
        _searchRequestService = searchRequestService;
        _userInterface = userInterface;
    }

    [HttpGet(ApiActionsV1.GetAllSearchRequests, Name = nameof(ApiActionsV1.GetAllSearchRequests))]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<SearchRequestDto>>> GetAllSearchRequests(
        [FromQuery] SearchRequestType? requestType = null,
        [FromQuery] string? city = null,
        [FromQuery] decimal? minBudget = null,
        [FromQuery] decimal? maxBudget = null)
    {
        var searchRequests = await _searchRequestService.GetAllSearchRequestsAsync(
            requestType, city, minBudget, maxBudget);
        return Ok(searchRequests);
    }

    [HttpGet(ApiActionsV1.GetSearchRequest, Name = nameof(ApiActionsV1.GetSearchRequest))]
    [AllowAnonymous]
    public async Task<ActionResult<SearchRequestDto>> GetSearchRequest([FromQuery] int id)
    {
        var searchRequest = await _searchRequestService.GetSearchRequestByIdAsync(id);
        if (searchRequest == null)
            return NotFound();
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
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid))
            return Unauthorized();

        var user = await _userInterface.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null)
            return Unauthorized();

        var searchRequest = await _searchRequestService.CreateSearchRequestAsync(user.UserId, input);
        return Ok(searchRequest);
    }

    [HttpPut(ApiActionsV1.UpdateSearchRequest, Name = nameof(ApiActionsV1.UpdateSearchRequest))]
    [Authorize]
    public async Task<ActionResult<SearchRequestDto>> UpdateSearchRequest([FromRoute] int id, [FromBody] SearchRequestInputDto input)
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid))
            return Unauthorized();

        var user = await _userInterface.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null)
            return Unauthorized();

        try
        {
            var searchRequest = await _searchRequestService.UpdateSearchRequestAsync(id, user.UserId, input);
            return Ok(searchRequest);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete(ApiActionsV1.DeleteSearchRequest, Name = nameof(ApiActionsV1.DeleteSearchRequest))]
    [Authorize]
    public async Task<ActionResult<bool>> DeleteSearchRequest([FromRoute] int id)
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid))
            return Unauthorized();

        var user = await _userInterface.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null)
            return Unauthorized();

        var result = await _searchRequestService.DeleteSearchRequestAsync(id, user.UserId);
        if (!result)
            return NotFound();
        return Ok(result);
    }
}

