using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Lander;
using Lander.src.Modules.Users.Domain.IRepository;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Dtos;
using Lander.src.Modules.Users.Implementation.PermissionImplementation;

namespace LandlordApp.Tests.Services;

public class PermissionServiceTests : IDisposable
{
    private readonly Mock<IPermissionRepository> _mockRepo;
    private readonly UsersContext _context;
    private readonly PermissionService _service;

    public PermissionServiceTests()
    {
        _mockRepo = new Mock<IPermissionRepository>();

        var options = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new UsersContext(options);
        _service = new PermissionService(_mockRepo.Object, _context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ─── GetAllPermissionsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAllPermissionsAsync_ReturnsAllPermissions()
    {
        var permissions = new List<Permission>
        {
            new() { PermissionId = 1, PermissionName = "Read", Description = "Read access" },
            new() { PermissionId = 2, PermissionName = "Write", Description = "Write access" }
        };
        _mockRepo.Setup(r => r.GetAllPermissionsAsync()).ReturnsAsync(permissions);

        var result = await _service.GetAllPermissionsAsync();

        var dtos = result.ToList();
        dtos.Should().HaveCount(2);
        dtos[0].PermissionId.Should().Be(1);
        dtos[0].PermissionName.Should().Be("Read");
        dtos[0].Description.Should().Be("Read access");
        dtos[1].PermissionId.Should().Be(2);
        dtos[1].PermissionName.Should().Be("Write");
    }

    [Fact]
    public async Task GetAllPermissionsAsync_EmptyRepo_ReturnsEmptyList()
    {
        _mockRepo.Setup(r => r.GetAllPermissionsAsync()).ReturnsAsync(new List<Permission>());

        var result = await _service.GetAllPermissionsAsync();

        result.Should().BeEmpty();
    }

    // ─── GetPermissionByIdAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetPermissionByIdAsync_ExistingId_ReturnsDto()
    {
        var permission = new Permission { PermissionId = 5, PermissionName = "Delete", Description = "Delete access" };
        _mockRepo.Setup(r => r.GetPermissionByIdAsync(5)).ReturnsAsync(permission);

        var result = await _service.GetPermissionByIdAsync(5);

        result.Should().NotBeNull();
        result!.PermissionId.Should().Be(5);
        result.PermissionName.Should().Be("Delete");
        result.Description.Should().Be("Delete access");
    }

    [Fact]
    public async Task GetPermissionByIdAsync_NonExistingId_ReturnsNull()
    {
        _mockRepo.Setup(r => r.GetPermissionByIdAsync(999)).ReturnsAsync((Permission?)null);

        var result = await _service.GetPermissionByIdAsync(999);

        result.Should().BeNull();
    }

    // ─── GetPermissionsByRoleIdAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetPermissionsByRoleIdAsync_ReturnsPermissionsForRole()
    {
        var permissions = new List<Permission>
        {
            new() { PermissionId = 10, PermissionName = "ViewReports", Description = null },
            new() { PermissionId = 11, PermissionName = "ManageUsers", Description = "Manage user accounts" }
        };
        _mockRepo.Setup(r => r.GetPermissionsByRoleIdAsync(3)).ReturnsAsync(permissions);

        var result = await _service.GetPermissionsByRoleIdAsync(3);

        var dtos = result.ToList();
        dtos.Should().HaveCount(2);
        dtos.Should().Contain(d => d.PermissionId == 10 && d.PermissionName == "ViewReports");
        dtos.Should().Contain(d => d.PermissionId == 11 && d.PermissionName == "ManageUsers");
    }

    // ─── GetPermissionsByUserIdAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetPermissionsByUserIdAsync_UserWithRole_ReturnsPermissions()
    {
        var user = new User
        {
            UserId = 42,
            UserGuid = Guid.NewGuid(),
            FirstName = "Ana",
            LastName = "Petrovic",
            Email = "ana@example.com",
            Password = "hash",
            IsActive = true,
            UserRoleId = 7
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var permissions = new List<Permission>
        {
            new() { PermissionId = 20, PermissionName = "CreateListing", Description = null }
        };
        _mockRepo.Setup(r => r.GetPermissionsByRoleIdAsync(7)).ReturnsAsync(permissions);

        var result = await _service.GetPermissionsByUserIdAsync(42);

        var dtos = result.ToList();
        dtos.Should().HaveCount(1);
        dtos[0].PermissionId.Should().Be(20);
        dtos[0].PermissionName.Should().Be("CreateListing");
    }

    [Fact]
    public async Task GetPermissionsByUserIdAsync_UserWithoutRole_ReturnsEmpty()
    {
        var user = new User
        {
            UserId = 43,
            UserGuid = Guid.NewGuid(),
            FirstName = "Marko",
            LastName = "Jovic",
            Email = "marko@example.com",
            Password = "hash",
            IsActive = true,
            UserRoleId = null
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var result = await _service.GetPermissionsByUserIdAsync(43);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionsByUserIdAsync_UserNotFound_ReturnsEmpty()
    {
        var result = await _service.GetPermissionsByUserIdAsync(9999);

        result.Should().BeEmpty();
    }

    // ─── Constructor null checks ──────────────────────────────────────────────

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new PermissionService(null!, _context);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("permissionRepository");
    }
}
