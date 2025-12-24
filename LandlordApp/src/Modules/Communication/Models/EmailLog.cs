namespace Lander.src.Modules.Communication.Models;

public partial class EmailLog
{
    public int EmailLogId { get; set; }
    public int? UserId { get; set; }
    public string RecipientEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string? HtmlContent { get; set; }
    public string? TemplateId { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsDelivered { get; set; }
    public string? SendGridMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? CreatedByGuid { get; set; }
    public DateTime? CreatedDate { get; set; }
}
