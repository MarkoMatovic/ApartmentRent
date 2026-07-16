using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Modules.Roommates.Dtos.InputDto;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Roommates.Models;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Roommates.Controllers;

[Route(ApiActionsV1.Roommates)]
[ApiController]
public class RoommatesController : ApiControllerBase
{
    private readonly IRoommateService _roommateService;
    private readonly Lander.src.Modules.Analytics.Interfaces.IAnalyticsService _analyticsService;

    public RoommatesController(
        IRoommateService roommateService,
        Lander.src.Modules.Analytics.Interfaces.IAnalyticsService analyticsService,
        IUserInterface userService) : base(userService)
    {
        _roommateService = roommateService;
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
        [FromQuery] string? profession = null,
        [FromQuery] DateOnly? availableFrom = null,
        [FromQuery] int? stayDuration = null,
        [FromQuery] int? apartmentId = null,
        [FromQuery] RoommateGender? gender = null,
        [FromQuery] WorkSchedule? workSchedule = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (location != null || minBudget.HasValue || maxBudget.HasValue || smokingAllowed.HasValue
            || petFriendly.HasValue || lifestyle != null || profession != null)
        {
            var searchQuery = $"Location:{location},Budget:{minBudget}-{maxBudget},Smoking:{smokingAllowed},Pets:{petFriendly},Lifestyle:{lifestyle},Profession:{profession}";
            _ = _analyticsService.TrackEventAsync(
                "RoommateSearch", "Roommates",
                searchQuery: searchQuery,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString());
        }

        var pagedResult = await _roommateService.GetAllRoommatesAsync(
            location, minBudget, maxBudget, smokingAllowed, petFriendly,
            lifestyle, profession, availableFrom, stayDuration, apartmentId, gender, workSchedule, page, pageSize);

        var viewerId = TryGetCurrentUserId();
        foreach (var item in pagedResult.Items)
            SanitizePii(item, viewerId);

        return Ok(pagedResult);
    }

    [HttpGet(ApiActionsV1.GetRoommate, Name = nameof(ApiActionsV1.GetRoommate))]
    [AllowAnonymous]
    public async Task<ActionResult<RoommateDto>> GetRoommate([FromQuery] int id)
    {
        _ = _analyticsService.TrackEventAsync(
            "RoommateView", "Roommates",
            entityId: id, entityType: "Roommate",
            userId: TryGetCurrentUserId(),
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        var roommate = await _roommateService.GetRoommateByIdAsync(id);
        if (roommate == null) return NotFound();

        SanitizePii(roommate, TryGetCurrentUserId());
        return Ok(roommate);
    }

    [HttpGet(ApiActionsV1.GetRoommateByUserId, Name = nameof(ApiActionsV1.GetRoommateByUserId))]
    [AllowAnonymous]
    public async Task<ActionResult<RoommateDto>> GetRoommateByUserId([FromQuery] int userId)
    {
        var roommate = await _roommateService.GetRoommateByUserIdAsync(userId);
        if (roommate == null) return NotFound();

        SanitizePii(roommate, TryGetCurrentUserId());
        return Ok(roommate);
    }

    // Phone number and date of birth are private PII. Only the profile owner may
    // see them; everyone else (anonymous or other authenticated users) gets them
    // stripped, which prevents both anonymous enumeration and single-account scraping.
    private static void SanitizePii(RoommateDto roommate, int? viewerUserId)
    {
        if (viewerUserId is null || roommate.UserId != viewerUserId.Value)
        {
            roommate.PhoneNumber = null;
            roommate.DateOfBirth = null;
        }
    }

    [HttpPost(ApiActionsV1.CreateRoommate, Name = nameof(ApiActionsV1.CreateRoommate))]
    [Authorize]
    public async Task<ActionResult<RoommateDto>> CreateRoommate([FromBody] RoommateInputDto input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();
        if (!user.IsActive)
            return Unauthorized(new { message = "User account is not active. Please contact support." });

        var roommate = await _roommateService.CreateRoommateAsync(user.UserId, input);
        return Ok(roommate);
    }

    [HttpPut(ApiActionsV1.UpdateRoommate, Name = nameof(ApiActionsV1.UpdateRoommate))]
    [Authorize]
    public async Task<ActionResult<RoommateDto>> UpdateRoommate([FromRoute] int id, [FromBody] RoommateInputDto input)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var roommate = await _roommateService.UpdateRoommateAsync(id, user.UserId, input);
        return Ok(roommate);
    }

    [HttpDelete(ApiActionsV1.DeleteRoommate, Name = nameof(ApiActionsV1.DeleteRoommate))]
    [Authorize]
    public async Task<ActionResult<bool>> DeleteRoommate([FromRoute] int id)
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return Unauthorized();

        var result = await _roommateService.DeleteRoommateAsync(id, user.UserId);
        if (!result) return NotFound();
        return Ok(result);
    }
}
