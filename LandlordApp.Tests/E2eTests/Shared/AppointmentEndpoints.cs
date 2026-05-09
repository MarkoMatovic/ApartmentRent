using System.Net.Http.Json;

namespace LandlordApp.Tests.E2eTests.Shared;

// ── Endpoint wrappers ─────────────────────────────────────────────────────────

/// <summary>POST /api/appointments</summary>
public class CreateAppointmentEndpoint
{
    private readonly HttpClient _client;
    public CreateAppointmentEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int apartmentId, DateTime date, string? notes = null)
        => _client.PostAsJsonAsync("/api/appointments", new
        {
            ApartmentId     = apartmentId,
            AppointmentDate = date,
            TenantNotes     = notes,
        });
}

/// <summary>GET /api/appointments/my-appointments</summary>
public class GetMyAppointmentsEndpoint
{
    private readonly HttpClient _client;
    public GetMyAppointmentsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/appointments/my-appointments");
}

/// <summary>GET /api/appointments/landlord-appointments</summary>
public class GetLandlordAppointmentsEndpoint
{
    private readonly HttpClient _client;
    public GetLandlordAppointmentsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/appointments/landlord-appointments");
}

/// <summary>PUT /api/appointments/{id}/status</summary>
public class UpdateAppointmentStatusEndpoint
{
    private readonly HttpClient _client;
    public UpdateAppointmentStatusEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id, string status, string? notes = null)
        => _client.PutAsJsonAsync($"/api/appointments/{id}/status", new
        {
            Status        = status,
            LandlordNotes = notes,
        });
}

/// <summary>DELETE /api/appointments/{id}</summary>
public class CancelAppointmentEndpoint
{
    private readonly HttpClient _client;
    public CancelAppointmentEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.DeleteAsync($"/api/appointments/{id}");
}

/// <summary>GET /api/appointments/{id}</summary>
public class GetAppointmentByIdEndpoint
{
    private readonly HttpClient _client;
    public GetAppointmentByIdEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int id)
        => _client.GetAsync($"/api/appointments/{id}");
}

/// <summary>GET /api/appointments/available-slots/{apartmentId}?date={date} [AllowAnonymous]</summary>
public class GetAvailableSlotsEndpoint
{
    private readonly HttpClient _client;
    public GetAvailableSlotsEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(int apartmentId, DateTime? date = null)
    {
        var d = (date ?? DateTime.UtcNow.Date).ToString("yyyy-MM-dd");
        return _client.GetAsync($"/api/appointments/available-slots/{apartmentId}?date={d}");
    }
}

/// <summary>GET /api/appointments/availability [Authorize]</summary>
public class GetMyAvailabilityEndpoint
{
    private readonly HttpClient _client;
    public GetMyAvailabilityEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync()
        => _client.GetAsync("/api/appointments/availability");
}

/// <summary>PUT /api/appointments/availability [Authorize]</summary>
public class SetMyAvailabilityEndpoint
{
    private readonly HttpClient _client;
    public SetMyAvailabilityEndpoint(HttpClient client) => _client = client;

    public Task<HttpResponseMessage> CallAsync(object dto)
        => _client.PutAsJsonAsync("/api/appointments/availability", dto);
}
