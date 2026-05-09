using System.Net.Http.Json;
using System.Text.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

// ── Endpoint wrappers ─────────────────────────────────────────────────────────

/// <summary>GET /api/v1/rent/get-all-apartments</summary>
public class GetAllApartmentsEndpoint
{
    private readonly HttpClient _client;
    public GetAllApartmentsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string? city = null, int page = 1, int pageSize = 20)
    {
        var url = $"/api/v1/rent/get-all-apartments?page={page}&pageSize={pageSize}";
        if (city is not null) url += $"&city={Uri.EscapeDataString(city)}";
        return _client.GetAsync(url);
    }
}

/// <summary>POST /api/v1/rent/create-apartment</summary>
public class CreateApartmentEndpoint
{
    private readonly HttpClient _client;
    public CreateApartmentEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(object dto)
        => _client.PostAsJsonAsync("/api/v1/rent/create-apartment", dto);
}

/// <summary>PUT /api/v1/rent/update-apartment/{id}</summary>
public class UpdateApartmentEndpoint
{
    private readonly HttpClient _client;
    public UpdateApartmentEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id, object dto)
        => _client.PutAsJsonAsync($"/api/v1/rent/update-apartment/{id}", dto);
}

/// <summary>DELETE /api/v1/rent/delete-apartment/{id}</summary>
public class DeleteApartmentEndpoint
{
    private readonly HttpClient _client;
    public DeleteApartmentEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.DeleteAsync($"/api/v1/rent/delete-apartment/{id}");
}

/// <summary>GET /api/v1/rent/get-apartment?id={id}</summary>
public class GetApartmentEndpoint
{
    private readonly HttpClient _client;
    public GetApartmentEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.GetAsync($"/api/v1/rent/get-apartment?id={id}");
}

/// <summary>GET /api/v1/rent/get-my-apartments [Authorize]</summary>
public class GetMyApartmentsEndpoint
{
    private readonly HttpClient _client;
    public GetMyApartmentsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/v1/rent/get-my-apartments");
}

/// <summary>GET /api/v1/rent/keyset (keyset pagination, public)</summary>
public class GetAllApartmentsKeysetEndpoint
{
    private readonly HttpClient _client;
    public GetAllApartmentsKeysetEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int? lastId = null, int pageSize = 20, string? city = null)
    {
        var url = $"/api/v1/rent/keyset?pageSize={pageSize}";
        if (lastId.HasValue) url += $"&lastId={lastId}";
        if (city != null) url += $"&city={Uri.EscapeDataString(city)}";
        return _client.GetAsync(url);
    }
}

/// <summary>PUT /api/v1/rent/activate-apartment/{id} [Authorize]</summary>
public class ActivateApartmentEndpoint
{
    private readonly HttpClient _client;
    public ActivateApartmentEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.PutAsync($"/api/v1/rent/activate-apartment/{id}", null);
}
