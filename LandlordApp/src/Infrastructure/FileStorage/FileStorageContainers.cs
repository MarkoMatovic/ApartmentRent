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

    private static readonly HashSet<string> PublicContainers = new(StringComparer.OrdinalIgnoreCase)
    {
        ApartmentImages,
    };

    public static bool IsPublic(string containerName) => PublicContainers.Contains(containerName);
}
