namespace Lander.src.Modules.Users.Dtos.Dto;

public class AuthTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>The password was right; the account also wants a code from the authenticator app.</summary>
    public bool RequiresTwoFactor { get; set; }
    /// <summary>Hand this to the code step (valid for a few minutes).</summary>
    public string? TwoFactorToken { get; set; }
}
