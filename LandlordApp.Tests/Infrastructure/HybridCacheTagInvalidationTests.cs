using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LandlordApp.Tests.Infrastructure;

/// <summary>
/// ApartmentService relies on HybridCache tag invalidation to drop the apartment-list
/// cache after every mutation. RemoveByTagAsync compiles against any HybridCache, so
/// these tests pin the behaviour of the real (DI-registered) implementation — if a
/// package upgrade ever turned tag eviction into a no-op, the list would silently serve
/// stale data for the full expiry window instead of failing loudly.
/// </summary>
public class HybridCacheTagInvalidationTests
{
    private static HybridCache CreateRealHybridCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }

    [Fact]
    public async Task RemoveByTagAsync_EvictsEntryCarryingThatTag()
    {
        var cache = CreateRealHybridCache();
        var options = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) };

        var first = await cache.GetOrCreateAsync(
            "key-a", _ => ValueTask.FromResult("v1"), options, tags: ["apartments"]);
        first.Should().Be("v1");

        // Cached — the factory must not run again.
        var cached = await cache.GetOrCreateAsync(
            "key-a", _ => ValueTask.FromResult("v2"), options, tags: ["apartments"]);
        cached.Should().Be("v1", "the entry is still cached and within its expiry window");

        await cache.RemoveByTagAsync("apartments");

        var afterEviction = await cache.GetOrCreateAsync(
            "key-a", _ => ValueTask.FromResult("v3"), options, tags: ["apartments"]);
        afterEviction.Should().Be("v3", "RemoveByTagAsync must evict the entry so the factory re-runs");
    }

    [Fact]
    public async Task RemoveByTagAsync_EvictsEveryEntrySharingTheTag()
    {
        var cache = CreateRealHybridCache();
        var options = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) };

        // Mirrors the real usage: many filter/page permutations under one tag.
        foreach (var key in new[] { "page1", "page2", "page3" })
            await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult("old"), options, tags: ["apartments"]);

        await cache.RemoveByTagAsync("apartments");

        foreach (var key in new[] { "page1", "page2", "page3" })
        {
            var value = await cache.GetOrCreateAsync(key, _ => ValueTask.FromResult("new"), options, tags: ["apartments"]);
            value.Should().Be("new", $"entry '{key}' shared the evicted tag");
        }
    }

    [Fact]
    public async Task RemoveByTagAsync_LeavesEntriesWithOtherTagsIntact()
    {
        var cache = CreateRealHybridCache();
        var options = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) };

        await cache.GetOrCreateAsync("apt", _ => ValueTask.FromResult("apt-v1"), options, tags: ["apartments"]);
        await cache.GetOrCreateAsync("rm", _ => ValueTask.FromResult("rm-v1"), options, tags: ["roommates"]);

        await cache.RemoveByTagAsync("apartments");

        var roommate = await cache.GetOrCreateAsync("rm", _ => ValueTask.FromResult("rm-v2"), options, tags: ["roommates"]);
        roommate.Should().Be("rm-v1", "evicting one tag must not blow away unrelated caches");
    }
}
