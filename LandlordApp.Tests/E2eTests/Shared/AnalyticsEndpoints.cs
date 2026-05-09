using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>POST /api/v1/analytics/track-event (AllowAnonymous)</summary>
public class TrackEventEndpoint
{
    private readonly HttpClient _client;
    public TrackEventEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string eventType = "View", string category = "Apartment",
        int? entityId = null, string? entityType = "Apartment")
        => _client.PostAsJsonAsync("/api/v1/analytics/track-event", new
        {
            EventType     = eventType,
            EventCategory = category,
            EntityId      = entityId,
            EntityType    = entityType,
        });
}

/// <summary>GET /api/v1/analytics/summary</summary>
public class GetAnalyticsSummaryEndpoint
{
    private readonly HttpClient _client;
    public GetAnalyticsSummaryEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/v1/analytics/summary");
}

/// <summary>GET /api/v1/analytics/top-apartments</summary>
public class GetTopViewedApartmentsEndpoint
{
    private readonly HttpClient _client;
    public GetTopViewedApartmentsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/v1/analytics/top-apartments");
}

/// <summary>GET /api/v1/analytics/top-roommates</summary>
public class GetTopViewedRoommatesEndpoint
{
    private readonly HttpClient _client;
    public GetTopViewedRoommatesEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/v1/analytics/top-roommates");
}

/// <summary>GET /api/v1/analytics/top-searches</summary>
public class GetTopSearchTermsEndpoint
{
    private readonly HttpClient _client;
    public GetTopSearchTermsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/v1/analytics/top-searches");
}

/// <summary>GET /api/v1/analytics/trends (Admin only)</summary>
public class GetEventTrendsEndpoint
{
    private readonly HttpClient _client;
    public GetEventTrendsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(DateTime from, DateTime to)
        => _client.GetAsync($"/api/v1/analytics/trends?from={from:O}&to={to:O}");
}

/// <summary>GET /api/v1/analytics/user-roommate-summary?userId={userId}</summary>
public class GetUserRoommateSummaryEndpoint
{
    private readonly HttpClient _client;
    public GetUserRoommateSummaryEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/analytics/user-roommate-summary?userId={userId}");
}

/// <summary>GET /api/v1/analytics/user-complete-analytics?userId={userId}</summary>
public class GetUserCompleteAnalyticsEndpoint
{
    private readonly HttpClient _client;
    public GetUserCompleteAnalyticsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/analytics/user-complete-analytics?userId={userId}");
}

/// <summary>GET /api/v1/analytics/my-viewed-apartments</summary>
public class GetMyViewedApartmentsEndpoint
{
    private readonly HttpClient _client;
    public GetMyViewedApartmentsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/v1/analytics/my-viewed-apartments");
}

/// <summary>GET /api/v1/analytics/my-apartment-views</summary>
public class GetMyApartmentViewsEndpoint
{
    private readonly HttpClient _client;
    public GetMyApartmentViewsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/v1/analytics/my-apartment-views");
}

/// <summary>GET /api/v1/analytics/my-messages-sent</summary>
public class GetMyMessagesSentEndpoint
{
    private readonly HttpClient _client;
    public GetMyMessagesSentEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/v1/analytics/my-messages-sent");
}
