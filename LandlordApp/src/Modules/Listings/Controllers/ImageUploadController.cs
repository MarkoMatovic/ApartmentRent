using Lander.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Listings.Controllers;

[Route(ApiActionsV1.Rent)]
[ApiController]
[Authorize]
public class ImageUploadController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ImageUploadController> _logger;

    public ImageUploadController(IWebHostEnvironment environment, ILogger<ImageUploadController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpPost(ApiActionsV1.UploadImages, Name = nameof(ApiActionsV1.UploadImages))]
    public async Task<ActionResult<List<string>>> UploadImages([FromForm] List<IFormFile> files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest("No files uploaded");
            }

            var uploadedUrls = new List<string>();
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "apartments");

            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var file in files)
            {
                // Validate file
                if (file.Length == 0)
                {
                    continue;
                }

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".jfif", ".bmp" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest($"File type {extension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");
                }

                // Validate file size (max 5MB)
                if (file.Length > 5 * 1024 * 1024)
                {
                    return BadRequest($"File {file.FileName} is too large. Maximum size is 5MB");
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Generate URL
                var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/apartments/{uniqueFileName}";
                uploadedUrls.Add(fileUrl);

                _logger.LogInformation($"Uploaded image: {uniqueFileName}");
            }

            return Ok(uploadedUrls);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading images");
            return StatusCode(500, "An error occurred while uploading images");
        }
    }
}
