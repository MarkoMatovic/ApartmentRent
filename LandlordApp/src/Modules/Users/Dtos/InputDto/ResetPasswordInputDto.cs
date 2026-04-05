namespace Lander.src.Modules.Users.Dtos.InputDto;
public class ResetPasswordInputDto
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
