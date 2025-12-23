using Grpc.Net.Client;
using Lander.src.Modules.Reviews.proto;

namespace Lander.src.Modules.Reviews.Client;

public class GrpcServiceClient
{
    private readonly GrpcChannel _channel;
    private readonly ReviewFavoriteGrpcService.ReviewFavoriteGrpcServiceClient _client;

    public GrpcServiceClient(string grpcAddress)
    {
        _channel = GrpcChannel.ForAddress(grpcAddress);
        _client = new ReviewFavoriteGrpcService.ReviewFavoriteGrpcServiceClient(_channel);
    }

    public async Task<FavoriteResponse> CreateFavoriteAsync(CreateFavoriteRequest request)
    {
        return await _client.CreateFavoriteAsync(request);
    }

    public async Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request)
    {
        return await _client.CreateReviewAsync(request);
    }

    public async Task<ReviewResponse> GetReviewByIdAsync(int reviewId)
    {
        var request = new GetReviewByIdRequest
        {
            ReviewId = reviewId
        };
        return await _client.GetReviewByIdAsync(request);
    }

    public async Task<GetReviewsResponse> GetReviewsByApartmentIdAsync(int apartmentId)
    {
        var request = new GetReviewsByApartmentIdRequest
        {
            ApartmentId = apartmentId
        };
        return await _client.GetReviewsByApartmentIdAsync(request);
    }

    public async Task<DeleteResponse> DeleteReviewAsync(int reviewId)
    {
        var request = new DeleteReviewRequest
        {
            ReviewId = reviewId
        };
        return await _client.DeleteReviewAsync(request);
    }

    public async Task<DeleteResponse> DeleteFavoriteAsync(int favoriteId)
    {
        var request = new DeleteFavoriteRequest
        {
            FavoriteId = favoriteId
        };
        return await _client.DeleteFavoriteAsync(request);
    }

    public async Task<GetFavoritesResponse> GetUserFavoritesAsync(int userId)
    {
        var request = new GetUserFavoritesRequest
        {
            UserId = userId
        };
        return await _client.GetUserFavoritesAsync(request);
    }
}
