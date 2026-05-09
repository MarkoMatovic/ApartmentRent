using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.E2eTests.Shared;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// E2E tests for reviews and favorites.
/// The gRPC service is replaced by a mock that returns default empty responses.
/// Tests verify HTTP-layer logic (auth guards, ownership checks, routing).
/// </summary>
[Collection(E2eCollection.Name)]
public class ReviewFavoriteE2eTests : E2eTestBase
{
    public ReviewFavoriteE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── Favorites ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateFavorite_AsAuthenticatedUser_Returns200()
    {
        var user     = await Data.CreateUserAsync("fav-create@e2e.com");
        var landlord = await Data.CreateUserAsync("fav-land@e2e.com", role: "Landlord");
        var apt      = await Data.CreateApartmentAsync(landlord.UserId);
        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new CreateFavoriteEndpoint(client).CallAsync(apt.ApartmentId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateFavorite_WithoutAuth_Returns401()
    {
        var landlord = await Data.CreateUserAsync("fav-land-na@e2e.com", role: "Landlord");
        var apt      = await Data.CreateApartmentAsync(landlord.UserId);

        var response = await new CreateFavoriteEndpoint(HttpClient).CallAsync(apt.ApartmentId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserFavorites_AsOwner_Returns200()
    {
        var user   = await Data.CreateUserAsync("fav-get@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new GetUserFavoritesEndpoint(client).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserFavorites_AsOtherUser_Returns403()
    {
        var owner    = await Data.CreateUserAsync("fav-get-own@e2e.com");
        var intruder = await Data.CreateUserAsync("fav-get-int@e2e.com");
        var client   = CreateAuthenticatedClient(intruder.UserId, intruder.UserGuid);

        var response = await new GetUserFavoritesEndpoint(client).CallAsync(owner.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserFavorites_WithoutAuth_Returns401()
    {
        var user = await Data.CreateUserAsync("fav-get-na@e2e.com");

        var response = await new GetUserFavoritesEndpoint(HttpClient).CallAsync(user.UserId);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteFavorite_AsAuthenticatedUser_Returns200()
    {
        var user   = await Data.CreateUserAsync("fav-del@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        // Mock returns DeleteResponse with Success=false, Message="" → controller returns Ok
        var response = await new DeleteFavoriteEndpoint(client).CallAsync(1);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteFavorite_WithoutAuth_Returns401()
    {
        var response = await new DeleteFavoriteEndpoint(HttpClient).CallAsync(1);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Reviews ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReview_AsAuthenticatedUser_Returns200()
    {
        var user     = await Data.CreateUserAsync("rev-create@e2e.com");
        var landlord = await Data.CreateUserAsync("rev-land@e2e.com", role: "Landlord");
        var apt      = await Data.CreateApartmentAsync(landlord.UserId);
        var client   = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        var response = await new CreateReviewEndpoint(client).CallAsync(apt.ApartmentId, 5, "Odličan stan!");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateReview_WithoutAuth_Returns401()
    {
        var response = await new CreateReviewEndpoint(HttpClient).CallAsync(1, 4, "Komentar");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReviewById_Anonymous_Returns200()
    {
        // GetReviewById is AllowAnonymous — no auth needed
        var response = await new GetReviewByIdEndpoint(HttpClient).CallAsync(1);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReviewsByApartmentId_Anonymous_Returns200()
    {
        // GetReviewsByApartmentId is AllowAnonymous
        var landlord = await Data.CreateUserAsync("rev-apt-land@e2e.com", role: "Landlord");
        var apt      = await Data.CreateApartmentAsync(landlord.UserId);

        var response = await new GetReviewsByApartmentIdEndpoint(HttpClient).CallAsync(apt.ApartmentId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteReview_AsAuthenticatedUser_Returns200()
    {
        var user   = await Data.CreateUserAsync("rev-del@e2e.com");
        var client = CreateAuthenticatedClient(user.UserId, user.UserGuid);

        // Mock returns DeleteResponse with Success=false, Message="" → controller returns Ok
        var response = await new DeleteReviewEndpoint(client).CallAsync(1);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteReview_WithoutAuth_Returns401()
    {
        var response = await new DeleteReviewEndpoint(HttpClient).CallAsync(1);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
