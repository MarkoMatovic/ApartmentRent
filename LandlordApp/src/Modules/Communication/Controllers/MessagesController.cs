using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Intefaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Lander.src.Modules.Communication.Controllers;
[Route(ApiActionsV1.Messages)]
[ApiController]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly Lander.src.Modules.Analytics.Interfaces.IAnalyticsService _analyticsService;
    public MessagesController(IMessageService messageService, Lander.src.Modules.Analytics.Interfaces.IAnalyticsService analyticsService)
    {
        _messageService = messageService;
        _analyticsService = analyticsService;
    }
    [HttpGet(ApiActionsV1.GetConversation, Name = nameof(ApiActionsV1.GetConversation))]
    public async Task<ActionResult<ConversationMessagesDto>> GetConversation(
        [FromQuery] int userId1, 
        [FromQuery] int userId2, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var conversation = await _messageService.GetConversationAsync(userId1, userId2, page, pageSize);
        return Ok(conversation);
    }
    [HttpGet(ApiActionsV1.GetUserConversations, Name = nameof(ApiActionsV1.GetUserConversations))]
    public async Task<ActionResult<List<ConversationDto>>> GetUserConversations([FromRoute] int userId)
    {
        var conversations = await _messageService.GetUserConversationsAsync(userId);
        return Ok(conversations);
    }
    [HttpPost(ApiActionsV1.SendMessage, Name = nameof(ApiActionsV1.SendMessage))]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageInputDto input)
    {
        var message = await _messageService.SendMessageAsync(input.SenderId, input.ReceiverId, input.MessageText);
        
        // Track MessageSent analytics event
        _ = _analyticsService.TrackEventAsync(
            "MessageSent",
            "Communication",
            entityId: input.ReceiverId,
            entityType: "User",
            userId: input.SenderId,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers["User-Agent"].ToString()
        );
        
        return Ok(message);
    }
    [HttpPut(ApiActionsV1.MarkMessageAsRead, Name = nameof(ApiActionsV1.MarkMessageAsRead))]
    public async Task<IActionResult> MarkAsRead([FromRoute] int messageId)
    {
        await _messageService.MarkAsReadAsync(messageId);
        return Ok();
    }
    [HttpGet(ApiActionsV1.GetUnreadCount, Name = nameof(ApiActionsV1.GetUnreadCount))]
    public async Task<ActionResult<int>> GetUnreadCount([FromRoute] int userId)
    {
        var count = await _messageService.GetUnreadCountAsync(userId);
        return Ok(count);
    }
}
