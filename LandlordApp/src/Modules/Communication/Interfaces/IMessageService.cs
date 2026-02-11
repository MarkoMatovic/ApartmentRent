using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Microsoft.AspNetCore.Http;
namespace Lander.src.Modules.Communication.Intefaces;
public interface IMessageService
{
    Task<MessageDto> SendMessageAsync(int senderId, int receiverId, string messageText);
    Task<ConversationMessagesDto> GetConversationAsync(int userId1, int userId2, int page = 1, int pageSize = 50);
    Task<List<ConversationDto>> GetUserConversationsAsync(int userId);
    Task MarkAsReadAsync(int messageId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<MessageDto?> GetMessageByIdAsync(int messageId);
    
    // Conversation settings
    Task ArchiveConversationAsync(int userId, int otherUserId);
    Task UnarchiveConversationAsync(int userId, int otherUserId);
    Task MuteConversationAsync(int userId, int otherUserId);
    Task UnmuteConversationAsync(int userId, int otherUserId);
    Task BlockUserAsync(int userId, int blockedUserId);
    Task UnblockUserAsync(int userId, int blockedUserId);
    Task<bool> IsUserBlockedAsync(int userId, int otherUserId);
    
    // Conversation management
    Task DeleteConversationAsync(int userId, int otherUserId);
    
    // File upload
    Task<string> UploadFileAsync(IFormFile file, int userId);
    
    // Search
    Task<List<MessageDto>> SearchMessagesAsync(int userId, string query);
    
    // Report abuse
    Task ReportAbuseAsync(int reportedByUserId, ReportMessageDto reportDto);
}
