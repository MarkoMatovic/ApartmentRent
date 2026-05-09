using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for the machine learning endpoints.
/// Tests verify auth guards, ownership checks, and admin-only restrictions.
/// The ML service returns predictions even without training data (default/fallback values).
/// </summary>
[Collection(E2eCollection.Name)]
public class MachineLearningE2eTests : E2eTestBase
{
    public MachineLearningE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── IsModelTrained (no auth required) ────────────────────────────────────

    [Fact]
    public async Task IsModelTrained_Anonymous_Returns200WithBoolResult()
    {
        var response = await new IsModelTrainedEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("isTrained", out _).Should().BeTrue();
    }

    // ── PredictPrice [Authorize] ──────────────────────────────────────────────

    [Fact]
    public async Task PredictPrice_AsAuthenticatedUser_Returns200WithPrediction()
    {
        var user   = await Data.CreateUserAsync("ml-pred@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new PredictPriceEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("predictedPrice", out _).Should().BeTrue();
    }

    [Fact]
    public async Task PredictPrice_WithoutAuth_Returns401()
    {
        var response = await new PredictPriceEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── TrainModel [AdminPolicy] ──────────────────────────────────────────────

    [Fact]
    public async Task TrainModel_AsAdmin_Returns200()
    {
        var admin    = await Data.CreateUserAsync("ml-train-adm@e2e.com", role: "Admin");
        var landlord = await Data.CreateUserAsync("ml-train-landlord@e2e.com");

        // Service requires ≥10 apartments; FastTree needs minimumExampleCountPerLeaf=10
        // after an 80/20 split, so seed 30 to ensure training set has ≥10 per leaf
        for (var i = 1; i <= 30; i++)
            await Data.CreateApartmentAsync(landlord.UserId, $"ML Train Apt {i}",
                rent: 300m + i * 30m, sizeSquareMeters: 30 + i * 2);

        var client = CreateAuthenticatedClient(admin.UserId, admin.UserGuid, role: "Admin");

        var response = await new TrainModelEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TrainModel_AsRegularUser_Returns403()
    {
        var user   = await Data.CreateUserAsync("ml-train-reg@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new TrainModelEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TrainModel_WithoutAuth_Returns401()
    {
        var response = await new TrainModelEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GetModelMetrics [AdminPolicy] ─────────────────────────────────────────

    [Fact]
    public async Task GetModelMetrics_AsAdmin_Returns200()
    {
        var admin  = await Data.CreateUserAsync("ml-metrics-adm@e2e.com", role: "Admin");
        var client = CreateAuthenticatedClient(admin.UserId, admin.UserGuid, role: "Admin");

        var response = await new GetModelMetricsEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetModelMetrics_AsRegularUser_Returns403()
    {
        var user   = await Data.CreateUserAsync("ml-metrics-reg@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetModelMetricsEndpoint(client).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetModelMetrics_WithoutAuth_Returns401()
    {
        var response = await new GetModelMetricsEndpoint(HttpClient).CallAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── GetRoommateMatches [Authorize + ownership] ────────────────────────────

    [Fact]
    public async Task GetRoommateMatches_AsOwner_Returns200()
    {
        var user   = await Data.CreateUserAsync("ml-match@e2e.com");
        await Data.CreateRoommateAsync(user.UserId);
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetRoommateMatchesEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRoommateMatches_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("ml-match-own@e2e.com");
        var intruder = await Data.CreateUserAsync("ml-match-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new GetRoommateMatchesEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetRoommateMatches_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("ml-match-na@e2e.com");

        var response = await new GetRoommateMatchesEndpoint(HttpClient).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── CalculateMatchScore [Authorize + ownership] ───────────────────────────

    [Fact]
    public async Task CalculateMatchScore_AsOneOfTheParticipants_Returns200()
    {
        var u1     = await Data.CreateUserAsync("ml-score-a@e2e.com");
        var u2     = await Data.CreateUserAsync("ml-score-b@e2e.com");
        await Data.CreateRoommateAsync(u1.UserId);
        await Data.CreateRoommateAsync(u2.UserId);
        var client = CreateAuthenticatedClient(u1.UserId, u1.UserGuid);

        var response = await new CalculateMatchScoreEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("matchScore", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CalculateMatchScore_AsThirdParty_Returns403()
    {
        var u1       = await Data.CreateUserAsync("ml-score-u1@e2e.com");
        var u2       = await Data.CreateUserAsync("ml-score-u2@e2e.com");
        var intruder = await Data.CreateUserAsync("ml-score-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new CalculateMatchScoreEndpoint(client).CallAsync(u1.UserId, u2.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CalculateMatchScore_WithoutAuth_Returns401()
    {
        var response = await new CalculateMatchScoreEndpoint(HttpClient).CallAsync(1, 2);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
