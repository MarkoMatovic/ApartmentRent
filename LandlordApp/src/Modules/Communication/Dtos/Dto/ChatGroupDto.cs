namespace Lander.src.Modules.Communication.Dtos.Dto;

public class ChatGroupMemberDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
}

public class ChatGroupMessageDto
{
    public int MessageId { get; set; }
    public int GroupId { get; set; }
    public int SenderId { get; set; }
    public string? SenderName { get; set; }
    public string? SenderProfilePicture { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class ChatGroupDto
{
    public int GroupId { get; set; }
    /// <summary>Everybody in the group, the signed-in user included.</summary>
    public List<ChatGroupMemberDto> Members { get; set; } = new();
    public ChatGroupMessageDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}

public class ChatGroupMessagesDto
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<ChatGroupMessageDto> Messages { get; set; } = new();
}
