using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LandlordApp.Tests.E2eTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LandlordApp.Tests.E2eTests.Tests.Billing;

/// <summary>
/// Bills and payments end to end: entering utility bills, the IPS QR payment order, "paid" → "arrived"
/// / "did not arrive", company bills paid by one tenant and paid back by the roommates.
/// </summary>
[Collection(E2eCollection.Name)]
public class BillPaymentE2eTests : BillingE2eBase
{
    public BillPaymentE2eTests(E2eFixture fixture) : base(fixture) { }

    private async Task<(Setup S, int TenancyId, JsonElement Rent)> WithRentAsync(string prefix, decimal rent = 30001m)
    {
        var s = await SetupAsync(prefix);
        var tenancy = await CreateTenancyAsync(s, rent: rent);
        var bill = (await BillsOfAsync(s.Landlord.Client, IdOf(tenancy))).Single();
        return (s, IdOf(tenancy), bill);
    }

    // ── entering bills ────────────────────────────────────────────────────────

    [Fact]
    public async Task LandlordBill_IsSplitEquallyAmongTheTenants_ToThePara()
    {
        var s = await SetupAsync("split");
        var third = await PersonAsync("split-third@e2e.com", "Jovan", "Jović");
        await Data.CreateApprovedApplicationAsync(third.Id, s.ApartmentId);
        var tenancy = await CreateTenancyAsync(s, dueDay: DueDayFar, tenants: new[] { s.Ana.Id, s.Marko.Id, third.Id });

        var bill = await CreateBillAsync(s.Landlord.Client, LandlordBill(IdOf(tenancy), 100.01m));

        bill.GetProperty("payTo").GetString().Should().Be("landlord");
        bill.GetProperty("payeeAccount").GetString().Should().Be(LandlordAccount);
        bill.GetProperty("shares").EnumerateArray().Select(x => x.GetProperty("amount").GetDecimal())
            .Should().BeEquivalentTo(new[] { 33.34m, 33.34m, 33.33m });
        bill.GetProperty("status").GetString().Should().Be("open");
        (await NotificationKeysAsync(third.Id)).Should().Contain("bill_new");
    }

    [Fact]
    public async Task ATenant_CannotAskToBePaid_OnlyEnterCompanyBills()
    {
        var (s, tenancyId, _) = await WithRentAsync("tenantlandlord");

        var response = await s.Ana.Client.PostAsJsonAsync("/api/v1/bills", LandlordBill(tenancyId, 1000m));

        (await BusinessCodeAsync(response)).Should().Be("ONLY_LANDLORD_CAN_BE_PAID");
    }

    [Fact]
    public async Task CompanyBill_ByATenant_HasNoPartsUntilPaid_AndTheOthersAreTold()
    {
        var (s, tenancyId, _) = await WithRentAsync("company");

        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId));

        bill.GetProperty("payTo").GetString().Should().Be("company");
        bill.GetProperty("payeeName").GetString().Should().Be("EPS Snabdevanje");
        bill.GetProperty("payeeAccount").GetString().Should().Be(CompanyAccount);
        bill.GetProperty("paymentModel").GetString().Should().Be("97");
        bill.GetProperty("shares").GetArrayLength().Should().Be(0);
        bill.GetProperty("tenantCount").GetInt32().Should().Be(2);
        (await NotificationKeysAsync(s.Marko.Id)).Should().Contain("bill_company_new");
        (await NotificationKeysAsync(s.Landlord.Id)).Should().Contain("bill_company_new");
        (await NotificationKeysAsync(s.Ana.Id)).Should().NotContain("bill_company_new", "she entered it");
    }

    [Fact]
    public async Task Bills_BySomeoneOutsideTheTenancy_Return403()
    {
        var (s, tenancyId, rent) = await WithRentAsync("outsider");

        (await s.Stranger.Client.PostAsJsonAsync("/api/v1/bills", CompanyBill(tenancyId))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await s.Stranger.Client.GetAsync($"/api/v1/bills/{rent.GetProperty("billId").GetInt32()}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompanyBill_WithoutTheCompanyAccount_Returns400()
    {
        var (s, tenancyId, _) = await WithRentAsync("noaccount");

        var response = await s.Ana.Client.PostAsJsonAsync("/api/v1/bills", new
        {
            tenancyId, kind = "Internet", title = "Internet", amount = 2500m,
            dueDate = Today.AddDays(5).ToString("yyyy-MM-dd"), payTo = "Company", payeeName = "SBB",
        });

        (await ValidationCodesAsync(response)).Should().Contain("BANK_ACCOUNT_INVALID");
    }

    [Theory]
    [InlineData("Rent", "BILL_KIND_INVALID")]
    public async Task Bill_OfKindRent_CannotBeEnteredByHand(string kind, string code)
    {
        var (s, tenancyId, _) = await WithRentAsync("kindrent");

        var response = await s.Landlord.Client.PostAsJsonAsync("/api/v1/bills", new
        {
            tenancyId, kind, title = "Kirija", amount = 1000m, dueDate = Today.AddDays(5).ToString("yyyy-MM-dd"), payTo = "Landlord",
        });

        (await ValidationCodesAsync(response)).Should().Contain(code);
    }

    [Fact]
    public async Task Bill_WithAWrongPaymentCodeModelOrReference_Returns400WithTheCodes()
    {
        var (s, tenancyId, _) = await WithRentAsync("badcodes");

        var response = await s.Ana.Client.PostAsJsonAsync("/api/v1/bills", new
        {
            tenancyId, kind = "Water", title = "Voda", amount = 900m, dueDate = Today.AddDays(5).ToString("yyyy-MM-dd"),
            payTo = "Company", payeeName = "BVK", payeeAccount = CompanyAccount,
            paymentCode = "389", paymentModel = "9", paymentReference = "abc",
        });

        (await ValidationCodesAsync(response)).Should().Contain(new[] { "PAYMENT_CODE_INVALID", "PAYMENT_MODEL_INVALID", "PAYMENT_REFERENCE_INVALID" });
    }

    [Fact]
    public async Task Bill_WithAnAmountOfThreeDecimalsOrZero_Returns400()
    {
        var (s, tenancyId, _) = await WithRentAsync("badamount");

        var zero = await s.Landlord.Client.PostAsJsonAsync("/api/v1/bills", LandlordBill(tenancyId, 0m));
        var para = await s.Landlord.Client.PostAsJsonAsync("/api/v1/bills", LandlordBill(tenancyId, 10.005m));

        (await ValidationCodesAsync(zero)).Should().Contain("AMOUNT_INVALID");
        (await ValidationCodesAsync(para)).Should().Contain("AMOUNT_INVALID");
    }

    // ── changing and cancelling ───────────────────────────────────────────────

    [Fact]
    public async Task Update_WithoutPayments_SplitsTheNewAmount_AndTellsTheTenants()
    {
        var (s, _, rent) = await WithRentAsync("edit", rent: 30000m);
        var billId = rent.GetProperty("billId").GetInt32();

        var updated = await OkAsync(await s.Landlord.Client.PutAsJsonAsync($"/api/v1/bills/{billId}", new
        {
            title = rent.GetProperty("title").GetString(), amount = 28000.01m, dueDate = Today.AddDays(9).ToString("yyyy-MM-dd"),
        }));

        updated.GetProperty("amount").GetDecimal().Should().Be(28000.01m);
        updated.GetProperty("shares").EnumerateArray().Select(x => x.GetProperty("amount").GetDecimal())
            .Should().BeEquivalentTo(new[] { 14000.01m, 14000.00m });
        (await NotificationKeysAsync(s.Ana.Id)).Should().Contain("bill_changed");
    }

    [Fact]
    public async Task Update_AfterSomeoneMarkedTheirPartPaid_IsRefused()
    {
        var (s, _, rent) = await WithRentAsync("editpaid");
        var billId = rent.GetProperty("billId").GetInt32();
        await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{ShareIdOf(rent, s.Ana.Id)}/mark-paid"));

        var response = await s.Landlord.Client.PutAsJsonAsync($"/api/v1/bills/{billId}", new
        {
            title = "Kirija", amount = 1m, dueDate = Today.AddDays(9).ToString("yyyy-MM-dd"),
        });

        (await BusinessCodeAsync(response)).Should().Be("BILL_HAS_PAYMENTS");
    }

    [Fact]
    public async Task ATenant_CannotChangeTheRent()
    {
        var (s, _, rent) = await WithRentAsync("editrent403");

        var response = await s.Ana.Client.PutAsJsonAsync($"/api/v1/bills/{rent.GetProperty("billId").GetInt32()}", new
        {
            title = "Kirija", amount = 1m, dueDate = Today.AddDays(9).ToString("yyyy-MM-dd"),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_TheRentOfAMonth_TakesItOffTheListAndTheJobDoesNotMakeItAgain()
    {
        var (s, tenancyId, rent) = await WithRentAsync("cancel");
        var billId = rent.GetProperty("billId").GetInt32();

        (await PostAsync(s.Landlord.Client, $"/api/v1/bills/{billId}/cancel")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await RunBillingJobAsync(Today);

        (await OkAsync(await s.Ana.Client.GetAsync("/api/v1/bills/mine"))).GetProperty("toPay").GetArrayLength().Should().Be(0);
        var bills = await BillsOfAsync(s.Landlord.Client, tenancyId);
        bills.Should().ContainSingle().Which.GetProperty("status").GetString().Should().Be("cancelled");
    }

    [Fact]
    public async Task Cancel_ACompanyBill_ByAnotherTenant_Returns403_ByWhoEnteredIt_Works()
    {
        var (s, tenancyId, _) = await WithRentAsync("cancelcompany");
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId));
        var billId = bill.GetProperty("billId").GetInt32();

        (await PostAsync(s.Marko.Client, $"/api/v1/bills/{billId}/cancel")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await PostAsync(s.Ana.Client, $"/api/v1/bills/{billId}/cancel")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── the IPS QR payment order ──────────────────────────────────────────────

    [Fact]
    public async Task SharePayment_IsTheNbsIpsQrPayload_ForTheLandlordsAccount()
    {
        var (s, tenancyId, rent) = await WithRentAsync("qr");
        var due = DueDateSoon;

        var payment = await OkAsync(await s.Ana.Client.GetAsync($"/api/v1/bills/shares/{ShareIdOf(rent, s.Ana.Id)}/payment"));

        payment.GetProperty("qrPayload").GetString().Should().Be(
            $"K:PR|V:01|C:1|R:{Digits(LandlordAccount)}|N:Lazar Lazić|I:RSD15000,50|P:Ana Anić|SF:289" +
            $"|S:Kirija {due.Month:D2}/{due.Year}|RO:00{tenancyId}-{due.Year:D4}{due.Month:D2}");
        payment.GetProperty("qrCodeDataUrl").GetString().Should().StartWith("data:image/png;base64,");
        payment.GetProperty("payeeAccount").GetString().Should().Be(LandlordAccount);
        payment.GetProperty("amount").GetDecimal().Should().Be(15000.50m);
        payment.GetProperty("currency").GetString().Should().Be("RSD");
    }

    [Fact]
    public async Task SharePayment_OfSomeoneElse_Returns403()
    {
        var (s, _, rent) = await WithRentAsync("qr403");

        var response = await s.Marko.Client.GetAsync($"/api/v1/bills/shares/{ShareIdOf(rent, s.Ana.Id)}/payment");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CompanyPayment_UsesTheCompanysAccountAndReference()
    {
        var (s, tenancyId, _) = await WithRentAsync("qrcompany");
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId, amount: 6123.40m));

        var payment = await OkAsync(await s.Marko.Client.GetAsync($"/api/v1/bills/{bill.GetProperty("billId").GetInt32()}/payment"));

        payment.GetProperty("qrPayload").GetString().Should().Be(
            $"K:PR|V:01|C:1|R:{Digits(CompanyAccount)}|N:EPS Snabdevanje|I:RSD6123,40|P:Marko Marković|SF:289|S:Struja oktobar|RO:975501234");
    }

    // ── "paid" → "arrived" / "did not arrive" ─────────────────────────────────

    [Fact]
    public async Task MarkPaid_ThenTheLandlordConfirms_TheBillIsPaidWhenEveryPartIs()
    {
        var (s, _, rent) = await WithRentAsync("flow");
        var billId = rent.GetProperty("billId").GetInt32();
        var anasShare = ShareIdOf(rent, s.Ana.Id);
        var markosShare = ShareIdOf(rent, s.Marko.Id);

        var marked = await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{anasShare}/mark-paid"));
        ShareOf(marked, s.Ana.Id).GetProperty("status").GetString().Should().Be("marked_paid");
        marked.GetProperty("status").GetString().Should().Be("partly_paid");
        (await NotificationKeysAsync(s.Landlord.Id)).Should().Contain("bill_marked_paid");

        var toConfirm = await OkAsync(await s.Landlord.Client.GetAsync("/api/v1/bills/mine"));
        toConfirm.GetProperty("toConfirm").EnumerateArray().Single().GetProperty("share").GetProperty("billShareId").GetInt32().Should().Be(anasShare);

        await OkAsync(await PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{anasShare}/confirm"));
        (await NotificationKeysAsync(s.Ana.Id)).Should().Contain("bill_confirmed");
        await OkAsync(await PostAsync(s.Marko.Client, $"/api/v1/bills/shares/{markosShare}/mark-paid"));
        var paid = await OkAsync(await PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{markosShare}/confirm"));

        paid.GetProperty("status").GetString().Should().Be("paid");
        (await BillAsync(s.Ana.Client, billId)).GetProperty("status").GetString().Should().Be("paid");
        (await OkAsync(await s.Ana.Client.GetAsync("/api/v1/bills/mine"))).GetProperty("toPay").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task OnlyThePayerMarks_AndOnlyTheReceiverConfirms()
    {
        var (s, _, rent) = await WithRentAsync("roles");
        var anasShare = ShareIdOf(rent, s.Ana.Id);

        (await PostAsync(s.Marko.Client, $"/api/v1/bills/shares/{anasShare}/mark-paid")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{anasShare}/mark-paid")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{anasShare}/confirm")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await PostAsync(s.Stranger.Client, $"/api/v1/bills/shares/{anasShare}/confirm")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NotReceived_PutsThePartBackToUnpaid_AndTellsThePayer()
    {
        var (s, _, rent) = await WithRentAsync("reject");
        var anasShare = ShareIdOf(rent, s.Ana.Id);
        await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{anasShare}/mark-paid"));

        var rejected = await OkAsync(await PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{anasShare}/reject"));

        ShareOf(rejected, s.Ana.Id).GetProperty("status").GetString().Should().Be("unpaid");
        (await NotificationKeysAsync(s.Ana.Id)).Should().Contain("bill_rejected");
    }

    [Fact]
    public async Task NotReceived_ForAPartNobodyMarked_IsRefused()
    {
        var (s, _, rent) = await WithRentAsync("rejectunmarked");

        var response = await PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{ShareIdOf(rent, s.Ana.Id)}/reject");

        (await BusinessCodeAsync(response)).Should().Be("PAYMENT_NOT_MARKED");
    }

    [Fact]
    public async Task ThePayer_CanTakeBackTheMark_UntilItIsConfirmed()
    {
        var (s, _, rent) = await WithRentAsync("unmark");
        var anasShare = ShareIdOf(rent, s.Ana.Id);
        await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{anasShare}/mark-paid"));

        var undone = await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{anasShare}/unmark"));
        ShareOf(undone, s.Ana.Id).GetProperty("status").GetString().Should().Be("unpaid");

        await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{anasShare}/mark-paid"));
        await OkAsync(await PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{anasShare}/confirm"));
        (await BusinessCodeAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{anasShare}/unmark")))
            .Should().Be("PAYMENT_ALREADY_CONFIRMED");
    }

    [Fact]
    public async Task TheLandlord_CanConfirmMoneyThatArrivedBeforeTheTenantSaidAnything()
    {
        var (s, _, rent) = await WithRentAsync("earlyconfirm");

        var confirmed = await OkAsync(await PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{ShareIdOf(rent, s.Ana.Id)}/confirm"));

        ShareOf(confirmed, s.Ana.Id).GetProperty("status").GetString().Should().Be("confirmed");
    }

    [Fact]
    public async Task ConfirmingTwice_IsRefused()
    {
        var (s, _, rent) = await WithRentAsync("twiceconfirm");
        var anasShare = ShareIdOf(rent, s.Ana.Id);
        await OkAsync(await PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{anasShare}/confirm"));

        (await BusinessCodeAsync(await PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{anasShare}/confirm")))
            .Should().Be("PAYMENT_ALREADY_CONFIRMED");
        (await BusinessCodeAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{anasShare}/mark-paid")))
            .Should().Be("PAYMENT_ALREADY_CONFIRMED");
    }

    [Fact]
    public async Task TwoConfirmationsAtTheSameMoment_OnlyOneGoesThrough()
    {
        var (s, _, rent) = await WithRentAsync("raceconfirm");
        var anasShare = ShareIdOf(rent, s.Ana.Id);
        var other = CreateAuthenticatedClient(s.Landlord.Id, s.Landlord.User.UserGuid);

        var results = await Task.WhenAll(
            PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{anasShare}/confirm"),
            PostAsync(other, $"/api/v1/bills/shares/{anasShare}/confirm"));

        results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
        results.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity).Should().Be(1);
        (await NotificationKeysAsync(s.Ana.Id)).Count(k => k == "bill_confirmed").Should().Be(1);
    }

    [Fact]
    public async Task MarkingPaidTwice_ChangesNothing()
    {
        var (s, _, rent) = await WithRentAsync("marktwice");
        var anasShare = ShareIdOf(rent, s.Ana.Id);

        await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{anasShare}/mark-paid"));
        await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{anasShare}/mark-paid"));

        (await NotificationKeysAsync(s.Landlord.Id)).Count(k => k == "bill_marked_paid").Should().Be(1);
    }

    // ── what waits for whom ───────────────────────────────────────────────────

    [Fact]
    public async Task Summary_CountsWhatTheUserHasToPayAndToConfirm()
    {
        var (s, tenancyId, rent) = await WithRentAsync("summary");
        await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId, amount: 6000m));

        var anas = await OkAsync(await s.Ana.Client.GetAsync("/api/v1/bills/summary"));
        anas.GetProperty("toPay").GetInt32().Should().Be(2);
        anas.GetProperty("amountToPay").GetDecimal().Should().Be(15000.50m + 6000m);

        await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{ShareIdOf(rent, s.Ana.Id)}/mark-paid"));
        (await OkAsync(await s.Landlord.Client.GetAsync("/api/v1/bills/summary"))).GetProperty("toConfirm").GetInt32().Should().Be(1);
        (await OkAsync(await s.Ana.Client.GetAsync("/api/v1/bills/summary"))).GetProperty("toPay").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Mine_ListsWhatToPay_SoonestFirst_WithWhoReceivesIt()
    {
        var (s, tenancyId, _) = await WithRentAsync("mine");
        await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId, due: Today.AddDays(1)));

        var mine = await OkAsync(await s.Marko.Client.GetAsync("/api/v1/bills/mine"));

        var toPay = mine.GetProperty("toPay").EnumerateArray().ToList();
        toPay.Select(p => p.GetProperty("type").GetString()).Should().Equal("company_bill", "share");
        toPay[1].GetProperty("receiver").GetProperty("name").GetString().Should().Be("Lazar Lazić");
        toPay[1].GetProperty("amount").GetDecimal().Should().Be(15000.50m);
    }

    // ── company bills: one payment, then the roommates pay back ───────────────

    [Fact]
    public async Task CompanyBill_PaidByOneTenant_TheRoommatesOweTheirPartToThem()
    {
        var (s, tenancyId, _) = await WithRentAsync("payback");
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId, amount: 6000.01m));
        var billId = bill.GetProperty("billId").GetInt32();

        var paid = await OkAsync(await s.Ana.Client.PostAsJsonAsync($"/api/v1/bills/{billId}/pay", new { payoutName = "Ana Anić", payoutAccount = PayoutAccount }));

        paid.GetProperty("paidBy").GetProperty("userId").GetInt32().Should().Be(s.Ana.Id);
        paid.GetProperty("status").GetString().Should().Be("partly_paid");
        var share = paid.GetProperty("shares").EnumerateArray().Should().ContainSingle().Subject;
        share.GetProperty("payer").GetProperty("userId").GetInt32().Should().Be(s.Marko.Id);
        share.GetProperty("receiver").GetProperty("userId").GetInt32().Should().Be(s.Ana.Id);
        share.GetProperty("amount").GetDecimal().Should().BeOneOf(3000.00m, 3000.01m);
        (await NotificationKeysAsync(s.Marko.Id)).Should().Contain("bill_company_paid");
        (await NotificationKeysAsync(s.Landlord.Id)).Should().Contain("bill_company_paid_landlord");

        var payment = await OkAsync(await s.Marko.Client.GetAsync($"/api/v1/bills/shares/{share.GetProperty("billShareId").GetInt32()}/payment"));
        payment.GetProperty("qrPayload").GetString().Should().Contain($"|R:{Digits(PayoutAccount)}|N:Ana Anić|")
            .And.Contain("|S:Udeo: Struja oktobar").And.NotContain("|RO:");
    }

    [Fact]
    public async Task Payback_MarkedByTheRoommate_ConfirmedByThePayer_SettlesTheBill()
    {
        var (s, tenancyId, _) = await WithRentAsync("paybackflow");
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId));
        var billId = bill.GetProperty("billId").GetInt32();
        var paid = await OkAsync(await s.Ana.Client.PostAsJsonAsync($"/api/v1/bills/{billId}/pay", new { payoutName = "Ana Anić", payoutAccount = PayoutAccount }));
        var shareId = ShareIdOf(paid, s.Marko.Id);

        await OkAsync(await PostAsync(s.Marko.Client, $"/api/v1/bills/shares/{shareId}/mark-paid"));
        (await PostAsync(s.Landlord.Client, $"/api/v1/bills/shares/{shareId}/confirm")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var settled = await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/shares/{shareId}/confirm"));

        settled.GetProperty("status").GetString().Should().Be("paid");
    }

    [Fact]
    public async Task PayingWithRoommates_NeedsTheAccountToPayBackTo()
    {
        var (s, tenancyId, _) = await WithRentAsync("needaccount");
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId));

        var response = await s.Ana.Client.PostAsJsonAsync($"/api/v1/bills/{bill.GetProperty("billId").GetInt32()}/pay", new { });

        (await BusinessCodeAsync(response)).Should().Be("PAYOUT_ACCOUNT_REQUIRED");
    }

    [Fact]
    public async Task PayingWithRoommates_UsesTheSavedPayoutAccount()
    {
        var (s, tenancyId, _) = await WithRentAsync("savedaccount");
        await OkAsync(await s.Ana.Client.PutAsJsonAsync("/api/v1/bills/profile", new { payoutName = "Ana A.", payoutAccount = PayoutAccount }));
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId));

        var paid = await OkAsync(await s.Ana.Client.PostAsJsonAsync($"/api/v1/bills/{bill.GetProperty("billId").GetInt32()}/pay", new { }));

        var stored = await InBillingDbAsync(db => db.BillShares.AsNoTracking().SingleAsync(x => x.BillId == paid.GetProperty("billId").GetInt32()));
        stored.ReceiverAccount.Should().Be(Digits(PayoutAccount));
        stored.ReceiverName.Should().Be("Ana A.");
    }

    [Fact]
    public async Task PayingTheWholeBill_ForgetsNothing_TheAccountIsRememberedForNextTime()
    {
        var (s, tenancyId, _) = await WithRentAsync("remember");
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId));
        await OkAsync(await s.Ana.Client.PostAsJsonAsync($"/api/v1/bills/{bill.GetProperty("billId").GetInt32()}/pay", new { payoutName = "Ana Anić", payoutAccount = PayoutAccount }));

        var profile = await OkAsync(await s.Ana.Client.GetAsync("/api/v1/bills/profile"));

        profile.GetProperty("payoutAccount").GetString().Should().Be(PayoutAccount);
        profile.GetProperty("payoutName").GetString().Should().Be("Ana Anić");
    }

    [Fact]
    public async Task ASingleTenant_PayingTheCompany_SettlesTheBillAtOnce()
    {
        var s = await SetupAsync("single");
        var tenancy = await CreateTenancyAsync(s, dueDay: DueDayFar, tenants: s.Ana.Id);
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(IdOf(tenancy)));

        var paid = await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/{bill.GetProperty("billId").GetInt32()}/pay"));

        paid.GetProperty("status").GetString().Should().Be("paid");
        paid.GetProperty("shares").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task PayingACompanyBillTwice_IsRefused_AndTheLandlordCannotPayIt()
    {
        var (s, tenancyId, _) = await WithRentAsync("paytwice");
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId));
        var billId = bill.GetProperty("billId").GetInt32();

        (await PostAsync(s.Landlord.Client, $"/api/v1/bills/{billId}/pay")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await OkAsync(await s.Ana.Client.PostAsJsonAsync($"/api/v1/bills/{billId}/pay", new { payoutName = "Ana", payoutAccount = PayoutAccount }));
        (await BusinessCodeAsync(await s.Marko.Client.PostAsJsonAsync($"/api/v1/bills/{billId}/pay", new { payoutName = "Marko", payoutAccount = Account("170", "5") })))
            .Should().Be("BILL_ALREADY_PAID");
    }

    [Fact]
    public async Task TwoTenantsPayingTheSameCompanyBillAtOnce_OnlyOneIsThePayer()
    {
        var (s, tenancyId, _) = await WithRentAsync("payrace");
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId));
        var billId = bill.GetProperty("billId").GetInt32();

        var results = await Task.WhenAll(
            s.Ana.Client.PostAsJsonAsync($"/api/v1/bills/{billId}/pay", new { payoutName = "Ana", payoutAccount = PayoutAccount }),
            s.Marko.Client.PostAsJsonAsync($"/api/v1/bills/{billId}/pay", new { payoutName = "Marko", payoutAccount = Account("170", "5") }));

        results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
        results.Count(r => r.StatusCode == HttpStatusCode.UnprocessableEntity).Should().Be(1);
        (await InBillingDbAsync(db => db.BillShares.CountAsync(x => x.BillId == billId))).Should().Be(1);
    }

    [Fact]
    public async Task UndoingACompanyPayment_RemovesThePaybacks_UntilSomeoneMarkedTheirs()
    {
        var (s, tenancyId, _) = await WithRentAsync("undo");
        var bill = await CreateBillAsync(s.Ana.Client, CompanyBill(tenancyId));
        var billId = bill.GetProperty("billId").GetInt32();
        await OkAsync(await s.Ana.Client.PostAsJsonAsync($"/api/v1/bills/{billId}/pay", new { payoutName = "Ana", payoutAccount = PayoutAccount }));

        (await PostAsync(s.Marko.Client, $"/api/v1/bills/{billId}/undo-payment")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var undone = await OkAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/{billId}/undo-payment"));
        undone.GetProperty("status").GetString().Should().Be("open");
        undone.GetProperty("shares").GetArrayLength().Should().Be(0);

        var paid = await OkAsync(await s.Ana.Client.PostAsJsonAsync($"/api/v1/bills/{billId}/pay", new { payoutName = "Ana", payoutAccount = PayoutAccount }));
        await OkAsync(await PostAsync(s.Marko.Client, $"/api/v1/bills/shares/{ShareIdOf(paid, s.Marko.Id)}/mark-paid"));
        (await BusinessCodeAsync(await PostAsync(s.Ana.Client, $"/api/v1/bills/{billId}/undo-payment"))).Should().Be("BILL_HAS_PAYMENTS");
    }

    // ── payout profile ────────────────────────────────────────────────────────

    [Fact]
    public async Task BillingProfile_IsSavedFormatted_AndAWrongAccountIsRefused()
    {
        var s = await SetupAsync("profile");

        (await OkAsync(await s.Ana.Client.GetAsync("/api/v1/bills/profile"))).GetProperty("payoutAccount").ValueKind.Should().Be(JsonValueKind.Null);
        var saved = await OkAsync(await s.Ana.Client.PutAsJsonAsync("/api/v1/bills/profile", new { payoutName = "Ana", payoutAccount = Digits(PayoutAccount) }));
        saved.GetProperty("payoutAccount").GetString().Should().Be(PayoutAccount);

        var bad = await s.Ana.Client.PutAsJsonAsync("/api/v1/bills/profile", new { payoutName = "Ana", payoutAccount = "265-777-00" });
        (await ValidationCodesAsync(bad)).Should().Contain("BANK_ACCOUNT_INVALID");
    }
}
