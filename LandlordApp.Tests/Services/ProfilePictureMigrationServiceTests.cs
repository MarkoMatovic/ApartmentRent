using FluentAssertions;
using Lander;
using Lander.src.Infrastructure.FileStorage;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Services;
using LandlordApp.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LandlordApp.Tests.Services;

public class ProfilePictureMigrationServiceTests : IDisposable
{
    private readonly UsersContext _db;
    private readonly InMemoryFileStorage _storage = new();

    public ProfilePictureMigrationServiceTests()
    {
        _db = new UsersContext(new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    }

    public void Dispose() => _db.Dispose();

    private ProfilePictureMigrationService Service(int batchSize = 200) => new(
        _db,
        new ProfilePictureService(_storage, NullLogger<ProfilePictureService>.Instance),
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ProfilePictures:MigrationBatchSize"] = batchSize.ToString() })
            .Build(),
        NullLogger<ProfilePictureMigrationService>.Instance);

    private User AddUser(string? picture)
    {
        var user = new User
        {
            UserGuid = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = $"{Guid.NewGuid():N}@test.com",
            Password = "x",
            ProfilePicture = picture,
        };
        _db.Users.Add(user);
        _db.SaveChanges();
        return user;
    }

    private string? PictureOf(int userId) =>
        _db.Users.AsNoTracking().Single(u => u.UserId == userId).ProfilePicture;

    [Fact]
    public async Task RunAsync_MovesInlinePicturesToStorage_AndLeavesTheRestAlone()
    {
        var inline = AddUser(ProfilePictureServiceTests.PhotoDataUrl());
        var stored = AddUser(InMemoryFileStorage.BaseUrl + "/avatars/a/b.webp");
        var none = AddUser(null);

        var moved = await Service().RunAsync();

        moved.Should().Be(1);
        var path = _storage.PathsIn(FileStorageContainers.Avatars).Should().ContainSingle().Subject;
        path.Should().StartWith($"{inline.UserGuid:N}/");
        PictureOf(inline.UserId).Should().Be($"{InMemoryFileStorage.BaseUrl}/avatars/{path}");
        PictureOf(stored.UserId).Should().Be(InMemoryFileStorage.BaseUrl + "/avatars/a/b.webp");
        PictureOf(none.UserId).Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_UnreadablePicture_IsRemovedSoItIsNotRetriedForever()
    {
        var broken = AddUser("data:image/png;base64,bm90IGFuIGltYWdl");

        await Service().RunAsync();

        PictureOf(broken.UserId).Should().BeNull();
        _storage.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_StorageUnreachable_StopsAndKeepsThePictures()
    {
        var first = AddUser(ProfilePictureServiceTests.PhotoDataUrl());
        var second = AddUser(ProfilePictureServiceTests.PhotoDataUrl());
        _storage.FailUploadsWith = new IOException("storage down");

        var moved = await Service().RunAsync();

        moved.Should().Be(0);
        PictureOf(first.UserId).Should().StartWith("data:image/jpeg");
        PictureOf(second.UserId).Should().StartWith("data:image/jpeg");
    }

    [Fact]
    public async Task RunAsync_ConvertsOneBatchPerRun_AndTheNextRunContinues()
    {
        var users = Enumerable.Range(0, 3).Select(_ => AddUser(ProfilePictureServiceTests.PhotoDataUrl(64, 64))).ToList();

        (await Service(batchSize: 2).RunAsync()).Should().Be(2);
        (await Service(batchSize: 2).RunAsync()).Should().Be(1);
        (await Service(batchSize: 2).RunAsync()).Should().Be(0);

        users.Should().OnlyContain(u => PictureOf(u.UserId)!.StartsWith(InMemoryFileStorage.BaseUrl));
    }
}
