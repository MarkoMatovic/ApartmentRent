using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace Lander.Helpers;

public sealed class TokenProvider(IConfiguration configuration)
{
    public string Create(User user)
    {
        string secretKey = configuration["Jwt:Secret"];
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.UserGuid.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                
            ]),
            Expires = DateTime.UtcNow.AddMinutes(60),
            SigningCredentials = credentials,
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"]

        };
        var handler = new JsonWebTokenHandler();
        string token = handler.CreateToken(tokenDescriptor);
        return token;
    }
}
