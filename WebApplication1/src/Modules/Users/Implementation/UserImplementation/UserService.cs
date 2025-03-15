using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Lander.Helpers;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Interfaces.UserInterface;
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
    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
