using FluentAssertions;
using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using Lander.src.Infrastructure.FileStorage;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using LandlordApp.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace LandlordApp.Tests.Services;

public class ProfilePictureServiceTests
{
    private readonly InMemoryFileStorage _storage = new();
    private readonly ProfilePictureService _service;
    private readonly Guid _user = Guid.NewGuid();

    public ProfilePictureServiceTests()
    {
        _service = new ProfilePictureService(_storage, NullLogger<ProfilePictureService>.Instance);
    }

    /// <summary>A JPEG photo with a camera name and a GPS position in its EXIF data, as a data URL.</summary>
    public static string PhotoDataUrl(int width = 800, int height = 600)
    {
        using var image = new Image<Rgba32>(width, height, Color.SteelBlue);
        image.Metadata.ExifProfile = new ExifProfile();
        image.Metadata.ExifProfile.SetValue(ExifTag.Make, "TestCam");
        image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitudeRef, "N");
        image.Metadata.ExifProfile.SetValue(ExifTag.GPSLatitude, new[] { new Rational(44), new Rational(49), new Rational(0) });
        using var output = new MemoryStream();
        image.SaveAsJpeg(output);
        return "data:image/jpeg;base64," + Convert.ToBase64String(output.ToArray());
    }

    private (string Path, byte[] Bytes) SingleAvatar()
    {
        var file = _storage.Files.Should().ContainSingle().Subject;
        file.Key.Container.Should().Be(FileStorageContainers.Avatars);
        return (file.Key.Path, file.Value.Bytes);
    }

    [Fact]
    public async Task ResolveAsync_DataUrl_StoresA320pxWebpWithoutMetadata()
    {
        var url = await _service.ResolveAsync(_user, null, PhotoDataUrl());

        var (path, bytes) = SingleAvatar();
        url.Should().Be($"{InMemoryFileStorage.BaseUrl}/avatars/{path}");
        path.Should().MatchRegex($"^{_user:N}/[0-9a-f]{{32}}\\.webp$");
        _storage.Files.Single().Value.ContentType.Should().Be("image/webp");

        Image.DetectFormat(bytes).Should().Be(WebpFormat.Instance);
        using var stored = Image.Load(bytes);
        stored.Width.Should().Be(ProfilePictureService.SizePx);
        stored.Height.Should().Be(ProfilePictureService.SizePx);
        (stored.Metadata.ExifProfile?.Values ?? []).Should().BeEmpty("the camera and the GPS position are not published");
    }

    [Fact]
    public async Task ResolveAsync_SameValueAsStored_KeepsItWithoutUploading()
    {
        const string current = InMemoryFileStorage.BaseUrl + "/avatars/a/b.webp";

        var result = await _service.ResolveAsync(_user, current, current);

        result.Should().Be(current);
        _storage.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_EmptyString_RemovesThePicture()
    {
        var result = await _service.ResolveAsync(_user, InMemoryFileStorage.BaseUrl + "/avatars/a/b.webp", "");

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("https://evil.example/tracker.png")]
    [InlineData("/uploads/apartments/x.webp")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=")]
    [InlineData("data:image/png;base64,bm90IGFuIGltYWdl")] // "not an image"
    [InlineData("data:image/png;base64,***")]
    public async Task ResolveAsync_AnythingButAnImage_IsRefused(string submitted)
    {
        var act = () => _service.ResolveAsync(_user, null, submitted);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.InvalidProfilePicture);
        _storage.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ATiffPassedOffAsPng_IsRefused_BecauseOnlyTheAcceptedFormatsAreDecoded()
    {
        using var image = new Image<Rgba32>(50, 50, Color.Red);
        using var tiff = new MemoryStream();
        image.Save(tiff, new SixLabors.ImageSharp.Formats.Tiff.TiffEncoder());
        var disguised = "data:image/png;base64," + Convert.ToBase64String(tiff.ToArray());

        var act = () => _service.ResolveAsync(_user, null, disguised);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.InvalidProfilePicture);
        _storage.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_ASmallFileWithHugeDimensions_IsRefusedBeforeItsPixelsAreAllocated()
    {
        var bomb = "data:image/png;base64," + Convert.ToBase64String(PngHeaderOnly(width: 50_000, height: 50_000));

        var act = () => _service.ResolveAsync(_user, null, bomb);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.ProfilePictureTooLarge);
    }

    /// <summary>A PNG signature, an IHDR chunk claiming the given size and IEND: enough to identify, nothing to decode.</summary>
    private static byte[] PngHeaderOnly(int width, int height)
    {
        static byte[] Chunk(string type, byte[] data)
        {
            var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            var chunk = new List<byte>();
            chunk.AddRange(BigEndian(data.Length));
            chunk.AddRange(typeBytes);
            chunk.AddRange(data);
            chunk.AddRange(BigEndian((int)Crc32(typeBytes.Concat(data).ToArray())));
            return chunk.ToArray();
        }
        static byte[] BigEndian(int value) => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];
        static uint Crc32(byte[] bytes)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var b in bytes)
            {
                crc ^= b;
                for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            }
            return ~crc;
        }

        var ihdr = BigEndian(width).Concat(BigEndian(height)).Concat(new byte[] { 8, 6, 0, 0, 0 }).ToArray();
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }
            .Concat(Chunk("IHDR", ihdr))
            .Concat(Chunk("IEND", []))
            .ToArray();
    }

    [Fact]
    public async Task ResolveAsync_LargerThan5Mb_IsRefusedBeforeDecoding()
    {
        var huge = "data:image/png;base64," + new string('A', (ProfilePictureService.MaxBytes / 3 + 10) * 4);

        var act = () => _service.ResolveAsync(_user, null, huge);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.ProfilePictureTooLarge);
        _storage.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_NewPicture_DoesNotDeleteThePreviousOne()
    {
        var first = await _service.ResolveAsync(_user, null, PhotoDataUrl());

        var second = await _service.ResolveAsync(_user, first, PhotoDataUrl(400, 400));

        second.Should().NotBe(first);
        _storage.PathsIn(FileStorageContainers.Avatars).Should().HaveCount(2, "the caller deletes the old picture once the new one is saved");
    }

    [Fact]
    public async Task DeleteAsync_OurAvatar_RemovesIt_WhetherBlobOrLocalAddress()
    {
        var blobUrl = await _service.StoreInlineAsync(_user, PhotoDataUrl());
        var localKey = $"{Guid.NewGuid():N}/{Guid.NewGuid():N}.webp";
        await _storage.UploadAsync(FileStorageContainers.Avatars, localKey, new MemoryStream([1]), "image/webp");

        await _service.DeleteAsync(blobUrl);
        await _service.DeleteAsync($"/uploads/avatars/{localKey}");

        _storage.Files.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("data:image/png;base64,AAAA")]
    [InlineData("https://elsewhere.example/avatars/x.webp")]
    [InlineData("https://storage.test/apartments/0123456789abcdef0123456789abcdef/0123456789abcdef0123456789abcdef.webp")]
    [InlineData("https://storage.test/avatars/../apartments/x.webp")]
    public async Task DeleteAsync_AnythingElse_DeletesNothing(string? stored)
    {
        var key = $"{Guid.NewGuid():N}/{Guid.NewGuid():N}.webp";
        await _storage.UploadAsync(FileStorageContainers.Avatars, key, new MemoryStream([1]), "image/webp");
        await _storage.UploadAsync(FileStorageContainers.ApartmentImages, key, new MemoryStream([1]), "image/webp");

        await _service.DeleteAsync(stored);

        _storage.Files.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteAsync_StorageFailure_DoesNotThrow()
    {
        var storage = new Mock<IFileStorageService>();
        storage.Setup(s => s.DeleteIfExistsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("storage down"));
        var service = new ProfilePictureService(storage.Object, NullLogger<ProfilePictureService>.Instance);

        var act = () => service.DeleteAsync($"https://storage.test/avatars/{Guid.NewGuid():N}/{Guid.NewGuid():N}.webp");

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("https://acc.blob.core.windows.net/avatars/0123456789abcdef0123456789abcdef/fedcba9876543210fedcba9876543210.webp",
                "0123456789abcdef0123456789abcdef/fedcba9876543210fedcba9876543210.webp")]
    [InlineData("/uploads/avatars/0123456789abcdef0123456789abcdef/fedcba9876543210fedcba9876543210.webp?v=2",
                "0123456789abcdef0123456789abcdef/fedcba9876543210fedcba9876543210.webp")]
    [InlineData("https://acc.blob.core.windows.net/avatars/other.png", null)]
    public void KeyOf_ReadsOnlyOurOwnAvatarKeys(string url, string? expected)
    {
        ProfilePictureService.KeyOf(url).Should().Be(expected);
    }
}
