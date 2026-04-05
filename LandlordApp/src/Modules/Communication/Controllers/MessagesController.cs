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
    private readonly IdempotencyService _idempotencyService;

    public MessagesController(
        IMessageService messageService,
        Lander.src.Modules.Analytics.Interfaces.IAnalyticsService analyticsService,
        IdempotencyService idempotencyService)
    {
        _messageService = messageService;
        _analyticsService = analyticsService;
        _idempotencyService = idempotencyService;
    }

    // ─── helpers ────────────────────────────────────────────────────────────────
    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue("userId");
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            throw new UnauthorizedAccessException("Korisnički ID nije pronađen u tokenu.");
        return id;
    }

    // Returns non-null ActionResult when access is denied; null when allowed.
    // Non-generic ActionResult is implicitly convertible to ActionResult<T>.
    private ActionResult? ForbiddenIfNotOwner(int requestedUserId)
    {
        var currentId = GetCurrentUserId();
        return currentId != requestedUserId ? Forbid() : null;
    }

    // ─── endpoints ──────────────────────────────────────────────────────────────

    /// <summary>GET conversation between two users. Current user must be userId1 or userId2.</summary>
    [HttpGet(ApiActionsV1.GetConversation, Name = nameof(ApiActionsV1.GetConversation))]
    public async Task<ActionResult<ConversationMessagesDto>> GetConversation(
        [FromQuery] int userId1,
        [FromQuery] int userId2,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageSize > 100) pageSize = 100;

        var currentId = GetCurrentUserId();
        if (currentId != userId1 && currentId != userId2)
            return Forbid();

        var conversation = await _messageService.GetConversationAsync(userId1, userId2, page, pageSize);
        return Ok(conversation);
    }

    /// <summary>GET all conversations for the given userId — must match the authenticated user.</summary>
    [HttpGet(ApiActionsV1.GetUserConversations, Name = nameof(ApiActionsV1.GetUserConversations))]
    public async Task<ActionResult<List<ConversationDto>>> GetUserConversations([FromRoute] int userId)
    {
        var guard = ForbiddenIfNotOwner(userId);
        if (guard != null) return guard;
        var conversations = await _messageService.GetUserConversationsAsync(userId);
        return Ok(conversations);
    }

    /// <summary>POST send a message. SenderId must match the authenticated user.</summary>
    [HttpPost(ApiActionsV1.SendMessage, Name = nameof(ApiActionsV1.SendMessage))]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageInputDto input)
    {
        var currentId = GetCurrentUserId();
        if (input.SenderId != currentId)
            return Forbid();

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrEmpty(idempotencyKey) && _idempotencyService.IsDuplicate($"msg:{currentId}:{idempotencyKey}"))
            return Conflict(new { message = "Duplicate request." });

        var message = await _messageService.SendMessageAsync(
            input.SenderId, input.ReceiverId, input.MessageText, input.IsSuperLike);


        _ = _analyticsService.TrackEventAsync(
            "MessageSent", "Communication",
            entityId: input.ReceiverId, entityType: "User",
            userId: currentId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

        return Ok(message);
    }

    /// <summary>PUT mark a message as read.</summary>
    [HttpPut(ApiActionsV1.MarkMessageAsRead, Name = nameof(ApiActionsV1.MarkMessageAsRead))]
    public async Task<IActionResult> MarkAsRead([FromRoute] int messageId)
    {
        await _messageService.MarkAsReadAsync(messageId);
        return Ok();
    }

    /// <summary>GET unread count — userId in route must match the authenticated user.</summary>
    [HttpGet(ApiActionsV1.GetUnreadCount, Name = nameof(ApiActionsV1.GetUnreadCount))]
    public async Task<ActionResult<int>> GetUnreadCount([FromRoute] int userId)
    {
        var guard = ForbiddenIfNotOwner(userId);
        if (guard != null) return guard;
        var count = await _messageService.GetUnreadCountAsync(userId);
        return Ok(count);
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
        var messages = await _messageService.SearchMessagesAsync(userId, query);
        return Ok(messages);
    }

    [HttpPost("report")]
    public async Task<IActionResult> ReportAbuse([FromBody] ReportAbuseRequestDto dto)
    {
        var currentId = GetCurrentUserId();
        if (dto.UserId != currentId)
            return Forbid();

        var reportDto = new ReportMessageDto
        {
            ReportedUserId = dto.ReportedUserId,
            MessageId = dto.MessageId,
            Reason = dto.Reason
        };
        await _messageService.ReportAbuseAsync(dto.UserId, reportDto);
        return Ok();
    }
}
