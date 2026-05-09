using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the appointment booking flow.
///
/// Happy path:
///   Tenant books appointment → Both sides see it → Landlord confirms → Tenant cancels
///
/// Negative cases:
///   No auth, wrong role, missing appointment.
/// </summary>
[Collection(E2eCollection.Name)]
public class AppointmentE2eTests : E2eTestBase
{
    public AppointmentE2eTests(E2eFixture fixture) : base(fixture) { }

    // helper: a future date guaranteed not to be in the past
    private static DateTime FutureDate(int daysFromNow = 7) =>
        DateTime.UtcNow.Date.AddDays(daysFromNow).AddHours(10);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAppointment_AsTenant_Returns200WithAppointmentId()
    {
        var landlord  = await Data.CreateUserAsync("appt-landlord1@e2e.com", role: "Landlord");
        var tenant    = await Data.CreateUserAsync("appt-tenant1@e2e.com",   role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);
        await Data.CreateApprovedApplicationAsync(tenant.UserId, apartment.ApartmentId);
        var client    = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);

        var response = await new CreateAppointmentEndpoint(client)
            .CallAsync(apartment.ApartmentId, FutureDate(), notes: "Vidimo se!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("appointmentId").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("apartmentId").GetInt32().Should().Be(apartment.ApartmentId);
    }

    [Fact]
    public async Task CreateAppointment_WithoutAuth_Returns401()
    {
        var landlord  = await Data.CreateUserAsync("appt-landlord2@e2e.com", role: "Landlord");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        var response = await new CreateAppointmentEndpoint(HttpClient)
            .CallAsync(apartment.ApartmentId, FutureDate());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyAppointments_AsTenant_ReturnsOwnAppointment()
    {
        var landlord  = await Data.CreateUserAsync("appt-landlord3@e2e.com", role: "Landlord");
        var tenantA   = await Data.CreateUserAsync("appt-tenantA@e2e.com",   role: "Tenant");
        var tenantB   = await Data.CreateUserAsync("appt-tenantB@e2e.com",   role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        await Data.CreateApprovedApplicationAsync(tenantA.UserId, apartment.ApartmentId);

        var clientA = CreateAuthenticatedClient(tenantA.UserId, tenantA.UserGuid);
        var clientB = CreateAuthenticatedClient(tenantB.UserId, tenantB.UserGuid);

        await new CreateAppointmentEndpoint(clientA).CallAsync(apartment.ApartmentId, FutureDate());

        var response = await new GetMyAppointmentsEndpoint(clientA).CallAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        items.EnumerateArray().Should().NotBeEmpty();

        // Tenant B has no appointments
        var responseB = await new GetMyAppointmentsEndpoint(clientB).CallAsync();
        var itemsB    = await responseB.Content.ReadFromJsonAsync<JsonElement>();
        itemsB.EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task GetLandlordAppointments_ShowsAllAppointmentsForLandlordsApartments()
    {
        var landlord  = await Data.CreateUserAsync("appt-ll-view@e2e.com", role: "Landlord");
        var tenantA   = await Data.CreateUserAsync("appt-ta-view@e2e.com", role: "Tenant");
        var tenantB   = await Data.CreateUserAsync("appt-tb-view@e2e.com", role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        await Data.CreateApprovedApplicationAsync(tenantA.UserId, apartment.ApartmentId);
        await Data.CreateApprovedApplicationAsync(tenantB.UserId, apartment.ApartmentId);

        var clientA        = CreateAuthenticatedClient(tenantA.UserId, tenantA.UserGuid);
        var clientB        = CreateAuthenticatedClient(tenantB.UserId, tenantB.UserGuid);
        var landlordClient = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");

        await new CreateAppointmentEndpoint(clientA).CallAsync(apartment.ApartmentId, FutureDate(3));
        await new CreateAppointmentEndpoint(clientB).CallAsync(apartment.ApartmentId, FutureDate(5));

        var response = await new GetLandlordAppointmentsEndpoint(landlordClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        items.EnumerateArray().Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetAppointmentById_ByParticipant_Returns200()
    {
        var landlord  = await Data.CreateUserAsync("appt-byid-ll@e2e.com", role: "Landlord");
        var tenant    = await Data.CreateUserAsync("appt-byid-t@e2e.com",  role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);
        await Data.CreateApprovedApplicationAsync(tenant.UserId, apartment.ApartmentId);
        var client    = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);

        var createResp = await new CreateAppointmentEndpoint(client)
            .CallAsync(apartment.ApartmentId, FutureDate());
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
                     .GetProperty("appointmentId").GetInt32();

        var response = await new GetAppointmentByIdEndpoint(client).CallAsync(id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("appointmentId").GetInt32().Should().Be(id);
    }

    [Fact]
    public async Task GetAppointmentById_NonExistent_Returns404()
    {
        var tenant = await Data.CreateUserAsync("appt-404@e2e.com", role: "Tenant");
        var client = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);

        var response = await new GetAppointmentByIdEndpoint(client).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Full flow: book → confirm → cancel ───────────────────────────────────

    [Fact]
    public async Task FullFlow_TenantBooks_LandlordConfirms_TenantCancels()
    {
        var landlord  = await Data.CreateUserAsync("appt-ll-flow@e2e.com", role: "Landlord");
        var tenant    = await Data.CreateUserAsync("appt-t-flow@e2e.com",  role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        await Data.CreateApprovedApplicationAsync(tenant.UserId, apartment.ApartmentId);

        var tenantClient   = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);
        var landlordClient = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid, "Landlord");

        // 1. Tenant books
        var createResp = await new CreateAppointmentEndpoint(tenantClient)
            .CallAsync(apartment.ApartmentId, FutureDate(10), notes: "Molim potvrdu");
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var apptId = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
                         .GetProperty("appointmentId").GetInt32();

        // 2. Landlord confirms
        var confirmResp = await new UpdateAppointmentStatusEndpoint(landlordClient)
            .CallAsync(apptId, "Confirmed", notes: "Vidimo se!");
        confirmResp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await confirmResp.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("status").GetString().Should().Be("Confirmed");

        // 3. Tenant cancels
        var cancelResp = await new CancelAppointmentEndpoint(tenantClient).CallAsync(apptId);
        cancelResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Appointment no longer in tenant's list (or marked cancelled)
        var listResp = await new GetMyAppointmentsEndpoint(tenantClient).CallAsync();
        var items    = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var cancelled = items.EnumerateArray()
                             .Where(a => a.GetProperty("appointmentId").GetInt32() == apptId)
                             .ToList();

        // Either it's gone or it has Cancelled status
        if (cancelled.Count > 0)
            cancelled[0].GetProperty("status").GetString().Should().Be("Cancelled");
    }

    // ── Status update guards ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatus_ByThirdParty_ReturnsForbidOrNotFound()
    {
        var landlord  = await Data.CreateUserAsync("appt-sec-ll@e2e.com",   role: "Landlord");
        var tenant    = await Data.CreateUserAsync("appt-sec-t@e2e.com",    role: "Tenant");
        var intruder  = await Data.CreateUserAsync("appt-sec-int@e2e.com",  role: "Tenant");
        var apartment = await Data.CreateApartmentAsync(landlord.UserId);

        await Data.CreateApprovedApplicationAsync(tenant.UserId, apartment.ApartmentId);

        var tenantClient   = CreateAuthenticatedClient(tenant.UserId, tenant.UserGuid);
        var intruderClient = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var createResp = await new CreateAppointmentEndpoint(tenantClient)
            .CallAsync(apartment.ApartmentId, FutureDate());
        var id = (await createResp.Content.ReadFromJsonAsync<JsonElement>())
                     .GetProperty("appointmentId").GetInt32();

        var response = await new UpdateAppointmentStatusEndpoint(intruderClient)
            .CallAsync(id, "Confirmed");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound,
            HttpStatusCode.BadRequest);
    }
}
