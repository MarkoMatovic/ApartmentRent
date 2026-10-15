using System.Security.Cryptography;
using System.Text;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

/// <summary>
/// Refresh tokens, one session per sign-in. Every sign-in starts a session (a token "family"); every
/// refresh exchanges the session's token for a new one (rotation). Signing in on a phone no longer
/// signs the laptop out, and signing out ends only that device's session.
///
/// Reuse detection: a token that was already exchanged and is presented again was copied, so the
/// whole session it belongs to is ended. The exception is a short grace period right after the
/// exchange, because two tabs of one browser share the cookie and refresh at the same moment.
/// </summary>
public class RefreshTokenService
{
    private readonly UsersContext _context;
    private readonly TimeProvider _time;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(UsersContext context, TimeProvider? timeProvider = null, ILogger<RefreshTokenService>? logger = null)
    {
        _context = context;
        _time = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<RefreshTokenService>.Instance;
    }

    /// <summary>
    /// How long a refresh token (and the cookie that carries it) stays valid. One constant for
    /// both: the cookie used to say 30 days while the server-side row expired after 7, so the
    /// browser kept sending a token the server had already stopped accepting.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    /// <summary>How long after an exchange the old token still works (parallel refreshes from several tabs).</summary>
    public static readonly TimeSpan ReuseGrace = TimeSpan.FromSeconds(30);

    /// <summary>Signed-in devices per user; signing in on one more ends the least recently used session.</summary>
    public const int MaxSessionsPerUser = 10;

    private const int MaxConcurrencyRetries = 3;

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    /// <summary>256 random bits from the OS generator, as 64 hex characters.</summary>
    public string GenerateRawToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public static string HashToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLower();
    }

    /// <summary>A new session (sign-in on a device). Sessions on other devices stay as they are.</summary>
    public async Task<string> CreateAsync(int userId, bool twoFactorVerified = false)
    {
        var raw = Issue(userId, Guid.NewGuid(), twoFactorVerified);
        await _context.SaveEntitiesAsync();
        await TrimSessionsAsync(userId);
        return raw;
    }

    public sealed record Rotation(User User, string RawToken, bool TwoFactorVerified);

    /// <summary>
    /// Exchanges a refresh token for a new one in the same session. Null when the token is unknown,
    /// expired, revoked, or reused after the grace period (which also ends that session).
    /// </summary>
    public async Task<Rotation?> RotateAsync(string raw)
    {
        var hash = HashToken(raw);
        for (var attempt = 1; ; attempt++)
        {
            var token = await _context.RefreshTokens
                .Include(t => t.User).ThenInclude(u => u.UserRole)
                .FirstOrDefaultAsync(t => t.TokenHash == hash);
            if (token is null || token.ExpiresAt <= Now) return null;

            if (token.RotatedAt is not null)
                return await ReuseAsync(token);
            if (token.IsRevoked)
                return null; // signed out, password changed, or the session was ended

            token.IsRevoked = true;
            token.RotatedAt = Now;
            var next = Issue(token.UserId, token.FamilyId, token.TwoFactorVerified);
            // Tokens handed out to parallel refreshes of this session: only the one in the cookie is used.
            var leftovers = await _context.RefreshTokens
                .Where(t => t.FamilyId == token.FamilyId && t.Id != token.Id && !t.IsRevoked)
                .ToListAsync();
            foreach (var t in leftovers) t.IsRevoked = true;

            try
            {
                await _context.SaveEntitiesAsync();
                return new Rotation(token.User, next, token.TwoFactorVerified);
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                // Another request exchanged the same token a moment ago (another tab): read it again.
                ForgetTokens();
            }
        }
    }

    private async Task<Rotation?> ReuseAsync(RefreshToken token)
    {
        if (Now - token.RotatedAt!.Value <= ReuseGrace)
        {
            var sessionActive = await _context.RefreshTokens.AnyAsync(t =>
                t.FamilyId == token.FamilyId && !t.IsRevoked && t.ExpiresAt > Now);
            if (!sessionActive) return null;

            var raw = Issue(token.UserId, token.FamilyId, token.TwoFactorVerified);
            await _context.SaveEntitiesAsync();
            return new Rotation(token.User, raw, token.TwoFactorVerified);
        }

        _logger.LogWarning(
            "Refresh token {TokenId} of user {UserId} was used again {Seconds:F0} s after it was exchanged; its session was ended",
            token.Id, token.UserId, (Now - token.RotatedAt.Value).TotalSeconds);
        await RevokeWhereAsync(t => t.FamilyId == token.FamilyId);
        return null;
    }

    /// <summary>The session of this token was confirmed with a second factor (turned on while signed in).</summary>
    public async Task MarkTwoFactorVerifiedAsync(string raw)
    {
        var hash = HashToken(raw);
        var familyId = await _context.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.FamilyId)
            .FirstOrDefaultAsync();
        if (familyId is null) return;
        await UpdateActiveWhereAsync(t => t.FamilyId == familyId.Value, t => t.TwoFactorVerified = true);
    }

    /// <summary>Two-step sign-in was turned off: no session counts as confirmed any more.</summary>
    public Task ForgetTwoFactorAsync(int userId) =>
        UpdateActiveWhereAsync(t => t.UserId == userId && t.TwoFactorVerified, t => t.TwoFactorVerified = false);

    public async Task<RefreshToken?> ValidateAsync(string raw)
    {
        var hash = HashToken(raw);
        return await _context.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.UserRole)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == hash &&
                !t.IsRevoked &&
                t.ExpiresAt > Now);
    }

    /// <summary>Ends the session the token belongs to (sign-out on this device).</summary>
    public async Task RevokeAsync(string raw)
    {
        var hash = HashToken(raw);
        var familyId = await _context.RefreshTokens
            .Where(t => t.TokenHash == hash)
            .Select(t => (Guid?)t.FamilyId)
            .FirstOrDefaultAsync();
        if (familyId is null) return;
        await RevokeWhereAsync(t => t.FamilyId == familyId.Value);
    }

    /// <summary>
    /// Revokes ALL active refresh tokens for a user (every device).
    /// Must be called on password reset/change so a compromised session
    /// cannot survive after credentials are updated.
    /// </summary>
    public Task RevokeAllByUserIdAsync(int userId) => RevokeWhereAsync(t => t.UserId == userId);

    private string Issue(int userId, Guid familyId, bool twoFactorVerified)
    {
        var raw = GenerateRawToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            FamilyId = familyId,
            TwoFactorVerified = twoFactorVerified,
            TokenHash = HashToken(raw),
            CreatedAt = Now,
            ExpiresAt = Now.Add(Lifetime),
        });
        return raw;
    }

    private async Task TrimSessionsAsync(int userId)
    {
        var now = Now;
        var surplus = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > now)
            .GroupBy(t => t.FamilyId)
            .Select(g => new { FamilyId = g.Key, LastUsed = g.Max(t => t.CreatedAt) })
            .OrderByDescending(f => f.LastUsed)
            .Skip(MaxSessionsPerUser)
            .Select(f => f.FamilyId)
            .ToListAsync();
        if (surplus.Count > 0)
            await RevokeWhereAsync(t => t.UserId == userId && surplus.Contains(t.FamilyId));
    }

    private Task RevokeWhereAsync(System.Linq.Expressions.Expression<Func<RefreshToken, bool>> filter) =>
        UpdateActiveWhereAsync(filter, t => t.IsRevoked = true);

    private async Task UpdateActiveWhereAsync(
        System.Linq.Expressions.Expression<Func<RefreshToken, bool>> filter, Action<RefreshToken> change)
    {
        for (var attempt = 1; ; attempt++)
        {
            var now = Now;
            var active = await _context.RefreshTokens
                .Where(filter)
                .Where(t => !t.IsRevoked && t.ExpiresAt > now)
                .ToListAsync();
            if (active.Count == 0) return;
            foreach (var t in active) change(t);
            try
            {
                await _context.SaveEntitiesAsync();
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                // One of them was exchanged at the same moment: read them again and revoke what is left.
                ForgetTokens();
            }
        }
    }

    private void ForgetTokens()
    {
        foreach (var entry in _context.ChangeTracker.Entries<RefreshToken>().ToList())
            entry.State = EntityState.Detached;
    }
}
