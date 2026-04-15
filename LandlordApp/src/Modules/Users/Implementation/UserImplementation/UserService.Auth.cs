using System.Security.Claims;
using Lander.src.Common.Exceptions;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;
public partial class UserService
{
    public async Task<AuthTokenDto?> LoginUserAsync(LoginUserInputDto userRegistrationInputDto)
    {
        User? user = await _context.Users
         .Include(u => u.UserRole)
         .FirstOrDefaultAsync(u => u.Email == userRegistrationInputDto.Email);
        if (user == null)
        {
            return null;
        }

        // Check lockout before verifying password to prevent timing attacks
        if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Login attempt for locked account {Email}", user.Email);
            return null;
        }

        if (!VerifyPassword(userRegistrationInputDto.Password, user.Password))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
                _logger.LogWarning("Account locked: {Email} after {Attempts} failed attempts", user.Email, user.FailedLoginAttempts);
            }
            await _context.SaveEntitiesAsync();
            return null;
        }

        // Successful auth — reset lockout counters
        if (user.FailedLoginAttempts > 0)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            await _context.SaveEntitiesAsync();
        }

        // Gradual migration: re-hash with BCrypt if stored as legacy SHA-256
        if (!user.Password.StartsWith("$2"))
        {
            var transaction = await _context.BeginTransactionAsync();
            try
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(userRegistrationInputDto.Password);
                await _context.SaveEntitiesAsync();
                await _context.CommitTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoginUserAsync");
                _context.RollBackTransaction();
            }
        }

        if (!user.IsActive)
        {
            return null;
        }

        var accessToken = await _tokenProvider.CreateAsync(user);
        var refreshToken = await _refreshTokenService.CreateAsync(user.UserId);
        return new AuthTokenDto { AccessToken = accessToken, RefreshToken = refreshToken };
    }

    public async Task<UserRegistrationDto> RegisterUserAsync(UserRegistrationInputDto userRegistrationInputDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

        // Duplicate email check
        var existingUser = await _context.Users.AnyAsync(u => u.Email == userRegistrationInputDto.Email);
        if (existingUser)
        {
            throw new ConflictException("User with this email already exists.");
        }

        var tenantRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Tenant");
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            if (tenantRole == null)
            {
                tenantRole = new Role
                {
                    RoleName = "Tenant",
                    Description = "Tenant role for users looking for apartments",
                    CreatedDate = DateTime.UtcNow,
                    CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : (Guid?)null
                };
                _context.Roles.Add(tenantRole);
                await _context.SaveEntitiesAsync();
            }
            var user = new User
            {
                FirstName = userRegistrationInputDto.FirstName,
                LastName = userRegistrationInputDto.LastName,
                Email = userRegistrationInputDto.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(userRegistrationInputDto.Password),
                DateOfBirth = userRegistrationInputDto.DateOfBirth,
                PhoneNumber = userRegistrationInputDto.PhoneNumber,
                ProfilePicture = userRegistrationInputDto.ProfilePicture,
                CreatedDate = DateTime.UtcNow,
                CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : (Guid?)null,
                ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : (Guid?)null,
                IsActive = false, // Korisnik mora verifikovati email
                UserRoleId = tenantRole.RoleId // Dodeli rolu "Tenant"
            };
            _context.Users.Add(user);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
            var userName = $"{user.FirstName} {user.LastName}";
            FireAndForget(_emailService.SendWelcomeEmailAsync(user.Email, userName), "SendWelcomeEmail");
            FireAndForget(SendVerificationEmailAsync(user.UserId), "SendVerificationEmail");
            return new UserRegistrationDto
        {
            UserId = user.UserId,
            UserGuid = user.UserGuid.ToString(),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfilePicture = user.ProfilePicture,
            UserRoleId = user.UserRoleId,
            DateOfBirth = user.DateOfBirth,
            IsActive = user.IsActive,
            IsLookingForRoommate = user.IsLookingForRoommate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RegisterUserAsync");
            _context.RollBackTransaction();
            throw;
        }
    }

    public async Task LogoutUserAsync(string? rawRefreshToken = null)
    {
        _httpContextAccessor.HttpContext?.SignOutAsync();
        if (!string.IsNullOrEmpty(rawRefreshToken))
            await _refreshTokenService.RevokeAsync(rawRefreshToken);
    }

    public async Task ChangePasswordAsync(ChangePasswordInputDto changePasswordInputDto)
    {
        var userGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == Guid.Parse(userGuid));
        if (user == null || !VerifyPassword(changePasswordInputDto.OldPassword, user.Password))
            throw new InvalidOperationException("Incorrect old password.");
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(changePasswordInputDto.NewPassword);
            user.ModifiedDate = DateTime.UtcNow;
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
        user.ModifiedDate = DateTime.UtcNow;
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

        user.IsActive = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        user.EmailVerificationToken = null;
        user.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();
        return true;
    }

    public async Task SendPasswordResetEmailAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return; // Don't reveal if email exists

        var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                           .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(2);
        user.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();

        var frontendBaseUrl = _configuration["App:FrontendBaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        var userName = $"{user.FirstName} {user.LastName}";
        FireAndForget(_emailService.SendPasswordResetEmailAsync(user.Email, userName, resetLink), "SendPasswordResetEmail");
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow);
        if (user == null) return false;

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();
        return true;
    }

    private void FireAndForget(Task task, string operation)
    {
        task.ContinueWith(
            t => _logger.LogError(t.Exception, "Background task failed: {Operation}", operation),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private static bool VerifyPassword(string plainText, string storedHash)
    {
        // BCrypt hashes start with "$2"
        if (storedHash.StartsWith("$2"))
            return BCrypt.Net.BCrypt.Verify(plainText, storedHash);

        // Legacy SHA-256 fallback (migration path)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var legacyHash = Convert.ToBase64String(
            sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(plainText)));
        return legacyHash == storedHash;
    }
}
