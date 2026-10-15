using System.Security.Claims;
using Lander.Helpers;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Users.Controllers;

/// <summary>Two-step sign-in with an authenticator app: the code step of signing in, and turning it on or off.</summary>
[Route(ApiActionsV1.TwoFactor)]
[ApiController]
public class TwoFactorController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITwoFactorService _twoFactor;
    private readonly RefreshTokenService _refreshTokens;
    private readonly TokenProvider _tokenProvider;
    private readonly UsersContext _users;

    public TwoFactorController(
        IAuthService authService,
        ITwoFactorService twoFactor,
        RefreshTokenService refreshTokens,
        TokenProvider tokenProvider,
        UsersContext users)
    {
        _authService = authService;
        _twoFactor = twoFactor;
        _refreshTokens = refreshTokens;
        _tokenProvider = tokenProvider;
        _users = users;
    }

    /// <summary>After the password: the code from the app (or a recovery code) finishes signing in.</summary>
    [HttpPost(ApiActionsV1.TwoFactorLogin, Name = nameof(ApiActionsV1.TwoFactorLogin))]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthTokenDto>> Login([FromBody] TwoFactorLoginInputDto dto)
    {
        var tokens = await _authService.CompleteTwoFactorLoginAsync(dto.TwoFactorToken, dto.Code);
        AuthCookie.SetRefreshToken(Response, tokens.RefreshToken);
        return Ok(new AuthTokenDto { AccessToken = tokens.AccessToken, RefreshToken = string.Empty });
    }

    /// <summary>A new secret: the QR code for the app and the same key as text, for typing it in.</summary>
    [HttpPost(ApiActionsV1.TwoFactorSetup, Name = nameof(ApiActionsV1.TwoFactorSetup))]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<TwoFactorSetup>> Setup() =>
        Ok(await _twoFactor.BeginSetupAsync(CurrentUserId()));

    /// <summary>
    /// The first code from the app turns it on. This device's session counts as confirmed from now on,
    /// so the answer carries a new access token as well as the recovery codes (shown only this once).
    /// </summary>
    [HttpPost(ApiActionsV1.TwoFactorEnable, Name = nameof(ApiActionsV1.TwoFactorEnable))]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<TwoFactorEnabledDto>> Enable([FromBody] TwoFactorCodeInputDto dto)
    {
        var userId = CurrentUserId();
        var recoveryCodes = await _twoFactor.EnableAsync(userId, dto.Code);

        var rawRefresh = Request.Cookies[AuthCookie.RefreshTokenName];
        if (!string.IsNullOrEmpty(rawRefresh))
            await _refreshTokens.MarkTwoFactorVerifiedAsync(rawRefresh);

        var user = await _users.Users.Include(u => u.UserRole).FirstAsync(u => u.UserId == userId);
        var accessToken = await _tokenProvider.CreateAsync(user, twoFactorVerified: !string.IsNullOrEmpty(rawRefresh));
        return Ok(new TwoFactorEnabledDto(recoveryCodes, accessToken));
    }

    /// <summary>Turning it off needs a current code (or a recovery code); sessions stop counting as confirmed.</summary>
    [HttpPost(ApiActionsV1.TwoFactorDisable, Name = nameof(ApiActionsV1.TwoFactorDisable))]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthTokenDto>> Disable([FromBody] TwoFactorCodeInputDto dto)
    {
        var userId = CurrentUserId();
        await _twoFactor.DisableAsync(userId, dto.Code);
        await _refreshTokens.ForgetTwoFactorAsync(userId);

        var user = await _users.Users.Include(u => u.UserRole).FirstAsync(u => u.UserId == userId);
        return Ok(new AuthTokenDto { AccessToken = await _tokenProvider.CreateAsync(user) });
    }

    private int CurrentUserId() =>
        int.TryParse(User.FindFirstValue("userId"), out var id) ? id : throw new UnauthorizedAccessException();
}

public sealed record TwoFactorEnabledDto(IReadOnlyList<string> RecoveryCodes, string AccessToken);
