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

        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();

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
            TenantId = request.TenantId,
            LandlordId = request.LandlordId,
            Rating = request.Rating,
            ReviewText = request.ReviewText,
            CreatedByGuid = Guid.Parse(request.CreatedByGuid),
            CreatedDate = DateTime.UtcNow,
            ModifiedByGuid = Guid.Parse(request.CreatedByGuid),
            ModifiedDate = DateTime.UtcNow
        };

         _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return new ReviewResponse
        {
            ReviewId = review.ReviewId,
            TenantId = (int)review.TenantId,
            LandlordId = (int)review.LandlordId,
            Rating = (int)review.Rating,
            ReviewText = review.ReviewText,
            CreatedByGuid = review.CreatedByGuid.ToString(),
            CreatedDate = Timestamp.FromDateTime((DateTime)review.CreatedDate),
            ModifiedByGuid = review.ModifiedByGuid.ToString(),
            ModifiedDate = Timestamp.FromDateTime((DateTime)review.ModifiedDate)
        };
    }

    public override async Task<ReviewResponse> GetReviewById(GetReviewByIdRequest request, ServerCallContext context)
    {
      
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewId == request.ReviewId);

            if (review == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Review with ID {request.ReviewId} not found"));
            }

            return new ReviewResponse
            {
                ReviewId = review.ReviewId,
                TenantId = (int)review.TenantId,
                LandlordId = (int)review.LandlordId,
                Rating = (int)review.Rating,
                ReviewText = review.ReviewText,
                CreatedByGuid = review.CreatedByGuid.ToString(),
                CreatedDate = review.CreatedDate.HasValue
               ? Timestamp.FromDateTime(review.CreatedDate.Value.ToUniversalTime())
               : null,
                ModifiedByGuid = review.ModifiedByGuid.ToString(),
                ModifiedDate = review.ModifiedDate.HasValue
                ? Timestamp.FromDateTime(review.ModifiedDate.Value.ToUniversalTime())
                : null
            };
       
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
}
