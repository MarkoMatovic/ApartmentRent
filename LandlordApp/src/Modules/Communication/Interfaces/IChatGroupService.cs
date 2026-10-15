using Lander.src.Modules.Communication.Dtos.Dto;

namespace Lander.src.Modules.Communication.Interfaces;

public interface IChatGroupService
{
    /// <summary>
    /// Opens the group of the caller, their roommate and <paramref name="otherUserId"/>, creating it the first
    /// time. The conversation the roommates already had with that person is carried over, so the roommate
    /// who joins sees everything that was said before.
    /// </summary>
    Task<ChatGroupDto> OpenWithRoommateAsync(int userId, int otherUserId);

    Task<List<ChatGroupDto>> GetUserGroupsAsync(int userId);
    Task<ChatGroupMessagesDto> GetMessagesAsync(int groupId, int userId, int page = 1, int pageSize = 50);
    Task<ChatGroupMessageDto> SendMessageAsync(int groupId, int userId, string messageText);
    Task MarkReadAsync(int groupId, int userId);
    Task LeaveAsync(int groupId, int userId);

    /// <summary>Unread group messages across all of the user's groups (added to the inbox badge).</summary>
    Task<int> GetUnreadCountAsync(int userId);
}
