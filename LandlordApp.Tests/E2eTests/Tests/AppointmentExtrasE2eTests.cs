using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for appointment endpoints not covered in the main AppointmentE2eTests:
///   – GetAvailableSlots (AllowAnonymous)
///   – GetMyAvailability (Authorize)
///   – SetMyAvailability (Authorize)
/// </summary>
[Collection(E2eCollection.Name)]
public class AppointmentExtrasE2eTests : E2eTestBase
{
    public AppointmentExtrasE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── GetAvailableSlots [AllowAnonymous] ────────────────────────────────────

    [Fact]
    public async Task GetAvailableSlots_Anonymous_Returns200()
    {
        var landlord = await Data.CreateUserAsync("appt-slots-anon@e2e.com");
        var apt      = await Data.CreateApartmentAsync(landlord.UserId, "Slot Test Apt");

        var response = await new GetAvailableSlotsEndpoint(HttpClient).CallAsync(apt.ApartmentId);

        // Returns 200 with empty array when no availability is set
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAvailableSlots_NonExistentApartment_Returns400()
    {
        var response = await new GetAvailableSlotsEndpoint(HttpClient).CallAsync(999_999);

        // Service throws ArgumentException("Apartment not found") → 400 via GlobalExceptionHandlerMiddleware
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAvailableSlots_WithDate_Returns200()
    {
        var landlord = await Data.CreateUserAsync("appt-slots-date@e2e.com");
        var apt      = await Data.CreateApartmentAsync(landlord.UserId, "Date Slot Apt");
        var date     = DateTime.UtcNow.Date.AddDays(3);

        var response = await new GetAvailableSlotsEndpoint(HttpClient).CallAsync(apt.ApartmentId, date);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GetMyAvailability [Authorize] ─────────────────────────────────────────

    [Fact]
    public async Task GetMyAvailability_AsAuthenticatedLandlord_Returns200()
    {
        var landlord = await Data.CreateUserAsync("appt-avail-get@e2e.com");
        var client   = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid);

        var response = await new GetMyAvailabilityEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetMyAvailability_WithoutAuth_Returns401()
    {
        var response = await new GetMyAvailabilityEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── SetMyAvailability [Authorize] ─────────────────────────────────────────

    [Fact]
    public async Task SetMyAvailability_AsAuthenticatedLandlord_Returns200WithSlots()
    {
        var landlord = await Data.CreateUserAsync("appt-avail-set@e2e.com");
        var client   = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid);

        var dto = new
        {
            Slots = new[]
            {
                new { DayOfWeek = 1, StartTime = "09:00:00", EndTime = "12:00:00" }, // Monday
                new { DayOfWeek = 3, StartTime = "14:00:00", EndTime = "17:00:00" }, // Wednesday
            }
        };

        var response = await new SetMyAvailabilityEndpoint(client).CallAsync(dto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task SetMyAvailability_EmptySlots_Returns200()
    {
        var landlord = await Data.CreateUserAsync("appt-avail-empty@e2e.com");
        var client   = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid);

        var dto = new { Slots = Array.Empty<object>() };

        var response = await new SetMyAvailabilityEndpoint(client).CallAsync(dto);

        // Clearing all slots is a valid operation
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetMyAvailability_WithoutAuth_Returns401()
    {
        var dto = new
        {
            Slots = new[] { new { DayOfWeek = 1, StartTime = "09:00:00", EndTime = "12:00:00" } }
        };

        var response = await new SetMyAvailabilityEndpoint(HttpClient).CallAsync(dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetMyAvailability_OverwritesPreviousSlots()
    {
        var landlord = await Data.CreateUserAsync("appt-avail-overwrite@e2e.com");
        var client   = CreateAuthenticatedClient(landlord.UserId, landlord.UserGuid);

        // First set
        var firstDto = new
        {
            Slots = new[] { new { DayOfWeek = 1, StartTime = "09:00:00", EndTime = "12:00:00" } }
        };
        await new SetMyAvailabilityEndpoint(client).CallAsync(firstDto);

        // Overwrite with a different set
        var secondDto = new
        {
            Slots = new[]
            {
                new { DayOfWeek = 2, StartTime = "10:00:00", EndTime = "13:00:00" },
                new { DayOfWeek = 4, StartTime = "15:00:00", EndTime = "18:00:00" },
            }
        };
        var response = await new SetMyAvailabilityEndpoint(client).CallAsync(secondDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Should have exactly the 2 new slots
        body.GetArrayLength().Should().Be(2);
    }
}
