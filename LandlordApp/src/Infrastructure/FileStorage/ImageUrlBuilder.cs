using Microsoft.AspNetCore.Http;

namespace Lander.src.Infrastructure.FileStorage;

/// <summary>
/// Default <see cref="IImageUrlBuilder"/>. Delegates to <see cref="IFileStorageService.GetUrl"/>
/// (Azure blob/CDN URL in production, relative path in local dev) and promotes relative local
/// URLs to absolute using the current request host.
/// </summary>
public class ImageUrlBuilder : IImageUrlBuilder
{
    private readonly IFileStorageService _storage;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ImageUrlBuilder(IFileStorageService storage, IHttpContextAccessor httpContextAccessor)
    {
        _storage = storage;
        _httpContextAccessor = httpContextAccessor;
    }

    public string? BuildUrl(string container, string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        // Legacy rows may store a full absolute URL; pass it through unchanged.
        if (Uri.IsWellFormedUriString(key, UriKind.Absolute)) return key;

        var url = _storage.GetUrl(container, key.TrimStart('/'));

        // Local storage returns a site-relative URL; make it absolute for API clients.
        if (url.StartsWith('/'))
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is not null)
                url = $"{request.Scheme}://{request.Host}{url}";
        }

        return url;
    }
}
