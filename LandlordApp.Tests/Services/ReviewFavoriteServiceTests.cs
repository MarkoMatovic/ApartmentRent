using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lander;
using Lander.src.Modules.Reviews.Implementation;
using Lander.src.Modules.Reviews.Modules;
using Lander.src.Modules.Reviews.proto;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace LandlordApp.Tests.Services;

public class ReviewFavoriteServiceTests : IDisposable
{
    private readonly ReviewsContext _context;
    private readonly ReviewFavoriteService _service;

    public ReviewFavoriteServiceTests()
    {
        var options = new DbContextOptionsBuilder<ReviewsContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ReviewsContext(options);
        _service = new ReviewFavoriteService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task CreateFavorite_ValidRequest_ShouldSaveToDb()
    {
        // Arrange
        var request = new CreateFavoriteRequest
        {
            UserId = 1,
            ApartmentId = 101,
            CreatedByGuid = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _service.CreateFavorite(request, null!);

        // Assert
        response.Should().NotBeNull();
        response.UserId.Should().Be(1);
        response.ApartmentId.Should().Be(101);

        var dbFavorite = await _context.Favorites.FirstOrDefaultAsync();
        dbFavorite.Should().NotBeNull();
        dbFavorite!.UserId.Should().Be(1);
    }

    [Fact]
    public async Task CreateReview_ValidRequest_ShouldSaveToDb()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            UserId = 1,
            ApartmentId = 101,
            Rating = 5,
            Comment = "Great place!",
            IsAnonymous = false,
            IsPublic = true,
            CreatedByGuid = Guid.NewGuid().ToString()
        };

        // Act
        var response = await _service.CreateReview(request, null!);

        // Assert
        response.Should().NotBeNull();
        response.Rating.Should().Be(5);
        response.Comment.Should().Be("Great place!");

        var dbReview = await _context.Reviews.FirstOrDefaultAsync();
        dbReview.Should().NotBeNull();
        dbReview!.Rating.Should().Be(5);
    }

    [Fact]
    public async Task GetReviewById_ExistentReview_ShouldReturnReview()
    {
        // Arrange
        var review = new Review
        {
            ReviewId = 1,
            TenantId = 1,
            ApartmentId = 101,
            Rating = 4,
            ReviewText = "Good",
            CreatedByGuid = Guid.NewGuid()
        };
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        var request = new GetReviewByIdRequest { ReviewId = 1 };

        // Act
        var response = await _service.GetReviewById(request, null!);

        // Assert
        response.Should().NotBeNull();
        response.ReviewId.Should().Be(1);
        response.Comment.Should().Be("Good");
    }

    [Fact]
    public async Task DeleteFavorite_ExistentId_ShouldRemoveFromDb()
    {
        // Arrange
        var favorite = new Favorite
        {
            FavoriteId = 1,
            UserId = 1,
            ApartmentId = 101,
            CreatedByGuid = Guid.NewGuid()
        };
        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();

        var request = new DeleteFavoriteRequest 
        { 
            FavoriteId = 1,
            RequestUserGuid = favorite.CreatedByGuid.ToString()
        };

        // Act
        var response = await _service.DeleteFavorite(request, null!);

        // Assert
        response.Success.Should().BeTrue();
        var dbFavorite = await _context.Favorites.FindAsync(1);
        dbFavorite.Should().BeNull();
    }

    [Fact]
    public async Task CreateReview_InvalidRating_ShouldThrowRpcException()
    {
        // Arrange
        var request = new CreateReviewRequest
        {
            UserId = 1,
            Rating = 6, // Invalid
            CreatedByGuid = Guid.NewGuid().ToString()
        };

        // Act
        var act = async () => await _service.CreateReview(request, null!);

        // Assert
        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.Status.StatusCode == StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DeleteReview_UnauthorizedUser_ShouldReturnFailure()
    {
        // Arrange
        var ownerGuid = Guid.NewGuid();
        var review = new Review
        {
            ReviewId = 1,
            CreatedByGuid = ownerGuid
        };
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        var request = new DeleteReviewRequest 
        { 
            ReviewId = 1, 
            RequestUserGuid = Guid.NewGuid().ToString() // Different user
        };

        // Act
        var response = await _service.DeleteReview(request, null!);

        // Assert
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task DeleteReview_ExistentId_ShouldRemoveFromDb()
    {
        // Arrange
        var ownerGuid = Guid.NewGuid();
        var review = new Review
        {
            ReviewId = 1,
            CreatedByGuid = ownerGuid
        };
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        var request = new DeleteReviewRequest 
        { 
            ReviewId = 1, 
            RequestUserGuid = ownerGuid.ToString()
        };

        // Act
        var response = await _service.DeleteReview(request, null!);

        // Assert
        response.Success.Should().BeTrue();
        var dbReview = await _context.Reviews.FindAsync(1);
        dbReview.Should().BeNull();
    }
}
