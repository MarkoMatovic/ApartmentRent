namespace Lander.src.Notifications.Models;

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string ActionType { get; set; }
    public string ActionTarget { get; set; } 
    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; }
    public Guid CreatedByGuid { get; set; } 
    public int SenderUserId { get; set; }
    public int RecipientUserId { get; set; }
}
