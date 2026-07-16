using System.Collections.Concurrent;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace Lander.src.Infrastructure.FileStorage;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IFileStorageService"/>, ported from the
/// Investment project. Containers are cached and created lazily using double-checked locking
/// so concurrent uploads (e.g. Task.WhenAll over a batch) never race on CreateIfNotExists.
///
/// Public containers (<see cref="FileStorageContainers.IsPublic"/>) are created with
/// PublicAccessType.Blob and served via plain URLs (no SAS). Private containers are created
/// with no public access and only handed out as short-lived read-only SAS URLs.
/// </summary>
public class AzureBlobStorageService : IFileStorageService
{
    private readonly AzureBlobStorageOptions _options;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ConcurrentDictionary<string, BlobContainerClient> _containerClients = new();
    private readonly SemaphoreSlim _containerCreationLock = new(1, 1);

    public AzureBlobStorageService(
        IOptions<AzureBlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _blobServiceClient = new BlobServiceClient(_options.ConnectionString);
    }

    /// <summary>
    /// Returns a cached <see cref="BlobContainerClient"/> for <paramref name="containerName"/>,
    /// creating the container (with the correct access level) the first time it is referenced.
    /// Double-checked locking ensures CreateIfNotExistsAsync runs at most once per container.
    /// </summary>
    private async Task<BlobContainerClient> GetOrCreateContainerAsync(string containerName, CancellationToken ct)
    {
        if (_containerClients.TryGetValue(containerName, out var cached))
            return cached;

        await _containerCreationLock.WaitAsync(ct);
        try
        {
            if (_containerClients.TryGetValue(containerName, out cached))
                return cached;

            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            var access = FileStorageContainers.IsPublic(containerName)
                ? PublicAccessType.Blob
                : PublicAccessType.None;
            await container.CreateIfNotExistsAsync(access, cancellationToken: ct);
            _containerClients[containerName] = container;
            return container;
        }
        finally
        {
            _containerCreationLock.Release();
        }
    }

    public async Task<string> UploadAsync(string containerName, string filePath, Stream content, string contentType, CancellationToken ct = default)
    {
        try
        {
            var containerClient = await GetOrCreateContainerAsync(containerName, ct);
            var blobClient = containerClient.GetBlobClient(filePath);

            if (content.CanSeek) content.Position = 0;

            await blobClient.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            }, ct);

            _logger.LogDebug("Uploaded {FilePath} to container {Container}", filePath, containerName);

            return BuildUrl(containerName, blobClient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload {FilePath} to container {Container}", filePath, containerName);
            throw;
        }
    }

    public async Task<bool> DeleteIfExistsAsync(string containerName, string filePath, CancellationToken ct = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            if (!await containerClient.ExistsAsync(ct)) return false;

            var blobClient = containerClient.GetBlobClient(filePath);
            var result = await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {FilePath} from container {Container}", filePath, containerName);
            return false;
        }
    }

    public async Task<Stream?> DownloadAsync(string containerName, string filePath, CancellationToken ct = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(filePath);

            if (!await blobClient.ExistsAsync(ct)) return null;

            var ms = new MemoryStream();
            await blobClient.DownloadToAsync(ms, ct);
            ms.Position = 0;
            return ms;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download {FilePath} from container {Container}", filePath, containerName);
            return null;
        }
    }

    public string GetUrl(string containerName, string filePath)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(filePath);
        return BuildUrl(containerName, blobClient);
    }

    private string BuildUrl(string containerName, BlobClient blobClient)
    {
        if (FileStorageContainers.IsPublic(containerName))
        {
            // Public container — plain URL. Prefer the configured CDN/public base URL if set.
            if (!string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
                return $"{_options.PublicBaseUrl.TrimEnd('/')}/{containerName}/{blobClient.Name}";
            return blobClient.Uri.ToString();
        }

        // Private container — short-lived read-only SAS URL.
        return GenerateReadOnlySasUri(blobClient).ToString();
    }

    private Uri GenerateReadOnlySasUri(BlobClient blobClient)
    {
        var sasBuilder = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddMinutes(_options.SasExpiryMinutes))
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        return blobClient.GenerateSasUri(sasBuilder);
    }
}
