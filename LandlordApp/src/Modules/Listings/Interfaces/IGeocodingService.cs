namespace Lander.src.Modules.Listings.Interfaces;

/// <summary>A point on the map (WGS84), rounded to the 6 decimals the Apartments columns store.</summary>
public readonly record struct GeoPoint(decimal Latitude, decimal Longitude);

/// <summary>
/// Turns a street address into coordinates so a listing can be shown on the map. Free and keyless
/// (OpenStreetMap Nominatim). It never throws: when the address cannot be resolved, or the service
/// is unreachable, it returns null and the listing simply has no map.
/// </summary>
public interface IGeocodingService
{
    Task<GeoPoint?> GeocodeAsync(string? address, string? city, string? postalCode, CancellationToken ct = default);
}
