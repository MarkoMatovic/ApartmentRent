using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace Lander.src.Infrastructure.FileStorage;

/// <summary>
/// How images that users upload are decoded. ImageSharp finds the format from the bytes, not from
/// the file name or the declared type, so a TIFF sent as "photo.png" would reach the TIFF decoder,
/// where ImageSharp 3.x has unpatched flaws (GHSA-j3p4-wp97-rph4, GHSA-wmxv-xphr-5c9g; fixed only
/// in 4.x, which needs a commercial licence). Only the formats the site accepts are known here,
/// so anything else fails as an unknown format. Animated images are read as their first frame, and
/// huge dimensions (a small file that expands to gigabytes of pixels) are refused before decoding.
/// </summary>
public static class SafeImageDecoding
{
    /// <summary>40 megapixels: more than any phone photo, far less than a decompression bomb.</summary>
    public const long MaxPixels = 40_000_000;

    private static readonly Configuration Formats = new(
        new JpegConfigurationModule(),
        new PngConfigurationModule(),
        new WebpConfigurationModule(),
        new GifConfigurationModule(),
        new BmpConfigurationModule());

    public static DecoderOptions Options { get; } = new()
    {
        Configuration = Formats,
        MaxFrames = 1,
    };

    /// <summary>Decodes an uploaded image; throws <see cref="UnknownImageFormatException"/>, <see cref="InvalidImageContentException"/> or <see cref="ImageTooLargeException"/>.</summary>
    public static Image Load(byte[] bytes)
    {
        var info = Image.Identify(Options, bytes);
        if ((long)info.Width * info.Height > MaxPixels)
            throw new ImageTooLargeException(info.Width, info.Height);
        return Image.Load(Options, bytes);
    }

    public static async Task<Image> LoadAsync(Stream stream, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        return Load(buffer.ToArray());
    }
}

public sealed class ImageTooLargeException(int width, int height)
    : Exception($"The image is {width}×{height} pixels, more than {SafeImageDecoding.MaxPixels} in total.");
