using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record CreateApplicationRequest(int ApartmentId, bool IsPriority = false);
public record UpdateApplicationStatusRequest(string Status);

// ── Endpoint wrappers ─────────────────────────────────────────────────────────

/// <summary>POST /api/applications</summary>
public class ApplyForApartmentEndpoint
{
    private readonly HttpClient _client;
    public ApplyForApartmentEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int apartmentId, bool isPriority = false)
        => _client.PostAsJsonAsync("/api/applications", new { ApartmentId = apartmentId, IsPriority = isPriority });
}

/// <summary>GET /api/applications/tenant</summary>
public class GetTenantApplicationsEndpoint
{
    private readonly HttpClient _client;
    public GetTenantApplicationsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/applications/tenant");
}

/// <summary>GET /api/applications/landlord</summary>
public class GetLandlordApplicationsEndpoint
{
    private readonly HttpClient _client;
    public GetLandlordApplicationsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/applications/landlord");
}

/// <summary>PUT /api/applications/{id}/status</summary>
public class UpdateApplicationStatusEndpoint
{
    private readonly HttpClient _client;
    public UpdateApplicationStatusEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id, string status)
        => _client.PutAsJsonAsync($"/api/applications/{id}/status", new { Status = status });
}

/// <summary>GET /api/applications/check-approval/{apartmentId}</summary>
public class CheckApprovalEndpoint
{
    private readonly HttpClient _client;
    public CheckApprovalEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int apartmentId)
        => _client.GetAsync($"/api/applications/check-approval/{apartmentId}");
}
