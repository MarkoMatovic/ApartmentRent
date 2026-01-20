using System.ComponentModel.DataAnnotations;
namespace Lander.src.Notifications.Dtos.InputDto;
public class CreateNotificationInputDto
{   
    public string Title { get; set; }
    public string Message { get; set; }
    public string ActionType { get; set; }
    public string ActionTarget { get; set; }
    public Guid CreatedByGuid { get; set; } 
    public int SenderUserId { get; set; } 
    public int RecipientUserId { get; set; } 
}
