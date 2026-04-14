using FluentAssertions;
using Lander.src.Modules.Listings.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Text;

namespace LandlordApp.Tests.Controllers;

public class ImageUploadControllerTests
{
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<ILogger<ImageUploadController>> _mockLogger;
    private readonly ImageUploadController _controller;
    private readonly string _tempRoot;

    public ImageUploadControllerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);

        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockEnv.Setup(e => e.WebRootPath).Returns(_tempRoot);

        _mockLogger = new Mock<ILogger<ImageUploadController>>();

        _controller = new ImageUploadController(_mockEnv.Object, _mockLogger.Object);
        _controller.ControllerContext = MakeControllerContext();
    }

    // ─── No files ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadImages_NoFiles_ReturnsBadRequest()
    {
        var result = await _controller.UploadImages(new List<IFormFile>());

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadImages_NullFiles_ReturnsBadRequest()
    {
        var result = await _controller.UploadImages(null!);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ─── Too many files ───────────────────────────────────────────────────────

    [Fact]
    public async Task UploadImages_TooManyFiles_ReturnsBadRequest()
    {
        var files = Enumerable.Range(0, 11)
            .Select(_ => MakeFakeFile("img.jpg", JpegBytes()))
            .ToList();

        var result = await _controller.UploadImages(files);

        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.As<string>().Should().Contain("Maximum");
    }

    // ─── Extension check ──────────────────────────────────────────────────────

    [Fact]
    public async Task UploadImages_DisallowedExtension_ReturnsBadRequest()
    {
        var file = MakeFakeFile("virus.exe", new byte[] { 0x4D, 0x5A, 0x00 });

        var result = await _controller.UploadImages(new List<IFormFile> { file });

        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.As<string>().Should().Contain("not allowed");
    }

    // ─── Size check ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadImages_FileTooLarge_ReturnsBadRequest()
    {
        // 6 MB — over the 5 MB limit
        var bigContent = new byte[6 * 1024 * 1024];
        Array.Copy(JpegBytes(), bigContent, 3); // valid magic bytes
        var file = MakeFakeFile("big.jpg", bigContent);

        var result = await _controller.UploadImages(new List<IFormFile> { file });

        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.As<string>().Should().Contain("5 MB");
    }

    // ─── Magic byte validation ────────────────────────────────────────────────

    [Fact]
    public async Task UploadImages_WrongMagicBytes_ReturnsBadRequest()
    {
        // .jpg extension but content is plaintext (wrong magic bytes)
        var file = MakeFakeFile("fake.jpg", Encoding.UTF8.GetBytes("this is not an image"));

        var result = await _controller.UploadImages(new List<IFormFile> { file });

        result.Result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.As<string>().Should().Contain("content does not match");
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadImages_ValidJpeg_ReturnsOkWithUrl()
    {
        var file = MakeFakeFile("photo.jpg", JpegBytes());

        var result = await _controller.UploadImages(new List<IFormFile> { file });

        result.Result.Should().BeOfType<OkObjectResult>();
        var urls = result.Result.As<OkObjectResult>().Value.As<List<string>>();
        urls.Should().HaveCount(1);
        urls[0].Should().Contain("apartments");
    }

    [Fact]
    public async Task UploadImages_ValidPng_ReturnsOkWithUrl()
    {
        var file = MakeFakeFile("photo.png", PngBytes());

        var result = await _controller.UploadImages(new List<IFormFile> { file });

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UploadImages_MultipleValidFiles_ReturnsAllUrls()
    {
        var files = new List<IFormFile>
        {
            MakeFakeFile("a.jpg", JpegBytes()),
            MakeFakeFile("b.jpg", JpegBytes()),
        };

        var result = await _controller.UploadImages(files);

        result.Result.Should().BeOfType<OkObjectResult>();
        result.Result.As<OkObjectResult>().Value.As<List<string>>().Should().HaveCount(2);
    }

    [Fact]
    public async Task UploadImages_EmptyFileInBatch_Skipped()
    {
        var emptyFile = MakeFakeFile("empty.jpg", Array.Empty<byte>());
        var validFile = MakeFakeFile("real.jpg", JpegBytes());

        var result = await _controller.UploadImages(new List<IFormFile> { emptyFile, validFile });

        // Empty file is skipped; only the valid one is returned
        result.Result.Should().BeOfType<OkObjectResult>();
        result.Result.As<OkObjectResult>().Value.As<List<string>>().Should().HaveCount(1);
    }

    // ─── Cleanup ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static ControllerContext MakeControllerContext()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("userId", "1") }, "Test")),
            Request = { Scheme = "https", Host = new HostString("localhost") }
        };
        return new ControllerContext { HttpContext = httpContext };
    }

    private static IFormFile MakeFakeFile(string fileName, byte[] content)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.Length).Returns(content.Length);
        // Svaki poziv OpenReadStream vraća nov stream (sprečava ObjectDisposedException)
        mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((dest, ct) =>
                new MemoryStream(content).CopyToAsync(dest, ct));
        return mock.Object;
    }

    // Minimal valid magic byte sequences
    private static byte[] JpegBytes() => new byte[] { 0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    private static byte[] PngBytes()  => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };
}
