using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Lander;
using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LandlordApp.Tests.Services;

public class TotpTests
{
    // RFC 6238, appendix B: SHA-1 with the ASCII secret "12345678901234567890", 8 digits.
    private static readonly byte[] RfcSecret = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(59L, "94287082")]
    [InlineData(1111111109L, "07081804")]
    [InlineData(1111111111L, "14050471")]
    [InlineData(1234567890L, "89005924")]
    [InlineData(2000000000L, "69279037")]
    [InlineData(20000000000L, "65353130")]
    public void Compute_MatchesTheRfc6238TestVectors(long unixSeconds, string expected)
    {
        var step = Totp.StepAt(DateTimeOffset.FromUnixTimeSeconds(unixSeconds));

        Totp.Compute(RfcSecret, step, digits: 8).Should().Be(expected);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("f", "MY")]
    [InlineData("fo", "MZXQ")]
    [InlineData("foo", "MZXW6")]
    [InlineData("foob", "MZXW6YQ")]
    [InlineData("fooba", "MZXW6YTB")]
    [InlineData("foobar", "MZXW6YTBOI")]
    public void Base32_MatchesTheRfc4648TestVectors(string text, string base32)
    {
        Totp.ToBase32(Encoding.ASCII.GetBytes(text)).Should().Be(base32);
        Encoding.ASCII.GetString(Totp.FromBase32(base32)).Should().Be(text);
    }

    [Fact]
    public void FromBase32_AcceptsWhatPeopleType_LowercaseSpacesAndPadding()
    {
        Encoding.ASCII.GetString(Totp.FromBase32(" mzxw 6ytb oi====")).Should().Be("foobar");
    }

    [Fact]
    public void Match_AcceptsOneStepOfClockDifference_ButNotTwo()
    {
        var secret = Totp.NewSecret();
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var step = Totp.StepAt(now);

        Totp.Match(secret, Totp.Compute(secret, step), now).Should().Be(step);
        Totp.Match(secret, Totp.Compute(secret, step - 1), now).Should().Be(step - 1);
        Totp.Match(secret, Totp.Compute(secret, step + 1), now).Should().Be(step + 1);
        Totp.Match(secret, Totp.Compute(secret, step - 2), now).Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("abcdef")]
    public void Match_RefusesWhatIsNotSixDigits(string code)
    {
        Totp.Match(Totp.NewSecret(), code, DateTimeOffset.UtcNow).Should().BeNull();
    }

    [Fact]
    public void Match_IgnoresTheSpaceAppsShowInTheMiddle()
    {
        var secret = Totp.NewSecret();
        var now = DateTimeOffset.UtcNow;
        var code = Totp.Compute(secret, Totp.StepAt(now));

        Totp.Match(secret, $"{code[..3]} {code[3..]}", now).Should().NotBeNull();
    }

    [Fact]
    public void SetupUri_IsTheFormatAuthenticatorAppsRead()
    {
        var uri = Totp.SetupUri("TuRentaj", "ana@test.com", Encoding.ASCII.GetBytes("foobar"));

        uri.Should().Be("otpauth://totp/TuRentaj:ana%40test.com?secret=MZXW6YTBOI&issuer=TuRentaj&algorithm=SHA1&digits=6&period=30");
    }
}

public class TwoFactorServiceTests : IDisposable
{
    private readonly UsersContext _context;
    private readonly ManualClock _clock = new(new DateTimeOffset(2026, 10, 14, 12, 0, 0, TimeSpan.Zero));
    private readonly TwoFactorService _service;
    private readonly User _user;

    private sealed class ManualClock(DateTimeOffset start) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = start;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    public TwoFactorServiceTests()
    {
        _context = new UsersContext(new DbContextOptionsBuilder<UsersContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        _service = new TwoFactorService(_context, new EphemeralDataProtectionProvider(), _clock, NullLogger<TwoFactorService>.Instance);
        _user = new User
        {
            UserId = 1, UserGuid = Guid.NewGuid(), FirstName = "Ana", LastName = "Admin",
            Email = "ana@test.com", Password = "x", IsActive = true,
        };
        _context.Users.Add(_user);
        _context.SaveChanges();
    }

    public void Dispose() => _context.Dispose();

    private string CodeNow(string base32Secret, int stepOffset = 0) =>
        Totp.Compute(Totp.FromBase32(base32Secret), Totp.StepAt(_clock.Now) + stepOffset);

    private async Task<(TwoFactorSetup Setup, IReadOnlyList<string> RecoveryCodes)> EnableAsync()
    {
        var setup = await _service.BeginSetupAsync(1);
        var codes = await _service.EnableAsync(1, CodeNow(setup.Secret));
        _clock.Now += Totp.Step; // the next sign-in happens later, with the next code
        return (setup, codes);
    }

    [Fact]
    public async Task BeginSetupAsync_GivesTheKeyTheQrCodeAndTheSetupLink_WithoutTurningItOn()
    {
        var setup = await _service.BeginSetupAsync(1);

        setup.Secret.Should().MatchRegex("^[A-Z2-7]{32}$", "160 bits in Base32");
        setup.SetupUri.Should().StartWith("otpauth://totp/TuRentaj:ana%40test.com?secret=" + setup.Secret);
        setup.QrCodeDataUrl.Should().StartWith("data:image/png;base64,");
        _user.TwoFactorEnabled.Should().BeFalse();
        _user.TwoFactorSecret.Should().NotBeNull().And.NotContain(setup.Secret, "the secret is stored encrypted");
    }

    [Fact]
    public async Task EnableAsync_WithTheAppsCode_TurnsItOnAndGivesTenRecoveryCodes()
    {
        var setup = await _service.BeginSetupAsync(1);

        var codes = await _service.EnableAsync(1, CodeNow(setup.Secret));

        _user.TwoFactorEnabled.Should().BeTrue();
        codes.Should().HaveCount(TwoFactorService.RecoveryCodeCount).And.OnlyHaveUniqueItems();
        codes.Should().AllSatisfy(c => c.Should().MatchRegex("^[a-z2-9]{5}-[a-z2-9]{5}$"));
        _user.TwoFactorRecoveryCodes.Should().NotContain(codes[0], "only hashes are kept");
    }

    [Fact]
    public async Task EnableAsync_WrongCode_LeavesItOff()
    {
        await _service.BeginSetupAsync(1);

        var act = () => _service.EnableAsync(1, "000000");

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.InvalidTwoFactorCode);
        _user.TwoFactorEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task EnableAsync_WithoutSetup_AsksForTheSetupFirst()
    {
        var act = () => _service.EnableAsync(1, "123456");

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.TwoFactorSetupRequired);
    }

    [Fact]
    public async Task BeginSetupAsync_WhenAlreadyOn_IsRefused_SoTheWorkingSecretIsNotReplaced()
    {
        await EnableAsync();

        var act = () => _service.BeginSetupAsync(1);

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.TwoFactorAlreadyEnabled);
    }

    [Fact]
    public async Task VerifyAsync_AcceptsTheCurrentCode_ButNeverTheSameCodeTwice()
    {
        var (setup, _) = await EnableAsync();
        var code = CodeNow(setup.Secret);

        (await _service.VerifyAsync(_user, code)).Should().BeTrue();
        (await _service.VerifyAsync(_user, code)).Should().BeFalse("a code seen over someone's shoulder must not work again");
    }

    [Fact]
    public async Task VerifyAsync_RefusesAnOlderCodeOnceANewerOneWasUsed()
    {
        var (setup, _) = await EnableAsync();
        (await _service.VerifyAsync(_user, CodeNow(setup.Secret))).Should().BeTrue();

        (await _service.VerifyAsync(_user, CodeNow(setup.Secret, stepOffset: -1))).Should().BeFalse();
    }

    [Fact]
    public async Task VerifyAsync_RecoveryCode_WorksOnceInAnyFormat()
    {
        var (_, recoveryCodes) = await EnableAsync();
        var code = recoveryCodes[3];

        (await _service.VerifyAsync(_user, $" {code.ToUpperInvariant().Replace("-", "")} ")).Should().BeTrue();
        (await _service.VerifyAsync(_user, code)).Should().BeFalse("a recovery code is used up");
        (await _service.VerifyAsync(_user, recoveryCodes[4])).Should().BeTrue("the others still work");
    }

    [Fact]
    public async Task VerifyAsync_RecoveryCodesOfAnotherAccountDoNotWork()
    {
        var (_, recoveryCodes) = await EnableAsync();
        var other = new User
        {
            UserId = 2, UserGuid = Guid.NewGuid(), FirstName = "B", LastName = "B", Email = "b@test.com",
            Password = "x", IsActive = true, TwoFactorEnabled = true, TwoFactorRecoveryCodes = _user.TwoFactorRecoveryCodes,
        };

        (await _service.VerifyAsync(other, recoveryCodes[0])).Should().BeFalse("the hashes are salted per account");
    }

    [Fact]
    public async Task VerifyAsync_WhenItIsOff_AcceptsNothing()
    {
        var setup = await _service.BeginSetupAsync(1);

        (await _service.VerifyAsync(_user, CodeNow(setup.Secret))).Should().BeFalse();
    }

    [Fact]
    public async Task DisableAsync_NeedsACode_AndClearsEverything()
    {
        var (setup, _) = await EnableAsync();

        var wrong = () => _service.DisableAsync(1, "000000");
        (await wrong.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.InvalidTwoFactorCode);
        _user.TwoFactorEnabled.Should().BeTrue();

        await _service.DisableAsync(1, CodeNow(setup.Secret));

        _user.TwoFactorEnabled.Should().BeFalse();
        _user.TwoFactorSecret.Should().BeNull();
        _user.TwoFactorRecoveryCodes.Should().BeNull();
    }

    [Fact]
    public async Task DisableAsync_WhenItIsOff_SaysSo()
    {
        var act = () => _service.DisableAsync(1, "123456");

        (await act.Should().ThrowAsync<BusinessRuleException>()).Which.Code.Should().Be(BusinessErrorCodes.TwoFactorNotEnabled);
    }

    [Fact]
    public void LoginChallenge_CarriesTheUser_AndAnythingElseIsRefused()
    {
        var challenge = _service.CreateLoginChallenge(42);

        _service.ReadLoginChallenge(challenge).Should().Be(42);
        _service.ReadLoginChallenge(challenge[..^4] + "AAAA").Should().BeNull();
        _service.ReadLoginChallenge("not-a-challenge").Should().BeNull();
        new TwoFactorService(_context, new EphemeralDataProtectionProvider(), _clock, NullLogger<TwoFactorService>.Instance)
            .ReadLoginChallenge(challenge).Should().BeNull("another key ring did not make it");
    }
}

public class TwoFactorPolicyTests
{
    private static ClaimsIdentity Identity(string role, bool confirmed)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role), new("userId", "1") };
        if (confirmed) claims.Add(new Claim("amr", "mfa"));
        return new ClaimsIdentity(claims, "Bearer");
    }

    [Fact]
    public void AnAdminSessionWithoutTheSecondFactor_LosesTheAdminRole()
    {
        var identity = Identity("Admin", confirmed: false);

        TwoFactorPolicy.RemoveUnconfirmedAdminRole(identity);

        new ClaimsPrincipal(identity).IsInRole("Admin").Should().BeFalse();
        identity.IsAuthenticated.Should().BeTrue("the account still works as a regular one");
    }

    [Fact]
    public void AConfirmedAdminSession_KeepsIt()
    {
        var identity = Identity("Admin", confirmed: true);

        TwoFactorPolicy.RemoveUnconfirmedAdminRole(identity);

        new ClaimsPrincipal(identity).IsInRole("Admin").Should().BeTrue();
    }

    [Fact]
    public void OtherRoles_DoNotNeedIt()
    {
        var identity = Identity("Tenant", confirmed: false);

        TwoFactorPolicy.RemoveUnconfirmedAdminRole(identity);

        new ClaimsPrincipal(identity).IsInRole("Tenant").Should().BeTrue();
    }
}
