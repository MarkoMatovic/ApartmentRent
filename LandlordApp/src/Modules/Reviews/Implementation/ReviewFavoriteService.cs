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
            UserId = (int)review.TenantId,
            ApartmentId = review.ApartmentId ?? 0,
            Rating = (int)review.Rating,
            Comment = review.ReviewText,
            IsAnonymous = review.IsAnonymous,
            IsPublic = review.IsPublic,
            CreatedByGuid = review.CreatedByGuid.ToString(),
            CreatedDate = review.CreatedDate.HasValue
                ? Timestamp.FromDateTime(review.CreatedDate.Value.ToUniversalTime())
                : null,
            ModifiedByGuid = review.ModifiedByGuid.ToString(),
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
                UserId = (int)r.TenantId,
                ApartmentId = (int)r.ApartmentId,
                Rating = (int)r.Rating,
                Comment = r.ReviewText,
                IsAnonymous = r.IsAnonymous,
                IsPublic = r.IsPublic,
                CreatedByGuid = r.CreatedByGuid.ToString(),
                CreatedDate = r.CreatedDate.HasValue
                    ? Timestamp.FromDateTime(r.CreatedDate.Value.ToUniversalTime())
                    : null,
                ModifiedByGuid = r.ModifiedByGuid.ToString(),
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
                UserId = (int)f.UserId,
                ApartmentId = (int)f.ApartmentId,
                CreatedByGuid = f.CreatedByGuid.ToString(),
                CreatedDate = f.CreatedDate.HasValue
                    ? Timestamp.FromDateTime(f.CreatedDate.Value.ToUniversalTime())
                    : null,
                ModifiedByGuid = f.ModifiedByGuid.ToString(),
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
                UserId = (int)f.UserId,
                ApartmentId = (int)f.ApartmentId,
                CreatedByGuid = f.CreatedByGuid.ToString(),
                CreatedDate = f.CreatedDate.HasValue
                    ? Timestamp.FromDateTime(f.CreatedDate.Value.ToUniversalTime())
                    : null,
                ModifiedByGuid = f.ModifiedByGuid.ToString(),
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
