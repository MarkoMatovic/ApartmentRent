using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lander;
using LandlordApp.Tests.E2eTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LandlordApp.Tests.E2eTests.Tests.Billing;

/// <summary>Tenancies end to end: who can be a tenant, the first rent bill, settings, tenants coming and going, ending.</summary>
[Collection(E2eCollection.Name)]
public class TenancyE2eTests : BillingE2eBase
{
    public TenancyE2eTests(E2eFixture fixture) : base(fixture) { }

    // ── who can be added ──────────────────────────────────────────────────────

    [Fact]
    public async Task Candidates_AreApprovedApplicantsAndTheirRoommates_NobodyElse()
    {
        var s = await SetupAsync("cand");
        var pending = await PersonAsync("cand-pending@e2e.com", "Pera", "Perić");
        await Scope.ServiceProvider.GetRequiredService<ApplicationsContext>().ApartmentApplications.AddAsync(
            new Lander.src.Modules.ApartmentApplications.Models.ApartmentApplication
            {
                UserId = pending.Id, ApartmentId = s.ApartmentId, Status = "Pending", ApplicationDate = DateTime.UtcNow,
            });
        await Scope.ServiceProvider.GetRequiredService<ApplicationsContext>().SaveChangesAsync();

        var candidates = await OkAsync(await s.Landlord.Client.GetAsync($"/api/v1/tenancies/candidates?apartmentId={s.ApartmentId}"));

        var byId = candidates.EnumerateArray().ToDictionary(c => c.GetProperty("userId").GetInt32(), c => c.GetProperty("reason").GetString());
        byId.Should().BeEquivalentTo(new Dictionary<int, string?> { [s.Ana.Id] = "application", [s.Marko.Id] = "roommate" });
    }

    [Fact]
    public async Task Candidates_OfSomeoneElsesApartment_Returns403()
    {
        var s = await SetupAsync("cand403");

        var response = await s.Stranger.Client.GetAsync($"/api/v1/tenancies/candidates?apartmentId={s.ApartmentId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Candidates_LeaveOutWhoIsAlreadyATenant()
    {
        var s = await SetupAsync("candin");
        await CreateTenancyAsync(s, tenants: s.Ana.Id);

        var candidates = await OkAsync(await s.Landlord.Client.GetAsync($"/api/v1/tenancies/candidates?apartmentId={s.ApartmentId}"));

        candidates.EnumerateArray().Select(c => c.GetProperty("userId").GetInt32()).Should().Equal(s.Marko.Id);
    }

    // ── creating ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ByTheLandlord_ShowsItToEveryoneInIt_AndTellsTheTenants()
    {
        var s = await SetupAsync("create");

        var tenancy = await CreateTenancyAsync(s);

        tenancy.GetProperty("isLandlord").GetBoolean().Should().BeTrue();
        tenancy.GetProperty("apartmentTitle").GetString().Should().Be("Stan na Vračaru");
        tenancy.GetProperty("status").GetString().Should().Be("active");
        tenancy.GetProperty("payeeAccount").GetString().Should().Be(LandlordAccount);
        tenancy.GetProperty("members").EnumerateArray().Select(m => m.GetProperty("name").GetString())
            .Should().BeEquivalentTo("Ana Anić", "Marko Marković");

        var anasView = (await OkAsync(await s.Ana.Client.GetAsync("/api/v1/tenancies"))).EnumerateArray().Single();
        anasView.GetProperty("isLandlord").GetBoolean().Should().BeFalse();
        anasView.GetProperty("landlord").GetProperty("name").GetString().Should().Be("Lazar Lazić");
        (await NotificationKeysAsync(s.Ana.Id)).Should().Contain("tenancy_added");
        (await NotificationKeysAsync(s.Marko.Id)).Should().Contain("tenancy_added");
        (await OkAsync(await s.Stranger.Client.GetAsync("/api/v1/tenancies"))).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Create_WhenTheRentIsDueSoon_MakesThisMonthsRentBill_SplitToThePara()
    {
        var s = await SetupAsync("rent");

        var tenancy = await CreateTenancyAsync(s, rent: 30001m);

        var bill = (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Should().ContainSingle().Subject;
        bill.GetProperty("kind").GetString().Should().Be("rent");
        bill.GetProperty("period").GetString().Should().Be(Period(DueDateSoon));
        bill.GetProperty("dueDate").GetString().Should().Be(DueDateSoon.ToString("yyyy-MM-dd"));
        bill.GetProperty("amount").GetDecimal().Should().Be(30001m);
        bill.GetProperty("payTo").GetString().Should().Be("landlord");
        var shares = bill.GetProperty("shares").EnumerateArray().ToList();
        shares.Select(x => x.GetProperty("amount").GetDecimal()).Should().BeEquivalentTo(new[] { 15000.50m, 15000.50m });
        shares.Should().OnlyContain(x => x.GetProperty("receiver").GetProperty("userId").GetInt32() == s.Landlord.Id);
        (await NotificationKeysAsync(s.Ana.Id)).Should().Contain("bill_new");
    }

    [Fact]
    public async Task Create_WhenTheRentIsDueLater_MakesNoBillYet()
    {
        var s = await SetupAsync("rentfar");

        var tenancy = await CreateTenancyAsync(s, dueDay: DueDayFar);

        (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ForSomeoneElsesApartment_Returns403()
    {
        var s = await SetupAsync("notmine");

        var response = await s.Stranger.Client.PostAsJsonAsync("/api/v1/tenancies", TenancyInput(s, s.Ana.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithSomeoneWithoutAnApprovedApplication_IsRefused()
    {
        var s = await SetupAsync("noteligible");

        var response = await s.Landlord.Client.PostAsJsonAsync("/api/v1/tenancies", TenancyInput(s, s.Stranger.Id));

        (await BusinessCodeAsync(response)).Should().Be("TENANT_NOT_ELIGIBLE");
    }

    [Fact]
    public async Task Create_WithThemselvesAsTenant_IsRefused()
    {
        var s = await SetupAsync("self");

        var response = await s.Landlord.Client.PostAsJsonAsync("/api/v1/tenancies", TenancyInput(s, s.Landlord.Id));

        (await BusinessCodeAsync(response)).Should().Be("LANDLORD_CANNOT_BE_TENANT");
    }

    [Fact]
    public async Task Create_ASecondActiveTenancyForTheApartment_IsRefused()
    {
        var s = await SetupAsync("twice");
        await CreateTenancyAsync(s, tenants: s.Ana.Id);

        var response = await s.Landlord.Client.PostAsJsonAsync("/api/v1/tenancies", TenancyInput(s, s.Marko.Id));

        (await BusinessCodeAsync(response)).Should().Be("TENANCY_ALREADY_ACTIVE");
    }

    [Fact]
    public async Task Create_AgainAfterTheFirstOneEnded_Works()
    {
        var s = await SetupAsync("again");
        var first = await CreateTenancyAsync(s, tenants: s.Ana.Id);
        await OkAsync(await PostAsync(s.Landlord.Client, $"/api/v1/tenancies/{IdOf(first)}/end"));

        var second = await CreateTenancyAsync(s, tenants: s.Marko.Id);

        IdOf(second).Should().NotBe(IdOf(first));
    }

    [Theory]
    [InlineData("160-123456-00", "BANK_ACCOUNT_INVALID")]   // wrong control digits
    [InlineData("1234", "BANK_ACCOUNT_INVALID")]
    [InlineData("", "BANK_ACCOUNT_INVALID")]
    public async Task Create_WithAnInvalidAccount_Returns400WithTheCode(string account, string code)
    {
        var s = await SetupAsync("badacc" + account.Length);

        var response = await s.Landlord.Client.PostAsJsonAsync("/api/v1/tenancies", new
        {
            apartmentId = s.ApartmentId, tenantUserIds = new[] { s.Ana.Id }, monthlyRent = 30000m, rentDueDay = 5,
            payeeName = "Lazar", payeeAccount = account, startDate = Today.ToString("yyyy-MM-dd"),
        });

        (await ValidationCodesAsync(response)).Should().Contain(code);
    }

    [Fact]
    public async Task Create_WithoutTenantsWithAnInvalidDayAndNoRent_ReturnsEveryCode()
    {
        var s = await SetupAsync("badinput");

        var response = await s.Landlord.Client.PostAsJsonAsync("/api/v1/tenancies", new
        {
            apartmentId = s.ApartmentId, tenantUserIds = Array.Empty<int>(), monthlyRent = 0m, rentDueDay = 31,
            payeeName = "", payeeAccount = LandlordAccount, startDate = Today.ToString("yyyy-MM-dd"),
        });

        (await ValidationCodesAsync(response)).Should().Contain(new[]
        {
            "TENANTS_REQUIRED", "AMOUNT_INVALID", "DUE_DAY_INVALID", "PAYEE_NAME_REQUIRED",
        });
    }

    [Fact]
    public async Task Create_AcceptsTheAccountWithSpacesOrAsEighteenDigits()
    {
        var s = await SetupAsync("accfmt");

        var response = await s.Landlord.Client.PostAsJsonAsync("/api/v1/tenancies", new
        {
            apartmentId = s.ApartmentId, tenantUserIds = new[] { s.Ana.Id }, monthlyRent = 30000m, rentDueDay = DueDayFar,
            payeeName = "Lazar Lazić", payeeAccount = Digits(LandlordAccount), startDate = Today.ToString("yyyy-MM-dd"),
        });

        (await OkAsync(response)).GetProperty("payeeAccount").GetString().Should().Be(LandlordAccount);
    }

    // ── reading ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_BySomeoneOutsideTheTenancy_Returns403()
    {
        var s = await SetupAsync("get403");
        var tenancy = await CreateTenancyAsync(s);

        (await s.Stranger.Client.GetAsync($"/api/v1/tenancies/{IdOf(tenancy)}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await s.Stranger.Client.GetAsync($"/api/v1/tenancies/{IdOf(tenancy)}/bills")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_AnUnknownTenancy_Returns404()
    {
        var s = await SetupAsync("get404");

        (await s.Landlord.Client.GetAsync("/api/v1/tenancies/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Endpoints_WithoutSigningIn_Return401()
    {
        (await HttpClient.GetAsync("/api/v1/tenancies")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await HttpClient.GetAsync("/api/v1/bills/mine")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await HttpClient.PostAsJsonAsync("/api/v1/bills", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── settings ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ByTheLandlord_ChangesTheNextBills_NotTheOnesAlreadyMade()
    {
        var s = await SetupAsync("update");
        var tenancy = await CreateTenancyAsync(s, rent: 30000m);
        var newAccount = Account("205", "99");

        var updated = await OkAsync(await s.Landlord.Client.PutAsJsonAsync($"/api/v1/tenancies/{IdOf(tenancy)}", new
        {
            monthlyRent = 32000m, rentDueDay = 10, payeeName = "Lazar L.", payeeAccount = newAccount,
        }));

        updated.GetProperty("monthlyRent").GetDecimal().Should().Be(32000m);
        updated.GetProperty("rentDueDay").GetInt32().Should().Be(10);
        updated.GetProperty("payeeAccount").GetString().Should().Be(newAccount);
        (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Single().GetProperty("amount").GetDecimal().Should().Be(30000m);
    }

    [Fact]
    public async Task Update_ByATenant_Returns403()
    {
        var s = await SetupAsync("update403");
        var tenancy = await CreateTenancyAsync(s);

        var response = await s.Ana.Client.PutAsJsonAsync($"/api/v1/tenancies/{IdOf(tenancy)}", new
        {
            monthlyRent = 1m, rentDueDay = 10, payeeName = "Ana", payeeAccount = PayoutAccount,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── tenants coming and going ──────────────────────────────────────────────

    [Fact]
    public async Task AddTenant_TheRoommateOfTheApplicant_JoinsAndIsTold()
    {
        var s = await SetupAsync("add");
        var tenancy = await CreateTenancyAsync(s, tenants: s.Ana.Id);

        var updated = await OkAsync(await s.Landlord.Client.PostAsJsonAsync($"/api/v1/tenancies/{IdOf(tenancy)}/tenants", new { userId = s.Marko.Id }));

        updated.GetProperty("members").GetArrayLength().Should().Be(2);
        (await NotificationKeysAsync(s.Marko.Id)).Should().Contain("tenancy_added");
    }

    [Fact]
    public async Task AddTenant_SomeoneAlreadyIn_IsRefused()
    {
        var s = await SetupAsync("addtwice");
        var tenancy = await CreateTenancyAsync(s, tenants: s.Ana.Id);

        var response = await s.Landlord.Client.PostAsJsonAsync($"/api/v1/tenancies/{IdOf(tenancy)}/tenants", new { userId = s.Ana.Id });

        (await BusinessCodeAsync(response)).Should().Be("TENANT_ALREADY_IN_TENANCY");
    }

    [Fact]
    public async Task AddTenant_SomeoneWithNoApplication_IsRefused()
    {
        var s = await SetupAsync("addstranger");
        var tenancy = await CreateTenancyAsync(s, tenants: s.Ana.Id);

        var response = await s.Landlord.Client.PostAsJsonAsync($"/api/v1/tenancies/{IdOf(tenancy)}/tenants", new { userId = s.Stranger.Id });

        (await BusinessCodeAsync(response)).Should().Be("TENANT_NOT_ELIGIBLE");
    }

    [Fact]
    public async Task RemoveTenant_ByTheLandlord_TheirUnpaidRentStaysOwed()
    {
        var s = await SetupAsync("remove");
        var tenancy = await CreateTenancyAsync(s);

        var updated = await OkAsync(await s.Landlord.Client.DeleteAsync($"/api/v1/tenancies/{IdOf(tenancy)}/tenants/{s.Marko.Id}"));

        updated.GetProperty("members").EnumerateArray()
            .Single(m => m.GetProperty("userId").GetInt32() == s.Marko.Id)
            .GetProperty("leftDate").ValueKind.Should().NotBe(JsonValueKind.Null);
        var markos = await OkAsync(await s.Marko.Client.GetAsync("/api/v1/bills/mine"));
        markos.GetProperty("toPay").GetArrayLength().Should().Be(1, "leaving does not cancel what is owed");
        (await NotificationKeysAsync(s.Marko.Id)).Should().Contain("tenancy_removed");
    }

    [Fact]
    public async Task Leave_ByATenant_TellsTheLandlord()
    {
        var s = await SetupAsync("leave");
        var tenancy = await CreateTenancyAsync(s);

        await OkAsync(await s.Marko.Client.DeleteAsync($"/api/v1/tenancies/{IdOf(tenancy)}/tenants/{s.Marko.Id}"));

        (await NotificationKeysAsync(s.Landlord.Id)).Should().Contain("tenancy_member_left");
    }

    [Fact]
    public async Task RemoveTenant_ByAnotherTenant_Returns403()
    {
        var s = await SetupAsync("remove403");
        var tenancy = await CreateTenancyAsync(s);

        var response = await s.Ana.Client.DeleteAsync($"/api/v1/tenancies/{IdOf(tenancy)}/tenants/{s.Marko.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ATenantWhoLeft_CanStillSeeTheHistory_ButNotAddBills()
    {
        var s = await SetupAsync("leftview");
        var tenancy = await CreateTenancyAsync(s);
        await OkAsync(await s.Marko.Client.DeleteAsync($"/api/v1/tenancies/{IdOf(tenancy)}/tenants/{s.Marko.Id}"));

        (await s.Marko.Client.GetAsync($"/api/v1/tenancies/{IdOf(tenancy)}")).StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await s.Marko.Client.PostAsJsonAsync("/api/v1/bills", CompanyBill(IdOf(tenancy)));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── ending ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task End_ByTheLandlord_NoMoreRentBills_AndTenantsAreTold()
    {
        var s = await SetupAsync("end");
        var tenancy = await CreateTenancyAsync(s);

        var ended = await OkAsync(await PostAsync(s.Landlord.Client, $"/api/v1/tenancies/{IdOf(tenancy)}/end"));
        await RunBillingJobAsync(DueDateSoon.AddMonths(1).AddDays(-5));

        ended.GetProperty("status").GetString().Should().Be("ended");
        (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Should().HaveCount(1, "only the bill from before the end");
        (await NotificationKeysAsync(s.Ana.Id)).Should().Contain("tenancy_ended");
    }

    [Fact]
    public async Task End_ByATenant_Returns403()
    {
        var s = await SetupAsync("end403");
        var tenancy = await CreateTenancyAsync(s);

        (await PostAsync(s.Ana.Client, $"/api/v1/tenancies/{IdOf(tenancy)}/end")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnEndedTenancy_TakesNoNewBillsOrTenants()
    {
        var s = await SetupAsync("endedrules");
        var tenancy = await CreateTenancyAsync(s, tenants: s.Ana.Id);
        await OkAsync(await PostAsync(s.Landlord.Client, $"/api/v1/tenancies/{IdOf(tenancy)}/end"));

        (await BusinessCodeAsync(await s.Landlord.Client.PostAsJsonAsync("/api/v1/bills", LandlordBill(IdOf(tenancy), 1000m))))
            .Should().Be("TENANCY_NOT_ACTIVE");
        (await BusinessCodeAsync(await s.Landlord.Client.PostAsJsonAsync($"/api/v1/tenancies/{IdOf(tenancy)}/tenants", new { userId = s.Marko.Id })))
            .Should().Be("TENANCY_NOT_ACTIVE");
        (await BusinessCodeAsync(await PostAsync(s.Landlord.Client, $"/api/v1/tenancies/{IdOf(tenancy)}/end")))
            .Should().Be("TENANCY_NOT_ACTIVE");
    }

    // ── accounts deleted ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeletingATenantsAccount_RemovesTheirPartsAndMembership()
    {
        var s = await SetupAsync("deltenant");
        var tenancy = await CreateTenancyAsync(s);

        (await s.Marko.Client.DeleteAsync($"/api/v1/auth/delete-user/{s.Marko.User.UserGuid}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var bill = (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Single();
        bill.GetProperty("shares").EnumerateArray().Select(x => x.GetProperty("payer").GetProperty("userId").GetInt32())
            .Should().Equal(s.Ana.Id);
        (await OkAsync(await s.Landlord.Client.GetAsync($"/api/v1/tenancies/{IdOf(tenancy)}")))
            .GetProperty("members").EnumerateArray().Select(m => m.GetProperty("userId").GetInt32()).Should().Equal(s.Ana.Id);
    }

    [Fact]
    public async Task DeletingTheLandlordsAccount_RemovesTheirTenancies()
    {
        var s = await SetupAsync("dellandlord");
        await CreateTenancyAsync(s);

        (await s.Landlord.Client.DeleteAsync($"/api/v1/auth/delete-user/{s.Landlord.User.UserGuid}")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await OkAsync(await s.Ana.Client.GetAsync("/api/v1/tenancies"))).GetArrayLength().Should().Be(0);
        (await OkAsync(await s.Ana.Client.GetAsync("/api/v1/bills/mine"))).GetProperty("toPay").GetArrayLength().Should().Be(0);
    }
}
