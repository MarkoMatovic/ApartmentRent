using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lander;
using Lander.src.Modules.Reviews.Implementation;
using Lander.src.Modules.Reviews.Modules;
using Lander.src.Modules.Reviews.proto;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
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

        // Seed the User required by the Review.TenantId FK
        _context.Users.Add(new User
        {
            UserId    = 1,
            FirstName = "Tenant",
            LastName  = "Test",
            Email     = "t@t.com",
            Password  = "x"
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Create & Delete

    [Fact]
    public async Task CreateFavorite_ValidRequest_ShouldSaveToDb()
    {
        var request = new CreateFavoriteRequest { UserId = 1, ApartmentId = 101, CreatedByGuid = Guid.NewGuid().ToString() };
        var response = await _service.CreateFavorite(request, null!);
        response.UserId.Should().Be(1);
        (await _context.Favorites.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateReview_ValidRequest_ShouldSaveToDb()
    {
        var request = new CreateReviewRequest { UserId = 1, ApartmentId = 101, Rating = 5, Comment = "Great", CreatedByGuid = Guid.NewGuid().ToString() };
        var response = await _service.CreateReview(request, null!);
        response.Rating.Should().Be(5);
        (await _context.Reviews.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateReview_InvalidRating_ShouldThrowRpcException()
    {
        var request = new CreateReviewRequest { UserId = 1, Rating = 6, CreatedByGuid = Guid.NewGuid().ToString() };
        var act = async () => await _service.CreateReview(request, null!);
        await act.Should().ThrowAsync<RpcException>().Where(e => e.Status.StatusCode == StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task DeleteReview_Unauthorized_ShouldReturnFailure()
    {
        var ownerGuid = Guid.NewGuid();
        _context.Reviews.Add(new Review { ReviewId = 1, CreatedByGuid = ownerGuid });
        await _context.SaveChangesAsync();

        var response = await _service.DeleteReview(new DeleteReviewRequest { ReviewId = 1, RequestUserGuid = Guid.NewGuid().ToString() }, null!);
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Unauthorized");
    }

    #endregion

    #region Collection Retrieval

    [Fact]
    public async Task GetReviewsByApartmentId_ShouldOnlyReturnPublic()
    {
        _context.Reviews.AddRange(
            new Review { ApartmentId = 1, TenantId = 1, ReviewText = "Public",  IsPublic = true,  CreatedByGuid = Guid.NewGuid() },
            new Review { ApartmentId = 1, TenantId = 1, ReviewText = "Private", IsPublic = false, CreatedByGuid = Guid.NewGuid() }
        );
        await _context.SaveChangesAsync();

        var response = await _service.GetReviewsByApartmentId(new GetReviewsByApartmentIdRequest { ApartmentId = 1 }, null!);
        response.Reviews.Should().HaveCount(1);
        response.Reviews[0].Comment.Should().Be("Public");
    }

    [Fact]
    public async Task GetFavorites_Limit_ShouldRespectMaxLimit()
    {
        for (int i = 0; i < 15; i++)
            _context.Favorites.Add(new Favorite { FavoriteId = i + 1, UserId = 1, ApartmentId = i + 100, CreatedByGuid = Guid.NewGuid() });
        await _context.SaveChangesAsync();

        var response = await _service.GetFavorites(new GetFavoritesRequest { Limit = 5 }, null!);
        response.Favorites.Should().HaveCount(5);

        var largeResponse = await _service.GetFavorites(new GetFavoritesRequest { Limit = 100 }, null!);
        largeResponse.Favorites.Should().HaveCount(10); // Service caps at 10
    }

    [Fact]
    public async Task GetUserFavorites_ShouldReturnOnlyForUser()
    {
        _context.Favorites.AddRange(
            new Favorite { UserId = 1, ApartmentId = 101, CreatedByGuid = Guid.NewGuid() },
            new Favorite { UserId = 1, ApartmentId = 102, CreatedByGuid = Guid.NewGuid() },
            new Favorite { UserId = 2, ApartmentId = 101, CreatedByGuid = Guid.NewGuid() }
        );
        await _context.SaveChangesAsync();

        var response = await _service.GetUserFavorites(new GetUserFavoritesRequest { UserId = 1 }, null!);
        response.Favorites.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetReviewById_NotFound_ShouldThrowRpcException()
    {
        var act = async () => await _service.GetReviewById(new GetReviewByIdRequest { ReviewId = 999 }, null!);
        await act.Should().ThrowAsync<RpcException>().Where(e => e.Status.StatusCode == StatusCode.NotFound);
    }

    #endregion
}
