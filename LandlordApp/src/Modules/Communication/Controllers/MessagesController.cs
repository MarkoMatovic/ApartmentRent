using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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

    private int GetCurrentUserId()
    {
        var claim = User.FindFirstValue("userId");
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            throw new UnauthorizedAccessException();
        return id;
    }

    private ActionResult? ForbiddenIfNotOwner(int requestedUserId)
    {
        var currentId = GetCurrentUserId();
        return currentId != requestedUserId ? Forbid() : null;
    }

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

    [HttpPost(ApiActionsV1.UploadMessageFile, Name = nameof(ApiActionsV1.UploadMessageFile))]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting("auth")]
    [Microsoft.AspNetCore.Http.Timeouts.RequestTimeout(60_000)]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var userId = GetCurrentUserId();
        try
        {
            var relativeUrl = await _messageService.UploadFileAsync(file, userId);
            return Ok(new
            {
                fileUrl = relativeUrl,
                fileName = file.FileName,
                fileSize = file.Length,
                fileType = file.ContentType
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost(ApiActionsV1.SendMessage, Name = nameof(ApiActionsV1.SendMessage))]
    [EnableRateLimiting("messages-send")]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageInputDto input)
    {
        var senderId = GetCurrentUserId();

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        var message = await _messageService.SendMessageAsync(
            senderId, input.ReceiverId, input.MessageText, input.IsSuperLike,
            idempotencyKey: idempotencyKey,
            fileUrl: input.FileUrl, fileName: input.FileName,
            fileSize: input.FileSize, fileType: input.FileType);

        if (message is null)
            return Conflict(new { message = "Duplicate request." });

        _ = _analyticsService.TrackEventAsync(
            "MessageSent", "Communication",
            entityId: input.ReceiverId, entityType: "User",
            userId: senderId,
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

    [HttpGet(ApiActionsV1.DownloadMessageFile, Name = nameof(ApiActionsV1.DownloadMessageFile))]
    public async Task<IActionResult> DownloadFile([FromRoute] string filename)
    {
        var userId = GetCurrentUserId();

        if (!await _messageService.IsFileAccessibleAsync(filename, userId))
            return Forbid();

        var filePath = Path.Combine(
            HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().ContentRootPath,
            "chat-files",
            filename);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        // Derive content type from extension; default to octet-stream
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".pdf"            => "application/pdf",
            _                 => "application/octet-stream"
        };

        return PhysicalFile(filePath, contentType, enableRangeProcessing: false);
    }

    [HttpPost(ApiActionsV1.ArchiveConversation, Name = nameof(ApiActionsV1.ArchiveConversation))]
    public async Task<IActionResult> ArchiveConversation([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.ArchiveConversationAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpPost(ApiActionsV1.UnarchiveConversation, Name = nameof(ApiActionsV1.UnarchiveConversation))]
    public async Task<IActionResult> UnarchiveConversation([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.UnarchiveConversationAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpPost(ApiActionsV1.MuteConversation, Name = nameof(ApiActionsV1.MuteConversation))]
    public async Task<IActionResult> MuteConversation([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.MuteConversationAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpPost(ApiActionsV1.UnmuteConversation, Name = nameof(ApiActionsV1.UnmuteConversation))]
    public async Task<IActionResult> UnmuteConversation([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.UnmuteConversationAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpPost(ApiActionsV1.BlockUser, Name = nameof(ApiActionsV1.BlockUser))]
    public async Task<IActionResult> BlockUser([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.BlockUserAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpPost(ApiActionsV1.UnblockUser, Name = nameof(ApiActionsV1.UnblockUser))]
    public async Task<IActionResult> UnblockUser([FromBody] ChatActionDto dto)
    {
        var guard = ForbiddenIfNotOwner(dto.UserId);
        if (guard != null) return guard;
        await _messageService.UnblockUserAsync(dto.UserId, dto.OtherUserId);
        return Ok();
    }

    [HttpDelete(ApiActionsV1.DeleteConversation, Name = nameof(ApiActionsV1.DeleteConversation))]
    public async Task<IActionResult> DeleteConversation([FromQuery] int otherUserId)
    {
        var userId = GetCurrentUserId();
        await _messageService.DeleteConversationAsync(userId, otherUserId);
        return Ok();
    }

    [HttpGet(ApiActionsV1.SearchMessages, Name = nameof(ApiActionsV1.SearchMessages))]
    public async Task<ActionResult<List<MessageDto>>> SearchMessages([FromQuery] string query)
    {
        var userId = GetCurrentUserId();
        return Ok(await _messageService.SearchMessagesAsync(userId, query));
    }

    [HttpPost(ApiActionsV1.ReportAbuse, Name = nameof(ApiActionsV1.ReportAbuse))]
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
