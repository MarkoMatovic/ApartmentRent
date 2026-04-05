using Microsoft.Extensions.Caching.Memory;

namespace Lander.Helpers;

/// <summary>
/// Prevents duplicate processing of POST requests via client-supplied Idempotency-Key header.
/// Keys are retained for 24 hours.
/// </summary>
public class IdempotencyService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan KeyTtl = TimeSpan.FromHours(24);

    public IdempotencyService(IMemoryCache cache) => _cache = cache;

    /// <summary>
    /// Returns true if this key was already seen (duplicate request).
    /// Registers the key if it is new.
    /// </summary>
    public bool IsDuplicate(string key)
    {
        if (_cache.TryGetValue(IdempotencyKey(key), out _))
            return true;

        _cache.Set(IdempotencyKey(key), true, KeyTtl);
        return false;
    }

    private static string IdempotencyKey(string key) => $"idempotency:{key}";
}
