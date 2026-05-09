using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LandlordApp.Tests.IntegrationTests.Infrastructure;

public static class TestJwtGenerator
{
    public const string TestSecret = "TestSecretKeyForIntegrationTestsMustBeAtLeast32Chars!!";
    public const string TestIssuer = "test-issuer";
    public const string TestAudience = "test-audience";

    public static string Generate(int userId, Guid userGuid, string role = "Tenant")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", userId.ToString()),
            new Claim("sub", userGuid.ToString()),
            new Claim(ClaimTypes.Role, role),
        };

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
