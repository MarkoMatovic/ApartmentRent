namespace Lander.src.Infrastructure.FileStorage;

/// <summary>
/// Builds publicly reachable URLs from stored container-relative keys. Centralising URL
/// construction here means switching CDN/domain is a single configuration change
/// (<c>AzureBlobStorage:PublicBaseUrl</c>) rather than a data migration.
/// </summary>
public interface IImageUrlBuilder
{
    /// <summary>
    /// Returns the absolute URL for <paramref name="key"/> in <paramref name="container"/>.
    /// Returns null when the key is null/empty. Keys that are already absolute URLs (legacy rows)
    /// are returned unchanged.
    /// </summary>
    string? BuildUrl(string container, string? key);
}
