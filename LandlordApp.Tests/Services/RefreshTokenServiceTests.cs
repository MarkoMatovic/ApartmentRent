using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Lander;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Implementation.UserImplementation;

namespace LandlordApp.Tests.Services;

public class RefreshTokenServiceTests : IDisposable
{
    private readonly DbContextOptions<UsersContext> _options;
    private readonly UsersContext _context;
    private readonly RefreshTokenService _service;
    private readonly ManualClock _clock = new(new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));

    /// <summary>A clock the test moves by hand.</summary>
    private sealed class ManualClock(DateTimeOffset start) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = start;
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance(TimeSpan by) => Now += by;
    }

    public RefreshTokenServiceTests()
    {
        _options = new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new UsersContext(_options);
        _service = new RefreshTokenService(_context, _clock);

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

    private async Task<bool> WorksAsync(string raw) => await _service.ValidateAsync(raw) is not null;

    /// <summary>The row as the database has it now, not as this context last saw it.</summary>
    private RefreshToken Stored(string raw)
    {
        using var fresh = new UsersContext(_options);
        return fresh.RefreshTokens.AsNoTracking().Single(t => t.TokenHash == RefreshTokenService.HashToken(raw));
    }

    // ─── CreateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ReturnsNonEmptyToken()
    {
        var raw = await _service.CreateAsync(1);
        raw.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateAsync_IsA256BitRandomToken()
    {
        var raw = await _service.CreateAsync(1);

        raw.Should().MatchRegex("^[0-9a-f]{64}$");
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
    public async Task CreateAsync_SetsExpiryTo7Days()
    {
        await _service.CreateAsync(1);
        var stored = await _context.RefreshTokens.FirstAsync(t => t.UserId == 1);

        stored.ExpiresAt.Should().Be(_clock.Now.UtcDateTime.AddDays(7));
    }

    [Fact]
    public async Task CreateAsync_SigningInOnAnotherDevice_KeepsTheFirstOneSignedIn()
    {
        var laptop = await _service.CreateAsync(1);

        var phone = await _service.CreateAsync(1);

        (await WorksAsync(laptop)).Should().BeTrue("signing in on the phone used to sign the laptop out");
        (await WorksAsync(phone)).Should().BeTrue();
        Stored(laptop).FamilyId.Should().NotBe(Stored(phone).FamilyId, "each sign-in is its own session");
    }

    [Fact]
    public async Task CreateAsync_BeyondTheSessionLimit_EndsTheLeastRecentlyUsedSession()
    {
        var oldest = await _service.CreateAsync(1);
        var others = new List<string>();
        for (var i = 0; i < RefreshTokenService.MaxSessionsPerUser; i++)
        {
            _clock.Advance(TimeSpan.FromMinutes(1));
            others.Add(await _service.CreateAsync(1));
        }

        (await WorksAsync(oldest)).Should().BeFalse();
        foreach (var raw in others) (await WorksAsync(raw)).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_TwoCallsProduceDifferentTokens()
    {
        var first = await _service.CreateAsync(1);
        var second = await _service.CreateAsync(1);

        first.Should().NotBe(second);
    }

    // ─── RotateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RotateAsync_ExchangesTheTokenWithinTheSameSession()
    {
        var first = await _service.CreateAsync(1);

        var rotation = await _service.RotateAsync(first);

        rotation.Should().NotBeNull();
        rotation!.User.UserId.Should().Be(1);
        rotation.RawToken.Should().NotBe(first);
        (await WorksAsync(first)).Should().BeFalse();
        (await WorksAsync(rotation.RawToken)).Should().BeTrue();
        Stored(rotation.RawToken).FamilyId.Should().Be(Stored(first).FamilyId);
        Stored(first).RotatedAt.Should().Be(_clock.Now.UtcDateTime);
    }

    [Fact]
    public async Task RotateAsync_DoesNotTouchTheOtherDevices()
    {
        var laptop = await _service.CreateAsync(1);
        var phone = await _service.CreateAsync(1);

        await _service.RotateAsync(phone);

        (await WorksAsync(laptop)).Should().BeTrue();
    }

    [Fact]
    public async Task RotateAsync_TwoTabsWithTheSameCookie_BothStaySignedIn()
    {
        var cookie = await _service.CreateAsync(1);
        var firstTab = await _service.RotateAsync(cookie);
        _clock.Advance(TimeSpan.FromSeconds(2));

        var secondTab = await _service.RotateAsync(cookie);

        secondTab.Should().NotBeNull("the second tab sent the cookie before the first answer replaced it");
        (await WorksAsync(firstTab!.RawToken)).Should().BeTrue("whichever answer lands last ends up in the cookie");
        (await WorksAsync(secondTab!.RawToken)).Should().BeTrue();
    }

    [Fact]
    public async Task RotateAsync_TheNextExchangeEndsTheTokensOfEarlierParallelRefreshes()
    {
        var cookie = await _service.CreateAsync(1);
        var firstTab = await _service.RotateAsync(cookie);
        var secondTab = await _service.RotateAsync(cookie);
        _clock.Advance(TimeSpan.FromMinutes(15));

        var next = await _service.RotateAsync(secondTab!.RawToken);

        next.Should().NotBeNull();
        (await WorksAsync(firstTab!.RawToken)).Should().BeFalse("only the token in the cookie is still in use");
        (await WorksAsync(next!.RawToken)).Should().BeTrue();
    }

    [Fact]
    public async Task RotateAsync_ExchangedTokenUsedAgainLater_EndsThatSession()
    {
        var stolen = await _service.CreateAsync(1);
        var otherDevice = await _service.CreateAsync(1);
        var legit = await _service.RotateAsync(stolen);
        _clock.Advance(RefreshTokenService.ReuseGrace + TimeSpan.FromSeconds(1));

        var attacker = await _service.RotateAsync(stolen);

        attacker.Should().BeNull();
        (await WorksAsync(legit!.RawToken)).Should().BeFalse("a copied token ends the session it was copied from");
        (await WorksAsync(otherDevice)).Should().BeTrue("other devices are a different session");
    }

    [Fact]
    public async Task RotateAsync_AfterTheSessionWasEnded_DoesNotReviveIt_EvenWithinTheGracePeriod()
    {
        var cookie = await _service.CreateAsync(1);
        var rotated = await _service.RotateAsync(cookie);
        await _service.RevokeAsync(rotated!.RawToken); // signed out on this device

        (await _service.RotateAsync(cookie)).Should().BeNull();
    }

    [Fact]
    public async Task RotateAsync_RevokedUnknownOrExpiredToken_ReturnsNull()
    {
        var revoked = await _service.CreateAsync(1);
        await _service.RevokeAsync(revoked);
        var expired = await _service.CreateAsync(1);
        _clock.Advance(RefreshTokenService.Lifetime + TimeSpan.FromSeconds(1));

        (await _service.RotateAsync(revoked)).Should().BeNull();
        (await _service.RotateAsync(expired)).Should().BeNull();
        (await _service.RotateAsync("0123456789abcdef")).Should().BeNull();
    }

    [Fact]
    public async Task RotateAsync_ExchangedByAnotherRequestAtTheSameMoment_TreatsItAsAParallelRefresh()
    {
        var cookie = await _service.CreateAsync(1);
        using var otherContext = new UsersContext(_options);
        var otherRequest = new RefreshTokenService(otherContext, _clock);
        // The other request has read the token before this one exchanges it.
        await otherContext.RefreshTokens.SingleAsync(t => t.TokenHash == RefreshTokenService.HashToken(cookie));
        var mine = await _service.RotateAsync(cookie);

        var theirs = await otherRequest.RotateAsync(cookie);

        theirs.Should().NotBeNull();
        (await WorksAsync(mine!.RawToken)).Should().BeTrue("the exchange that came second must not end the first one's token");
        (await WorksAsync(theirs!.RawToken)).Should().BeTrue();
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

        _clock.Advance(RefreshTokenService.Lifetime + TimeSpan.FromSeconds(1));

        var result = await _service.ValidateAsync(raw);
        result.Should().BeNull();
    }

    // ─── RevokeAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RevokeAsync_ValidToken_SetsIsRevoked()
    {
        var raw = await _service.CreateAsync(1);
        await _service.RevokeAsync(raw);

        Stored(raw).IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAsync_EndsTheWholeSessionOfThisDeviceOnly()
    {
        var laptop = await _service.CreateAsync(1);
        var phone = await _service.CreateAsync(1);
        var phoneNow = (await _service.RotateAsync(phone))!.RawToken;

        await _service.RevokeAsync(phone); // the cookie the phone had before its last refresh

        (await WorksAsync(phoneNow)).Should().BeFalse();
        (await WorksAsync(laptop)).Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAllByUserIdAsync_EndsEverySession()
    {
        var laptop = await _service.CreateAsync(1);
        var phone = await _service.CreateAsync(1);

        await _service.RevokeAllByUserIdAsync(1);

        (await WorksAsync(laptop)).Should().BeFalse();
        (await WorksAsync(phone)).Should().BeFalse();
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
