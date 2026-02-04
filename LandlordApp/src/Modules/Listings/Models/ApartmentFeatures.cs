namespace Lander.src.Modules.Listings.Models;

/// <summary>
/// .NET 10 Feature: JSON column mapping for apartment features
/// Groups all boolean features into a single JSON object for better maintainability
/// </summary>
public class ApartmentFeatures
{
    public bool IsFurnished { get; set; }
    public bool HasBalcony { get; set; }
    public bool HasElevator { get; set; }
    public bool HasParking { get; set; }
    public bool HasInternet { get; set; }
    public bool HasAirCondition { get; set; }
    public bool IsPetFriendly { get; set; }
    public bool IsSmokingAllowed { get; set; }
}
