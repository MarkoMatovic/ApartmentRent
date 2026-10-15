using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lander;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using LandlordApp.Tests.E2eTests.Infrastructure;
using LandlordApp.Tests.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LandlordApp.Tests.E2eTests.Tests;

/// <summary>
/// Two-step sign-in end to end: the code step of signing in, turning it on over the API, and admin
/// powers that need a session confirmed with a code.
/// </summary>
[Collection(E2eCollection.Name)]
public class TwoFactorE2eTests : E2eTestBase
{
    private const string Password = "Pass123!";
    private const string AdminOnly = "/api/v1/analytics/summary";

    public TwoFactorE2eTests(E2eFixture fixture) : base(fixture) { }

    private HttpClient NewDevice() => Fixture.Factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies     = true,
        BaseAddress       = new Uri("https://localhost"),
    });

    private static void UseToken(HttpClient client, string accessToken) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static string CurrentCode(string base32Secret, int stepOffset = 0) =>
        Totp.Compute(Totp.FromBase32(base32Secret), Totp.StepAt(DateTimeOffset.UtcNow) + stepOffset);

    /// <summary>Turns two-step sign-in on the way the profile page does; returns the secret.</summary>
    private async Task<string> TurnOnAsync(int userId)
    {
        using var scope = Fixture.Factory.Services.CreateScope();
        var twoFactor = scope.ServiceProvider.GetRequiredService<ITwoFactorService>();
        var setup = await twoFactor.BeginSetupAsync(userId);
        // One step back: the sign-in in the test then uses the current code, which was not used yet.
        await twoFactor.EnableAsync(userId, CurrentCode(setup.Secret, stepOffset: -1));
        return setup.Secret;
    }

    private static async Task<string> PasswordStepAsync(HttpClient device, string email)
    {
        var response = await device.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await JsonAsync(response);
        body.GetProperty("requiresTwoFactor").GetBoolean().Should().BeTrue();
        body.GetProperty("accessToken").GetString().Should().BeEmpty();
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        (cookies ?? []).Should().NotContain(c => c.StartsWith("refreshToken="), "no session before the code");
        return body.GetProperty("twoFactorToken").GetString()!;
    }

    private static Task<HttpResponseMessage> CodeStepAsync(HttpClient device, string challenge, string code) =>
        device.PostAsJsonAsync("/api/v1/auth/two-factor/login", new { TwoFactorToken = challenge, Code = code });

    [Fact]
    public async Task Admin_SignsInWithPasswordAndCode_AndHasAdminPowers_AlsoAfterARefresh()
    {
        var admin  = await Data.CreateUserAsync("2fa-admin@e2e.com", Password, role: "Admin");
        var secret = await TurnOnAsync(admin.UserId);
        var device = NewDevice();

        var challenge = await PasswordStepAsync(device, "2fa-admin@e2e.com");
        var signIn = await CodeStepAsync(device, challenge, CurrentCode(secret));

        signIn.StatusCode.Should().Be(HttpStatusCode.OK);
        UseToken(device, (await JsonAsync(signIn)).GetProperty("accessToken").GetString()!);
        (await device.GetAsync(AdminOnly)).StatusCode.Should().Be(HttpStatusCode.OK);

        var refresh = await device.PostAsync("/api/v1/auth/token/refresh", null);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        UseToken(device, (await JsonAsync(refresh)).GetProperty("accessToken").GetString()!);
        (await device.GetAsync(AdminOnly)).StatusCode.Should().Be(HttpStatusCode.OK, "the session stays confirmed");
    }

    [Fact]
    public async Task Admin_WithoutTwoStepSignIn_IsARegularAccount_AndTheProfileSaysWhy()
    {
        var admin  = await Data.CreateUserAsync("2fa-none@e2e.com", Password, role: "Admin");
        var device = await LoginAsync("2fa-none@e2e.com", Password);

        (await device.GetAsync(AdminOnly)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var profile = await device.GetFromJsonAsync<JsonElement>($"/api/v1/auth/profile/{admin.UserId}");
        profile.GetProperty("twoFactorRequired").GetBoolean().Should().BeTrue();
        profile.GetProperty("twoFactorEnabled").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task AdminToken_WithoutTheSecondFactor_IsRefusedByAdminEndpoints()
    {
        var admin  = await Data.CreateUserAsync("2fa-token@e2e.com", Password, role: "Admin");
        var client = Fixture.Factory.CreateClient();
        UseToken(client, TestJwtGenerator.Generate(admin.UserId, admin.UserGuid, "Admin", twoFactorVerified: false));

        (await client.GetAsync(AdminOnly)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TurningItOnOverTheApi_ConfirmsThisSessionAtOnce()
    {
        await Data.CreateUserAsync("2fa-setup@e2e.com", Password, role: "Admin");
        var device = await LoginAsync("2fa-setup@e2e.com", Password);
        (await device.GetAsync(AdminOnly)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var setup = await JsonAsync(await device.PostAsync("/api/v1/auth/two-factor/setup", null));
        var secret = setup.GetProperty("secret").GetString()!;
        setup.GetProperty("qrCodeDataUrl").GetString().Should().StartWith("data:image/png;base64,");
        var enable = await device.PostAsJsonAsync("/api/v1/auth/two-factor/enable", new { Code = CurrentCode(secret) });

        enable.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await JsonAsync(enable);
        body.GetProperty("recoveryCodes").GetArrayLength().Should().Be(TwoFactorService.RecoveryCodeCount);
        UseToken(device, body.GetProperty("accessToken").GetString()!);
        (await device.GetAsync(AdminOnly)).StatusCode.Should().Be(HttpStatusCode.OK);

        var refresh = await device.PostAsync("/api/v1/auth/token/refresh", null);
        UseToken(device, (await JsonAsync(refresh)).GetProperty("accessToken").GetString()!);
        (await device.GetAsync(AdminOnly)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CodeStep_WrongCode_Returns422WithCode_AndFiveWrongCodesLockTheAccount()
    {
        var user   = await Data.CreateUserAsync("2fa-lock@e2e.com", Password);
        var secret = await TurnOnAsync(user.UserId);
        var device = NewDevice();
        var challenge = await PasswordStepAsync(device, "2fa-lock@e2e.com");

        var wrong = await CodeStepAsync(device, challenge, "000000");
        wrong.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await JsonAsync(wrong)).GetProperty("code").GetString().Should().Be("INVALID_TWO_FACTOR_CODE");
        for (var i = 0; i < 4; i++) await CodeStepAsync(device, challenge, "000000");

        var right = await CodeStepAsync(device, challenge, CurrentCode(secret));

        right.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity, "the account is locked for a while");
        using var scope = Fixture.Factory.Services.CreateScope();
        var stored = await scope.ServiceProvider.GetRequiredService<UsersContext>().Users.AsNoTracking()
            .SingleAsync(u => u.UserId == user.UserId);
        stored.LockoutUntil.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task CodeStep_ATamperedChallenge_AsksToSignInAgain()
    {
        var response = await CodeStepAsync(NewDevice(), "not-a-challenge", "123456");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await JsonAsync(response)).GetProperty("code").GetString().Should().Be("TWO_FACTOR_EXPIRED");
    }

    [Fact]
    public async Task SignIn_WithARecoveryCode_Works()
    {
        var user = await Data.CreateUserAsync("2fa-recovery@e2e.com", Password);
        IReadOnlyList<string> recoveryCodes;
        using (var scope = Fixture.Factory.Services.CreateScope())
        {
            var twoFactor = scope.ServiceProvider.GetRequiredService<ITwoFactorService>();
            var setup = await twoFactor.BeginSetupAsync(user.UserId);
            recoveryCodes = await twoFactor.EnableAsync(user.UserId, CurrentCode(setup.Secret));
        }
        var device = NewDevice();

        var challenge = await PasswordStepAsync(device, "2fa-recovery@e2e.com");
        var signIn = await CodeStepAsync(device, challenge, recoveryCodes[0]);

        signIn.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TurningItOff_TakesTheAdminPowersAway()
    {
        var admin  = await Data.CreateUserAsync("2fa-off@e2e.com", Password, role: "Admin");
        var secret = await TurnOnAsync(admin.UserId);
        var device = NewDevice();
        var challenge = await PasswordStepAsync(device, "2fa-off@e2e.com");
        UseToken(device, (await JsonAsync(await CodeStepAsync(device, challenge, CurrentCode(secret)))).GetProperty("accessToken").GetString()!);

        var off = await device.PostAsJsonAsync("/api/v1/auth/two-factor/disable", new { Code = CurrentCode(secret, stepOffset: 1) });

        off.StatusCode.Should().Be(HttpStatusCode.OK);
        UseToken(device, (await JsonAsync(off)).GetProperty("accessToken").GetString()!);
        (await device.GetAsync(AdminOnly)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var refresh = await device.PostAsync("/api/v1/auth/token/refresh", null);
        UseToken(device, (await JsonAsync(refresh)).GetProperty("accessToken").GetString()!);
        (await device.GetAsync(AdminOnly)).StatusCode.Should().Be(HttpStatusCode.Forbidden, "the session is no longer confirmed");
    }

    [Fact]
    public async Task Setup_WithoutAuth_Returns401()
    {
        (await HttpClient.PostAsync("/api/v1/auth/two-factor/setup", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
