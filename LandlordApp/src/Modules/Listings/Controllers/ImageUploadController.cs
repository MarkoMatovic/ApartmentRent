using Lander.Helpers;
using Lander.src.Infrastructure.FileStorage;
using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Listings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;

namespace Lander.src.Modules.Listings.Controllers;

[Route(ApiActionsV1.Rent)]
[ApiController]
[Authorize]
public class ImageUploadController : ControllerBase
{
    private readonly IFileStorageService _fileStorage;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ImageUploadController> _logger;

    // Maximum 10 files per request, 5 MB each
    private const int MaxFileSizeBytes = 5 * 1024 * 1024;
    private const int MaxFilesPerRequest = 10;

    private static readonly string[] AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"];

    /// <summary>
    /// Magic-byte signatures for allowed image formats.
    /// Key = human-readable name, Value = byte sequence that must appear at the start of the file.
    /// </summary>
    private static readonly Dictionary<string, byte[]> MagicBytes = new()
    {
        ["JPEG"]  = [0xFF, 0xD8, 0xFF],
        ["PNG"]   = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
        ["GIF87"] = [0x47, 0x49, 0x46, 0x38, 0x37, 0x61],  // GIF87a
        ["GIF89"] = [0x47, 0x49, 0x46, 0x38, 0x39, 0x61],  // GIF89a
        ["BMP"]   = [0x42, 0x4D],
    };

    // WebP has RIFF header + "WEBP" at offset 8 — checked separately
    private static readonly byte[] RiffHeader = [0x52, 0x49, 0x46, 0x46];
    private static readonly byte[] WebpMarker  = [0x57, 0x45, 0x42, 0x50];

    public ImageUploadController(
        IFileStorageService fileStorage,
        IServiceScopeFactory scopeFactory,
        ILogger<ImageUploadController> logger)
    {
        _fileStorage = fileStorage;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [HttpPost(ApiActionsV1.UploadImages, Name = nameof(ApiActionsV1.UploadImages))]
    [Microsoft.AspNetCore.Http.Timeouts.RequestTimeout(60_000)] // 60s — ImageSharp processing može biti sporo za 10 fajlova
    public async Task<ActionResult<List<UploadedImageDto>>> UploadImages([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded.");

        if (files.Count > MaxFilesPerRequest)
            return BadRequest($"Maximum {MaxFilesPerRequest} files per request.");

        var uploadedImages = new List<UploadedImageDto>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            // ── Extension check ───────────────────────────────────────────────
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return BadRequest($"File type '{extension}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}");

            // ── Size check ────────────────────────────────────────────────────
            if (file.Length > MaxFileSizeBytes)
                return BadRequest($"File '{Path.GetFileName(file.FileName)}' exceeds the 5 MB limit.");

            // ── MIME magic-byte check ─────────────────────────────────────────
            if (!await IsValidImageMagicBytes(file))
            {
                _logger.LogWarning(
                    "File upload rejected — magic byte mismatch. Filename: {Name}, Extension: {Ext}, ContentType: {Ct}",
                    file.FileName, extension, file.ContentType);
                return BadRequest($"File '{Path.GetFileName(file.FileName)}' content does not match its extension.");
            }

            // ── Process via ImageSharp: strip EXIF + normalise format ─────────
            // Re-encoding through ImageSharp:
            //   • Removes all EXIF metadata (GPS location, device info, etc.)
            //   • Acts as a second-layer polyglot defence (re-decodes pixel data)
            //   • Converts to WebP for smaller file sizes; falls back to JPEG/PNG
            //     for formats where WebP is unsuitable.
            // Always save as .webp for uniformity except for original PNG/GIF/BMP
            // which may need lossless handling.
            byte[] processedBytes;
            string saveExtension;
            try
            {
                using var img = await Image.LoadAsync(file.OpenReadStream());
                // Strip all EXIF, XMP, IPTC metadata
                img.Metadata.ExifProfile = null;
                img.Metadata.XmpProfile = null;
                img.Metadata.IptcProfile = null;

                // Downscale if wider/taller than 2000px while keeping aspect ratio
                const int MaxDimension = 2000;
                if (img.Width > MaxDimension || img.Height > MaxDimension)
                    img.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new SixLabors.ImageSharp.Size(MaxDimension, MaxDimension)
                    }));

                using var ms = new MemoryStream();
                if (extension == ".png")
                {
                    await img.SaveAsync(ms, new PngEncoder());
                    saveExtension = ".png";
                }
                else
                {
                    // Convert everything else to WebP (smaller & widely supported)
                    await img.SaveAsync(ms, new WebpEncoder { Quality = 85 });
                    saveExtension = ".webp";
                }
                processedBytes = ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ImageSharp processing failed for {File}; rejecting upload.", file.FileName);
                return BadRequest($"File '{Path.GetFileName(file.FileName)}' could not be processed as a valid image.");
            }

            // ── Save via storage abstraction (Azure Blob in prod, local disk in dev) ──
            // Folder scheme {yyyy}/{MM}/{guid}.ext. (apartmentId is not known at upload
            // time — images are uploaded before the apartment is created — so it is not
            // part of the path; can be added once the flow passes it.)
            var now = DateTime.UtcNow;
            var blobPath = $"{now:yyyy}/{now:MM}/{Guid.NewGuid()}{saveExtension}";
            var contentType = saveExtension == ".png" ? "image/png" : "image/webp";

            using var uploadStream = new MemoryStream(processedBytes);
            var fileUrl = await _fileStorage.UploadAsync(
                FileStorageContainers.ApartmentImages, blobPath, uploadStream, contentType);
            fileUrl = ToAbsolute(fileUrl);

            // ── Thumbnail (best-effort) ──────────────────────────────────────
            // 400x300 WebP @ q50. Failure never aborts the original upload — the client
            // simply falls back to the full-size image. Path follows the -thumb.webp convention
            // so ApartmentService can derive ThumbnailPath without round-tripping it.
            string? thumbnailUrl = null;
            var thumbBytes = await ImageThumbnailFactory.TryBuildWebpThumbnailAsync(processedBytes);
            if (thumbBytes is not null)
            {
                var thumbPath = ImageThumbnailFactory.DeriveThumbnailKey(blobPath);
                using var thumbStream = new MemoryStream(thumbBytes);
                var thumbUrl = await _fileStorage.UploadAsync(
                    FileStorageContainers.ApartmentImages, thumbPath, thumbStream, "image/webp");
                thumbnailUrl = ToAbsolute(thumbUrl);
            }
            else
            {
                _logger.LogWarning("Thumbnail generation failed for {BlobPath}; serving full image only.", blobPath);
            }

            uploadedImages.Add(new UploadedImageDto { Url = fileUrl, ThumbnailUrl = thumbnailUrl });

            _logger.LogInformation("Image uploaded: {BlobPath}", blobPath);
        }

        return Ok(uploadedImages);
    }

    // Local storage returns a site-relative URL; promote it to absolute for the client.
    // Azure already returns an absolute blob/CDN URL.
    private string ToAbsolute(string url)
        => url.StartsWith('/') ? $"{Request.Scheme}://{Request.Host}{url}" : url;

    /// <summary>
    /// Admin-only, idempotent one-off migration: backfills BlobPath/ThumbnailPath for legacy
    /// ApartmentImage rows and generates missing thumbnails. Runs in the background; progress
    /// and the final summary are written to the logs. Safe to re-run.
    /// </summary>
    [HttpPost("admin/migrate-images-to-blob")]
    [Authorize(Roles = "Admin")]
    public IActionResult MigrateImagesToBlob()
    {
        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var migrator = scope.ServiceProvider.GetRequiredService<ApartmentImageBlobMigrationService>();
            try
            {
                await migrator.MigrateAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                scope.ServiceProvider
                    .GetRequiredService<ILogger<ImageUploadController>>()
                    .LogError(ex, "ApartmentImage blob migration failed.");
            }
        });

        return Accepted(new { message = "Apartment image blob migration started. Check logs for progress." });
    }

    // ── Magic byte validation ────────────────────────────────────────────────

    private static async Task<bool> IsValidImageMagicBytes(IFormFile file)
    {
        // Read first 12 bytes — enough for any signature we check
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read < 3) return false;

        // Standard magic-byte formats
        foreach (var (_, magic) in MagicBytes)
        {
            if (read >= magic.Length && header.Take(magic.Length).SequenceEqual(magic))
                return true;
        }

        // WebP: starts with RIFF (4 bytes) + 4-byte file size + "WEBP" (4 bytes)
        if (read >= 12
            && header[..4].SequenceEqual(RiffHeader)
            && header[8..12].SequenceEqual(WebpMarker))
        {
            return true;
        }

        return false;
    }
}
