namespace Lander.src.Modules.Communication.Dtos.InputDto;

/// <summary>Start (or open) the group made of the caller, their roommate and <see cref="OtherUserId"/> (usually the landlord).</summary>
public class OpenChatGroupInputDto
{
    public int OtherUserId { get; set; }
}

/// <summary>The sender is always the authenticated user; never part of the body.</summary>
public class SendGroupMessageInputDto
{
    public string MessageText { get; set; } = string.Empty;
}
