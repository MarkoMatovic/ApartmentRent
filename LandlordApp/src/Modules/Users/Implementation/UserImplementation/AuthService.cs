using System.Security.Claims;
using Lander.src.Common.Exceptions;
using Lander.Helpers;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.src.Modules.Communication.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

public class AuthService : IAuthService
{
    private readonly UsersContext _context;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly TokenProvider _tokenProvider;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    private readonly IPasswordService _passwordService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        UsersContext context,
        IPasswordHashingService passwordHashingService,
        TokenProvider tokenProvider,
        RefreshTokenService refreshTokenService,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        IPasswordService passwordService,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        TimeProvider timeProvider)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
        _tokenProvider = tokenProvider;
        _refreshTokenService = refreshTokenService;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _passwordService = passwordService;
        _configuration = configuration;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<AuthTokenDto?> LoginUserAsync(LoginUserInputDto dto)
    {
        var user = await _context.Users
            .Include(u => u.UserRole)
            .FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return null;

        if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > _timeProvider.GetUtcNow().UtcDateTime)
        {
            _logger.LogWarning("Login attempt for locked account {Email}", user.Email);
            return null;
        }

        if (!_passwordHashingService.Verify(dto.Password, user.Password))
        {
            // Atomic increment to avoid lost-update race on concurrent login attempts
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE [users].[Users] SET FailedLoginAttempts = FailedLoginAttempts + 1 WHERE UserId = {0}",
                user.UserId);
            await _context.Entry(user).ReloadAsync();
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutUntil = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(15);
                await _context.SaveEntitiesAsync();
                _logger.LogWarning("Account locked: {Email} after {Attempts} failed attempts",
                    user.Email, user.FailedLoginAttempts);
            }
            return null;
        }

        if (user.FailedLoginAttempts > 0)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            await _context.SaveEntitiesAsync();
        }

        // Gradual migration: re-hash with BCrypt if stored as legacy SHA-256
        if (_passwordHashingService.NeedsRehash(user.Password))
        {
            var transaction = await _context.BeginTransactionAsync();
            try
            {
                user.Password = _passwordHashingService.Hash(dto.Password);
                await _context.SaveEntitiesAsync();
                await _context.CommitTransactionAsync(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rehashing password in LoginUserAsync");
                _context.RollBackTransaction();
            }
        }

        if (!user.IsActive)
            throw new ForbiddenException("EMAIL_NOT_VERIFIED");

        var accessToken = await _tokenProvider.CreateAsync(user);
        var refreshToken = await _refreshTokenService.CreateAsync(user.UserId);
        return new AuthTokenDto { AccessToken = accessToken, RefreshToken = refreshToken };
    }

    public async Task<UserRegistrationDto> RegisterUserAsync(UserRegistrationInputDto dto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

        if (_timeProvider.GetUtcNow().UtcDateTime.AddYears(-18) < dto.DateOfBirth)
            throw new ArgumentException("User must be at least 18 years old.");

        var existingUser = await _context.Users.AnyAsync(u => u.Email == dto.Email);
        if (existingUser)
            throw new ConflictException("User with this email already exists.");

        Guid? callerGuid = Guid.TryParse(currentUserGuid, out var authCg) ? authCg : null;

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
                    CreatedDate = _timeProvider.GetUtcNow().UtcDateTime,
                    CreatedByGuid = callerGuid
                };
                _context.Roles.Add(tenantRole);
                await _context.SaveEntitiesAsync();
            }

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Password = _passwordHashingService.Hash(dto.Password),
                DateOfBirth = dto.DateOfBirth,
                PhoneNumber = dto.PhoneNumber,
                ProfilePicture = dto.ProfilePicture,
                CreatedDate = now,
                CreatedByGuid = callerGuid,
                ModifiedByGuid = callerGuid,
                IsActive = false,
                UserRoleId = tenantRole.RoleId
            };
            _context.Users.Add(user);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);

            var userName = $"{user.FirstName} {user.LastName}";
            FireAndForget(_emailService.SendWelcomeEmailAsync(user.Email, userName), "SendWelcomeEmail");
            FireAndForget(_passwordService.SendVerificationEmailAsync(user.UserId), "SendVerificationEmail");

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
}
