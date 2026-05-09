using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>GET /api/v1/notification/get-user-notifications?id={userId}</summary>
public class GetUserNotificationsEndpoint
{
    private readonly HttpClient _client;
    public GetUserNotificationsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/notification/get-user-notifications?id={userId}");
}

/// <summary>POST /api/v1/notification/mark-as-read?notificationId={id}</summary>
public class MarkNotificationAsReadEndpoint
{
    private readonly HttpClient _client;
    public MarkNotificationAsReadEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int notificationId)
        => _client.PostAsync($"/api/v1/notification/mark-as-read?notificationId={notificationId}", null);
}

/// <summary>POST /api/v1/notification/mark-all-as-read</summary>
public class MarkAllNotificationsAsReadEndpoint
{
    private readonly HttpClient _client;
    public MarkAllNotificationsAsReadEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.PostAsync("/api/v1/notification/mark-all-as-read", null);
}

/// <summary>DELETE /api/v1/notification/delete/{id}</summary>
public class DeleteNotificationEndpoint
{
    private readonly HttpClient _client;
    public DeleteNotificationEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.DeleteAsync($"/api/v1/notification/delete/{id}");
}
