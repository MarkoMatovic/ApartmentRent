using Microsoft.Extensions.Caching.Memory;

namespace Lander.src.Modules.Roommates.Implementation;

/// <summary>
/// The roommate list is cached per filter combination under a key that contains a version number;
/// bumping the version drops every cached page at once. Shared by everything that changes who shows
/// up in the list (profiles, pairing).
/// </summary>
internal static class RoommateListCache
{
    private const string VersionKey = "Roommates_CacheVersion";

    public static long Version(IMemoryCache cache) => cache.GetOrCreate(VersionKey, _ => 0L);

    public static void Invalidate(IMemoryCache cache) => cache.Set(VersionKey, Version(cache) + 1);
}
