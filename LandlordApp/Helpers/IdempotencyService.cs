using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;

namespace Lander.Helpers;

/// <summary>
/// Prevents duplicate processing of POST requests via client-supplied Idempotency-Key header.
/// Uses IDistributedCache so it works across multiple app instances (Redis/SQL-backed).
/// Keys are retained for 24 hours.
///
/// Race-condition safety: per-key SemaphoreSlim serialises the check-and-set within a single
/// process. For multi-instance deployments with Redis, replace with a Lua SET NX script.
/// </summary>
public class IdempotencyService
{
    private readonly IDistributedCache _cache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private static readonly DistributedCacheEntryOptions KeyTtl =
        new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };

    public IdempotencyService(IDistributedCache cache) => _cache = cache;

    /// <summary>
    /// Returns true if this key was already seen (duplicate request).
    /// Registers the key if it is new. Thread-safe via double-checked locking.
    /// </summary>
    public async Task<bool> IsDuplicateAsync(string key)
    {
        var cacheKey = IdempotencyKey(key);

        // Fast path: key already stored — no lock needed
        if (await _cache.GetStringAsync(cacheKey) is not null) return true;

        // Serialise concurrent first-time registrations for the same key
        var gate = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            // Re-check under the lock (another thread may have written in the interim)
            if (await _cache.GetStringAsync(cacheKey) is not null) return true;

            await _cache.SetStringAsync(cacheKey, "1", KeyTtl);
            return false;
        }
        finally
        {
            gate.Release();
        }
    }

    private static string IdempotencyKey(string key) => $"idempotency:{key}";
}
