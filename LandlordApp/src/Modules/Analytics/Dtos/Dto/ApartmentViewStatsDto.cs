namespace Lander.src.Modules.Analytics.Dtos.Dto;

public class ApartmentViewStatsDto
{
    public int ApartmentId { get; set; }
    public string Title { get; set; } = null!;
    public string? City { get; set; }
    public decimal? Rent { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewed { get; set; }
}
