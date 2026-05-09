using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>GET /api/v1/roommates/get-all-roommates</summary>
public class GetAllRoommatesEndpoint
{
    private readonly HttpClient _client;
    public GetAllRoommatesEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string? location = null, decimal? minBudget = null, decimal? maxBudget = null)
    {
        var url = "/api/v1/roommates/get-all-roommates?page=1&pageSize=20";
        if (location is not null) url += $"&location={Uri.EscapeDataString(location)}";
        if (minBudget.HasValue)  url += $"&minBudget={minBudget}";
        if (maxBudget.HasValue)  url += $"&maxBudget={maxBudget}";
        return _client.GetAsync(url);
    }
}

/// <summary>GET /api/v1/roommates/get-roommate?id={id}</summary>
public class GetRoommateEndpoint
{
    private readonly HttpClient _client;
    public GetRoommateEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.GetAsync($"/api/v1/roommates/get-roommate?id={id}");
}

/// <summary>GET /api/v1/roommates/get-roommate-by-user-id?userId={userId}</summary>
public class GetRoommateByUserIdEndpoint
{
    private readonly HttpClient _client;
    public GetRoommateByUserIdEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/roommates/get-roommate-by-user-id?userId={userId}");
}

/// <summary>POST /api/v1/roommates/create-roommate</summary>
public class CreateRoommateEndpoint
{
    private readonly HttpClient _client;
    public CreateRoommateEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(object dto)
        => _client.PostAsJsonAsync("/api/v1/roommates/create-roommate", dto);
}

/// <summary>PUT /api/v1/roommates/update-roommate/{id}</summary>
public class UpdateRoommateEndpoint
{
    private readonly HttpClient _client;
    public UpdateRoommateEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id, object dto)
        => _client.PutAsJsonAsync($"/api/v1/roommates/update-roommate/{id}", dto);
}

/// <summary>DELETE /api/v1/roommates/delete-roommate/{id}</summary>
public class DeleteRoommateEndpoint
{
    private readonly HttpClient _client;
    public DeleteRoommateEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.DeleteAsync($"/api/v1/roommates/delete-roommate/{id}");
}
