using Lander.src.Modules.Users.Implementation.UserImplementation;

namespace Lander.src.Modules.Users.Controllers;

/// <summary>The httpOnly cookie that carries the refresh token; only requests under /api/v1/auth get it.</summary>
internal static class AuthCookie
{
    public const string RefreshTokenName = "refreshToken";
    public const string Path = "/api/v1/auth";

    public static void SetRefreshToken(HttpResponse response, string rawRefreshToken) =>
        response.Cookies.Append(RefreshTokenName, rawRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.Add(RefreshTokenService.Lifetime),
            Path     = Path,
        });
}
