using Google.Protobuf.WellKnownTypes;
using Lander.Helpers;
using Lander.src.Common.Exceptions;
using Lander.src.Modules.Reviews.Interfaces;
using Lander.src.Modules.Reviews.Modules;
using Lander.src.Modules.Reviews.proto;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Reviews.Implementation;

/// <summary>
/// Reviews/Favorites module implementation, invoked in-process by
/// <see cref="Controllers.ReviewsFavoritesController"/>.
/// </summary>
/// <remarks>
/// Callers are authenticated at the REST boundary: the controller carries [Authorize],
/// resolves the caller from the JWT, and overwrites any client-supplied UserId /
/// CreatedByGuid before calling in. This service therefore treats <c>callerGuid</c> and
/// <c>request.CreatedByGuid</c> as trusted, and enforces ownership against them —
/// it must never be given a value taken straight from a request body.
/// </remarks>
public class ReviewFavoriteService : IReviewFavoriteService
{
    private readonly ReviewsContext _context;

    public ReviewFavoriteService(ReviewsContext context)
    {
        _context = context;
    }

    public async Task<FavoriteResponse> CreateFavoriteAsync(CreateFavoriteRequest request)
    {
        if (!Guid.TryParse(request.CreatedByGuid, out var favGuid))
            throw new ArgumentException("Invalid CreatedByGuid format", nameof(request));

        var favorite = new Favorite
        {
            UserId = request.UserId,
            ApartmentId = request.ApartmentId,
            CreatedByGuid = favGuid,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = favGuid,
            ModifiedDate = DateTime.UtcNow
        };
        await _context.RunInTransactionAsync(async () =>
        {
            _context.Favorites.Add(favorite);
            await _context.SaveEntitiesAsync();
        });
        return ToResponse(favorite);
    }

    public async Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(request));

        if (!Guid.TryParse(request.CreatedByGuid, out var reviewGuid))
            throw new ArgumentException("Invalid CreatedByGuid format", nameof(request));

        var review = new Review
        {
            TenantId = request.UserId,
            ApartmentId = request.ApartmentId,
            Rating = request.Rating,
            ReviewText = request.Comment,
            IsAnonymous = request.IsAnonymous,
            IsPublic = request.IsPublic,
            CreatedByGuid = reviewGuid,
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = reviewGuid,
            ModifiedDate = DateTime.UtcNow
        };
        await _context.RunInTransactionAsync(async () =>
        {
            _context.Reviews.Add(review);
            await _context.SaveEntitiesAsync();
        });

        var user = await _context.Users.FindAsync(request.UserId);
        return ToResponse(review, user is null ? null : new UserInfo
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePicture = user.ProfilePicture ?? string.Empty
        });
    }

    public async Task<ReviewResponse> GetReviewByIdAsync(int reviewId)
    {
        var review = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Tenant)
            .FirstOrDefaultAsync(r => r.ReviewId == reviewId);

        if (review == null)
            throw new NotFoundException($"Review with ID {reviewId} not found");

        return ToResponse(review, review.Tenant is null ? null : new UserInfo
        {
            FirstName = review.Tenant.FirstName,
            LastName = review.Tenant.LastName,
            ProfilePicture = review.Tenant.ProfilePicture ?? string.Empty
        });
    }

    public async Task<GetReviewsResponse> GetReviewsByApartmentIdAsync(int apartmentId)
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Tenant)
            .Where(r => r.ApartmentId == apartmentId && r.IsPublic)
            .OrderByDescending(r => r.CreatedDate)
            .Select(r => new ReviewResponse
            {
                ReviewId = r.ReviewId,
                UserId = r.TenantId ?? 0,
                ApartmentId = r.ApartmentId ?? 0,
                Rating = r.Rating ?? 0,
                Comment = r.ReviewText ?? string.Empty,
                IsAnonymous = r.IsAnonymous,
                IsPublic = r.IsPublic,
                CreatedByGuid = r.CreatedByGuid.HasValue ? r.CreatedByGuid.Value.ToString() : string.Empty,
                CreatedDate = r.CreatedDate.HasValue
                    ? Timestamp.FromDateTime(r.CreatedDate.Value.ToUniversalTime())
                    : null,
                ModifiedByGuid = r.ModifiedByGuid.HasValue ? r.ModifiedByGuid.Value.ToString() : string.Empty,
                ModifiedDate = r.ModifiedDate.HasValue
                    ? Timestamp.FromDateTime(r.ModifiedDate.Value.ToUniversalTime())
                    : null,
                User = r.Tenant != null ? new UserInfo
                {
                    FirstName = r.Tenant.FirstName,
                    LastName = r.Tenant.LastName,
                    ProfilePicture = r.Tenant.ProfilePicture ?? string.Empty
                } : null
            })
            .ToListAsync();

        var response = new GetReviewsResponse();
        response.Reviews.AddRange(reviews);
        return response;
    }

    public async Task<DeleteResponse> DeleteReviewAsync(int reviewId, string callerGuid)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null)
            return new DeleteResponse { Success = false, Message = "Review not found" };

        if (string.IsNullOrEmpty(callerGuid) || review.CreatedByGuid?.ToString() != callerGuid)
            return new DeleteResponse { Success = false, Message = "Unauthorized: You do not own this review" };

        await _context.RunInTransactionAsync(async () =>
        {
            _context.Reviews.Remove(review);
            await _context.SaveEntitiesAsync();
        });
        return new DeleteResponse { Success = true, Message = "Review deleted successfully" };
    }

    public async Task<DeleteResponse> DeleteFavoriteAsync(int favoriteId, string callerGuid)
    {
        var favorite = await _context.Favorites.FindAsync(favoriteId);
        if (favorite == null)
            return new DeleteResponse { Success = false, Message = "Favorite not found" };

        if (string.IsNullOrEmpty(callerGuid) || favorite.CreatedByGuid?.ToString() != callerGuid)
            return new DeleteResponse { Success = false, Message = "Unauthorized: You do not own this favorite" };

        await _context.RunInTransactionAsync(async () =>
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveEntitiesAsync();
        });
        return new DeleteResponse { Success = true, Message = "Favorite deleted successfully" };
    }

    public async Task<GetFavoritesResponse> GetUserFavoritesAsync(int userId)
    {
        var favorites = await _context.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedDate)
            .Select(f => new FavoriteResponse
            {
                FavoriteId = f.FavoriteId,
                UserId = f.UserId ?? 0,
                ApartmentId = f.ApartmentId ?? 0,
                CreatedByGuid = f.CreatedByGuid.HasValue ? f.CreatedByGuid.Value.ToString() : string.Empty,
                CreatedDate = f.CreatedDate.HasValue
                    ? Timestamp.FromDateTime(f.CreatedDate.Value.ToUniversalTime())
                    : null,
                ModifiedByGuid = f.ModifiedByGuid.HasValue ? f.ModifiedByGuid.Value.ToString() : string.Empty,
                ModifiedDate = f.ModifiedDate.HasValue
                    ? Timestamp.FromDateTime(f.ModifiedDate.Value.ToUniversalTime())
                    : null
            })
            .ToListAsync();

        var response = new GetFavoritesResponse();
        response.Favorites.AddRange(favorites);
        return response;
    }

    private static FavoriteResponse ToResponse(Favorite favorite) => new()
    {
        FavoriteId = favorite.FavoriteId,
        UserId = favorite.UserId ?? 0,
        ApartmentId = favorite.ApartmentId ?? 0,
        CreatedByGuid = favorite.CreatedByGuid.HasValue ? favorite.CreatedByGuid.Value.ToString() : string.Empty,
        CreatedDate = favorite.CreatedDate.HasValue
            ? Timestamp.FromDateTime(favorite.CreatedDate.Value.ToUniversalTime()) : null,
        ModifiedByGuid = favorite.ModifiedByGuid.HasValue ? favorite.ModifiedByGuid.Value.ToString() : string.Empty,
        ModifiedDate = favorite.ModifiedDate.HasValue
            ? Timestamp.FromDateTime(favorite.ModifiedDate.Value.ToUniversalTime()) : null
    };

    private static ReviewResponse ToResponse(Review review, UserInfo? user) => new()
    {
        ReviewId = review.ReviewId,
        UserId = review.TenantId ?? 0,
        ApartmentId = review.ApartmentId ?? 0,
        Rating = review.Rating ?? 0,
        Comment = review.ReviewText ?? string.Empty,
        IsAnonymous = review.IsAnonymous,
        IsPublic = review.IsPublic,
        CreatedByGuid = review.CreatedByGuid.HasValue ? review.CreatedByGuid.Value.ToString() : string.Empty,
        CreatedDate = review.CreatedDate.HasValue
            ? Timestamp.FromDateTime(review.CreatedDate.Value.ToUniversalTime()) : null,
        ModifiedByGuid = review.ModifiedByGuid.HasValue ? review.ModifiedByGuid.Value.ToString() : string.Empty,
        ModifiedDate = review.ModifiedDate.HasValue
            ? Timestamp.FromDateTime(review.ModifiedDate.Value.ToUniversalTime()) : null,
        User = user
    };
}
