using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the analytics endpoints.
/// Tests verify auth guards, ownership checks, and that the service returns 200 with valid data shapes.
/// </summary>
[Collection(E2eCollection.Name)]
public class AnalyticsE2eTests : E2eTestBase
{
    public AnalyticsE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── TrackEvent (AllowAnonymous) ───────────────────────────────────────────

    [Fact]
    public async Task TrackEvent_Anonymous_Returns200()
    {
        var response = await new TrackEventEndpoint(HttpClient).CallAsync("ApartmentView", "Apartment", 1);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TrackEvent_AsAuthenticatedUser_Returns200()
    {
        var user   = await Data.CreateUserAsync("anl-track@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new TrackEventEndpoint(client).CallAsync("RoommateSearch", "Roommate", null, "Roommate");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Summary (Authorize) ───────────────────────────────────────────────────

    [Fact]
    public async Task GetSummary_AsAuthenticatedUser_Returns200()
    {
        var user   = await Data.CreateUserAsync("anl-sum@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetAnalyticsSummaryEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetSummary_WithoutAuth_Returns401()
    {
        var response = await new GetAnalyticsSummaryEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── TopViewedApartments (Authorize) ──────────────────────────────────────

    [Fact]
    public async Task GetTopViewedApartments_AsAuthenticated_Returns200()
    {
        var user   = await Data.CreateUserAsync("anl-topapt@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetTopViewedApartmentsEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTopViewedApartments_WithoutAuth_Returns401()
    {
        var response = await new GetTopViewedApartmentsEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── TopViewedRoommates (Authorize) ────────────────────────────────────────

    [Fact]
    public async Task GetTopViewedRoommates_AsAuthenticated_Returns200()
    {
        var user   = await Data.CreateUserAsync("anl-toproom@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetTopViewedRoommatesEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTopViewedRoommates_WithoutAuth_Returns401()
    {
        var response = await new GetTopViewedRoommatesEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── TopSearchTerms (Authorize) ────────────────────────────────────────────

    [Fact]
    public async Task GetTopSearchTerms_AsAuthenticated_Returns200()
    {
        var user   = await Data.CreateUserAsync("anl-srch@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetTopSearchTermsEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTopSearchTerms_WithoutAuth_Returns401()
    {
        var response = await new GetTopSearchTermsEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── EventTrends (Admin only) ──────────────────────────────────────────────

    [Fact]
    public async Task GetEventTrends_AsAdmin_Returns200()
    {
        var admin  = await Data.CreateUserAsync("anl-adm@e2e.com", role: "Admin");
        var client = CreateAuthenticatedClient(admin.UserId, admin.UserGuid, role: "Admin");
        var from   = DateTime.UtcNow.AddDays(-30);
        var to     = DateTime.UtcNow;

        var response = await new GetEventTrendsEndpoint(client).CallAsync(from, to);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEventTrends_AsRegularUser_Returns403()
    {
        var user   = await Data.CreateUserAsync("anl-adm-reg@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);
        var from   = DateTime.UtcNow.AddDays(-30);
        var to     = DateTime.UtcNow;

        var response = await new GetEventTrendsEndpoint(client).CallAsync(from, to);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetEventTrends_WithoutAuth_Returns401()
    {
        var response = await new GetEventTrendsEndpoint(HttpClient)
            .CallAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEventTrends_RangeOver1Year_Returns400()
    {
        var admin  = await Data.CreateUserAsync("anl-adm2@e2e.com", role: "Admin");
        var client = CreateAuthenticatedClient(admin.UserId, admin.UserGuid, role: "Admin");

        var response = await new GetEventTrendsEndpoint(client)
            .CallAsync(DateTime.UtcNow.AddDays(-400), DateTime.UtcNow);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── User-scoped analytics (ownership guard) ───────────────────────────────

    [Fact]
    public async Task GetUserRoommateSummary_AsOwner_Returns200()
    {
        var user   = await Data.CreateUserAsync("anl-usr-sum@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetUserRoommateSummaryEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserRoommateSummary_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("anl-usr-own@e2e.com");
        var intruder = await Data.CreateUserAsync("anl-usr-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new GetUserRoommateSummaryEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserCompleteAnalytics_AsOwner_Returns200()
    {
        var user   = await Data.CreateUserAsync("anl-full@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetUserCompleteAnalyticsEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserCompleteAnalytics_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("anl-full-own@e2e.com");
        var intruder = await Data.CreateUserAsync("anl-full-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new GetUserCompleteAnalyticsEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserCompleteAnalytics_WithoutAuth_Returns401()
    {
        var owner = await Data.CreateUserAsync("anl-full-na@e2e.com");

        var response = await new GetUserCompleteAnalyticsEndpoint(HttpClient).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── My endpoints (implicit ownership via JWT) ─────────────────────────────

    [Fact]
    public async Task GetMyViewedApartments_AsAuthenticated_Returns200()
    {
        var user   = await Data.CreateUserAsync("anl-myapt@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetMyViewedApartmentsEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyViewedApartments_WithoutAuth_Returns401()
    {
        var response = await new GetMyViewedApartmentsEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyApartmentViews_AsAuthenticated_Returns200()
    {
        var user   = await Data.CreateUserAsync("anl-aptv@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetMyApartmentViewsEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyApartmentViews_WithoutAuth_Returns401()
    {
        var response = await new GetMyApartmentViewsEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyMessagesSent_AsAuthenticated_Returns200()
    {
        var user   = await Data.CreateUserAsync("anl-msg@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetMyMessagesSentEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyMessagesSent_WithoutAuth_Returns401()
    {
        var response = await new GetMyMessagesSentEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
