using Lander.src.Modules.Listings.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Listings.Services;

/// <summary>
/// Reads review statistics from the Reviews schema.
/// Lives inside the Listings module's infrastructure layer so the core
/// ApartmentService never touches ReviewsContext directly.
/// </summary>
public class ReviewStatsProvider : IReviewStatsProvider
{
    private readonly ReviewsContext _reviewsContext;

    public ReviewStatsProvider(ReviewsContext reviewsContext)
        => _reviewsContext = reviewsContext;

    public async Task<Dictionary<int, ReviewStats>> GetBatchAsync(
        IEnumerable<int> apartmentIds,
        CancellationToken ct = default)
    {
        var ids = apartmentIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<int, ReviewStats>();

        return await _reviewsContext.Reviews
            .AsNoTracking()
            .Where(r => r.ApartmentId.HasValue && ids.Contains(r.ApartmentId.Value) && r.IsPublic)
            .GroupBy(r => r.ApartmentId)
            .Select(g => new
            {
                ApartmentId = g.Key ?? 0,
                AverageRating = g.Average(r => (decimal?)r.Rating),
                ReviewCount = g.Count()
            })
            .ToDictionaryAsync(
                x => x.ApartmentId,
                x => new ReviewStats(x.AverageRating, x.ReviewCount),
                ct);
    }

    public async Task<ReviewStats?> GetForApartmentAsync(int apartmentId)
    {
        var result = await _reviewsContext.Reviews
            .AsNoTracking()
            .Where(r => r.ApartmentId == apartmentId && r.IsPublic)
            .GroupBy(r => r.ApartmentId)
            .Select(g => new
            {
                AverageRating = g.Average(r => (decimal?)r.Rating),
                ReviewCount = g.Count()
            })
            .FirstOrDefaultAsync();

        return result is null ? null : new ReviewStats(result.AverageRating, result.ReviewCount);
    }
}
