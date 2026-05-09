using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>POST /api/v1/reviews/create-favorite</summary>
public class CreateFavoriteEndpoint
{
    private readonly HttpClient _client;
    public CreateFavoriteEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int apartmentId)
        => _client.PostAsJsonAsync("/api/v1/reviews/create-favorite", new { ApartmentId = apartmentId });
}

/// <summary>POST /api/v1/reviews/create-review</summary>
public class CreateReviewEndpoint
{
    private readonly HttpClient _client;
    public CreateReviewEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int apartmentId, int rating, string comment)
        => _client.PostAsJsonAsync("/api/v1/reviews/create-review", new
        {
            ApartmentId = apartmentId,
            Rating      = rating,
            Comment     = comment,
            IsPublic    = true,
        });
}

/// <summary>GET /api/v1/reviews/get-review-by-id?reviewId={id}</summary>
public class GetReviewByIdEndpoint
{
    private readonly HttpClient _client;
    public GetReviewByIdEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int reviewId)
        => _client.GetAsync($"/api/v1/reviews/get-review-by-id?reviewId={reviewId}");
}

/// <summary>GET /api/v1/reviews/apartment/{apartmentId}</summary>
public class GetReviewsByApartmentIdEndpoint
{
    private readonly HttpClient _client;
    public GetReviewsByApartmentIdEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int apartmentId)
        => _client.GetAsync($"/api/v1/reviews/apartment/{apartmentId}");
}

/// <summary>DELETE /api/v1/reviews/delete-review/{id}</summary>
public class DeleteReviewEndpoint
{
    private readonly HttpClient _client;
    public DeleteReviewEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.DeleteAsync($"/api/v1/reviews/delete-review/{id}");
}

/// <summary>DELETE /api/v1/reviews/delete-favorite/{id}</summary>
public class DeleteFavoriteEndpoint
{
    private readonly HttpClient _client;
    public DeleteFavoriteEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.DeleteAsync($"/api/v1/reviews/delete-favorite/{id}");
}

/// <summary>GET /api/v1/reviews/favorites/{userId}</summary>
public class GetUserFavoritesEndpoint
{
    private readonly HttpClient _client;
    public GetUserFavoritesEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/reviews/favorites/{userId}");
}
