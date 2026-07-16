using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Lander.src.Infrastructure.FileStorage;

/// <summary>
/// Builds card-sized WebP thumbnails (adapted from the Investment project's ImageThumbnailFactory).
/// Thumbnail generation is best-effort: a failure returns null and must never abort the upload of
/// the original image.
/// </summary>
public static class ImageThumbnailFactory
{
    public const int WidthPx = 400;
    public const int HeightPx = 300;
    public const int Quality = 50;

    /// <summary>
    /// Produces a 400x300 (cropped-to-fill) WebP thumbnail from already-decoded image bytes.
    /// Returns null if the image cannot be processed.
    /// </summary>
    public static async Task<byte[]?> TryBuildWebpThumbnailAsync(byte[] sourceBytes, CancellationToken ct = default)
    {
        try
        {
            using var input = new MemoryStream(sourceBytes);
            using var image = await Image.LoadAsync(input, ct);

            image.Metadata.ExifProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IptcProfile = null;

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Size = new Size(WidthPx, HeightPx)
            }));

            using var output = new MemoryStream();
            await image.SaveAsync(output, new WebpEncoder { Quality = Quality }, ct);
            return output.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Derives the conventional thumbnail key from a full-size image key:
    /// "2026/06/{guid}.webp" -> "2026/06/{guid}-thumb.webp". Thumbnails are always WebP.
    /// </summary>
    public static string DeriveThumbnailKey(string imageKey)
    {
        var normalized = imageKey.Replace('\\', '/');
        var lastSlash = normalized.LastIndexOf('/');
        var dir = lastSlash >= 0 ? normalized[..lastSlash] : string.Empty;
        var fileName = lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;
        var dot = fileName.LastIndexOf('.');
        var nameNoExt = dot >= 0 ? fileName[..dot] : fileName;
        var thumbName = $"{nameNoExt}-thumb.webp";
        return string.IsNullOrEmpty(dir) ? thumbName : $"{dir}/{thumbName}";
    }
}
