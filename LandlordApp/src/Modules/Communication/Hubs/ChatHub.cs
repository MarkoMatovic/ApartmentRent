using Lander.src.Modules.Communication.Intefaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lander.src.Modules.Communication.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;

    public ChatHub(IMessageService messageService)
    {
        _messageService = messageService;
    }

    public async Task JoinChatRoom(int userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task SendMessage(int senderId, int receiverId, string messageText)
    {
        var message = await _messageService.SendMessageAsync(senderId, receiverId, messageText);
        
        // Notify receiver
        await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", new
        {
            message.MessageId,
            message.SenderId,
            message.ReceiverId,
            message.MessageText,
            message.SentAt,
            message.IsRead,
            message.SenderName,
            message.SenderProfilePicture
        });

        // Confirm to sender
        await Clients.Group($"user_{senderId}").SendAsync("MessageSent", new
        {
            message.MessageId,
            message.SenderId,
            message.ReceiverId,
            message.MessageText,
            message.SentAt,
            message.IsRead,
            message.ReceiverName,
            message.ReceiverProfilePicture
        });
    }

    public async Task MarkMessageAsRead(int messageId)
    {
        await _messageService.MarkAsReadAsync(messageId);
        
        var message = await _messageService.GetMessageByIdAsync(messageId);
        if (message != null)
        {
            // Notify sender that message was read
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
