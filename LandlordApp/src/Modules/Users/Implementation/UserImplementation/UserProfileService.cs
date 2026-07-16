using System.Security.Claims;
using Lander.Helpers;
using Lander.src.Common;
using Lander.src.Common.Exceptions;
using Lander.src.Infrastructure.Services;
using Lander.src.Modules.Listings.Interfaces;
using Lander.src.Modules.Roommates.Interfaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Lander.src.Modules.Users.Services;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

public class UserProfileService : IUserProfileService
{
    private readonly UsersContext _context;
    private readonly ReviewsContext _reviewsContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IApartmentService _apartmentService;
    private readonly IRoommateService _roommateService;
    private readonly IEnumerable<IUserDeletedHandler> _deletionHandlers;
    private readonly IUserRoleUpgradeService _roleUpgradeService;
    private readonly ILogger<UserProfileService> _logger;
    private readonly IAuditLogService _auditLog;
    private readonly RefreshTokenService _refreshTokenService;

    public UserProfileService(
        UsersContext context,
        ReviewsContext reviewsContext,
        IHttpContextAccessor httpContextAccessor,
        IApartmentService apartmentService,
        IRoommateService roommateService,
        IEnumerable<IUserDeletedHandler> deletionHandlers,
        IUserRoleUpgradeService roleUpgradeService,
        ILogger<UserProfileService> logger,
        IAuditLogService auditLog,
        RefreshTokenService refreshTokenService)
    {
        _context = context;
        _reviewsContext = reviewsContext;
        _httpContextAccessor = httpContextAccessor;
        _apartmentService = apartmentService;
        _roommateService = roommateService;
        _deletionHandlers = deletionHandlers;
        _roleUpgradeService = roleUpgradeService;
        _logger = logger;
        _auditLog = auditLog;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<User?> GetUserByGuidAsync(Guid userGuid)
        => await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == userGuid);

    public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.UserRole)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return null;

        var reviewsAsLandlord = await _reviewsContext.Reviews
            .Where(r => r.LandlordId == userId && r.Rating.HasValue)
            .ToListAsync();
        var reviewsAsTenant = await _reviewsContext.Reviews
            .Where(r => r.TenantId == userId && r.Rating.HasValue)
            .ToListAsync();

        var allReviews = reviewsAsLandlord.Concat(reviewsAsTenant).ToList();
        var averageRating = allReviews.Any() ? (decimal?)allReviews.Average(r => r.Rating!.Value) : null;

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
            ReviewCount = allReviews.Count,
            HasPersonalAnalytics = user.HasPersonalAnalytics,
            ListingCredits = user.ListingCredits,
            BoostedUntil = null, // stored on Roommate profile — use GET /api/payments/my-status
            PriorityInboxUntil = user.PriorityInboxUntil,
        };
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(int userId, UserProfileUpdateInputDto updateDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) throw new NotFoundException("User", userId);

        if (updateDto.FirstName != null) user.FirstName = updateDto.FirstName;
        if (updateDto.LastName != null) user.LastName = updateDto.LastName;
        // Email se ne mijenja ovdje — promjena emaila zahtijeva verifikaciju nove adrese (poseban endpoint).
        if (updateDto.PhoneNumber != null) user.PhoneNumber = updateDto.PhoneNumber;
        if (updateDto.ProfilePicture != null) user.ProfilePicture = updateDto.ProfilePicture;
        if (updateDto.DateOfBirth.HasValue) user.DateOfBirth = updateDto.DateOfBirth.Value;
        user.ModifiedByGuid = Guid.TryParse(currentUserGuid, out var upMg) ? upMg : null;
        user.ModifiedDate = DateTime.UtcNow;

        // Single-entity update — no explicit transaction needed
        await _context.SaveEntitiesAsync();
        _auditLog.Log("UpdateUserProfile", "User", userId, currentUserGuid);

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
            CreatedDate = user.CreatedDate
        };
    }

    public async Task<UserProfileDto> UpdatePrivacySettingsAsync(int userId, PrivacySettingsDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) throw new NotFoundException("User", userId);

        user.AnalyticsConsent = dto.AnalyticsConsent;
        user.ChatHistoryConsent = dto.ChatHistoryConsent;
        user.ProfileVisibility = dto.ProfileVisibility;
        user.IsIncognito = dto.IsIncognito;
        user.ModifiedDate = DateTime.UtcNow;

        // Single-entity update — no explicit transaction needed
        await _context.SaveEntitiesAsync();

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
            CreatedDate = user.CreatedDate
        };
    }

    public async Task DeactivateUserAsync(DeactivateUserInputDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == dto.UserGuid);
        if (user == null) return;

        user.IsActive = false;
        await _context.SaveEntitiesAsync();
        await _refreshTokenService.RevokeAllByUserIdAsync(user.UserId);
    }

    public async Task ReactivateUserAsync(ReactivateUserInputDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == dto.UserGuid);
        if (user == null) return;

        user.IsActive = true;
        await _context.SaveEntitiesAsync();
    }

    public async Task<bool> DeleteUserAsync(DeleteUserInputDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == dto.UserGuid);
        if (user == null) return true;

        await _refreshTokenService.RevokeAllByUserIdAsync(user.UserId);

        foreach (var handler in _deletionHandlers)
            await handler.HandleAsync(user.UserId);

        var callerGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        try
        {
            await _context.RunInTransactionAsync(async () =>
            {
                _context.Users.Remove(user);
                await _context.SaveEntitiesAsync();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteUserAsync");
            throw;
        }
        _auditLog.Log("DeleteUser", "User", dto.UserGuid, callerGuid);
        return true;
    }

    public async Task UpdateRoommateStatusAsync(UpdateRoommateStatusInputDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == dto.UserGuid);
        if (user == null) return;

        // Single-entity update — no transaction needed; SaveEntitiesAsync is atomic for one row
        user.IsLookingForRoommate = dto.IsLookingForRoommate;
        user.ModifiedDate = DateTime.UtcNow;
        await _context.SaveEntitiesAsync();
    }

    public async Task<UserExportDto> ExportUserDataAsync(int userId)
    {
        var userProfile = await GetUserProfileAsync(userId);
        if (userProfile == null) throw new NotFoundException("User", userId);

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
        => await _roleUpgradeService.UpgradeAsync(userId, targetRoleName);
}
