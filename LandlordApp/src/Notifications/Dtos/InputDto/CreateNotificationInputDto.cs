using System.ComponentModel.DataAnnotations;
namespace Lander.src.Notifications.Dtos.InputDto;
public class CreateNotificationInputDto
{   
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string ActionType { get; set; } = null!;
    public string ActionTarget { get; set; } = null!;
    public Guid CreatedByGuid { get; set; } 
    public int SenderUserId { get; set; } 
    public int RecipientUserId { get; set; } 
}
