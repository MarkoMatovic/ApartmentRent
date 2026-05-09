using System.Text.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, string RefreshToken);

public record RegisterRequest(string FirstName, string LastName, string Email, string Password);

// ── Endpoint wrappers ─────────────────────────────────────────────────────────

/// <summary>POST /api/v1/auth/login</summary>
public class LoginEndpoint : ApiEndpointBase<LoginRequest, LoginResponse>
{
    public LoginEndpoint(HttpClient client) : base(client) { }
    protected override string Url    => "/api/v1/auth/login";
    protected override HttpMethod Method => HttpMethod.Post;
}

/// <summary>POST /api/v1/auth/register</summary>
public class RegisterEndpoint : ApiEndpointBase<RegisterRequest, JsonElement>
{
    public RegisterEndpoint(HttpClient client) : base(client) { }
    protected override string Url       => "/api/v1/auth/register";
    protected override HttpMethod Method => HttpMethod.Post;
}

/// <summary>POST /api/v1/auth/logout</summary>
public class LogoutEndpoint
{
    private readonly HttpClient _client;
    public LogoutEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.PostAsync("/api/v1/auth/logout", null);
}

/// <summary>POST /api/v1/auth/token/refresh</summary>
public class RefreshTokenEndpoint
{
    private readonly HttpClient _client;
    public RefreshTokenEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.PostAsync("/api/v1/auth/token/refresh", null);
}

/// <summary>GET /api/v1/auth/profile/{userId}</summary>
public class GetProfileEndpoint
{
    private readonly HttpClient _client;
    public GetProfileEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/auth/profile/{userId}");
}
