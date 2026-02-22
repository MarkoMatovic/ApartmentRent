using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lander.src.Modules.Reviews.Modules;
using Lander.src.Modules.Reviews.proto;
using Microsoft.EntityFrameworkCore;
namespace Lander.src.Modules.Reviews.Implementation;
public class ReviewFavoriteService : ReviewFavoriteGrpcService.ReviewFavoriteGrpcServiceBase
{
    private readonly ReviewsContext _context;
    public ReviewFavoriteService(ReviewsContext context)
    {
        _context = context;
    }
    public override async Task<FavoriteResponse> CreateFavorite(CreateFavoriteRequest request, ServerCallContext context)
    {
        var favorite = new Favorite
        {
            UserId = request.UserId,
            ApartmentId = request.ApartmentId,
            CreatedByGuid = Guid.Parse(request.CreatedByGuid),
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = Guid.Parse(request.CreatedByGuid),
            ModifiedDate = DateTime.UtcNow
        };
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.Favorites.Add(favorite);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        return new FavoriteResponse
        {
            UserId = (int)favorite.UserId,
            ApartmentId = (int)favorite.ApartmentId,
            CreatedByGuid = favorite.CreatedByGuid.ToString(),
            CreatedDate = Timestamp.FromDateTime((DateTime)favorite.CreatedDate),
            ModifiedByGuid = favorite.ModifiedByGuid.ToString(),
            ModifiedDate = Timestamp.FromDateTime((DateTime)favorite.ModifiedDate)
        };
    }
    public override async Task<ReviewResponse> CreateReview(CreateReviewRequest request, ServerCallContext context)
    {
        if (request.Rating < 1 || request.Rating > 5)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Rating must be between 1 and 5"));
        }

        var review = new Review
        {
            TenantId = request.UserId,
            ApartmentId = request.ApartmentId,
            Rating = request.Rating,
            ReviewText = request.Comment,
            IsAnonymous = request.IsAnonymous,
            IsPublic = request.IsPublic,
            CreatedByGuid = Guid.Parse(request.CreatedByGuid),
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = Guid.Parse(request.CreatedByGuid),
            ModifiedDate = DateTime.UtcNow
        };
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.Reviews.Add(review);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        var user = await _context.Users.FindAsync(request.UserId);
        return new ReviewResponse
        {
            ReviewId = review.ReviewId,
            UserId = (int)review.TenantId,
            ApartmentId = (int)review.ApartmentId,
            Rating = (int)review.Rating,
            Comment = review.ReviewText,
            IsAnonymous = review.IsAnonymous,
            IsPublic = review.IsPublic,
            CreatedByGuid = review.CreatedByGuid.ToString(),
            CreatedDate = Timestamp.FromDateTime((DateTime)review.CreatedDate),
            ModifiedByGuid = review.ModifiedByGuid.ToString(),
            ModifiedDate = Timestamp.FromDateTime((DateTime)review.ModifiedDate),
            User = user != null ? new UserInfo
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfilePicture = user.ProfilePicture ?? string.Empty
            } : null
        };
    }
    public override async Task<ReviewResponse> GetReviewById(GetReviewByIdRequest request, ServerCallContext context)
    {
        var review = await _context.Reviews
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
    public override async Task<GetReviewsResponse> GetReviewsByApartmentId(GetReviewsByApartmentIdRequest request, ServerCallContext context)
    {
        var reviews = await _context.Reviews
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

        if (review.CreatedByGuid?.ToString() != request.RequestUserGuid)
        {
            return new DeleteResponse
            {
                Success = false,
                Message = "Unauthorized: You do not own this review"
            };
        }
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.Reviews.Remove(review);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
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

        if (favorite.CreatedByGuid?.ToString() != request.RequestUserGuid)
        {
            return new DeleteResponse
            {
                Success = false,
                Message = "Unauthorized: You do not own this favorite"
            };
        }
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        return new DeleteResponse
        {
            Success = true,
            Message = "Favorite deleted successfully"
        };
    }
    public override async Task<GetFavoritesResponse> GetUserFavorites(GetUserFavoritesRequest request, ServerCallContext context)
    {
        var favorites = await _context.Favorites
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
