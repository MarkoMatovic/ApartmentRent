using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>GET /api/v1/search-requests/get-all-search-requests</summary>
public class GetAllSearchRequestsEndpoint
{
    private readonly HttpClient _client;
    public GetAllSearchRequestsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string? city = null)
    {
        var url = "/api/v1/search-requests/get-all-search-requests";
        if (city != null) url += $"?city={city}";
        return _client.GetAsync(url);
    }
}

/// <summary>GET /api/v1/search-requests/get-search-request?id={id}</summary>
public class GetSearchRequestEndpoint
{
    private readonly HttpClient _client;
    public GetSearchRequestEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.GetAsync($"/api/v1/search-requests/get-search-request?id={id}");
}

/// <summary>GET /api/v1/search-requests/get-search-requests-by-user-id?userId={userId}</summary>
public class GetSearchRequestsByUserIdEndpoint
{
    private readonly HttpClient _client;
    public GetSearchRequestsByUserIdEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/search-requests/get-search-requests-by-user-id?userId={userId}");
}

/// <summary>POST /api/v1/search-requests/create-search-request</summary>
public class CreateSearchRequestEndpoint
{
    private readonly HttpClient _client;
    public CreateSearchRequestEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(object dto)
        => _client.PostAsJsonAsync("/api/v1/search-requests/create-search-request", dto);
}

/// <summary>PUT /api/v1/search-requests/update-search-request/{id}</summary>
public class UpdateSearchRequestEndpoint
{
    private readonly HttpClient _client;
    public UpdateSearchRequestEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id, object dto)
        => _client.PutAsJsonAsync($"/api/v1/search-requests/update-search-request/{id}", dto);
}

/// <summary>DELETE /api/v1/search-requests/delete-search-request/{id}</summary>
public class DeleteSearchRequestEndpoint
{
    private readonly HttpClient _client;
    public DeleteSearchRequestEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.DeleteAsync($"/api/v1/search-requests/delete-search-request/{id}");
}
