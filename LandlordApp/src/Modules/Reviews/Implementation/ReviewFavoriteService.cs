using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Lander.Helpers;
using Grpc.Core;
using Lander.src.Modules.Reviews.Modules;
using Lander.src.Modules.Reviews.proto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Reviews.Implementation;

// [Authorize] ensures that only callers who present a valid JWT (forwarded from the
// REST layer via GrpcServiceClient) can invoke these methods.  This prevents an
// external attacker from calling the gRPC endpoint directly and forging UserId /
// CreatedByGuid values — the REST controller (ReviewsFavoritesController) already
// overwrites those fields with the authenticated user's identity.
[Authorize]
public class ReviewFavoriteService : ReviewFavoriteGrpcService.ReviewFavoriteGrpcServiceBase
{
    private readonly ReviewsContext _context;
    public ReviewFavoriteService(ReviewsContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Resolves the caller's identity from the authenticated JWT ("sub" claim) rather than
    /// any client-supplied field. Ownership checks MUST use this — trusting a guid carried
    /// in the request lets any authenticated caller delete another user's data.
    /// </summary>
    // Null-safe: a missing call context means an unauthenticated caller,
    // which callers translate into an "Unauthorized" response.
    private static string? GetAuthenticatedUserGuid(ServerCallContext? context)
        => context?.GetHttpContext()?.User?.FindFirstValue("sub");
    public override async Task<FavoriteResponse> CreateFavorite(CreateFavoriteRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.CreatedByGuid, out var favGuid))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid CreatedByGuid format"));

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
        return new FavoriteResponse
        {
            UserId = favorite.UserId ?? 0,
            ApartmentId = favorite.ApartmentId ?? 0,
            CreatedByGuid = favorite.CreatedByGuid.HasValue ? favorite.CreatedByGuid.Value.ToString() : string.Empty,
            CreatedDate = favorite.CreatedDate.HasValue
                ? Timestamp.FromDateTime(favorite.CreatedDate.Value.ToUniversalTime()) : null,
            ModifiedByGuid = favorite.ModifiedByGuid.HasValue ? favorite.ModifiedByGuid.Value.ToString() : string.Empty,
            ModifiedDate = favorite.ModifiedDate.HasValue
                ? Timestamp.FromDateTime(favorite.ModifiedDate.Value.ToUniversalTime()) : null
        };
    }
    public override async Task<ReviewResponse> CreateReview(CreateReviewRequest request, ServerCallContext context)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Rating must be between 1 and 5"));
        }

        if (!Guid.TryParse(request.CreatedByGuid, out var reviewGuid))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid CreatedByGuid format"));

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
        return new ReviewResponse
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
            User = user != null ? new UserInfo
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicture = user.ProfilePicture ?? string.Empty
            } : null
        };
    }
    [AllowAnonymous]
    public override async Task<ReviewResponse> GetReviewById(GetReviewByIdRequest request, ServerCallContext context)
    {
        var review = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Tenant)
            .FirstOrDefaultAsync(r => r.ReviewId == request.ReviewId);
        if (review == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Review with ID {request.ReviewId} not found"));
        }
        return new ReviewResponse
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
                ? Timestamp.FromDateTime(review.CreatedDate.Value.ToUniversalTime())
                : null,
            ModifiedByGuid = review.ModifiedByGuid.HasValue ? review.ModifiedByGuid.Value.ToString() : string.Empty,
            ModifiedDate = review.ModifiedDate.HasValue
                ? Timestamp.FromDateTime(review.ModifiedDate.Value.ToUniversalTime())
                : null,
            User = review.Tenant != null ? new UserInfo
            {
                FirstName = review.Tenant.FirstName,
                LastName = review.Tenant.LastName,
                ProfilePicture = review.Tenant.ProfilePicture ?? string.Empty
            } : null
        };
    }
    [AllowAnonymous]
    public override async Task<GetReviewsResponse> GetReviewsByApartmentId(GetReviewsByApartmentIdRequest request, ServerCallContext context)
    {
        var reviews = await _context.Reviews
            .AsNoTracking()
            .Include(r => r.Tenant)
            .Where(r => r.ApartmentId == request.ApartmentId && r.IsPublic)
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
    public override async Task<GetFavoritesResponse> GetFavorites(GetFavoritesRequest request, ServerCallContext context)
    {
        int limit = Math.Min(request.Limit > 0 ? request.Limit : 10, 10);
        var favorites = await _context.Favorites
            .AsNoTracking()
            .OrderBy(f => f.FavoriteId)
            .Take(limit)
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
    public override async Task<DeleteResponse> DeleteReview(DeleteReviewRequest request, ServerCallContext context)
    {
        var review = await _context.Reviews.FindAsync(request.ReviewId);
        if (review == null)
        {
            return new DeleteResponse
            {
                Success = false,
                Message = "Review not found"
            };
        }

        var callerGuid = GetAuthenticatedUserGuid(context);
        if (string.IsNullOrEmpty(callerGuid) || review.CreatedByGuid?.ToString() != callerGuid)
        {
            return new DeleteResponse
            {
                Success = false,
                Message = "Unauthorized: You do not own this review"
            };
        }
        await _context.RunInTransactionAsync(async () =>
        {
            _context.Reviews.Remove(review);
            await _context.SaveEntitiesAsync();
                    });
        return new DeleteResponse
        {
            Success = true,
            Message = "Review deleted successfully"
        };
    }
    public override async Task<DeleteResponse> DeleteFavorite(DeleteFavoriteRequest request, ServerCallContext context)
    {
        var favorite = await _context.Favorites.FindAsync(request.FavoriteId);
        if (favorite == null)
        {
            return new DeleteResponse
            {
                Success = false,
                Message = "Favorite not found"
            };
        }

        var callerGuid = GetAuthenticatedUserGuid(context);
        if (string.IsNullOrEmpty(callerGuid) || favorite.CreatedByGuid?.ToString() != callerGuid)
        {
            return new DeleteResponse
            {
                Success = false,
                Message = "Unauthorized: You do not own this favorite"
            };
        }
        await _context.RunInTransactionAsync(async () =>
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveEntitiesAsync();
                    });
        return new DeleteResponse
        {
            Success = true,
            Message = "Favorite deleted successfully"
        };
    }
    public override async Task<GetFavoritesResponse> GetUserFavorites(GetUserFavoritesRequest request, ServerCallContext context)
    {
        var favorites = await _context.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == request.UserId)
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
}
