namespace Lander.src.Modules.Listings.Dtos.Dto;
public class ApartmentImageDto
{
    public int ImageId { get; set; }
    public int? ApartmentId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPrimary { get; set; }
}
