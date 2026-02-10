using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace Lander.Helpers;

public sealed class TokenProvider
{
    private readonly IConfiguration _configuration;
    private readonly UsersContext _context;

    public TokenProvider(IConfiguration configuration, UsersContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    public async Task<string> CreateAsync(User user)
    {
        string secretKey = _configuration["Jwt:Secret"];
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserGuid.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.UserGuid.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName ?? ""),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName ?? ""),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, user.UserRole?.RoleName ?? "Guest"),
            new Claim("userId", user.UserId.ToString()),
            new Claim("isActive", user.IsActive.ToString()),
            new Claim("isLookingForRoommate", user.IsLookingForRoommate.ToString())
        };

        if (!string.IsNullOrEmpty(user.PhoneNumber))
        {
            claims.Add(new Claim("phone_number", user.PhoneNumber));
        }

        if (user.UserRoleId.HasValue)
        {
            claims.Add(new Claim("userRoleId", user.UserRoleId.Value.ToString()));

            // Load permissions for the user's role and add them as claims
            var permissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == user.UserRoleId.Value)
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission.PermissionName)
                .ToListAsync();

            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(60),
            SigningCredentials = credentials,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]

        };
        var handler = new JsonWebTokenHandler();
        string token = handler.CreateToken(tokenDescriptor);
        return token;
    }
}
