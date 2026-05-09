using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the admin-only Reports endpoints.
/// Seeds real ReportedMessage rows so that review/resolve/delete have real IDs to act on.
/// </summary>
[Collection(E2eCollection.Name)]
public class ReportsE2eTests : E2eTestBase
{
    public ReportsE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Creates reporter, reported user, a message, and a report — returns the report id.</summary>
    private async Task<(int reportId, HttpClient adminClient)> SeedReportAsync(string emailSuffix)
    {
        var reporter = await Data.CreateUserAsync($"rep-reporter-{emailSuffix}@e2e.com");
        var reported = await Data.CreateUserAsync($"rep-reported-{emailSuffix}@e2e.com");
        var admin    = await Data.CreateUserAsync($"rep-admin-{emailSuffix}@e2e.com", role: "Admin");

        var msg    = await Data.CreateMessageAsync(reporter.UserId, reported.UserId, "Offensive message");
        var report = await Data.CreateReportedMessageAsync(msg.MessageId, reporter.UserId, reported.UserId);

        var adminClient = CreateAuthenticatedClient(admin.UserId, admin.UserGuid, role: "Admin");
        return (report.ReportId, adminClient);
    }

    // ── GetAllReports [Admin only] ────────────────────────────────────────────

    [Fact]
    public async Task GetAllReports_AsAdmin_Returns200WithList()
    {
        var (_, adminClient) = await SeedReportAsync("get-all");

        var response = await new GetAllReportsEndpoint(adminClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetAllReports_AsAdmin_FilterByStatus_Returns200()
    {
        var (_, adminClient) = await SeedReportAsync("get-filtered");

        var response = await new GetAllReportsEndpoint(adminClient).CallAsync(status: "Pending");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAllReports_AsRegularUser_Returns403()
    {
        var user   = await Data.CreateUserAsync("rep-getall-reg@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetAllReportsEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllReports_WithoutAuth_Returns401()
    {
        var response = await new GetAllReportsEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── ReviewReport [Admin only] ─────────────────────────────────────────────

    [Fact]
    public async Task ReviewReport_AsAdmin_Returns200()
    {
        var (reportId, adminClient) = await SeedReportAsync("review-ok");

        var response = await new ReviewReportEndpoint(adminClient).CallAsync(reportId, "Reviewed", "Reviewed — message is indeed problematic.");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReviewReport_NonExistent_Returns404()
    {
        var admin  = await Data.CreateUserAsync("rep-rev-404@e2e.com", role: "Admin");
        var client = CreateAuthenticatedClient(admin.UserId, admin.UserGuid, role: "Admin");

        var response = await new ReviewReportEndpoint(client).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReviewReport_AsRegularUser_Returns403()
    {
        var (reportId, _) = await SeedReportAsync("review-reg");
        var user   = await Data.CreateUserAsync("rep-rev-reg@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new ReviewReportEndpoint(client).CallAsync(reportId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReviewReport_WithoutAuth_Returns401()
    {
        var response = await new ReviewReportEndpoint(HttpClient).CallAsync(1);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── ResolveReport [Admin only] ────────────────────────────────────────────

    [Fact]
    public async Task ResolveReport_AsAdmin_Returns200()
    {
        var (reportId, adminClient) = await SeedReportAsync("resolve-ok");

        var response = await new ResolveReportEndpoint(adminClient).CallAsync(reportId, "Resolved — user warned.");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResolveReport_NonExistent_Returns404()
    {
        var admin  = await Data.CreateUserAsync("rep-res-404@e2e.com", role: "Admin");
        var client = CreateAuthenticatedClient(admin.UserId, admin.UserGuid, role: "Admin");

        var response = await new ResolveReportEndpoint(client).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResolveReport_AsRegularUser_Returns403()
    {
        var (reportId, _) = await SeedReportAsync("resolve-reg");
        var user   = await Data.CreateUserAsync("rep-res-reg@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new ResolveReportEndpoint(client).CallAsync(reportId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ResolveReport_WithoutAuth_Returns401()
    {
        var response = await new ResolveReportEndpoint(HttpClient).CallAsync(1);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── DeleteReport [Admin only] ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteReport_AsAdmin_Returns200()
    {
        var (reportId, adminClient) = await SeedReportAsync("delete-ok");

        var response = await new DeleteReportEndpoint(adminClient).CallAsync(reportId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteReport_NonExistent_Returns404()
    {
        var admin  = await Data.CreateUserAsync("rep-del-404@e2e.com", role: "Admin");
        var client = CreateAuthenticatedClient(admin.UserId, admin.UserGuid, role: "Admin");

        var response = await new DeleteReportEndpoint(client).CallAsync(999_999);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteReport_AsRegularUser_Returns403()
    {
        var (reportId, _) = await SeedReportAsync("delete-reg");
        var user   = await Data.CreateUserAsync("rep-del-reg@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new DeleteReportEndpoint(client).CallAsync(reportId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteReport_WithoutAuth_Returns401()
    {
        var response = await new DeleteReportEndpoint(HttpClient).CallAsync(1);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
