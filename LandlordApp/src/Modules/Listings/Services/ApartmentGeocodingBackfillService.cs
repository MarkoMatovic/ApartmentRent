using Lander.src.Modules.Listings.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// Hangfire recurring job (daily 05:00 UTC) that gives listings created before geocoding existed
/// (or when the geocoder was unreachable) their map coordinates.
///
/// A small batch per run keeps the job inside Nominatim's one-request-per-second policy; whatever
/// is left is picked up the next day. Apartments whose address cannot be resolved stay without a
/// map and are retried, which costs one cached lookup each.
///
/// Config: "Geocoding:BackfillBatchSize" (default 25).
/// </summary>
public sealed class ApartmentGeocodingBackfillService
{
    private readonly ListingsContext _db;
    private readonly IGeocodingService _geocoder;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApartmentGeocodingBackfillService> _logger;

    public ApartmentGeocodingBackfillService(
        ListingsContext db,
        IGeocodingService geocoder,
        IConfiguration configuration,
        ILogger<ApartmentGeocodingBackfillService> logger)
    {
        _db = db;
        _geocoder = geocoder;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> RunAsync()
    {
        var batchSize = Math.Max(1, _configuration.GetValue("Geocoding:BackfillBatchSize", 25));

        var pending = await _db.Apartments
            .IgnoreQueryFilters()
            .Where(a => !a.IsDeleted &&
                        a.IsActive &&
                        (a.Latitude == null || a.Longitude == null) &&
                        a.Address != null && a.City != null)
            .OrderBy(a => a.ApartmentId)
            .Take(batchSize)
            .ToListAsync();

        var updated = 0;
        foreach (var apartment in pending)
        {
            var point = await _geocoder.GeocodeAsync(apartment.Address, apartment.City, apartment.PostalCode);
            if (point is null) continue;

            apartment.Latitude = point.Value.Latitude;
            apartment.Longitude = point.Value.Longitude;
            updated++;
        }

        if (updated > 0)
            await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Geocoding backfill: {Updated} of {Checked} apartments received coordinates", updated, pending.Count);
        return updated;
    }
}
