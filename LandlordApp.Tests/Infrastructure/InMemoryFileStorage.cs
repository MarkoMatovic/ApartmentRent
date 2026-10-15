using System.Collections.Concurrent;
using Lander.src.Infrastructure.FileStorage;

namespace LandlordApp.Tests.Infrastructure;

/// <summary>File storage kept in memory, with blob-like addresses: "https://storage.test/{container}/{path}".</summary>
public sealed class InMemoryFileStorage : IFileStorageService
{
    public const string BaseUrl = "https://storage.test";

    public ConcurrentDictionary<(string Container, string Path), (byte[] Bytes, string ContentType)> Files { get; } = new();

    /// <summary>When set, every upload fails with this exception (storage unreachable).</summary>
    public Exception? FailUploadsWith { get; set; }

    public async Task<string> UploadAsync(string containerName, string filePath, Stream content, string contentType, CancellationToken ct = default)
    {
        if (FailUploadsWith is not null) throw FailUploadsWith;
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, ct);
        Files[(containerName, filePath)] = (copy.ToArray(), contentType);
        return GetUrl(containerName, filePath);
    }

    public Task<bool> DeleteIfExistsAsync(string containerName, string filePath, CancellationToken ct = default)
        => Task.FromResult(Files.TryRemove((containerName, filePath), out _));

    public Task<Stream?> DownloadAsync(string containerName, string filePath, CancellationToken ct = default)
        => Task.FromResult<Stream?>(Files.TryGetValue((containerName, filePath), out var f) ? new MemoryStream(f.Bytes) : null);

    public string GetUrl(string containerName, string filePath) => $"{BaseUrl}/{containerName}/{filePath}";

    public IReadOnlyList<string> PathsIn(string containerName) =>
        Files.Keys.Where(k => k.Container == containerName).Select(k => k.Path).ToList();

    public void Reset()
    {
        Files.Clear();
        FailUploadsWith = null;
    }
}
