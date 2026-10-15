using System.Net.Http.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LandlordApp.Tests.E2eTests.Tests.Billing;

/// <summary>The morning job end to end: next month's rent, reminders before the due date, and after it.</summary>
[Collection(E2eCollection.Name)]
public class BillingJobE2eTests : BillingE2eBase
{
    public BillingJobE2eTests(E2eFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Job_MakesNextMonthsRent_TenDaysBeforeItIsDue_OnlyOnce()
    {
        var s = await SetupAsync("jobrent");
        var tenancy = await CreateTenancyAsync(s, rent: 30000m);
        var nextDue = DueDateSoon.AddMonths(1);

        await RunBillingJobAsync(nextDue.AddDays(-11));
        (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Should().HaveCount(1, "11 days before is too early");

        await RunBillingJobAsync(nextDue.AddDays(-10));
        await RunBillingJobAsync(nextDue.AddDays(-9));

        var bills = await BillsOfAsync(s.Landlord.Client, IdOf(tenancy));
        bills.Should().HaveCount(2);
        var next = bills.Single(b => b.GetProperty("period").GetString() == Period(nextDue));
        next.GetProperty("dueDate").GetString().Should().Be(nextDue.ToString("yyyy-MM-dd"));
        next.GetProperty("shares").EnumerateArray().Select(x => x.GetProperty("amount").GetDecimal()).Should().Equal(15000m, 15000m);
    }

    [Fact]
    public async Task Job_UsesTheRentSettingsInForceWhenTheBillIsMade()
    {
        var s = await SetupAsync("jobsettings");
        var tenancy = await CreateTenancyAsync(s, rent: 30000m);
        await OkAsync(await s.Landlord.Client.PutAsJsonAsync($"/api/v1/tenancies/{IdOf(tenancy)}", new
        {
            monthlyRent = 33000m, rentDueDay = DueDaySoon, payeeName = "Lazar Lazić", payeeAccount = LandlordAccount,
        }));
        var nextDue = DueDateSoon.AddMonths(1);

        await RunBillingJobAsync(nextDue.AddDays(-10));

        var next = (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Single(b => b.GetProperty("period").GetString() == Period(nextDue));
        next.GetProperty("amount").GetDecimal().Should().Be(33000m);
    }

    [Fact]
    public async Task Job_SplitsNextMonthsRentAmongTheTenantsLivingThereThen()
    {
        var s = await SetupAsync("jobmembers");
        var tenancy = await CreateTenancyAsync(s, rent: 30000m);
        await OkAsync(await s.Marko.Client.DeleteAsync($"/api/v1/tenancies/{IdOf(tenancy)}/tenants/{s.Marko.Id}"));
        var nextDue = DueDateSoon.AddMonths(1);

        await RunBillingJobAsync(nextDue.AddDays(-5));

        var next = (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Single(b => b.GetProperty("period").GetString() == Period(nextDue));
        var share = next.GetProperty("shares").EnumerateArray().Should().ContainSingle().Subject;
        share.GetProperty("payer").GetProperty("userId").GetInt32().Should().Be(s.Ana.Id);
        share.GetProperty("amount").GetDecimal().Should().Be(30000m);
    }

    [Fact]
    public async Task Job_RemindsEveryPayer_ThreeDaysBefore_ByBellAndMail_Once()
    {
        var s = await SetupAsync("jobsoon");
        await CreateTenancyAsync(s, rent: 30000m);

        await RunBillingJobAsync(DueDateSoon.AddDays(-2));
        await RunBillingJobAsync(DueDateSoon.AddDays(-1));

        (await NotificationKeysAsync(s.Ana.Id)).Count(k => k == "bill_due_soon").Should().Be(1);
        (await NotificationKeysAsync(s.Marko.Id)).Count(k => k == "bill_due_soon").Should().Be(1);
        Fixture.Factory.MockEmail.Verify(e => e.SendBillReminderEmailAsync(
            s.Ana.User.Email, "Ana", It.IsAny<string>(), 15000m, DueDateSoon, false), Times.Once);
    }

    [Fact]
    public async Task Job_DoesNotRemindAPartThatIsMarkedPaid()
    {
        var s = await SetupAsync("jobmarked");
        var tenancy = await CreateTenancyAsync(s, rent: 30000m);
        var rent = (await BillsOfAsync(s.Ana.Client, IdOf(tenancy))).Single();
        await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{ShareIdOf(rent, s.Ana.Id)}/mark-paid"));

        await RunBillingJobAsync(DueDateSoon.AddDays(-1));
        await RunBillingJobAsync(DueDateSoon.AddDays(1));

        (await NotificationKeysAsync(s.Ana.Id)).Should().NotContain(new[] { "bill_due_soon", "bill_overdue" });
        (await NotificationKeysAsync(s.Marko.Id)).Should().Contain(new[] { "bill_due_soon", "bill_overdue" });
    }

    [Fact]
    public async Task Job_AfterTheDueDate_TellsThePayerAndTheReceiver_Once()
    {
        var s = await SetupAsync("joblate");
        await CreateTenancyAsync(s, rent: 30000m);

        await RunBillingJobAsync(DueDateSoon.AddDays(1));
        await RunBillingJobAsync(DueDateSoon.AddDays(2));

        (await NotificationKeysAsync(s.Ana.Id)).Count(k => k == "bill_overdue").Should().Be(1);
        (await NotificationKeysAsync(s.Landlord.Id)).Count(k => k == "bill_overdue_receiver").Should().Be(2, "one per late tenant");
        Fixture.Factory.MockEmail.Verify(e => e.SendBillReminderEmailAsync(
            s.Ana.User.Email, "Ana", It.IsAny<string>(), 15000m, DueDateSoon, true), Times.Once);
    }

    [Fact]
    public async Task Job_ACompanyBillNobodyPaid_RemindsAllTenants_AndTheLandlordWhenLate()
    {
        var s = await SetupAsync("jobcompany");
        var tenancy = await CreateTenancyAsync(s, dueDay: DueDayFar);
        var due = Today.AddDays(15);
        await CreateBillAsync(s.Ana.Client, CompanyBill(IdOf(tenancy), due: due));

        await RunBillingJobAsync(due.AddDays(-3));
        await RunBillingJobAsync(due.AddDays(1));

        (await NotificationKeysAsync(s.Ana.Id)).Should().Contain(new[] { "bill_due_soon", "bill_overdue" });
        (await NotificationKeysAsync(s.Marko.Id)).Should().Contain(new[] { "bill_due_soon", "bill_overdue" });
        (await NotificationKeysAsync(s.Landlord.Id)).Should().Contain("bill_company_overdue");
    }

    [Fact]
    public async Task Job_APaidCompanyBill_GetsNoReminder_ButThePaybacksDo()
    {
        var s = await SetupAsync("jobcompanypaid");
        var tenancy = await CreateTenancyAsync(s, dueDay: DueDayFar);
        var due = Today.AddDays(15);
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(IdOf(tenancy), due: due));
        await OkAsync(await s.Ana.Client.PostAsJsonAsync($"/api/v1/bills/{bill.GetProperty("billId").GetInt32()}/pay",
            new { payoutName = "Ana Anić", payoutAccount = PayoutAccount }));

        await RunBillingJobAsync(due.AddDays(-2));

        (await NotificationKeysAsync(s.Ana.Id)).Should().NotContain("bill_due_soon");
        (await NotificationKeysAsync(s.Marko.Id)).Should().Contain("bill_due_soon", "he still owes Ana his part");
        (await NotificationKeysAsync(s.Landlord.Id)).Should().NotContain("bill_company_overdue");
    }

    [Fact]
    public async Task Job_ACancelledBill_GetsNoReminders()
    {
        var s = await SetupAsync("jobcancel");
        var tenancy = await CreateTenancyAsync(s, rent: 30000m);
        var rent = (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Single();
        await PostAsync(s.Landlord.Client, $"/api/v1/bills/{rent.GetProperty("billId").GetInt32()}/cancel");

        await RunBillingJobAsync(DueDateSoon.AddDays(-1));
        await RunBillingJobAsync(DueDateSoon.AddDays(1));

        (await NotificationKeysAsync(s.Ana.Id)).Should().NotContain(new[] { "bill_due_soon", "bill_overdue" });
    }

    [Fact]
    public async Task Job_MovingTheDueDate_EarnsNewReminders()
    {
        var s = await SetupAsync("jobmove");
        var tenancy = await CreateTenancyAsync(s, rent: 30000m);
        var rent = (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Single();
        await RunBillingJobAsync(DueDateSoon.AddDays(-1));
        var later = DueDateSoon.AddDays(10);
        await OkAsync(await s.Landlord.Client.PutAsJsonAsync($"/api/v1/bills/{rent.GetProperty("billId").GetInt32()}", new
        {
            title = rent.GetProperty("title").GetString(), amount = 30000m, dueDate = later.ToString("yyyy-MM-dd"),
        }));

        await RunBillingJobAsync(later.AddDays(-1));

        (await NotificationKeysAsync(s.Ana.Id)).Count(k => k == "bill_due_soon").Should().Be(2);
    }

    [Fact]
    public async Task Job_ForAnEndedTenancy_StillRemindsOfWhatIsOwed()
    {
        var s = await SetupAsync("jobended");
        var tenancy = await CreateTenancyAsync(s, rent: 30000m);
        await OkAsync(await PostAsync(s.Landlord.Client, $"/api/v1/tenancies/{IdOf(tenancy)}/end"));

        await RunBillingJobAsync(DueDateSoon.AddDays(1));

        (await NotificationKeysAsync(s.Ana.Id)).Should().Contain("bill_overdue");
        (await InBillingDbAsync(db => db.Bills.CountAsync(b => b.TenancyId == IdOf(tenancy)))).Should().Be(1);
    }
}
