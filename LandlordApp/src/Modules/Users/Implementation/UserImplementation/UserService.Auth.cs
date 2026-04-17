using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

public partial class UserService
{
    public Task<AuthTokenDto?> LoginUserAsync(LoginUserInputDto dto)
        => _authService.LoginUserAsync(dto);

    public Task<UserRegistrationDto> RegisterUserAsync(UserRegistrationInputDto dto)
        => _authService.RegisterUserAsync(dto);

    public Task LogoutUserAsync(string? rawRefreshToken = null)
        => _authService.LogoutUserAsync(rawRefreshToken);

    public Task ChangePasswordAsync(ChangePasswordInputDto dto)
        => _passwordService.ChangePasswordAsync(dto);

    public Task SendVerificationEmailAsync(int userId)
        => _passwordService.SendVerificationEmailAsync(userId);

    public Task<bool> VerifyEmailAsync(string token)
        => _passwordService.VerifyEmailAsync(token);

    public Task SendPasswordResetEmailAsync(string email)
        => _passwordService.SendPasswordResetEmailAsync(email);

    public Task<bool> ResetPasswordAsync(string token, string newPassword)
        => _passwordService.ResetPasswordAsync(token, newPassword);
}
