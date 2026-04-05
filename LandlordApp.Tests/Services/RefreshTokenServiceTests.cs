using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lander;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Implementation.UserImplementation;

namespace LandlordApp.Tests.Services;

public class RefreshTokenServiceTests : IDisposable
{
    private readonly UsersContext _context;
    private readonly RefreshTokenService _service;

    public RefreshTokenServiceTests()
    {
        var options = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new UsersContext(options);
        _service = new RefreshTokenService(_context);

        SeedUser();
    }

    private void SeedUser()
    {
        _context.Roles.Add(new Role { RoleId = 1, RoleName = "Tenant", Description = "Tenant", CreatedDate = DateTime.UtcNow });
        _context.Users.Add(new User
        {
            UserId = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "rt@test.com",
            Password = "hash",
            UserRoleId = 1,
            IsActive = true,
            UserGuid = Guid.NewGuid()
        });
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    // ─── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsNonEmptyToken()
    {
        var raw = await _service.CreateAsync(1);
        raw.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAsync_StoresHashedToken_NotPlaintext()
    {
        var raw = await _service.CreateAsync(1);
        var stored = await _context.RefreshTokens.FirstAsync(t => t.UserId == 1);

        stored.TokenHash.Should().NotBe(raw);
        stored.TokenHash.Should().Be(RefreshTokenService.HashToken(raw));
    }

    [Fact]
    public async Task CreateAsync_SetsExpiryTo30Days()
    {
        await _service.CreateAsync(1);
        var stored = await _context.RefreshTokens.FirstAsync(t => t.UserId == 1);

        stored.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), precision: TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task CreateAsync_RevokesExistingActiveTokens()
    {
        // First token
        var first = await _service.CreateAsync(1);

        // Second token should revoke the first
        await _service.CreateAsync(1);

        var firstHash = RefreshTokenService.HashToken(first);
        var firstStored = await _context.RefreshTokens.FirstAsync(t => t.TokenHash == firstHash);
        firstStored.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_TwoCallsProduceDifferentTokens()
    {
        var first = await _service.CreateAsync(1);
        var second = await _service.CreateAsync(1);

        first.Should().NotBe(second);
    }

    // ─── ValidateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ValidToken_ReturnsRefreshToken()
    {
        var raw = await _service.CreateAsync(1);

        var result = await _service.ValidateAsync(raw);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(1);
    }

    [Fact]
    public async Task ValidateAsync_NonExistentToken_ReturnsNull()
    {
        var result = await _service.ValidateAsync("totallyfaketoken");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_RevokedToken_ReturnsNull()
    {
        var raw = await _service.CreateAsync(1);
        await _service.RevokeAsync(raw);

        var result = await _service.ValidateAsync(raw);
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ExpiredToken_ReturnsNull()
    {
        var raw = await _service.CreateAsync(1);

        // Manually expire the token
        var hash = RefreshTokenService.HashToken(raw);
        var token = await _context.RefreshTokens.FirstAsync(t => t.TokenHash == hash);
        token.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
        await _context.SaveChangesAsync();

        var result = await _service.ValidateAsync(raw);
        result.Should().BeNull();
    }

    // ─── RevokeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAsync_ValidToken_SetsIsRevoked()
    {
        var raw = await _service.CreateAsync(1);
        await _service.RevokeAsync(raw);

        var hash = RefreshTokenService.HashToken(raw);
        var stored = await _context.RefreshTokens.FirstAsync(t => t.TokenHash == hash);
        stored.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAsync_NonExistentToken_DoesNotThrow()
    {
        var act = async () => await _service.RevokeAsync("nonexistent");
        await act.Should().NotThrowAsync();
    }

    // ─── HashToken ────────────────────────────────────────────────────────────

    [Fact]
    public void HashToken_SameInput_ProducesSameHash()
    {
        var hash1 = RefreshTokenService.HashToken("mytoken");
        var hash2 = RefreshTokenService.HashToken("mytoken");
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashToken_DifferentInputs_ProduceDifferentHashes()
    {
        var hash1 = RefreshTokenService.HashToken("token1");
        var hash2 = RefreshTokenService.HashToken("token2");
        hash1.Should().NotBe(hash2);
    }
}
