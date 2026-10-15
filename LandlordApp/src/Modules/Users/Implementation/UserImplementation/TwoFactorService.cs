using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lander.Helpers;
using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

/// <summary>
/// Two-step sign-in with an authenticator app. Available to everyone; for an Admin it is what turns
/// the admin powers on: a session that was not confirmed with a code works as a regular account
/// (see <see cref="TwoFactorPolicy"/>).
///
/// The secret is kept encrypted with ASP.NET Data Protection (the key ring lives in blob storage),
/// recovery codes only as SHA-256 hashes, and a code that was used once is not accepted again.
/// </summary>
public interface ITwoFactorService
{
    Task<TwoFactorSetup> BeginSetupAsync(int userId);
    /// <summary>Turns it on after the first code from the app; returns the recovery codes, shown once.</summary>
    Task<IReadOnlyList<string>> EnableAsync(int userId, string code);
    Task DisableAsync(int userId, string code);
    /// <summary>An authenticator code or an unused recovery code of the user (a recovery code is used up).</summary>
    Task<bool> VerifyAsync(User user, string code);
    /// <summary>What the password step hands to the code step: the user, readable for a few minutes only.</summary>
    string CreateLoginChallenge(int userId);
    int? ReadLoginChallenge(string challenge);
}

public sealed record TwoFactorSetup(string Secret, string SetupUri, string QrCodeDataUrl);

public sealed class TwoFactorService : ITwoFactorService
{
    public const string Issuer = "TuRentaj";
    public const int RecoveryCodeCount = 10;
    public static readonly TimeSpan LoginChallengeLifetime = TimeSpan.FromMinutes(5);

    private readonly UsersContext _context;
    private readonly IDataProtector _secretProtector;
    private readonly ITimeLimitedDataProtector _challengeProtector;
    private readonly TimeProvider _time;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(
        UsersContext context,
        IDataProtectionProvider dataProtection,
        TimeProvider timeProvider,
        ILogger<TwoFactorService> logger)
    {
        _context = context;
        _secretProtector = dataProtection.CreateProtector("TwoFactor.Secret.v1");
        _challengeProtector = dataProtection.CreateProtector("TwoFactor.LoginChallenge.v1").ToTimeLimitedDataProtector();
        _time = timeProvider;
        _logger = logger;
    }

    public async Task<TwoFactorSetup> BeginSetupAsync(int userId)
    {
        var user = await LoadAsync(userId);
        if (user.TwoFactorEnabled)
            throw new BusinessRuleException(BusinessErrorCodes.TwoFactorAlreadyEnabled);

        // Stored now, switched on only by EnableAsync: a setup that is abandoned changes nothing.
        var secret = Totp.NewSecret();
        user.TwoFactorSecret = _secretProtector.Protect(Totp.ToBase32(secret));
        user.TwoFactorLastStep = null;
        await _context.SaveEntitiesAsync();

        var uri = Totp.SetupUri(Issuer, user.Email, secret);
        return new TwoFactorSetup(Totp.ToBase32(secret), uri, QrCodeDataUrl(uri));
    }

    public async Task<IReadOnlyList<string>> EnableAsync(int userId, string code)
    {
        var user = await LoadAsync(userId);
        if (user.TwoFactorEnabled)
            throw new BusinessRuleException(BusinessErrorCodes.TwoFactorAlreadyEnabled);
        if (user.TwoFactorSecret is null)
            throw new BusinessRuleException(BusinessErrorCodes.TwoFactorSetupRequired);
        if (!MatchAuthenticatorCode(user, code))
            throw new BusinessRuleException(BusinessErrorCodes.InvalidTwoFactorCode);

        var recoveryCodes = Enumerable.Range(0, RecoveryCodeCount).Select(_ => NewRecoveryCode()).ToList();
        user.TwoFactorRecoveryCodes = JsonSerializer.Serialize(recoveryCodes.Select(c => HashRecoveryCode(user, c)).ToList());
        user.TwoFactorEnabled = true;
        await _context.SaveEntitiesAsync();
        _logger.LogInformation("Two-step sign-in turned on for user {UserId}", userId);
        return recoveryCodes;
    }

    public async Task DisableAsync(int userId, string code)
    {
        var user = await LoadAsync(userId);
        if (!user.TwoFactorEnabled)
            throw new BusinessRuleException(BusinessErrorCodes.TwoFactorNotEnabled);
        if (!await VerifyAsync(user, code))
            throw new BusinessRuleException(BusinessErrorCodes.InvalidTwoFactorCode);

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.TwoFactorRecoveryCodes = null;
        user.TwoFactorLastStep = null;
        await _context.SaveEntitiesAsync();
        _logger.LogInformation("Two-step sign-in turned off for user {UserId}", userId);
    }

    public async Task<bool> VerifyAsync(User user, string code)
    {
        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(code)) return false;

        if (MatchAuthenticatorCode(user, code))
        {
            await _context.SaveEntitiesAsync();
            return true;
        }

        var hashes = user.TwoFactorRecoveryCodes is null
            ? []
            : JsonSerializer.Deserialize<List<string>>(user.TwoFactorRecoveryCodes) ?? [];
        var hash = HashRecoveryCode(user, code);
        if (!hashes.Remove(hash)) return false;

        user.TwoFactorRecoveryCodes = JsonSerializer.Serialize(hashes);
        await _context.SaveEntitiesAsync();
        _logger.LogWarning("User {UserId} signed in with a recovery code; {Left} left", user.UserId, hashes.Count);
        return true;
    }

    public string CreateLoginChallenge(int userId) =>
        _challengeProtector.Protect(userId.ToString(), LoginChallengeLifetime);

    public int? ReadLoginChallenge(string challenge)
    {
        try
        {
            return int.TryParse(_challengeProtector.Unprotect(challenge), out var userId) ? userId : null;
        }
        catch (CryptographicException)
        {
            return null; // expired, tampered with, or not ours
        }
    }

    /// <summary>Checks the app's code and remembers its time step, so the same code cannot be used twice.</summary>
    private bool MatchAuthenticatorCode(User user, string code)
    {
        if (user.TwoFactorSecret is null) return false;
        var secret = Totp.FromBase32(_secretProtector.Unprotect(user.TwoFactorSecret));
        var step = Totp.Match(secret, code, _time.GetUtcNow());
        if (step is null || step <= user.TwoFactorLastStep) return false;

        user.TwoFactorLastStep = step;
        return true;
    }

    private async Task<User> LoadAsync(int userId) =>
        await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
        ?? throw new NotFoundException("User", userId);

    /// <summary>Ten characters in two groups ("k7d2m-9xq4p"), without letters that look alike.</summary>
    private static string NewRecoveryCode()
    {
        const string alphabet = "abcdefghjkmnpqrstuvwxyz23456789";
        var chars = Enumerable.Range(0, 10).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray();
        return $"{new string(chars, 0, 5)}-{new string(chars, 5, 5)}";
    }

    /// <summary>Salted with the user's id, so one table of guesses does not fit every account.</summary>
    private static string HashRecoveryCode(User user, string code)
    {
        var normalized = code.Trim().Replace("-", "").Replace(" ", "").ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes($"{user.UserGuid:N}:{normalized}");
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static string QrCodeDataUrl(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(6);
        return "data:image/png;base64," + Convert.ToBase64String(png);
    }
}

/// <summary>
/// Admin powers need a session confirmed with a second factor. Tokens carry the "amr": "mfa" claim
/// when it was; without it the Admin role is taken away when the token is read, so every
/// [Authorize(Roles = "Admin")], AdminPolicy and IsInRole("Admin") check refuses.
/// </summary>
public static class TwoFactorPolicy
{
    public const string ClaimType = "amr";
    public const string ClaimValue = "mfa";

    public static bool RequiresTwoFactor(string? roleName) => roleName == RoleConstants.Admin;

    public static void RemoveUnconfirmedAdminRole(System.Security.Claims.ClaimsIdentity identity)
    {
        if (identity.HasClaim(ClaimType, ClaimValue)) return;
        foreach (var claim in identity.FindAll(identity.RoleClaimType).Where(c => RequiresTwoFactor(c.Value)).ToList())
            identity.RemoveClaim(claim);
    }
}
