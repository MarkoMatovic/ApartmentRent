namespace Lander.src.Modules.Communication.Dtos.InputDto;

public class ReportMessageDto
{
    public int MessageId { get; set; }
    public int ReportedUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
