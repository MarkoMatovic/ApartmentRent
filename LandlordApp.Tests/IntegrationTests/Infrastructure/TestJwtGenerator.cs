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

    /// <param name="twoFactorVerified">
    /// The session was confirmed with an authenticator code ("amr": "mfa"), which admin powers need.
    /// On by default so admin tests act as a confirmed admin; pass false to test an unconfirmed one.
    /// </param>
    public static string Generate(int userId, Guid userGuid, string role = "Tenant", bool twoFactorVerified = true)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("userId", userId.ToString()),
            new Claim("sub", userGuid.ToString()),
            new Claim(ClaimTypes.Role, role),
        };
        if (twoFactorVerified) claims.Add(new Claim("amr", "mfa"));

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
