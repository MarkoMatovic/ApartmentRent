using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Modules.SavedSearches.Dtos.Dto;
using Lander.src.Modules.SavedSearches.Dtos.InputDto;
using Lander.src.Modules.SavedSearches.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.SavedSearches.Controllers;

[Route(ApiActionsV1.SavedSearches)]
[ApiController]
public class SavedSearchesController : ApiControllerBase
{
    private readonly ISavedSearchService _savedSearchService;

    public SavedSearchesController(
        ISavedSearchService savedSearchService,
        IUserInterface userService) : base(userService)
    {
        _savedSearchService = savedSearchService;
    }

    [HttpGet(ApiActionsV1.GetSavedSearchesByUserId, Name = nameof(ApiActionsV1.GetSavedSearchesByUserId))]
    [Authorize]
    public async Task<ActionResult<IEnumerable<SavedSearchDto>>> GetSavedSearchesByUserId([FromQuery] int userId)
    {
        var user = await GetCurrentUserAsync();
        if (user is null || user.UserId != userId) return Forbid();

        var savedSearches = await _savedSearchService.GetSavedSearchesByUserIdAsync(userId);
        return Ok(savedSearches);
    }

    [HttpGet(ApiActionsV1.GetSavedSearch, Name = nameof(ApiActionsV1.GetSavedSearch))]
    [Authorize]
    public async Task<ActionResult<SavedSearchDto>> GetSavedSearch([FromQuery] int id)
    {
        var savedSearch = await _savedSearchService.GetSavedSearchByIdAsync(id);
        if (savedSearch == null) return NotFound();
        return Ok(savedSearch);
    }

    [HttpPost(ApiActionsV1.CreateSavedSearch, Name = nameof(ApiActionsV1.CreateSavedSearch))]
    [Authorize]
    public async Task<ActionResult<SavedSearchDto>> CreateSavedSearch([FromBody] SavedSearchInputDto input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var savedSearch = await _savedSearchService.CreateSavedSearchAsync(user.UserId, input);
        return Ok(savedSearch);
    }

    [HttpPut(ApiActionsV1.UpdateSavedSearch, Name = nameof(ApiActionsV1.UpdateSavedSearch))]
    [Authorize]
    public async Task<ActionResult<SavedSearchDto>> UpdateSavedSearch([FromRoute] int id, [FromBody] SavedSearchInputDto input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var savedSearch = await _savedSearchService.UpdateSavedSearchAsync(id, user.UserId, input);
        return Ok(savedSearch);
    }

    [HttpDelete(ApiActionsV1.DeleteSavedSearch, Name = nameof(ApiActionsV1.DeleteSavedSearch))]
    [Authorize]
    public async Task<ActionResult<bool>> DeleteSavedSearch([FromRoute] int id)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var result = await _savedSearchService.DeleteSavedSearchAsync(id, user.UserId);
        if (!result) return NotFound();
        return Ok(result);
    }
}
