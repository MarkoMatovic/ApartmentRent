using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>GET /api/v1/saved-searches/get-saved-searches-by-user-id?userId={userId}</summary>
public class GetSavedSearchesByUserIdEndpoint
{
    private readonly HttpClient _client;
    public GetSavedSearchesByUserIdEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/saved-searches/get-saved-searches-by-user-id?userId={userId}");
}

/// <summary>GET /api/v1/saved-searches/get-saved-search?id={id}</summary>
public class GetSavedSearchEndpoint
{
    private readonly HttpClient _client;
    public GetSavedSearchEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.GetAsync($"/api/v1/saved-searches/get-saved-search?id={id}");
}

/// <summary>POST /api/v1/saved-searches/create-saved-search</summary>
public class CreateSavedSearchEndpoint
{
    private readonly HttpClient _client;
    public CreateSavedSearchEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(object dto)
        => _client.PostAsJsonAsync("/api/v1/saved-searches/create-saved-search", dto);
}

/// <summary>PUT /api/v1/saved-searches/update-saved-search/{id}</summary>
public class UpdateSavedSearchEndpoint
{
    private readonly HttpClient _client;
    public UpdateSavedSearchEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id, object dto)
        => _client.PutAsJsonAsync($"/api/v1/saved-searches/update-saved-search/{id}", dto);
}

/// <summary>DELETE /api/v1/saved-searches/delete-saved-search/{id}</summary>
public class DeleteSavedSearchEndpoint
{
    private readonly HttpClient _client;
    public DeleteSavedSearchEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.DeleteAsync($"/api/v1/saved-searches/delete-saved-search/{id}");
}
