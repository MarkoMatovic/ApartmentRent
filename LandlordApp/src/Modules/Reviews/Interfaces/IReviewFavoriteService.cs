using Lander.src.Modules.Reviews.proto;

namespace Lander.src.Modules.Reviews.Interfaces;

/// <summary>
/// Reviews/Favorites module boundary. Implemented in-process by
/// <see cref="Implementation.ReviewFavoriteService"/>; the interface exists so callers
/// depend on the contract rather than the implementation (and so tests can substitute it).
/// </summary>
/// <remarks>
/// Every <c>callerGuid</c> argument must come from the authenticated principal
/// (the JWT "sub" claim), never from the request body — ownership checks are enforced
/// against it.
/// </remarks>
public interface IReviewFavoriteService
{
    Task<FavoriteResponse> CreateFavoriteAsync(CreateFavoriteRequest request);
    Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request);
    Task<ReviewResponse> GetReviewByIdAsync(int reviewId);
    Task<GetReviewsResponse> GetReviewsByApartmentIdAsync(int apartmentId);
    Task<DeleteResponse> DeleteReviewAsync(int reviewId, string callerGuid);
    Task<DeleteResponse> DeleteFavoriteAsync(int favoriteId, string callerGuid);
    Task<GetFavoritesResponse> GetUserFavoritesAsync(int userId);
}
