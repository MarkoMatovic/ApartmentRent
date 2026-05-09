using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

/// <summary>POST /api/v1/auth/register (with all required fields incl. DateOfBirth, PhoneNumber)</summary>
public class RegisterWithDetailsEndpoint
{
    private readonly HttpClient _client;
    public RegisterWithDetailsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string email, string password,
        string firstName = "Test", string lastName = "User")
        => _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            FirstName   = firstName,
            LastName    = lastName,
            Email       = email,
            Password    = password,
            DateOfBirth = new DateTime(1990, 1, 1),
            PhoneNumber = "+387601234567",
        });
}

/// <summary>POST /api/v1/auth/change-password</summary>
public class ChangePasswordEndpoint
{
    private readonly HttpClient _client;
    public ChangePasswordEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(Guid userId, string oldPassword, string newPassword)
        => _client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            UserId      = userId,
            OldPassword = oldPassword,
            NewPassword = newPassword,
        });
}

/// <summary>PUT /api/v1/auth/update-profile/{userId}</summary>
public class UpdateProfileEndpoint
{
    private readonly HttpClient _client;
    public UpdateProfileEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId, object dto)
        => _client.PutAsJsonAsync($"/api/v1/auth/update-profile/{userId}", dto);
}

/// <summary>DELETE /api/v1/auth/delete-user/{userGuid}</summary>
public class DeleteUserEndpoint
{
    private readonly HttpClient _client;
    public DeleteUserEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(Guid userGuid)
        => _client.DeleteAsync($"/api/v1/auth/delete-user/{userGuid}");
}

/// <summary>POST /api/v1/auth/deactivate-user</summary>
public class DeactivateUserEndpoint
{
    private readonly HttpClient _client;
    public DeactivateUserEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(Guid userGuid)
        => _client.PostAsJsonAsync("/api/v1/auth/deactivate-user", new { UserGuid = userGuid });
}

/// <summary>PUT /api/v1/auth/update-privacy-settings/{userId}</summary>
public class UpdatePrivacySettingsEndpoint
{
    private readonly HttpClient _client;
    public UpdatePrivacySettingsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId, bool analytics, bool chat, bool visible)
        => _client.PutAsJsonAsync($"/api/v1/auth/update-privacy-settings/{userId}", new
        {
            AnalyticsConsent   = analytics,
            ChatHistoryConsent = chat,
            ProfileVisibility  = visible,
        });
}

/// <summary>GET /api/v1/auth/profile/{userId}</summary>
public class GetUserProfileEndpoint
{
    private readonly HttpClient _client;
    public GetUserProfileEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/auth/profile/{userId}");
}

/// <summary>GET /api/v1/auth/export-data/{userId} [Authorize]</summary>
public class ExportUserDataEndpoint
{
    private readonly HttpClient _client;
    public ExportUserDataEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int userId)
        => _client.GetAsync($"/api/v1/auth/export-data/{userId}");
}

/// <summary>POST /api/v1/auth/update-roommate-status [Authorize]</summary>
public class UpdateRoommateStatusEndpoint
{
    private readonly HttpClient _client;
    public UpdateRoommateStatusEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(Guid userGuid, bool isActive)
        => _client.PostAsJsonAsync("/api/v1/auth/update-roommate-status", new
        {
            UserGuid = userGuid,
            IsActive = isActive,
        });
}

/// <summary>POST /api/v1/auth/forgot-password [AllowAnonymous]</summary>
public class ForgotPasswordEndpoint
{
    private readonly HttpClient _client;
    public ForgotPasswordEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string email)
        => _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = email });
}

/// <summary>POST /api/v1/auth/reset-password [AllowAnonymous]</summary>
public class ResetPasswordEndpoint
{
    private readonly HttpClient _client;
    public ResetPasswordEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string token, string newPassword)
        => _client.PostAsJsonAsync("/api/v1/auth/reset-password", new { Token = token, NewPassword = newPassword });
}

/// <summary>GET /api/v1/auth/verify-email?token={token} [AllowAnonymous]</summary>
public class VerifyEmailEndpoint
{
    private readonly HttpClient _client;
    public VerifyEmailEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(string token)
        => _client.GetAsync($"/api/v1/auth/verify-email?token={Uri.EscapeDataString(token)}");
}

/// <summary>POST /api/v1/auth/reactivate-user [Admin only]</summary>
public class ReactivateUserEndpoint
{
    private readonly HttpClient _client;
    public ReactivateUserEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(Guid userGuid)
        => _client.PostAsJsonAsync("/api/v1/auth/reactivate-user", new { UserGuid = userGuid });
}
