namespace Lander.src.Infrastructure.FileStorage;

/// <summary>
/// Canonical storage container names and their access level. Listing images are public
/// (served directly, no SAS). Private documents (future) live in private containers and are
/// only reachable via short-lived SAS tokens.
/// </summary>
public static class FileStorageContainers
{
    /// <summary>Apartment listing images — public.</summary>
    public const string ApartmentImages = "apartments";

    /// <summary>Chat attachments — private (authorized download / SAS only).</summary>
    public const string ChatFiles = "chat-files";

    /// <summary>Profile pictures (320×320 WebP, metadata removed) — public, shown next to names everywhere.</summary>
    public const string Avatars = "avatars";

    private static readonly HashSet<string> PublicContainers = new(StringComparer.OrdinalIgnoreCase)
    {
        ApartmentImages,
        Avatars,
    };

    public static bool IsPublic(string containerName) => PublicContainers.Contains(containerName);
}
