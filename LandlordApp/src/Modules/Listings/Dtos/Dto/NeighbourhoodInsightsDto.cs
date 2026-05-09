namespace Lander.src.Modules.Listings.Dtos.Dto;

/// <summary>
/// Neighbourhood livability data returned by GET /api/v1/rent/neighbourhood/{apartmentId}.
/// Scores come from Walk Score API; POIs come from OpenStreetMap Overpass API.
/// </summary>
public sealed class NeighbourhoodInsightsDto
{
    // ── Walk Score ───────────────────────────────────────────────────────────────
    /// <summary>0–100. Null when Walk Score API key is not configured or request failed.</summary>
    public int?    WalkScore                { get; set; }
    public string? WalkScoreDescription    { get; set; }

    // ── Transit Score ────────────────────────────────────────────────────────────
    public int?    TransitScore             { get; set; }
    public string? TransitScoreDescription { get; set; }

    // ── Bike Score ───────────────────────────────────────────────────────────────
    public int?    BikeScore                { get; set; }
    public string? BikeScoreDescription    { get; set; }

    // ── Points of interest (OpenStreetMap) ───────────────────────────────────────
    public List<PointOfInterestDto> NearbyPlaces { get; set; } = new();

    /// <summary>UTC timestamp of when the data was fetched (for cache-busting UI hints).</summary>
    public DateTime FetchedAt { get; set; }
}

public sealed class PointOfInterestDto
{
    /// <summary>Human-readable name from OSM tags.</summary>
    public string  Name            { get; set; } = string.Empty;

    /// <summary>Category key: school | supermarket | restaurant | park | transit | healthcare | pharmacy.</summary>
    public string  Category        { get; set; } = string.Empty;

    /// <summary>Emoji icon that maps to the category — for quick frontend display.</summary>
    public string  Icon            { get; set; } = string.Empty;

    /// <summary>Straight-line distance in metres from the apartment.</summary>
    public int     DistanceMeters  { get; set; }

    public double  Latitude        { get; set; }
    public double  Longitude       { get; set; }
}
