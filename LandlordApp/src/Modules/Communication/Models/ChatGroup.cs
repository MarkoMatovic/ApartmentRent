namespace Lander.src.Modules.Communication.Models;

/// <summary>
/// A conversation with more than two people: two roommates talking to the same landlord, so the
/// landlord and both roommates see the whole history and nobody has to retell it.
/// </summary>
public class ChatGroup
{
    public int GroupId { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ChatGroupMember> Members { get; set; } = new List<ChatGroupMember>();
    public virtual ICollection<ChatGroupMessage> Messages { get; set; } = new List<ChatGroupMessage>();
}

public class ChatGroupMember
{
    public int GroupId { get; set; }
    public int UserId { get; set; }

    /// <summary>
    /// Id of the newest group message this member has seen. Everything after it (from other people)
    /// is unread - one number instead of a read flag per message and member.
    /// </summary>
    public int LastReadMessageId { get; set; }

    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

    public virtual ChatGroup? Group { get; set; }
}

public class ChatGroupMessage
{
    public int GroupMessageId { get; set; }
    public int GroupId { get; set; }
    public int SenderId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public virtual ChatGroup? Group { get; set; }
}
