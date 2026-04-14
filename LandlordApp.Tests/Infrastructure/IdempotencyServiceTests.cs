using FluentAssertions;
using Lander.Helpers;
using Microsoft.Extensions.Caching.Memory;

namespace LandlordApp.Tests.Infrastructure;

public class IdempotencyServiceTests
{
    private static (IdempotencyService service, IMemoryCache cache) CreateService()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new IdempotencyService(cache);
        return (service, cache);
    }

    [Fact]
    public void IsDuplicate_NewKey_ReturnsFalse()
    {
        var (service, _) = CreateService();

        var result = service.IsDuplicate("key-1");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsDuplicate_SameKey_ReturnsTrue()
    {
        var (service, _) = CreateService();
        service.IsDuplicate("key-1");

        var result = service.IsDuplicate("key-1");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsDuplicate_DifferentKeys_BothReturnFalse()
    {
        var (service, _) = CreateService();

        var result1 = service.IsDuplicate("key-a");
        var result2 = service.IsDuplicate("key-b");

        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }

    [Fact]
    public void IsDuplicate_KeyRegisteredOnFirstCall()
    {
        var (service, _) = CreateService();
        service.IsDuplicate("reg-key");

        var secondCall = service.IsDuplicate("reg-key");

        secondCall.Should().BeTrue();
    }

    [Fact]
    public void IsDuplicate_EmptyString_Works()
    {
        var (service, _) = CreateService();

        var firstCall = service.IsDuplicate(string.Empty);
        var secondCall = service.IsDuplicate(string.Empty);

        firstCall.Should().BeFalse();
        secondCall.Should().BeTrue();
    }

    [Fact]
    public void IsDuplicate_MultipleKeys_EachTrackedIndependently()
    {
        var (service, _) = CreateService();

        var result1 = service.IsDuplicate("multi-1");
        var result2 = service.IsDuplicate("multi-2");
        var result3 = service.IsDuplicate("multi-3");

        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
    }

    [Fact]
    public void IsDuplicate_CachePrefix_DoesNotCollideWithOtherCacheKeys()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new IdempotencyService(cache);

        // Manually set a cache entry WITHOUT the "idempotency:" prefix
        cache.Set("some-key", true);

        // IsDuplicate checks "idempotency:some-key", not "some-key"
        var result = service.IsDuplicate("some-key");

        result.Should().BeFalse();
    }
}
