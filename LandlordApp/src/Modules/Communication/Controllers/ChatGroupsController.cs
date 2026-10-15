using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lander.src.Modules.Communication.Controllers;

/// <summary>Group conversations: a roommate pair together with the landlord they are both talking to.</summary>
[Route(ApiActionsV1.ChatGroups)]
[ApiController]
[Authorize]
public class ChatGroupsController : ControllerBase
{
    private readonly IChatGroupService _groups;

    public ChatGroupsController(IChatGroupService groups) => _groups = groups;

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue("userId");
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            throw new UnauthorizedAccessException();
        return id;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChatGroupDto>>> GetMine() =>
        Ok(await _groups.GetUserGroupsAsync(GetCurrentUserId()));

    [HttpPost(ApiActionsV1.OpenChatGroup)]
    public async Task<ActionResult<ChatGroupDto>> Open([FromBody] OpenChatGroupInputDto input) =>
        Ok(await _groups.OpenWithRoommateAsync(GetCurrentUserId(), input.OtherUserId));

    [HttpGet(ApiActionsV1.ChatGroupMessages)]
    public async Task<ActionResult<ChatGroupMessagesDto>> GetMessages(
        [FromRoute] int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        Ok(await _groups.GetMessagesAsync(id, GetCurrentUserId(), page, pageSize));

    [HttpPost(ApiActionsV1.ChatGroupMessages)]
    public async Task<ActionResult<ChatGroupMessageDto>> Send([FromRoute] int id, [FromBody] SendGroupMessageInputDto input) =>
        Ok(await _groups.SendMessageAsync(id, GetCurrentUserId(), input.MessageText));

    [HttpPost(ApiActionsV1.ReadChatGroup)]
    public async Task<IActionResult> MarkRead([FromRoute] int id)
    {
        await _groups.MarkReadAsync(id, GetCurrentUserId());
        return NoContent();
    }

    [HttpPost(ApiActionsV1.LeaveChatGroup)]
    public async Task<IActionResult> Leave([FromRoute] int id)
    {
        await _groups.LeaveAsync(id, GetCurrentUserId());
        return NoContent();
    }
}
