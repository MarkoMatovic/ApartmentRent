namespace Lander.src.Modules.Communication.Models;

public class ConversationSettings
{
    public int SettingId { get; set; }
    public int UserId { get; set; }
    public int OtherUserId { get; set; }
    public bool IsArchived { get; set; }
    public bool IsMuted { get; set; }
    public bool IsBlocked { get; set; }
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
