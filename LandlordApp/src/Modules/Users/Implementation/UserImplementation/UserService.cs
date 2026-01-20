using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lander.Helpers;
using Lander.src.Modules.Communication.Intefaces;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Lander.src.Modules.Users.Implementation.UserImplementation;
public class UserService : IUserInterface
{
    private readonly UsersContext _context;
    private readonly TokenProvider _tokenProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    public UserService(
        UsersContext context, 
        TokenProvider tokenProvider, 
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService)
    {
        _context = context;
        _tokenProvider = tokenProvider;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
    }
    public async Task<string?> LoginUserAsync(LoginUserInputDto userRegistrationInputDto)
    {
        User? user = await _context.Users
         .Include(u => u.UserRole)
         .FirstOrDefaultAsync(u => u.Email == userRegistrationInputDto.Email);
        if (user == null)
        {
            return null;
        }
        string hashedInputPassword = HashPassword(userRegistrationInputDto.Password);
        if (user.Password != hashedInputPassword)
        {
            return null;
        }
        var token = _tokenProvider.Create(user);
        return token;
    }
    public async Task<UserRegistrationDto> RegisterUserAsync(UserRegistrationInputDto userRegistrationInputDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
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
                Password = HashPassword(userRegistrationInputDto.Password),
                DateOfBirth = userRegistrationInputDto.DateOfBirth,
                PhoneNumber = userRegistrationInputDto.PhoneNumber,
                ProfilePicture = userRegistrationInputDto.ProfilePicture,
                CreatedDate = DateTime.UtcNow,
                CreatedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : (Guid?)null,
                ModifiedByGuid = currentUserGuid != null ? Guid.Parse(currentUserGuid) : (Guid?)null,
                IsActive = true, // Automatski aktiviraj korisnika
                UserRoleId = tenantRole.RoleId // Dodeli rolu "Tenant"
            };
            _context.Users.Add(user);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
            var userName = $"{user.FirstName} {user.LastName}";
            _ = _emailService.SendWelcomeEmailAsync(user.Email, userName);
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
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
    }
    public Task LogoutUserAsync()
    {
        _httpContextAccessor.HttpContext?.SignOutAsync();
        return Task.CompletedTask;
    }
    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
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
            catch
            {
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
            catch
            {
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
            var transaction = await _context.BeginTransactionAsync();
            try
            {
                _context.Users.Remove(user);
                await _context.SaveEntitiesAsync();
                await _context.CommitTransactionAsync(transaction);
            }
            catch
            {
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
        if (user == null || HashPassword(changePasswordInputDto.OldPassword) != user.Password)
            throw new Exception("Incorrect old password.");
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            user.Password = HashPassword(changePasswordInputDto.NewPassword);
            user.ModifiedDate = DateTime.UtcNow;
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
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
            catch
            {
                _context.RollBackTransaction();
                throw;
            }
        }
    }
    public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return null;
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
            UserRoleId = user.UserRoleId,
            CreatedDate = user.CreatedDate
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
        catch
        {
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
            UserRoleId = user.UserRoleId,
            CreatedDate = user.CreatedDate
        };
    }
}
