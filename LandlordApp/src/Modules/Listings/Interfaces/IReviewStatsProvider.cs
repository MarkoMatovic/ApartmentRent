namespace Lander.src.Modules.Listings.Interfaces;

/// <summary>
/// Provides review statistics for apartments without exposing the Reviews DbContext
/// to the Listings bounded context.  Implementations live in the Reviews module or a
/// cross-cutting infrastructure layer.
/// </summary>
public interface IReviewStatsProvider
{
    /// <summary>
    /// Returns average rating and public review count for each apartment in the list.
    /// Missing keys mean zero reviews for that apartment.
    /// </summary>
    Task<Dictionary<int, ReviewStats>> GetBatchAsync(
        IEnumerable<int> apartmentIds,
        CancellationToken ct = default);

    /// <summary>
    /// Returns review stats for a single apartment, or null if no public reviews exist.
    /// </summary>
    Task<ReviewStats?> GetForApartmentAsync(int apartmentId);
}

/// <param name="AverageRating">Weighted average of all public ratings, or null if no reviews.</param>
/// <param name="ReviewCount">Total number of public reviews.</param>
public record ReviewStats(decimal? AverageRating, int ReviewCount);
