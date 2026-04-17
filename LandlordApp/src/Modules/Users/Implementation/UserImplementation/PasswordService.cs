using System.Security.Claims;
using Lander.src.Common.Exceptions;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

public class PasswordService : IPasswordService
{
    private readonly UsersContext _context;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordService> _logger;
    private readonly TimeProvider _timeProvider;

    public PasswordService(
        UsersContext context,
        IPasswordHashingService passwordHashingService,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<PasswordService> logger,
        TimeProvider timeProvider)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task ChangePasswordAsync(ChangePasswordInputDto dto)
    {
        var userGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == Guid.Parse(userGuid!));
        if (user == null || !_passwordHashingService.Verify(dto.OldPassword, user.Password))
            throw new InvalidOperationException("Incorrect old password.");

        var transaction = await _context.BeginTransactionAsync();
        try
        {
            user.Password = _passwordHashingService.Hash(dto.NewPassword);
            user.ModifiedDate = _timeProvider.GetUtcNow().UtcDateTime;
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChangePasswordAsync");
            _context.RollBackTransaction();
            throw;
        }
    }

    public async Task SendVerificationEmailAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) throw new NotFoundException("User", userId);

        var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                           .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        user.EmailVerificationToken = token;
        user.ModifiedDate = _timeProvider.GetUtcNow().UtcDateTime;
        await _context.SaveEntitiesAsync();

        var frontendBaseUrl = _configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";
        var verificationLink = $"{frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(token)}";
        var userName = $"{user.FirstName} {user.LastName}";
        FireAndForget(_emailService.SendEmailVerificationAsync(user.Email, userName, verificationLink), "SendEmailVerification");
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        if (user == null) return false;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        user.IsActive = true;
        user.EmailVerifiedAt = now;
        user.EmailVerificationToken = null;
        user.ModifiedDate = now;
        await _context.SaveEntitiesAsync();
        return true;
    }

    public async Task SendPasswordResetEmailAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return; // Don't reveal if email exists

        var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                           .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = now.AddHours(2);
        user.ModifiedDate = now;
        await _context.SaveEntitiesAsync();

        var frontendBaseUrl = _configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        var userName = $"{user.FirstName} {user.LastName}";
        FireAndForget(_emailService.SendPasswordResetEmailAsync(user.Email, userName, resetLink), "SendPasswordResetEmail");
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token &&
            u.PasswordResetTokenExpiry > now);
        if (user == null) return false;

        user.Password = _passwordHashingService.Hash(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.ModifiedDate = now;
        await _context.SaveEntitiesAsync();
        return true;
    }

    private void FireAndForget(Task task, string operation)
    {
        task.ContinueWith(
            t => _logger.LogError(t.Exception, "Background task failed: {Operation}", operation),
            TaskContinuationOptions.OnlyOnFaulted);
    }
}
