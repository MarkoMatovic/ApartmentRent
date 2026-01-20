using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
namespace Lander.src.Modules.Communication.Models;
public partial class Message
{
    public int MessageId { get; set; }
    public int? SenderId { get; set; }
    public int? ReceiverId { get; set; }
    public string MessageText { get; set; } = null!;
    public DateTime? SentAt { get; set; }
    public bool? IsRead { get; set; }
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public virtual User? Receiver { get; set; }
    public virtual User? Sender { get; set; }
}
