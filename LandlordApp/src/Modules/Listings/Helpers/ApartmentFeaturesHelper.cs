using System.Text.Json;

namespace Lander.src.Modules.Listings.Helpers;

public class ApartmentFeaturesDto
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

public static class ApartmentFeaturesHelper
{
    private static readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web);

    public static string Serialize(
        bool isFurnished,
        bool hasBalcony,
        bool hasElevator,
        bool hasParking,
        bool hasInternet,
        bool hasAirCondition,
        bool isPetFriendly,
        bool isSmokingAllowed)
    {
        var dto = new ApartmentFeaturesDto
        {
            IsFurnished    = isFurnished,
            HasBalcony     = hasBalcony,
            HasElevator    = hasElevator,
            HasParking     = hasParking,
            HasInternet    = hasInternet,
            HasAirCondition = hasAirCondition,
            IsPetFriendly  = isPetFriendly,
            IsSmokingAllowed = isSmokingAllowed
        };
        return JsonSerializer.Serialize(dto, _opts);
    }

    public static ApartmentFeaturesDto Deserialize(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson) || featuresJson == "{}")
            return new ApartmentFeaturesDto();

        try
        {
            return JsonSerializer.Deserialize<ApartmentFeaturesDto>(featuresJson, _opts)
                   ?? new ApartmentFeaturesDto();
        }
        catch (JsonException)
        {
            return new ApartmentFeaturesDto();
        }
    }
}
