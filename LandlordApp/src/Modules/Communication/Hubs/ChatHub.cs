using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
namespace Lander.src.Modules.Communication.Hubs;
[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    public ChatHub(IMessageService messageService)
    {
        _messageService = messageService;
    }
    private int GetCurrentUserId()
    {
        var claim = Context.User?.FindFirstValue("userId");
        if (string.IsNullOrEmpty(claim) || !int.TryParse(claim, out var id))
            throw new HubException("Unauthorized");
        return id;
    }
    public async Task JoinChatRoom(int userId)
    {
        var callerId = GetCurrentUserId();
        if (callerId != userId) throw new HubException("Unauthorized");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }
    public async Task SendMessage(int receiverId, string messageText)
    {
        var senderId = GetCurrentUserId();
        MessageDto? message;
        try
        {
            message = await _messageService.SendMessageAsync(senderId, receiverId, messageText);
        }
        catch (InvalidOperationException ex)
        {
            throw new HubException(ex.Message);
        }
        if (message is null) return;
        await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", new
        {
            message.MessageId,
            message.SenderId,
            message.ReceiverId,
            message.MessageText,
            message.SentAt,
            message.IsRead,
            message.SenderName,
            message.SenderProfilePicture,
            message.FileUrl,
            message.FileName,
            message.FileSize,
            message.FileType,
            message.IsSuperLike
        });
        await Clients.Group($"user_{senderId}").SendAsync("MessageSent", new
        {
            message.MessageId,
            message.SenderId,
            message.ReceiverId,
            message.MessageText,
            message.SentAt,
            message.IsRead,
            message.ReceiverName,
            message.ReceiverProfilePicture,
            message.FileUrl,
            message.FileName,
            message.FileSize,
            message.FileType,
            message.IsSuperLike
        });
    }
    public async Task MarkMessageAsRead(int messageId)
    {
        await _messageService.MarkAsReadAsync(messageId);
        var message = await _messageService.GetMessageByIdAsync(messageId);
        if (message != null)
        {
            await Clients.Group($"user_{message.SenderId}").SendAsync("MessageRead", new
            {
                MessageId = messageId,
                ReadAt = DateTime.UtcNow
            });
        }
    }
    public async Task UserTyping(int userId, int receiverId)
    {
        await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", new { userId });
    }
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
