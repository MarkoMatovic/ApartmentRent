using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lander.src.Modules.Reviews.Modules;
using Lander.src.Modules.Reviews.proto;

namespace Lander.src.Modules.Reviews.Implementation
{
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
               // FavoriteId = favorite.FavoriteId,
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
    }
}
