namespace Lander.src.Modules.Communication.Dtos.Dto;

public class ReportedMessageDto
{
    public int ReportId { get; set; }
    public int MessageId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public int ReportedByUserId { get; set; }
    public string ReportedByUserName { get; set; } = string.Empty;
    public int ReportedUserId { get; set; }
    public string ReportedUserName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public string? ReviewedByAdminName { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string? AdminNotes { get; set; }
}
