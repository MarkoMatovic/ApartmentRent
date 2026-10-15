using System.Text.RegularExpressions;
using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using Lander.src.Infrastructure.FileStorage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Lander.src.Modules.Users.Implementation.UserImplementation;

/// <summary>
/// Profile pictures live in file storage (the public "avatars" container), not in the database: the
/// Users table only keeps the short address. They used to be stored as base64 text of up to ~7 MB and
/// travelled with every chat message, every roommate card and every list query.
/// </summary>
public interface IProfilePictureService
{
    /// <summary>True for a picture still kept inline, as a "data:image/..." string.</summary>
    bool IsInline(string? value);

    /// <summary>
    /// What should be stored after a profile update. A data URL is checked, shrunk and uploaded (its address
    /// is returned); the value that is already stored stays as it is; an empty string removes the picture.
    /// Any other value (for example somebody else's URL) is refused. Nothing is deleted here: the caller
    /// deletes the previous picture once the new value is saved (and the new one if saving fails).
    /// </summary>
    Task<string?> ResolveAsync(Guid userGuid, string? current, string submitted, CancellationToken ct = default);

    /// <summary>Uploads an inline picture and returns its address (also used to move old rows out of the database).</summary>
    Task<string> StoreInlineAsync(Guid userGuid, string dataUrl, CancellationToken ct = default);

    /// <summary>Deletes a stored picture of ours; addresses of anything else are left alone.</summary>
    Task DeleteAsync(string? storedUrl, CancellationToken ct = default);
}

public sealed class ProfilePictureService : IProfilePictureService
{
    /// <summary>Largest picture accepted, after base64 decoding.</summary>
    public const int MaxBytes = 5 * 1024 * 1024;

    /// <summary>Square side of the stored picture; avatars are shown at up to ~150 px.</summary>
    public const int SizePx = 320;

    private static readonly Regex DataUrl = new(
        @"^data:image/(png|jpe?g|webp|gif|bmp);base64,(?<data>[A-Za-z0-9+/=\s]+)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IFileStorageService _storage;
    private readonly ILogger<ProfilePictureService> _logger;

    public ProfilePictureService(IFileStorageService storage, ILogger<ProfilePictureService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public bool IsInline(string? value) =>
        value is not null && value.StartsWith("data:", StringComparison.OrdinalIgnoreCase);

    public async Task<string?> ResolveAsync(Guid userGuid, string? current, string submitted, CancellationToken ct = default)
    {
        if (submitted.Length == 0)
            return null;

        if (string.Equals(submitted, current, StringComparison.Ordinal))
            return current;

        if (!IsInline(submitted))
            throw new BusinessRuleException(BusinessErrorCodes.InvalidProfilePicture);

        return await StoreInlineAsync(userGuid, submitted, ct);
    }

    public async Task<string> StoreInlineAsync(Guid userGuid, string dataUrl, CancellationToken ct = default)
    {
        var match = DataUrl.Match(dataUrl);
        if (!match.Success)
            throw new BusinessRuleException(BusinessErrorCodes.InvalidProfilePicture);

        var base64 = match.Groups["data"].Value;
        // 4 base64 characters carry 3 bytes: refuse an oversized picture before decoding it.
        if ((long)base64.Length * 3 / 4 > MaxBytes + 3)
            throw new BusinessRuleException(BusinessErrorCodes.ProfilePictureTooLarge);

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            throw new BusinessRuleException(BusinessErrorCodes.InvalidProfilePicture);
        }
        if (bytes.Length > MaxBytes)
            throw new BusinessRuleException(BusinessErrorCodes.ProfilePictureTooLarge);

        byte[] webp;
        try
        {
            using var image = SafeImageDecoding.Load(bytes);
            // Photos from phones carry the place they were taken (GPS) and the camera: none of it is published.
            image.Metadata.ExifProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IptcProfile = null;
            image.Mutate(x => x.AutoOrient().Resize(new ResizeOptions
            {
                Mode = ResizeMode.Crop,
                Size = new Size(SizePx, SizePx),
            }));

            using var output = new MemoryStream();
            await image.SaveAsync(output, new WebpEncoder { Quality = 80 }, ct);
            webp = output.ToArray();
        }
        catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException or ImageFormatException)
        {
            throw new BusinessRuleException(BusinessErrorCodes.InvalidProfilePicture);
        }
        catch (ImageTooLargeException)
        {
            throw new BusinessRuleException(BusinessErrorCodes.ProfilePictureTooLarge);
        }

        // A new name for every upload: a browser or CDN that cached the old picture never shows it again.
        var key = $"{userGuid:N}/{Guid.NewGuid():N}.webp";
        using var content = new MemoryStream(webp);
        return await _storage.UploadAsync(FileStorageContainers.Avatars, key, content, "image/webp", ct);
    }

    public async Task DeleteAsync(string? storedUrl, CancellationToken ct = default)
    {
        var key = KeyOf(storedUrl);
        if (key is null) return;
        try
        {
            await _storage.DeleteIfExistsAsync(FileStorageContainers.Avatars, key, ct);
        }
        catch (Exception ex)
        {
            // A picture that stays behind costs a few kilobytes; it must never fail the profile update.
            _logger.LogWarning(ex, "Could not delete the old profile picture {Key}", key);
        }
    }

    /// <summary>
    /// The storage key of one of our avatars ("{user}/{name}.webp"), from either a blob/CDN address or the
    /// local "/uploads/avatars/..." one; null for anything else (inline data, foreign URLs, nothing).
    /// </summary>
    public static string? KeyOf(string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl) || storedUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return null;

        var path = Uri.TryCreate(storedUrl, UriKind.Absolute, out var uri) ? uri.AbsolutePath : storedUrl.Split('?')[0];
        var marker = "/" + FileStorageContainers.Avatars + "/";
        var at = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (at < 0) return null;

        var key = path[(at + marker.Length)..];
        return Regex.IsMatch(key, @"^[0-9a-f]{32}/[0-9a-f]{32}\.webp$", RegexOptions.IgnoreCase) ? key : null;
    }
}
