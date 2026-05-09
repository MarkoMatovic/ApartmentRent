using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>POST /api/v1/messages/send</summary>
public class SendMessageEndpoint
{
    private readonly HttpClient _client;
    public SendMessageEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int receiverId, string text)
        => _client.PostAsJsonAsync("/api/v1/messages/send", new
        {
            ReceiverId  = receiverId,
            MessageText = text,
        });
}

/// <summary>GET /api/v1/messages/conversation?userId1={u1}&userId2={u2}</summary>
public class GetConversationEndpoint
{
    private readonly HttpClient _client;
    public GetConversationEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId1, int userId2, int page = 1)
        => _client.GetAsync($"/api/v1/messages/conversation?userId1={userId1}&userId2={userId2}&page={page}&pageSize=50");
}

/// <summary>GET /api/v1/messages/user/{userId}</summary>
public class GetUserConversationsEndpoint
{
    private readonly HttpClient _client;
    public GetUserConversationsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/messages/user/{userId}");
}

/// <summary>GET /api/v1/messages/unread-count/{userId}</summary>
public class GetUnreadCountEndpoint
{
    private readonly HttpClient _client;
    public GetUnreadCountEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/messages/unread-count/{userId}");
}

/// <summary>PUT /api/v1/messages/mark-read/{messageId}</summary>
public class MarkMessageAsReadEndpoint
{
    private readonly HttpClient _client;
    public MarkMessageAsReadEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int messageId)
        => _client.PutAsync($"/api/v1/messages/mark-read/{messageId}", null);
}

/// <summary>POST /api/v1/messages/archive</summary>
public class ArchiveConversationEndpoint
{
    private readonly HttpClient _client;
    public ArchiveConversationEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId, int otherUserId)
        => _client.PostAsJsonAsync("/api/v1/messages/archive", new { UserId = userId, OtherUserId = otherUserId });
}

/// <summary>POST /api/v1/messages/block</summary>
public class BlockUserEndpoint
{
    private readonly HttpClient _client;
    public BlockUserEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId, int otherUserId)
        => _client.PostAsJsonAsync("/api/v1/messages/block", new { UserId = userId, OtherUserId = otherUserId });
}

/// <summary>DELETE /api/v1/messages/delete-conversation?otherUserId={id}</summary>
public class DeleteConversationEndpoint
{
    private readonly HttpClient _client;
    public DeleteConversationEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int otherUserId)
        => _client.DeleteAsync($"/api/v1/messages/delete-conversation?otherUserId={otherUserId}");
}

/// <summary>POST /api/v1/messages/unarchive</summary>
public class UnarchiveConversationEndpoint
{
    private readonly HttpClient _client;
    public UnarchiveConversationEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId, int otherUserId)
        => _client.PostAsJsonAsync("/api/v1/messages/unarchive", new { UserId = userId, OtherUserId = otherUserId });
}

/// <summary>POST /api/v1/messages/mute</summary>
public class MuteConversationEndpoint
{
    private readonly HttpClient _client;
    public MuteConversationEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId, int otherUserId)
        => _client.PostAsJsonAsync("/api/v1/messages/mute", new { UserId = userId, OtherUserId = otherUserId });
}

/// <summary>POST /api/v1/messages/unmute</summary>
public class UnmuteConversationEndpoint
{
    private readonly HttpClient _client;
    public UnmuteConversationEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId, int otherUserId)
        => _client.PostAsJsonAsync("/api/v1/messages/unmute", new { UserId = userId, OtherUserId = otherUserId });
}

/// <summary>POST /api/v1/messages/unblock</summary>
public class UnblockUserEndpoint
{
    private readonly HttpClient _client;
    public UnblockUserEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId, int otherUserId)
        => _client.PostAsJsonAsync("/api/v1/messages/unblock", new { UserId = userId, OtherUserId = otherUserId });
}

/// <summary>GET /api/v1/messages/search?query={query}</summary>
public class SearchMessagesEndpoint
{
    private readonly HttpClient _client;
    public SearchMessagesEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string query = "test")
        => _client.GetAsync($"/api/v1/messages/search?query={Uri.EscapeDataString(query)}");
}

/// <summary>POST /api/v1/messages/report</summary>
public class ReportAbuseEndpoint
{
    private readonly HttpClient _client;
    public ReportAbuseEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId, int reportedUserId, int messageId, string reason = "Spam")
        => _client.PostAsJsonAsync("/api/v1/messages/report", new
        {
            UserId         = userId,
            ReportedUserId = reportedUserId,
            MessageId      = messageId,
            Reason         = reason,
        });
}
