using Grpc.Core;
using Grpc.Net.Client;
using Lander.src.Modules.Reviews.proto;

namespace Lander.src.Modules.Reviews.Client;

/// <summary>
/// Internal gRPC client.  Forwards the caller's Bearer token so the gRPC service
/// can enforce [Authorize] and reject unauthenticated direct calls.
/// </summary>
public class GrpcServiceClient : IGrpcServiceClient
{
    private readonly GrpcChannel _channel;
    private readonly ReviewFavoriteGrpcService.ReviewFavoriteGrpcServiceClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GrpcServiceClient(string grpcAddress, IHttpContextAccessor httpContextAccessor)
    {
        _channel = GrpcChannel.ForAddress(grpcAddress);
        _client = new ReviewFavoriteGrpcService.ReviewFavoriteGrpcServiceClient(_channel);
        _httpContextAccessor = httpContextAccessor;
    }

    // Builds gRPC Metadata by forwarding the Authorization header from the
    // current HTTP request so the gRPC service can validate the JWT.
    private Metadata BuildAuthHeaders()
    {
        var headers = new Metadata();
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
            headers.Add("Authorization", authHeader);
        return headers;
    }

    public async Task<FavoriteResponse> CreateFavoriteAsync(CreateFavoriteRequest request)
        => await _client.CreateFavoriteAsync(request, BuildAuthHeaders());

    public async Task<ReviewResponse> CreateReviewAsync(CreateReviewRequest request)
        => await _client.CreateReviewAsync(request, BuildAuthHeaders());

    public async Task<ReviewResponse> GetReviewByIdAsync(int reviewId)
        => await _client.GetReviewByIdAsync(new GetReviewByIdRequest { ReviewId = reviewId });

    public async Task<GetReviewsResponse> GetReviewsByApartmentIdAsync(int apartmentId)
        => await _client.GetReviewsByApartmentIdAsync(new GetReviewsByApartmentIdRequest { ApartmentId = apartmentId });

    public async Task<DeleteResponse> DeleteReviewAsync(int reviewId, string callerGuid)
        => await _client.DeleteReviewAsync(new DeleteReviewRequest { ReviewId = reviewId, RequestUserGuid = callerGuid }, BuildAuthHeaders());

    public async Task<DeleteResponse> DeleteFavoriteAsync(int favoriteId, string callerGuid)
        => await _client.DeleteFavoriteAsync(new DeleteFavoriteRequest { FavoriteId = favoriteId, RequestUserGuid = callerGuid }, BuildAuthHeaders());

    public async Task<GetFavoritesResponse> GetUserFavoritesAsync(int userId)
        => await _client.GetUserFavoritesAsync(new GetUserFavoritesRequest { UserId = userId }, BuildAuthHeaders());
}
