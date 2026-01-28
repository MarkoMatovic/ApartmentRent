using Lander.Helpers;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Modules.Roommates.Dtos.InputDto;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace Lander.src.Modules.Roommates.Controllers;
[Route(ApiActionsV1.Roommates)]
[ApiController]
public class RoommatesController : ControllerBase
{
    private readonly IRoommateService _roommateService;
    private readonly IUserInterface _userInterface;
    private readonly Lander.src.Modules.Analytics.Interfaces.IAnalyticsService _analyticsService;
    public RoommatesController(IRoommateService roommateService, IUserInterface userInterface, Lander.src.Modules.Analytics.Interfaces.IAnalyticsService analyticsService)
    {
        _roommateService = roommateService;
        _userInterface = userInterface;
        _analyticsService = analyticsService;
    }
    [HttpGet(ApiActionsV1.GetAllRoommates, Name = nameof(ApiActionsV1.GetAllRoommates))]
    public async Task<ActionResult> GetAllRoommates(
        [FromQuery] string? location = null,
        [FromQuery] decimal? minBudget = null,
        [FromQuery] decimal? maxBudget = null,
        [FromQuery] bool? smokingAllowed = null,
        [FromQuery] bool? petFriendly = null,
        [FromQuery] string? lifestyle = null,
        [FromQuery] int? apartmentId = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        if (location != null || minBudget.HasValue || maxBudget.HasValue || smokingAllowed.HasValue || petFriendly.HasValue || lifestyle != null)
        {
            var searchQuery = $"Location:{location},Budget:{minBudget}-{maxBudget},Smoking:{smokingAllowed},Pets:{petFriendly},Lifestyle:{lifestyle}";
            _ = _analyticsService.TrackEventAsync(
                "RoommateSearch",
                "Roommates",
                searchQuery: searchQuery,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString()
            );
        }
        if (page.HasValue && pageSize.HasValue)
        {
            var pagedResult = await _roommateService.GetAllRoommatesAsync(
                location, minBudget, maxBudget, smokingAllowed, petFriendly, lifestyle, apartmentId, page.Value, pageSize.Value);
            return Ok(pagedResult);
        }
        var roommates = await _roommateService.GetAllRoommatesAsync(
            location, minBudget, maxBudget, smokingAllowed, petFriendly, lifestyle, apartmentId);
        return Ok(roommates);
    }
    [HttpGet(ApiActionsV1.GetRoommate, Name = nameof(ApiActionsV1.GetRoommate))]
    [AllowAnonymous]
    public async Task<ActionResult<RoommateDto>> GetRoommate([FromQuery] int id)
    {
        int? userId = null;
        var userIdClaim = User?.FindFirst("userId");
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedUserId))
        {
            userId = parsedUserId;
        }
        _ = _analyticsService.TrackEventAsync(
            "RoommateView",
            "Roommates",
            entityId: id,
            entityType: "Roommate",
            userId: userId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString()
        );
        var roommate = await _roommateService.GetRoommateByIdAsync(id);
        if (roommate == null)
            return NotFound();
        return Ok(roommate);
    }
    [HttpGet(ApiActionsV1.GetRoommateByUserId, Name = nameof(ApiActionsV1.GetRoommateByUserId))]
    public async Task<ActionResult<RoommateDto>> GetRoommateByUserId([FromQuery] int userId)
    {
        var roommate = await _roommateService.GetRoommateByUserIdAsync(userId);
        if (roommate == null)
            return NotFound();
        return Ok(roommate);
    }
    [HttpPost(ApiActionsV1.CreateRoommate, Name = nameof(ApiActionsV1.CreateRoommate))]
    [Authorize]
    public async Task<ActionResult<RoommateDto>> CreateRoommate([FromBody] RoommateInputDto input)
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid))
        {
            return Unauthorized(new { message = "User GUID not found in token claims" });
        }
        Guid parsedGuid;
        if (!Guid.TryParse(userGuid, out parsedGuid))
        {
            return Unauthorized(new { message = "Invalid user GUID format" });
        }
        var user = await _userInterface.GetUserByGuidAsync(parsedGuid);
        if (user == null)
        {
            return Unauthorized(new { message = $"User not found for GUID: {userGuid}" });
        }
        if (!user.IsActive)
        {
            return Unauthorized(new { message = "User account is not active. Please contact support." });
        }
        try 
        {
            var roommate = await _roommateService.CreateRoommateAsync(user.UserId, input);
            return Ok(roommate);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
    [HttpPut(ApiActionsV1.UpdateRoommate, Name = nameof(ApiActionsV1.UpdateRoommate))]
    [Authorize]
    public async Task<ActionResult<RoommateDto>> UpdateRoommate([FromRoute] int id, [FromBody] RoommateInputDto input)
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid))
            return Unauthorized();
        var user = await _userInterface.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null)
            return Unauthorized();
        try
        {
            var roommate = await _roommateService.UpdateRoommateAsync(id, user.UserId, input);
            return Ok(roommate);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpDelete(ApiActionsV1.DeleteRoommate, Name = nameof(ApiActionsV1.DeleteRoommate))]
    [Authorize]
    public async Task<ActionResult<bool>> DeleteRoommate([FromRoute] int id)
    {
        var userGuid = User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userGuid))
            return Unauthorized();
        var user = await _userInterface.GetUserByGuidAsync(Guid.Parse(userGuid));
        if (user == null)
            return Unauthorized();
        var result = await _roommateService.DeleteRoommateAsync(id, user.UserId);
        if (!result)
            return NotFound();
        return Ok(result);
    }
}
