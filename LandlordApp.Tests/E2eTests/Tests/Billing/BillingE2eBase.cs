using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Lander;
using Lander.src.Modules.Billing;
using Lander.src.Modules.Billing.Implementation;
using Lander.src.Modules.Roommates.Models;
using Lander.src.Modules.Users.Domain.Aggregates.RolesAggregate;
using LandlordApp.Tests.E2eTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LandlordApp.Tests.E2eTests.Tests.Billing;

/// <summary>
/// People and data the bill tests share: a landlord with an apartment, tenants with approved
/// applications, roommates, valid Serbian account numbers, and readers for what the API stored.
/// </summary>
public abstract class BillingE2eBase : E2eTestBase
{
    protected BillingE2eBase(E2eFixture fixture) : base(fixture) { }

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // ── accounts ──────────────────────────────────────────────────────────────

    /// <summary>A valid account written the usual way: bank-number-control.</summary>
    protected static string Account(string bank, string number)
    {
        var middle = number.PadLeft(13, '0');
        return $"{bank}-{middle}-{SerbianAccount.ControlDigits(bank + middle)}";
    }

    protected static readonly string LandlordAccount = Account("160", "123456");
    protected static readonly string CompanyAccount = Account("840", "1000");
    protected static readonly string PayoutAccount = Account("265", "777");
    protected static string Digits(string account) => account.Replace("-", "");

    // ── dates (the same Serbian calendar day the server uses) ─────────────────

    protected static DateOnly Today => BillingCalendar.Today(TimeProvider.System);

    /// <summary>A due day whose rent bill appears at once (within the 10 days before it is due).</summary>
    protected static int DueDaySoon => Today.Day <= 26 ? Today.Day + 2 : 1;

    protected static DateOnly DueDateSoon => Today.Day <= 26
        ? new DateOnly(Today.Year, Today.Month, Today.Day + 2)
        : new DateOnly(Today.Year, Today.Month, 1).AddMonths(1);

    /// <summary>A due day more than 10 days away: no rent bill yet.</summary>
    protected static int DueDayFar => Today.Day == 1 ? 28 : Math.Min(Today.Day - 1, 28);

    protected static string Period(DateOnly due) => $"{due.Year:D4}-{due.Month:D2}";

    // ── people ────────────────────────────────────────────────────────────────

    protected sealed record Person(User User, HttpClient Client)
    {
        public int Id => User.UserId;
        public string Name => $"{User.FirstName} {User.LastName}";
    }

    protected async Task<Person> PersonAsync(string email, string firstName, string lastName)
    {
        var user = await Data.CreateUserAsync(email);
        var users = Scope.ServiceProvider.GetRequiredService<UsersContext>();
        user.FirstName = firstName;
        user.LastName = lastName;
        await users.SaveChangesAsync();
        return new Person(user, CreateAuthenticatedClient(user.UserId, user.UserGuid));
    }

    protected sealed record Setup(Person Landlord, int ApartmentId, string ApartmentTitle, Person Ana, Person Marko, Person Stranger);

    /// <summary>
    /// Lazar rents out "Stan na Vračaru"; Ana's application for it is approved and Marko is her roommate
    /// (so both can be tenants); Sara has nothing to do with it.
    /// </summary>
    protected async Task<Setup> SetupAsync(string prefix)
    {
        var landlord = await PersonAsync($"{prefix}-landlord@e2e.com", "Lazar", "Lazić");
        var apartment = await Data.CreateApartmentAsync(landlord.Id, title: "Stan na Vračaru", rent: 450, city: "Beograd");
        var ana = await PersonAsync($"{prefix}-ana@e2e.com", "Ana", "Anić");
        var marko = await PersonAsync($"{prefix}-marko@e2e.com", "Marko", "Marković");
        var stranger = await PersonAsync($"{prefix}-sara@e2e.com", "Sara", "Sarić");
        await Data.CreateApprovedApplicationAsync(ana.Id, apartment.ApartmentId);
        await RoommatesAsync(ana.Id, marko.Id);
        return new Setup(landlord, apartment.ApartmentId, apartment.Title, ana, marko, stranger);
    }

    protected async Task RoommatesAsync(int a, int b)
    {
        var roommates = Scope.ServiceProvider.GetRequiredService<RoommatesContext>();
        roommates.Pairings.Add(new RoommatePairing
        {
            RequesterUserId = a, PartnerUserId = b, Status = PairingStatus.Accepted,
            CreatedDate = DateTime.UtcNow, RespondedDate = DateTime.UtcNow,
        });
        await roommates.SaveChangesAsync();
    }

    // ── API helpers ───────────────────────────────────────────────────────────

    protected static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        return string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    protected static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    /// <summary>The business-rule code of a 422.</summary>
    protected static async Task<string?> BusinessCodeAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity, await response.Content.ReadAsStringAsync());
        return (await ReadAsync(response)).GetProperty("code").GetString();
    }

    /// <summary>Every validation code of a 400.</summary>
    protected static async Task<List<string>> ValidationCodesAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
        var body = await ReadAsync(response);
        return body.GetProperty("errors").EnumerateObject()
            .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()!))
            .ToList();
    }

    protected object TenancyInput(Setup s, params int[] tenants) => new
    {
        apartmentId = s.ApartmentId,
        tenantUserIds = tenants,
        monthlyRent = 30001m,
        rentDueDay = DueDaySoon,
        payeeName = "Lazar Lazić",
        payeeAccount = LandlordAccount,
        startDate = Today.ToString("yyyy-MM-dd"),
    };

    protected async Task<JsonElement> CreateTenancyAsync(Setup s, int? dueDay = null, decimal rent = 30001m, params int[] tenants)
    {
        var response = await s.Landlord.Client.PostAsJsonAsync("/api/v1/tenancies", new
        {
            apartmentId = s.ApartmentId,
            tenantUserIds = tenants.Length > 0 ? tenants : new[] { s.Ana.Id, s.Marko.Id },
            monthlyRent = rent,
            rentDueDay = dueDay ?? DueDaySoon,
            payeeName = "Lazar Lazić",
            payeeAccount = LandlordAccount,
            startDate = Today.ToString("yyyy-MM-dd"),
        });
        return await OkAsync(response);
    }

    protected static int IdOf(JsonElement tenancy) => tenancy.GetProperty("tenancyId").GetInt32();

    protected async Task<List<JsonElement>> BillsOfAsync(HttpClient client, int tenancyId)
    {
        var page = await OkAsync(await client.GetAsync($"/api/v1/tenancies/{tenancyId}/bills?pageSize=50"));
        return page.GetProperty("bills").EnumerateArray().ToList();
    }

    protected async Task<JsonElement> CreateBillAsync(HttpClient client, object input) =>
        await OkAsync(await client.PostAsJsonAsync("/api/v1/bills", input));

    protected static object CompanyBill(int tenancyId, decimal amount = 6000m, string? reference = "97-5501234", DateOnly? due = null) => new
    {
        tenancyId,
        kind = "Electricity",
        title = "Struja oktobar",
        amount,
        dueDate = (due ?? Today.AddDays(20)).ToString("yyyy-MM-dd"),
        payTo = "Company",
        payeeName = "EPS Snabdevanje",
        payeeAccount = CompanyAccount,
        paymentCode = "289",
        paymentModel = reference is null ? null : "97",
        paymentReference = reference?.Replace("97-", ""),
    };

    protected static object LandlordBill(int tenancyId, decimal amount, DateOnly? due = null) => new
    {
        tenancyId,
        kind = "Water",
        title = "Voda",
        amount,
        dueDate = (due ?? Today.AddDays(20)).ToString("yyyy-MM-dd"),
        payTo = "Landlord",
    };

    protected static JsonElement ShareOf(JsonElement bill, int payerId) =>
        bill.GetProperty("shares").EnumerateArray().Single(s => s.GetProperty("payer").GetProperty("userId").GetInt32() == payerId);

    protected static int ShareIdOf(JsonElement bill, int payerId) => ShareOf(bill, payerId).GetProperty("billShareId").GetInt32();

    protected async Task<JsonElement> BillAsync(HttpClient client, int billId) =>
        await OkAsync(await client.GetAsync($"/api/v1/bills/{billId}"));

    protected Task<HttpResponseMessage> PostAsync(HttpClient client, string url) => client.PostAsync(url, null);

    // ── what was stored ───────────────────────────────────────────────────────

    /// <summary>The notification keys a user got, oldest first.</summary>
    protected async Task<List<string>> NotificationKeysAsync(int userId)
    {
        using var scope = Fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationContext>();
        return await db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == userId && n.Key != null)
            .OrderBy(n => n.Id)
            .Select(n => n.Key!)
            .ToListAsync();
    }

    protected async Task RunBillingJobAsync(DateOnly day)
    {
        using var scope = Fixture.Factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<BillingDailyJob>().RunForDateAsync(day);
    }

    protected async Task<T> InBillingDbAsync<T>(Func<BillingContext, Task<T>> read)
    {
        using var scope = Fixture.Factory.Services.CreateScope();
        return await read(scope.ServiceProvider.GetRequiredService<BillingContext>());
    }
}
