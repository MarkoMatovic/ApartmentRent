using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>GET /api/v1/reports (Admin only)</summary>
public class GetAllReportsEndpoint
{
    private readonly HttpClient _client;
    public GetAllReportsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string? status = null)
    {
        var url = "/api/v1/reports";
        if (status != null) url += $"?status={status}";
        return _client.GetAsync(url);
    }
}

/// <summary>PUT /api/v1/reports/{reportId}/review (Admin only)</summary>
public class ReviewReportEndpoint
{
    private readonly HttpClient _client;
    public ReviewReportEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int reportId, string status = "Reviewed", string? notes = null)
        => _client.PutAsJsonAsync($"/api/v1/reports/{reportId}/review", new
        {
            Status     = status,
            AdminNotes = notes ?? "Reviewed by admin.",
        });
}

/// <summary>PUT /api/v1/reports/{reportId}/resolve (Admin only)</summary>
public class ResolveReportEndpoint
{
    private readonly HttpClient _client;
    public ResolveReportEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int reportId, string? notes = null)
        => _client.PutAsJsonAsync($"/api/v1/reports/{reportId}/resolve", new
        {
            Status     = "Resolved",
            AdminNotes = notes ?? "Resolved by admin.",
        });
}

/// <summary>DELETE /api/v1/reports/{reportId} (Admin only)</summary>
public class DeleteReportEndpoint
{
    private readonly HttpClient _client;
    public DeleteReportEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int reportId)
        => _client.DeleteAsync($"/api/v1/reports/{reportId}");
}
