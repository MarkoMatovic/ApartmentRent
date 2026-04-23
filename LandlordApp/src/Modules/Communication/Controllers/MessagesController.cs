using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lander.src.Modules.Communication.Controllers;

[Route(ApiActionsV1.Messages)]
[ApiController]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly Lander.src.Modules.Analytics.Interfaces.IAnalyticsService _analyticsService;

    public MessagesController(
        IMessageService messageService,
        Lander.src.Modules.Analytics.Interfaces.IAnalyticsService analyticsService)
    {
        _messageService = messageService;
        _analyticsService = analyticsService;
    }

    // ─── helpers ────────────────────────────────────────────────────────────────
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue("userId");
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            throw new UnauthorizedAccessException("Korisnički ID nije pronađen u tokenu.");
        return id;
    }

    private ActionResult? ForbiddenIfNotOwner(int requestedUserId)
    {
        var currentId = GetCurrentUserId();
        return currentId != requestedUserId ? Forbid() : null;
    }

    // ─── endpoints ──────────────────────────────────────────────────────────────

    [HttpGet(ApiActionsV1.GetConversation, Name = nameof(ApiActionsV1.GetConversation))]
    public async Task<ActionResult<ConversationMessagesDto>> GetConversation(
        [FromQuery] int userId1, [FromQuery] int userId2,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (pageSize > 100) pageSize = 100;

        var currentId = GetCurrentUserId();
        if (currentId != userId1 && currentId != userId2)
            return Forbid();

        var conversation = await _messageService.GetConversationAsync(userId1, userId2, page, pageSize);
        return Ok(conversation);
    }

    [HttpGet(ApiActionsV1.GetUserConversations, Name = nameof(ApiActionsV1.GetUserConversations))]
    public async Task<ActionResult<List<ConversationDto>>> GetUserConversations([FromRoute] int userId)
    {
        var guard = ForbiddenIfNotOwner(userId);
        if (guard != null) return guard;
        return Ok(await _messageService.GetUserConversationsAsync(userId));
    }

    [HttpPost(ApiActionsV1.SendMessage, Name = nameof(ApiActionsV1.SendMessage))]
    [AllowAnonymous] // TEMP: k6 testing
    [Microsoft.AspNetCore.RateLimiting.DisableRateLimiting] // TEMP: k6 testing
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageInputDto input)
    {
        // TEMP: skip ownership check for anonymous k6 callers
        var claimStr = User.FindFirstValue("userId");
        if (!string.IsNullOrEmpty(claimStr))
        {
            if (!int.TryParse(claimStr, out var currentId) || input.SenderId != currentId)
                return Forbid();
        }

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var message = await _messageService.SendMessageAsync(
            input.SenderId, input.ReceiverId, input.MessageText, input.IsSuperLike,
            idempotencyKey: idempotencyKey);

        if (message is null)
            return Conflict(new { message = "Duplicate request." });

        _ = _analyticsService.TrackEventAsync(
            "MessageSent", "Communication",
            entityId: input.ReceiverId, entityType: "User",
            userId: input.SenderId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(message);
    }

    [HttpPut(ApiActionsV1.MarkMessageAsRead, Name = nameof(ApiActionsV1.MarkMessageAsRead))]
    public async Task<IActionResult> MarkAsRead([FromRoute] int messageId)
    {
        var currentUserId = GetCurrentUserId();
        var isRecipient = await _messageService.IsMessageRecipientAsync(messageId, currentUserId);
        if (!isRecipient) return Forbid();

        await _messageService.MarkAsReadAsync(messageId);
        return Ok();
    }

    [HttpGet(ApiActionsV1.GetUnreadCount, Name = nameof(ApiActionsV1.GetUnreadCount))]
    public async Task<ActionResult<int>> GetUnreadCount([FromRoute] int userId)
    {
        var guard = ForbiddenIfNotOwner(userId);
        if (guard != null) return guard;
        return Ok(await _messageService.GetUnreadCountAsync(userId));
    }

    [HttpPost("archive")]
    public async Task<IActionResult> ArchiveConversation([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.ArchiveConversationAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpPost("unarchive")]
    public async Task<IActionResult> UnarchiveConversation([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.UnarchiveConversationAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpPost("mute")]
    public async Task<IActionResult> MuteConversation([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.MuteConversationAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpPost("unmute")]
    public async Task<IActionResult> UnmuteConversation([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.UnmuteConversationAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpPost("block")]
    public async Task<IActionResult> BlockUser([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.BlockUserAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpPost("unblock")]
    public async Task<IActionResult> UnblockUser([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.UnblockUserAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpDelete("delete-conversation")]
    public async Task<IActionResult> DeleteConversation([FromQuery] int otherUserId)
    {
        var userId = GetCurrentUserId();
        await _messageService.DeleteConversationAsync(userId, otherUserId);
        return Ok();
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<MessageDto>>> SearchMessages([FromQuery] string query)
    {
        var userId = GetCurrentUserId();
        return Ok(await _messageService.SearchMessagesAsync(userId, query));
    }

    [HttpPost("report")]
    public async Task<IActionResult> ReportAbuse([FromBody] ReportAbuseRequestDto dto)
    {
        var currentId = GetCurrentUserId();
        if (dto.UserId != currentId) return Forbid();

        await _messageService.ReportAbuseAsync(dto.UserId, new ReportMessageDto
        {
            ReportedUserId = dto.ReportedUserId,
            MessageId = dto.MessageId,
            Reason = dto.Reason
        });
        return Ok();
    }
}
