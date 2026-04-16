using Microsoft.Extensions.Primitives;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// Singleton that issues a new <see cref="IChangeToken"/> whenever apartment data changes,
/// allowing all <see cref="IMemoryCache"/> entries tagged with this token to be evicted atomically.
/// </summary>
public sealed class ApartmentCacheVersionService
{
    private volatile CancellationTokenSource _cts = new();

    public IChangeToken GetChangeToken() => new CancellationChangeToken(_cts.Token);

    public void Invalidate()
    {
        var old = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
        old.Cancel();
        old.Dispose();
    }
}
