using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Interfaces.UserInterface;

public interface IPasswordService
{
    Task ChangePasswordAsync(ChangePasswordInputDto dto);
    Task SendVerificationEmailAsync(int userId);
    Task<bool> VerifyEmailAsync(string token);
    Task SendPasswordResetEmailAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
}
