namespace Lander.src.Modules.Communication.Dtos.InputDto;

public class ReportAbuseRequestDto
{
    public int UserId { get; set; }
    public int ReportedUserId { get; set; }
    public int MessageId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
