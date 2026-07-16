namespace Lander.src.Modules.Listings.Dtos.Dto;
public class ApartmentImageDto
{
    public int ImageId { get; set; }
    public int? ApartmentId { get; set; }
    public string? ImageUrl { get; set; }
    /// <summary>Small preview URL (400x300). Null when no thumbnail exists; clients fall back to ImageUrl.</summary>
    public string? ThumbnailUrl { get; set; }
    public bool IsPrimary { get; set; }
}
