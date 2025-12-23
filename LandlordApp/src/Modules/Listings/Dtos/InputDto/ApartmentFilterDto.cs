using Lander.src.Modules.Listings.Models;
using System.Text.Json.Serialization;

namespace Lander.src.Modules.Listings.Dtos.InputDto;

public class ApartmentFilterDto
{
    [JsonPropertyName("city")]
    public string? City { get; set; }
    
    [JsonPropertyName("minRent")]
    public decimal? MinRent { get; set; }
    
    [JsonPropertyName("maxRent")]
    public decimal? MaxRent { get; set; }
    
    [JsonPropertyName("numberOfRooms")]
    public int? NumberOfRooms { get; set; }
    
    [JsonPropertyName("apartmentType")]
    public ApartmentType? ApartmentType { get; set; }
    
    [JsonPropertyName("isFurnished")]
    public bool? IsFurnished { get; set; }
    
    [JsonPropertyName("isPetFriendly")]
    public bool? IsPetFriendly { get; set; }
    
    [JsonPropertyName("isSmokingAllowed")]
    public bool? IsSmokingAllowed { get; set; }
    
    [JsonPropertyName("hasParking")]
    public bool? HasParking { get; set; }
    
    [JsonPropertyName("hasBalcony")]
    public bool? HasBalcony { get; set; }
    
    [JsonPropertyName("isImmediatelyAvailable")]
    public bool? IsImmediatelyAvailable { get; set; }
    
    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;
}
