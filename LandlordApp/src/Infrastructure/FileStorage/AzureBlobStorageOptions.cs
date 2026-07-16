using System.ComponentModel.DataAnnotations;

namespace Lander.src.Infrastructure.FileStorage;

public class AzureBlobStorageOptions
{
    /// <summary>Azure Storage connection string. When empty, the app falls back to local disk storage.</summary>
    public string ConnectionString { get; init; } = "";

    /// <summary>
    /// Optional public base URL (e.g. a CDN endpoint) used to build public-blob URLs.
    /// When empty, the blob's native account URL is used. Changing CDN/domain = one config change.
    /// </summary>
    public string? PublicBaseUrl { get; init; }

    /// <summary>SAS token lifetime for private containers.</summary>
    [Range(1, 1440)]
    public int SasExpiryMinutes { get; init; } = 60;
}
