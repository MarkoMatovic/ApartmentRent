using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Interfaces.UserInterface;

public interface IAuthService
{
    Task<UserRegistrationDto> RegisterUserAsync(UserRegistrationInputDto dto);
    /// <summary>Null for wrong credentials; with two-step sign-in only a challenge for the code step.</summary>
    Task<AuthTokenDto?> LoginUserAsync(LoginUserInputDto dto);
    /// <summary>The code step of two-step sign-in. Throws a business rule error for a wrong or late code.</summary>
    Task<AuthTokenDto> CompleteTwoFactorLoginAsync(string twoFactorToken, string code);
    /// <summary>Signs out this device: the session of the refresh token and the current access token.</summary>
    Task LogoutUserAsync(string? rawRefreshToken = null);
    /// <summary>Signs the current user out of every device.</summary>
    Task LogoutEverywhereAsync();
}
