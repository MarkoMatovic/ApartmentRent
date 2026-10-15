namespace Lander.src.Notifications.Models;
public class ReadNotification
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public string ActionTarget { get; set; } = null!;
    /// <summary>Translation key of the notification (e.g. "pairing_accepted"); null for older ones.</summary>
    public string? Key { get; set; }
    /// <summary>JSON object with the values of the translated text (name, title...).</summary>
    public string? ParamsJson { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid CreatedByGuid { get; set; }
    public int SenderUserId { get; set; }
    public int RecipientUserId { get; set; }
}
