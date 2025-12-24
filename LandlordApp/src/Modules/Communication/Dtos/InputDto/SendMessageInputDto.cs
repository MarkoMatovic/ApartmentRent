namespace Lander.src.Modules.Communication.Dtos.InputDto;

public class SendMessageInputDto
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string MessageText { get; set; } = string.Empty;
}
