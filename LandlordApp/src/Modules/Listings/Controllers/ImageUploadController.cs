using Lander.Helpers;
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
    private readonly IWebHostEnvironment _environment;
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

    public ImageUploadController(IWebHostEnvironment environment, ILogger<ImageUploadController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpPost(ApiActionsV1.UploadImages, Name = nameof(ApiActionsV1.UploadImages))]
    public async Task<ActionResult<List<string>>> UploadImages([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded.");

        if (files.Count > MaxFilesPerRequest)
            return BadRequest($"Maximum {MaxFilesPerRequest} files per request.");

        var uploadedUrls = new List<string>();
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "apartments");

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

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

            // ── Save ──────────────────────────────────────────────────────────
            var uniqueFileName = $"{Guid.NewGuid()}{saveExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            await System.IO.File.WriteAllBytesAsync(filePath, processedBytes);

            var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/apartments/{uniqueFileName}";
            uploadedUrls.Add(fileUrl);

            _logger.LogInformation("Image uploaded: {FileName}", uniqueFileName);
        }

        return Ok(uploadedUrls);
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
