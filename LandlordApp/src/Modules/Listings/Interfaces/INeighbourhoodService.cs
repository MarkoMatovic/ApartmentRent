using Lander.src.Modules.Listings.Dtos.Dto;

namespace Lander.src.Modules.Listings.Interfaces;

public interface INeighbourhoodService
{
    /// <summary>
    /// Returns neighbourhood insights (Walk/Transit/Bike scores + nearby POIs) for the given apartment.
    /// Results are cached for 24 hours. Returns <c>null</c> if the apartment has no coordinates.
    /// </summary>
    Task<NeighbourhoodInsightsDto?> GetInsightsAsync(int apartmentId, CancellationToken ct = default);
}
