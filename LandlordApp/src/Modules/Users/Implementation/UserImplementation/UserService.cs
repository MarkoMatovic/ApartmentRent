using System.Security.Claims;
using Lander.Helpers;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Roommates.Interfaces;
namespace Lander.src.Modules.Users.Implementation.UserImplementation;
public class UserService : IUserInterface
{
    private readonly UsersContext _context;
    private readonly ReviewsContext _reviewsContext;
    private readonly TokenProvider _tokenProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IEmailService _emailService;
    private readonly IApartmentService _apartmentService;
    private readonly IRoommateService _roommateService;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly ILogger<UserService> _logger;
    private readonly IConfiguration _configuration;
    public UserService(
        UsersContext context,
        ReviewsContext reviewsContext,
        TokenProvider tokenProvider,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        IApartmentService apartmentService,
        IRoommateService roommateService,
        RefreshTokenService refreshTokenService,
        ILogger<UserService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _reviewsContext = reviewsContext;
        _tokenProvider = tokenProvider;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _apartmentService = apartmentService;
        _roommateService = roommateService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
        _configuration = configuration;
    }
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
            throw new Exception("User with this email already exists.");
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
    public async Task DeactivateUserAsync(DeactivateUserInputDto deactivateUserInputDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == deactivateUserInputDto.UserGuid);
        if (user != null)
        {
            var transaction = await _context.BeginTransactionAsync();
            try
            {
                user.IsActive = false;
                await _context.SaveEntitiesAsync();
                await _context.CommitTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeactivateUserAsync");
                _context.RollBackTransaction();
                throw;
            }
        }
    }
    public async Task ReactivateUserAsync(ReactivateUserInputDto reactivateUserInputDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == reactivateUserInputDto.UserGuid);
        if (user != null)
        {
            var transaction = await _context.BeginTransactionAsync();
            try
            {
                user.IsActive = true;
                await _context.SaveEntitiesAsync();
                await _context.CommitTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReactivateUserAsync");
                _context.RollBackTransaction();
                throw;
            }
        }
    }
    public async Task<bool> DeleteUserAsync(DeleteUserInputDto deleteUserInputDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == deleteUserInputDto.UserGuid);
        if (user != null)
        {
            // GDPR Cleanup: Delete related data first
            await _apartmentService.DeleteApartmentsByLandlordIdAsync(user.UserId);
            await _roommateService.DeleteRoommateByUserIdAsync(user.UserId);

            var transaction = await _context.BeginTransactionAsync();
            try
            {
                _context.Users.Remove(user);
                await _context.SaveEntitiesAsync();
                await _context.CommitTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteUserAsync");
                _context.RollBackTransaction();
                throw;
            }
        }
        return true;
    }
    public async  Task ChangePasswordAsync(ChangePasswordInputDto changePasswordInputDto)
    {
        var userGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == Guid.Parse(userGuid));
        if (user == null || !VerifyPassword(changePasswordInputDto.OldPassword, user.Password))
            throw new Exception("Incorrect old password.");
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
    public async Task<User?> GetUserByGuidAsync(Guid userGuid)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserGuid == userGuid);
    }
    public async Task UpdateRoommateStatusAsync(UpdateRoommateStatusInputDto updateRoommateStatusInputDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == updateRoommateStatusInputDto.UserGuid);
        if (user != null)
        {
            var transaction = await _context.BeginTransactionAsync();
            try
            {
                user.IsLookingForRoommate = updateRoommateStatusInputDto.IsLookingForRoommate;
                user.ModifiedDate = DateTime.UtcNow;
                await _context.SaveEntitiesAsync();
                await _context.CommitTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateRoommateStatusAsync");
                _context.RollBackTransaction();
                throw;
            }
        }
    }
    public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRole)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return null;

        // Calculate ratings from Reviews table
        // User can be reviewed as both landlord and tenant
        var reviewsAsLandlord = await _reviewsContext.Reviews
            .Where(r => r.LandlordId == userId && r.Rating.HasValue)
            .ToListAsync();
        
        var reviewsAsTenant = await _reviewsContext.Reviews
            .Where(r => r.TenantId == userId && r.Rating.HasValue)
            .ToListAsync();

        var allReviews = reviewsAsLandlord.Concat(reviewsAsTenant).ToList();
        var averageRating = allReviews.Any() 
            ? (decimal?)allReviews.Average(r => r.Rating!.Value) 
            : null;
        var reviewCount = allReviews.Count;

        return new UserProfileDto
        {
            UserId = user.UserId,
            UserGuid = user.UserGuid,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfilePicture = user.ProfilePicture,
            DateOfBirth = user.DateOfBirth,
            IsActive = user.IsActive,
            IsLookingForRoommate = user.IsLookingForRoommate,
            AnalyticsConsent = user.AnalyticsConsent,
            ChatHistoryConsent = user.ChatHistoryConsent,
            ProfileVisibility = user.ProfileVisibility,
            IsIncognito = user.IsIncognito,
            TokenBalance = user.TokenBalance,
            UserRoleId = user.UserRoleId,
            RoleName = user.UserRole?.RoleName,
            CreatedDate = user.CreatedDate,
            AverageRating = averageRating,
            ReviewCount = reviewCount,
        };
    }
    public async Task<UserProfileDto> UpdateUserProfileAsync(int userId, UserProfileUpdateInputDto updateDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }
        if (updateDto.FirstName != null) user.FirstName = updateDto.FirstName;
        if (updateDto.LastName != null) user.LastName = updateDto.LastName;
        if (updateDto.Email != null) user.Email = updateDto.Email;
        if (updateDto.PhoneNumber != null) user.PhoneNumber = updateDto.PhoneNumber;
        if (updateDto.ProfilePicture != null) user.ProfilePicture = updateDto.ProfilePicture;
        if (updateDto.DateOfBirth.HasValue) user.DateOfBirth = updateDto.DateOfBirth.Value;
        user.ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : null;
        user.ModifiedDate = DateTime.UtcNow;
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateUserProfileAsync");
            _context.RollBackTransaction();
            throw;
        }
        return new UserProfileDto
        {
            UserId = user.UserId,
            UserGuid = user.UserGuid,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfilePicture = user.ProfilePicture,
            DateOfBirth = user.DateOfBirth,
            IsActive = user.IsActive,
            IsLookingForRoommate = user.IsLookingForRoommate,
            AnalyticsConsent = user.AnalyticsConsent,
            ChatHistoryConsent = user.ChatHistoryConsent,
            ProfileVisibility = user.ProfileVisibility,
            IsIncognito = user.IsIncognito,
            TokenBalance = user.TokenBalance,
            UserRoleId = user.UserRoleId,
            CreatedDate = user.CreatedDate,
        };
    }


    public async Task<UserProfileDto> UpdatePrivacySettingsAsync(int userId, PrivacySettingsDto privacySettingsDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        user.AnalyticsConsent = privacySettingsDto.AnalyticsConsent;
        user.ChatHistoryConsent = privacySettingsDto.ChatHistoryConsent;
        user.ProfileVisibility = privacySettingsDto.ProfileVisibility;
        user.IsIncognito = privacySettingsDto.IsIncognito;
        user.ModifiedDate = DateTime.UtcNow;

        var transaction = await _context.BeginTransactionAsync();
        try
        {
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdatePrivacySettingsAsync");
            _context.RollBackTransaction();
            throw;
        }

        return new UserProfileDto
        {
            UserId = user.UserId,
            UserGuid = user.UserGuid,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            ProfilePicture = user.ProfilePicture,
            DateOfBirth = user.DateOfBirth,
            IsActive = user.IsActive,
            IsLookingForRoommate = user.IsLookingForRoommate,
            AnalyticsConsent = user.AnalyticsConsent,
            ChatHistoryConsent = user.ChatHistoryConsent,
            ProfileVisibility = user.ProfileVisibility,
            IsIncognito = user.IsIncognito,
            TokenBalance = user.TokenBalance,
            UserRoleId = user.UserRoleId,
            CreatedDate = user.CreatedDate,
        };
    }


    public async Task<UserExportDto> ExportUserDataAsync(int userId)
    {
        var userProfile = await GetUserProfileAsync(userId);
        if (userProfile == null) throw new Exception("User not found");

        var apartments = await _apartmentService.GetApartmentsByLandlordIdAsync(userId);
        var roommateProfile = await _roommateService.GetRoommateByUserIdAsync(userId);

        return new UserExportDto
        {
            UserProfile = userProfile,
            ListedApartments = apartments,
            RoommateProfile = roommateProfile,
            ExportedAt = DateTime.UtcNow
        };
    }

    public async Task UpgradeUserRoleAsync(int userId, string targetRoleName)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        var targetRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == targetRoleName);
        if (targetRole == null)
        {
            throw new Exception($"Role '{targetRoleName}' not found");
        }

        user.UserRoleId = targetRole.RoleId;
        user.ModifiedDate = DateTime.UtcNow;

        await _context.SaveEntitiesAsync();
    }

    public async Task SendVerificationEmailAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) throw new Exception("User not found");

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
}
