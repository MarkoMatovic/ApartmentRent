namespace Lander.src.Modules.Communication.Dtos.Dto;

public class MessageDto
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    public string? SenderName { get; set; }
    public string? ReceiverName { get; set; }
    public string? SenderProfilePicture { get; set; }
    public string? ReceiverProfilePicture { get; set; }
}

public class ConversationDto
{
    public int OtherUserId { get; set; }
    public string? OtherUserName { get; set; }
    public string? OtherUserProfilePicture { get; set; }
    public MessageDto? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}

public class ConversationMessagesDto
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<MessageDto> Messages { get; set; } = new();
}
