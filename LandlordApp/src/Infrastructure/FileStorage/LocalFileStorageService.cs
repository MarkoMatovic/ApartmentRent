namespace Lander.src.Infrastructure.FileStorage;

/// <summary>
/// Local-disk fallback used in development when no Azure Storage connection string is set.
/// Writes under <c>wwwroot/uploads/{container}/{filePath}</c> (served by UseStaticFiles) and
/// returns a relative URL. Mirrors <see cref="AzureBlobStorageService"/> so the rest of the
/// app is storage-agnostic.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IWebHostEnvironment env, ILogger<LocalFileStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    private string RootFor(string containerName)
        => Path.Combine(_env.WebRootPath, "uploads", containerName);

    public async Task<string> UploadAsync(string containerName, string filePath, Stream content, string contentType, CancellationToken ct = default)
    {
        var safeRelative = filePath.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.Combine(RootFor(containerName), safeRelative.Replace('/', Path.DirectorySeparatorChar));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        if (content.CanSeek) content.Position = 0;
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            await content.CopyToAsync(fs, ct);

        _logger.LogDebug("Saved {FilePath} to local container {Container}", safeRelative, containerName);
        return GetUrl(containerName, safeRelative);
    }

    public Task<bool> DeleteIfExistsAsync(string containerName, string filePath, CancellationToken ct = default)
    {
        var safeRelative = filePath.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.Combine(RootFor(containerName), safeRelative.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<Stream?> DownloadAsync(string containerName, string filePath, CancellationToken ct = default)
    {
        var safeRelative = filePath.Replace('\\', '/').TrimStart('/');
        var fullPath = Path.Combine(RootFor(containerName), safeRelative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }

    // Relative URL served by UseStaticFiles; callers compose an absolute URL if needed.
    public string GetUrl(string containerName, string filePath)
        => $"/uploads/{containerName}/{filePath.Replace('\\', '/').TrimStart('/')}";
}
