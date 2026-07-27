using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lander;
using Lander.src.Common.Exceptions;
using Lander.src.Modules.Reviews.Implementation;
using Lander.src.Modules.Reviews.Modules;
using Lander.src.Modules.Reviews.proto;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;

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
        var response = await _service.CreateFavoriteAsync(request);
        response.UserId.Should().Be(1);
        (await _context.Favorites.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateReview_ValidRequest_ShouldSaveToDb()
    {
        var request = new CreateReviewRequest { UserId = 1, ApartmentId = 101, Rating = 5, Comment = "Great", CreatedByGuid = Guid.NewGuid().ToString() };
        var response = await _service.CreateReviewAsync(request);
        response.Rating.Should().Be(5);
        (await _context.Reviews.CountAsync()).Should().Be(1);
    }

    // ArgumentException is what GlobalExceptionHandlerMiddleware turns into a 400.
    [Fact]
    public async Task CreateReview_InvalidRating_ShouldThrowArgumentException()
    {
        var request = new CreateReviewRequest { UserId = 1, Rating = 6, CreatedByGuid = Guid.NewGuid().ToString() };
        var act = async () => await _service.CreateReviewAsync(request);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateFavorite_MalformedGuid_ShouldThrowArgumentException()
    {
        var request = new CreateFavoriteRequest { UserId = 1, ApartmentId = 101, CreatedByGuid = "not-a-guid" };
        var act = async () => await _service.CreateFavoriteAsync(request);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteReview_CallerIsNotOwner_ShouldReturnFailure()
    {
        var ownerGuid = Guid.NewGuid();
        _context.Reviews.Add(new Review { ReviewId = 1, CreatedByGuid = ownerGuid });
        await _context.SaveChangesAsync();

        var response = await _service.DeleteReviewAsync(1, Guid.NewGuid().ToString());

        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Unauthorized");
        (await _context.Reviews.CountAsync()).Should().Be(1, "a non-owner must not delete the review");
    }

    [Fact]
    public async Task DeleteReview_CallerIsOwner_ShouldDelete()
    {
        var ownerGuid = Guid.NewGuid();
        _context.Reviews.Add(new Review { ReviewId = 1, CreatedByGuid = ownerGuid });
        await _context.SaveChangesAsync();

        var response = await _service.DeleteReviewAsync(1, ownerGuid.ToString());

        response.Success.Should().BeTrue();
        (await _context.Reviews.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DeleteReview_EmptyCallerGuid_ShouldReturnFailure()
    {
        _context.Reviews.Add(new Review { ReviewId = 1, CreatedByGuid = Guid.NewGuid() });
        await _context.SaveChangesAsync();

        var response = await _service.DeleteReviewAsync(1, string.Empty);

        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task DeleteFavorite_CallerIsNotOwner_ShouldReturnFailure()
    {
        var ownerGuid = Guid.NewGuid();
        _context.Favorites.Add(new Favorite { FavoriteId = 1, UserId = 1, CreatedByGuid = ownerGuid });
        await _context.SaveChangesAsync();

        var response = await _service.DeleteFavoriteAsync(1, Guid.NewGuid().ToString());

        response.Success.Should().BeFalse();
        response.Message.Should().Contain("Unauthorized");
        (await _context.Favorites.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task DeleteFavorite_CallerIsOwner_ShouldDelete()
    {
        var ownerGuid = Guid.NewGuid();
        _context.Favorites.Add(new Favorite { FavoriteId = 1, UserId = 1, CreatedByGuid = ownerGuid });
        await _context.SaveChangesAsync();

        var response = await _service.DeleteFavoriteAsync(1, ownerGuid.ToString());

        response.Success.Should().BeTrue();
        (await _context.Favorites.CountAsync()).Should().Be(0);
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

        var response = await _service.GetReviewsByApartmentIdAsync(1);
        response.Reviews.Should().HaveCount(1);
        response.Reviews[0].Comment.Should().Be("Public");
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

        var response = await _service.GetUserFavoritesAsync(1);
        response.Favorites.Should().HaveCount(2);
    }

    // NotFoundException is what GlobalExceptionHandlerMiddleware turns into a 404.
    [Fact]
    public async Task GetReviewById_NotFound_ShouldThrowNotFoundException()
    {
        var act = async () => await _service.GetReviewByIdAsync(999);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetReviewById_Found_ShouldReturnReview()
    {
        _context.Reviews.Add(new Review
        {
            ReviewId = 5, ApartmentId = 1, TenantId = 1, Rating = 4,
            ReviewText = "Solid", IsPublic = true, CreatedByGuid = Guid.NewGuid()
        });
        await _context.SaveChangesAsync();

        var response = await _service.GetReviewByIdAsync(5);

        response.ReviewId.Should().Be(5);
        response.Rating.Should().Be(4);
        response.Comment.Should().Be("Solid");
    }

    #endregion
}
