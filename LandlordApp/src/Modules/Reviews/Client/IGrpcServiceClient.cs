using Lander.src.Modules.Reviews.proto;

namespace Lander.src.Modules.Reviews.Client;

/// <summary>
/// Interfejs za GrpcServiceClient — omogućava mockovanje u testovima.
/// </summary>
public interface IGrpcServiceClient
{
    Task<FavoriteResponse> CreateFavoriteAsync(CreateFavoriteRequest request);
    Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request);
    Task<ReviewResponse> GetReviewByIdAsync(int reviewId);
    Task<GetReviewsResponse> GetReviewsByApartmentIdAsync(int apartmentId);
    Task<DeleteResponse> DeleteReviewAsync(int reviewId);
    Task<DeleteResponse> DeleteFavoriteAsync(int favoriteId);
    Task<GetFavoritesResponse> GetUserFavoritesAsync(int userId);
}
