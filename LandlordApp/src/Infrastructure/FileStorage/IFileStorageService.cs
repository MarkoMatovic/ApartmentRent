namespace Lander.src.Infrastructure.FileStorage;

/// <summary>
/// Storage abstraction for binary files (images, documents). Adapted from the Investment
/// project's blob service. Implemented by <see cref="AzureBlobStorageService"/> in
/// production and <see cref="LocalFileStorageService"/> for local dev (no connection string).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads <paramref name="content"/> to <paramref name="containerName"/>/<paramref name="filePath"/>
    /// and returns the publicly reachable URL of the stored object. For private containers the
    /// returned URL includes a read-only SAS token.
    /// </summary>
    Task<string> UploadAsync(string containerName, string filePath, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>Deletes the object if it exists. Returns true if something was deleted.</summary>
    Task<bool> DeleteIfExistsAsync(string containerName, string filePath, CancellationToken ct = default);

    /// <summary>Downloads the object to a stream, or null if it does not exist.</summary>
    Task<Stream?> DownloadAsync(string containerName, string filePath, CancellationToken ct = default);

    /// <summary>
    /// Returns the URL for an already-stored object without downloading it. Public containers
    /// get a plain URL; private containers get a short-lived read-only SAS URL.
    /// </summary>
    string GetUrl(string containerName, string filePath);
}
