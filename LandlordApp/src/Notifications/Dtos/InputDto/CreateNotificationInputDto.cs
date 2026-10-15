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

    /// <summary>
    /// Translation key of the notification (e.g. "pairing_accepted"). It is stored next to Title and Message, which stay
    /// as the Serbian fallback; the browser shows its own translation of the key in the user's language.
    /// </summary>
    public string? Key { get; set; }

    /// <summary>Values for the translated text (e.g. name, title). Stored with the key.</summary>
    public Dictionary<string, string>? Params { get; set; }

    /// <summary>Kind of the live push: "info" (default), "success", "warning", "error" or "rejection". Not stored.</summary>
    public string? Type { get; set; }

    /// <summary>JSON the live push carries as its 4th argument (the rejection dialog reads it). Defaults to the target. Not stored.</summary>
    public string? Metadata { get; set; }
}
