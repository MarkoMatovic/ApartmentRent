namespace Lander.src.Modules.Communication.Dtos.InputDto;

/// <summary>
/// Input DTO for sending a message. The sender is always derived from the authenticated
/// user's JWT claims — do NOT include a SenderId in the request body.
/// </summary>
public class SendMessageInputDto
{
    public int ReceiverId { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public bool IsSuperLike { get; set; } = false;
}
