using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lander.Helpers;
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

    public UserService(UsersContext context, TokenProvider tokenProvider, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _tokenProvider = tokenProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string>LoginUserAsync(LoginUserInputDto userRegistrationInputDto)
    {
        User? user = await _context.Users
         .Include(u => u.UserRole)
         .FirstOrDefaultAsync(u => u.Email == userRegistrationInputDto.Email);

        string hashedInputPassword = HashPassword(userRegistrationInputDto.Password);
        if (user.Password != hashedInputPassword)
        {
            return "Invalid password";
        }

        var token = _tokenProvider.Create(user);
        return token;
    }

    public async Task<UserRegistrationDto> RegisterUserAsync(UserRegistrationInputDto userRegistrationInputDto)
    {
        var currentUserGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
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

        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return new UserRegistrationDto
        {
            FirstName = userRegistrationInputDto.FirstName,
            Email = userRegistrationInputDto.Email,
        };

     
       
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
            user.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ReactivateUserAsync(ReactivateUserInputDto reactivateUserInputDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == reactivateUserInputDto.UserGuid);
        if (user != null)
        {
            user.IsActive = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> DeleteUserAsync(DeleteUserInputDto deleteUserInputDto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == deleteUserInputDto.UserGuid);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
        return true;
    }

    public async  Task ChangePasswordAsync(ChangePasswordInputDto changePasswordInputDto)
    {
        var userGuid = _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserGuid == Guid.Parse(userGuid));

        if (user == null || HashPassword(changePasswordInputDto.OldPassword) != user.Password)
            throw new Exception("Incorrect old password.");

        user.Password = HashPassword(changePasswordInputDto.NewPassword);
        user.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<User?> GetUserByGuidAsync(Guid userGuid)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserGuid == userGuid);
    }

}
