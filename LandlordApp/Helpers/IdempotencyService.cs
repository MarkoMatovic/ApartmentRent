using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace Lander.Helpers;

/// <summary>
/// Prevents duplicate processing of POST requests via client-supplied Idempotency-Key header.
/// Uses IDistributedCache so it works across multiple app instances (Redis/SQL-backed).
/// Keys are retained for 24 hours.
/// </summary>
public class IdempotencyService
{
    private readonly IDistributedCache _cache;
    private static readonly DistributedCacheEntryOptions KeyTtl =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };

    public IdempotencyService(IDistributedCache cache) => _cache = cache;

    /// <summary>
    /// Returns true if this key was already seen (duplicate request).
    /// Registers the key if it is new.
    /// </summary>
    public async Task<bool> IsDuplicateAsync(string key)
    {
        var cacheKey = IdempotencyKey(key);
        var existing = await _cache.GetStringAsync(cacheKey);
        if (existing is not null)
            return true;

        await _cache.SetStringAsync(cacheKey, "1", KeyTtl);
        return false;
    }

    private static string IdempotencyKey(string key) => $"idempotency:{key}";
}
