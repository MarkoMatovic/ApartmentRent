using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Interfaces.UserInterface;

public interface IAuthService
{
    Task<UserRegistrationDto> RegisterUserAsync(UserRegistrationInputDto dto);
    Task<AuthTokenDto?> LoginUserAsync(LoginUserInputDto dto);
    Task LogoutUserAsync(string? rawRefreshToken = null);
}
