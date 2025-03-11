using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Interfaces.UserInterface;

public interface IUserInterface
{
    Task<UserRegistrationDto> RegisterUserAsync(UserRegistrationInputDto userRegistrationInputDto);
    Task<string> LoginUserAsync(LoginUserInputDto userRegistrationInputDto);
}
