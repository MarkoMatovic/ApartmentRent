namespace Lander.src.Modules.Users.Dtos.InputDto;

/// <summary>The second step of signing in: the token from the password step and the app's code.</summary>
public class TwoFactorLoginInputDto
{
    public string TwoFactorToken { get; set; } = string.Empty;
    /// <summary>Six digits from the authenticator app, or one recovery code.</summary>
    public string Code { get; set; } = string.Empty;
}

public class TwoFactorCodeInputDto
{
    public string Code { get; set; } = string.Empty;
}
