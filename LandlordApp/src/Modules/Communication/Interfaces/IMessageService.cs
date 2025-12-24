using Lander.src.Modules.Communication.Dtos.Dto;

namespace Lander.src.Modules.Communication.Intefaces;

public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(int senderId, int receiverId, string messageText);
    Task<ConversationMessagesDto> GetConversationAsync(int userId1, int userId2, int page = 1, int pageSize = 50);
    Task<List<ConversationDto>> GetUserConversationsAsync(int userId);
    Task MarkAsReadAsync(int messageId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<MessageDto?> GetMessageByIdAsync(int messageId);
}
