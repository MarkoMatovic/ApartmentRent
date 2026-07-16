namespace Lander.src.Modules.Listings.Dtos.Dto;

/// <summary>Result of uploading a single image: the full-size URL and its thumbnail (null if generation failed).</summary>
public class UploadedImageDto
{
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}
