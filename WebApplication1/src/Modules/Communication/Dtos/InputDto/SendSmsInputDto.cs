namespace Lander.src.Modules.Communication.Dtos.InputDto;

public class SendSmsInputDto
{
    public string ToPhoneNumber { get; set; }
    public string MessageText { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
}
