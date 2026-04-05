namespace Lander.src.Modules.Users.Dtos.Dto;

public class AuthTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
