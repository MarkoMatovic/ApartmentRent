using Microsoft.Extensions.Caching.Distributed;

namespace Lander.src.Infrastructure.Services;

public interface IJwtBlacklistService
{
    Task BlacklistAsync(string jti, DateTime tokenExpiry);
    Task<bool> IsBlacklistedAsync(string jti);
}

// Stores revoked JTI claims in IDistributedCache (Redis in prod, in-memory in dev).
// TTL matches the token's remaining lifetime so entries self-expire.
public sealed class JwtBlacklistService : IJwtBlacklistService
{
    private readonly IDistributedCache _cache;

    public JwtBlacklistService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task BlacklistAsync(string jti, DateTime tokenExpiry)
    {
        var ttl = tokenExpiry.ToUniversalTime() - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero) return;

        await _cache.SetStringAsync(
            CacheKey(jti),
            "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
    }

    public async Task<bool> IsBlacklistedAsync(string jti)
        => await _cache.GetStringAsync(CacheKey(jti)) is not null;

    private static string CacheKey(string jti) => $"jwt:bl:{jti}";
}
