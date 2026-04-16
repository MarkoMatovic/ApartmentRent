using FluentAssertions;
using Lander.src.Modules.Reviews.Client;
using Lander.src.Modules.Reviews.Controllers;
using Lander.src.Modules.Reviews.proto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace LandlordApp.Tests.Controllers;

public class ReviewsFavoritesControllerTests
{
    private readonly Mock<IGrpcServiceClient> _mockGrpc;
    private readonly ReviewsFavoritesController _controller;

    public ReviewsFavoritesControllerTests()
    {
        _mockGrpc = new Mock<IGrpcServiceClient>();
        _controller = new ReviewsFavoritesController(_mockGrpc.Object);
        _controller.ControllerContext = MakeAuthContext();
    }

    // ─── CreateFavorite ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateFavorite_ReturnsOk()
    {
        var request = new CreateFavoriteRequest { UserId = 1, ApartmentId = 10 };
        var response = new FavoriteResponse();
        _mockGrpc.Setup(g => g.CreateFavoriteAsync(request)).ReturnsAsync(response);

        var result = await _controller.CreateFavorite(request);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task CreateFavorite_GrpcThrows_Propagates()
    {
        _mockGrpc.Setup(g => g.CreateFavoriteAsync(It.IsAny<CreateFavoriteRequest>()))
            .ThrowsAsync(new Exception("gRPC unavailable"));

        Func<Task> act = () => _controller.CreateFavorite(new CreateFavoriteRequest());

        await act.Should().ThrowAsync<Exception>().WithMessage("gRPC unavailable");
    }

    // ─── CreateReview ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateReview_ReturnsOk()
    {
        var request = new CreateReviewRequest { UserId = 1, ApartmentId = 5, Rating = 5 };
        var response = new ReviewResponse();
        _mockGrpc.Setup(g => g.CreateReviewAsync(request)).ReturnsAsync(response);

        var result = await _controller.CreateReview(request);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task CreateReview_GrpcThrows_Propagates()
    {
        _mockGrpc.Setup(g => g.CreateReviewAsync(It.IsAny<CreateReviewRequest>()))
            .ThrowsAsync(new Exception("gRPC unavailable"));

        Func<Task> act = () => _controller.CreateReview(new CreateReviewRequest());

        await act.Should().ThrowAsync<Exception>().WithMessage("gRPC unavailable");
    }

    // ─── GetReviewById ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReviewById_ReturnsOk()
    {
        var response = new ReviewResponse { ReviewId = 3 };
        _mockGrpc.Setup(g => g.GetReviewByIdAsync(3)).ReturnsAsync(response);

        var result = await _controller.GetReviewById(3);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetReviewById_GrpcThrows_Propagates()
    {
        _mockGrpc.Setup(g => g.GetReviewByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("gRPC unavailable"));

        Func<Task> act = () => _controller.GetReviewById(99);

        await act.Should().ThrowAsync<Exception>().WithMessage("gRPC unavailable");
    }

    // ─── GetReviewsByApartmentId ──────────────────────────────────────────────

    [Fact]
    public async Task GetReviewsByApartmentId_ReturnsOkWithReviews()
    {
        var reviewsResponse = new GetReviewsResponse();
        _mockGrpc.Setup(g => g.GetReviewsByApartmentIdAsync(7)).ReturnsAsync(reviewsResponse);

        var result = await _controller.GetReviewsByApartmentId(7);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetReviewsByApartmentId_GrpcThrows_Propagates()
    {
        _mockGrpc.Setup(g => g.GetReviewsByApartmentIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("gRPC unavailable"));

        Func<Task> act = () => _controller.GetReviewsByApartmentId(7);

        await act.Should().ThrowAsync<Exception>().WithMessage("gRPC unavailable");
    }

    // ─── DeleteReview ─────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteReview_ReturnsOk()
    {
        var response = new DeleteResponse { Success = true };
        _mockGrpc.Setup(g => g.DeleteReviewAsync(4)).ReturnsAsync(response);

        var result = await _controller.DeleteReview(4);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task DeleteReview_GrpcThrows_Propagates()
    {
        _mockGrpc.Setup(g => g.DeleteReviewAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("gRPC unavailable"));

        Func<Task> act = () => _controller.DeleteReview(4);

        await act.Should().ThrowAsync<Exception>().WithMessage("gRPC unavailable");
    }

    // ─── DeleteFavorite ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteFavorite_ReturnsOk()
    {
        var response = new DeleteResponse { Success = true };
        _mockGrpc.Setup(g => g.DeleteFavoriteAsync(9)).ReturnsAsync(response);

        var result = await _controller.DeleteFavorite(9);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(response);
    }

    [Fact]
    public async Task DeleteFavorite_GrpcThrows_Propagates()
    {
        _mockGrpc.Setup(g => g.DeleteFavoriteAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("gRPC unavailable"));

        Func<Task> act = () => _controller.DeleteFavorite(9);

        await act.Should().ThrowAsync<Exception>().WithMessage("gRPC unavailable");
    }

    // ─── GetUserFavorites ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserFavorites_ReturnsOkWithFavorites()
    {
        var favResponse = new GetFavoritesResponse();
        _mockGrpc.Setup(g => g.GetUserFavoritesAsync(2)).ReturnsAsync(favResponse);

        var result = await _controller.GetUserFavorites(2);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetUserFavorites_GrpcThrows_Propagates()
    {
        _mockGrpc.Setup(g => g.GetUserFavoritesAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("gRPC unavailable"));

        Func<Task> act = () => _controller.GetUserFavorites(2);

        await act.Should().ThrowAsync<Exception>().WithMessage("gRPC unavailable");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static ControllerContext MakeAuthContext(int userId = 1)
    {
        var claims = new List<Claim> { new("userId", userId.ToString()) };
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        return new ControllerContext { HttpContext = httpContext };
    }
}
