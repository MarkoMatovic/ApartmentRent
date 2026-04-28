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
    Task<DeleteResponse> DeleteReviewAsync(int reviewId, string callerGuid);
    Task<DeleteResponse> DeleteFavoriteAsync(int favoriteId, string callerGuid);
    Task<GetFavoritesResponse> GetUserFavoritesAsync(int userId);
}
