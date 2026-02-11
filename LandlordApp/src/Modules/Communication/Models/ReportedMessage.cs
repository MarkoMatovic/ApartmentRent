namespace Lander.src.Modules.Communication.Models;

public class ReportedMessage
{
    public int ReportId { get; set; }
    public int MessageId { get; set; }
    public int ReportedByUserId { get; set; }
    public int ReportedUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Reviewed, Resolved
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? AdminNotes { get; set; }
    public Guid? ModifiedByGuid { get; set; }
    public DateTime? ModifiedDate { get; set; }

    // Navigation properties (only within same schema)
    public virtual Message? Message { get; set; }
}

