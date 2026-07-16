using Lander.src.Infrastructure.FileStorage;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// One-off, idempotent migration that backfills <c>BlobPath</c>/<c>ThumbnailPath</c> for
/// <see cref="Models.ApartmentImage"/> rows created before blob storage was introduced.
///
/// For each row missing a BlobPath it:
///   1. Derives the container-relative key from the legacy <c>ImageUrl</c>.
///   2. If the original file still exists under <c>wwwroot/uploads/apartments</c>, uploads it to
///      the configured storage (no-op when storage is already local disk) and generates a
///      400x300 WebP thumbnail.
///   3. Writes BlobPath (always) and ThumbnailPath (when a thumbnail was produced).
///
/// Idempotent: rows that already have a BlobPath are skipped, so it is safe to re-run.
/// Triggered manually by an admin endpoint; runs in the background.
/// </summary>
public sealed class ApartmentImageBlobMigrationService
{
    private readonly ListingsContext _context;
    private readonly IFileStorageService _storage;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ApartmentImageBlobMigrationService> _logger;

    public ApartmentImageBlobMigrationService(
        ListingsContext context,
        IFileStorageService storage,
        IWebHostEnvironment env,
        ILogger<ApartmentImageBlobMigrationService> logger)
    {
        _context = context;
        _storage = storage;
        _env = env;
        _logger = logger;
    }

    public sealed record Result(int Total, int Migrated, int ThumbnailsGenerated, int Skipped);

    public async Task<Result> MigrateAsync(CancellationToken ct = default)
    {
        // IgnoreQueryFilters so soft-deleted images are migrated too (they may be restored / audited).
        var pending = await _context.ApartmentImages
            .IgnoreQueryFilters()
            .Where(img => img.BlobPath == null && img.ImageUrl != null)
            .ToListAsync(ct);

        _logger.LogInformation("ApartmentImage blob migration: {Count} rows to process.", pending.Count);

        int migrated = 0, thumbs = 0, skipped = 0;
        var localRoot = Path.Combine(_env.WebRootPath ?? string.Empty, "uploads", FileStorageContainers.ApartmentImages);

        foreach (var img in pending)
        {
            ct.ThrowIfCancellationRequested();

            var key = ExtractKey(img.ImageUrl!);
            if (key is null)
            {
                _logger.LogWarning("Skipping image {ImageId}: could not derive a key from '{Url}'.", img.ImageId, img.ImageUrl);
                skipped++;
                continue;
            }

            try
            {
                var localPath = Path.Combine(localRoot, key.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(localPath))
                {
                    var bytes = await File.ReadAllBytesAsync(localPath, ct);
                    var contentType = key.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/webp";

                    using (var original = new MemoryStream(bytes))
                        await _storage.UploadAsync(FileStorageContainers.ApartmentImages, key, original, contentType, ct);

                    var thumbBytes = await ImageThumbnailFactory.TryBuildWebpThumbnailAsync(bytes, ct);
                    if (thumbBytes is not null)
                    {
                        var thumbKey = ImageThumbnailFactory.DeriveThumbnailKey(key);
                        using var thumb = new MemoryStream(thumbBytes);
                        await _storage.UploadAsync(FileStorageContainers.ApartmentImages, thumbKey, thumb, "image/webp", ct);
                        img.ThumbnailPath = thumbKey;
                        thumbs++;
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Original file not found for image {ImageId} at '{Path}'. Setting BlobPath only; thumbnail skipped.",
                        img.ImageId, localPath);
                }

                img.BlobPath = key;
                migrated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate image {ImageId} ('{Url}').", img.ImageId, img.ImageUrl);
                skipped++;
            }
        }

        if (migrated > 0)
            await _context.SaveChangesAsync(ct);

        var result = new Result(pending.Count, migrated, thumbs, skipped);
        _logger.LogInformation(
            "ApartmentImage blob migration complete: {Migrated}/{Total} migrated, {Thumbs} thumbnails, {Skipped} skipped.",
            result.Migrated, result.Total, result.ThumbnailsGenerated, result.Skipped);
        return result;
    }

    // Extracts the container-relative key from a legacy URL or relative path.
    // Handles both the legacy flat layout ("/uploads/apartments/{guid}.webp")
    // and the dated layout ("/apartments/{yyyy}/{MM}/{guid}.webp").
    private static string? ExtractKey(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Contains("..")) return null;

        var value = url.Trim();
        var cut = value.IndexOfAny(['?', '#']);
        if (cut >= 0) value = value[..cut];

        const string marker = "/" + FileStorageContainers.ApartmentImages + "/";
        var idx = value.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        string key;
        if (idx >= 0)
            key = value[(idx + marker.Length)..];
        else if (!Uri.TryCreate(value, UriKind.Absolute, out _))
            key = value;
        else
            return null;

        key = key.Trim('/');
        return key.Length == 0 || key.Contains("..") ? null : key;
    }
}
