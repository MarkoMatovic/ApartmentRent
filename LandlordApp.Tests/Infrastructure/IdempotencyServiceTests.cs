using FluentAssertions;
using Lander.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LandlordApp.Tests.Infrastructure;

public class IdempotencyServiceTests
{
    private static IdempotencyService CreateService()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new IdempotencyService(cache);
    }

    [Fact]
    public async Task IsDuplicateAsync_NewKey_ReturnsFalse()
    {
        var service = CreateService();

        var result = await service.IsDuplicateAsync("key-1");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsDuplicateAsync_SameKey_ReturnsTrue()
    {
        var service = CreateService();
        await service.IsDuplicateAsync("key-1");

        var result = await service.IsDuplicateAsync("key-1");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsDuplicateAsync_DifferentKeys_BothReturnFalse()
    {
        var service = CreateService();

        var result1 = await service.IsDuplicateAsync("key-a");
        var result2 = await service.IsDuplicateAsync("key-b");

        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Fact]
    public async Task IsDuplicateAsync_KeyRegisteredOnFirstCall()
    {
        var service = CreateService();
        await service.IsDuplicateAsync("reg-key");

        var secondCall = await service.IsDuplicateAsync("reg-key");

        secondCall.Should().BeTrue();
    }

    [Fact]
    public async Task IsDuplicateAsync_EmptyString_Works()
    {
        var service = CreateService();

        var firstCall = await service.IsDuplicateAsync(string.Empty);
        var secondCall = await service.IsDuplicateAsync(string.Empty);

        firstCall.Should().BeFalse();
        secondCall.Should().BeTrue();
    }

    [Fact]
    public async Task IsDuplicateAsync_MultipleKeys_EachTrackedIndependently()
    {
        var service = CreateService();

        var result1 = await service.IsDuplicateAsync("multi-1");
        var result2 = await service.IsDuplicateAsync("multi-2");
        var result3 = await service.IsDuplicateAsync("multi-3");

        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
    }

    [Fact]
    public async Task IsDuplicateAsync_CachePrefix_DoesNotCollideWithOtherCacheKeys()
    {
        IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new IdempotencyService(cache);

        // Manually set a raw entry WITHOUT the "idempotency:" prefix
        await cache.SetStringAsync("some-key", "1");

        // IsDuplicateAsync checks "idempotency:some-key", not "some-key"
        var result = await service.IsDuplicateAsync("some-key");

        result.Should().BeFalse();
    }
}
