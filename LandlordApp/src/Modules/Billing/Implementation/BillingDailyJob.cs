using Lander.src.Modules.Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Billing.Implementation;

/// <summary>
/// Hangfire recurring job, every morning: makes the month's rent bills that are due within
/// <see cref="BillingCalendar.RentLeadDays"/> days, reminds whoever still has to pay
/// <see cref="BillingCalendar.ReminderDaysBefore"/> days before the due date, and once more when the
/// due date has passed (telling the receiver too). Every reminder goes out once; running the job twice
/// a day changes nothing.
/// </summary>
public sealed class BillingDailyJob
{
    private readonly BillingContext _db;
    private readonly BillingNotifier _notifier;
    private readonly TimeProvider _time;
    private readonly ILogger<BillingDailyJob> _logger;

    public BillingDailyJob(BillingContext db, BillingNotifier notifier, TimeProvider time, ILogger<BillingDailyJob> logger)
    {
        _db = db;
        _notifier = notifier;
        _time = time;
        _logger = logger;
    }

    public Task RunAsync() => RunForDateAsync(BillingCalendar.Today(_time));

    public async Task RunForDateAsync(DateOnly today)
    {
        var made = await MakeRentBillsAsync(today);
        var reminded = await SendRemindersAsync(today);
        _logger.LogInformation("Billing: {Bills} rent bills made, {Reminders} reminders sent for {Day}", made, reminded, today);
    }

    public async Task<int> MakeRentBillsAsync(DateOnly today)
    {
        var tenancies = await _db.Tenancies
            .Include(t => t.Members)
            .Where(t => t.Status == TenancyStatus.Active)
            .ToListAsync();
        var periods = await _db.Bills.AsNoTracking()
            .Where(b => b.Period != null && b.Tenancy.Status == TenancyStatus.Active)
            .Select(b => new { b.TenancyId, b.Period })
            .ToListAsync();
        var byTenancy = periods.GroupBy(p => p.TenancyId).ToDictionary(g => g.Key, g => g.Select(p => p.Period!).ToHashSet());

        var now = _time.GetUtcNow().UtcDateTime;
        var made = new List<Bill>();
        foreach (var tenancy in tenancies)
        {
            var existing = byTenancy.GetValueOrDefault(tenancy.TenancyId) ?? new HashSet<string>();
            var createdOn = BillingCalendar.Today(new FixedTime(tenancy.CreatedDate));
            made.AddRange(RentBills.AddMissing(_db, tenancy, existing, today, createdOn, now));
        }
        if (made.Count == 0) return 0;

        try
        {
            await _db.SaveEntitiesAsync();
        }
        catch (DbUpdateException ex) when (TenancyService.IsUniqueViolation(ex))
        {
            // Another run made them a moment ago.
            _logger.LogInformation("Billing: rent bills were already made by another run");
            return 0;
        }

        foreach (var bill in made)
            await _notifier.NotifyRentBillAsync(bill);
        return made.Count;
    }

    public async Task<int> SendRemindersAsync(DateOnly today)
    {
        var soonUntil = today.AddDays(BillingCalendar.ReminderDaysBefore);
        var now = _time.GetUtcNow().UtcDateTime;
        var sent = 0;

        // Parts still unpaid (a part marked as paid waits for the receiver, not for the payer).
        var dueSoon = await _db.BillShares.Include(s => s.Bill)
            .Where(s => s.Status == ShareStatus.Unpaid && s.DueSoonNotifiedAt == null && s.Bill.CancelledAt == null
                        && s.Bill.DueDate >= today && s.Bill.DueDate <= soonUntil)
            .ToListAsync();
        foreach (var share in dueSoon)
        {
            share.DueSoonNotifiedAt = now;
            await RemindPayerAsync(share.PayerUserId, share.ReceiverUserId, share.Bill, share.Amount, overdue: false);
            sent++;
        }

        var overdue = await _db.BillShares.Include(s => s.Bill)
            .Where(s => s.Status == ShareStatus.Unpaid && s.OverdueNotifiedAt == null && s.Bill.CancelledAt == null
                        && s.Bill.DueDate < today)
            .ToListAsync();
        foreach (var share in overdue)
        {
            share.OverdueNotifiedAt = now;
            share.DueSoonNotifiedAt ??= now; // nothing to remind "soon" about any more
            await RemindPayerAsync(share.PayerUserId, share.ReceiverUserId, share.Bill, share.Amount, overdue: true);
            var payer = await _notifier.NameOfAsync(share.PayerUserId);
            await _notifier.NotifyAsync(share.ReceiverUserId, share.PayerUserId, "bill_overdue_receiver", $"Uplata kasni: {share.Bill.Title}",
                $"{payer} još nije platio/la {share.Amount:N2} RSD (rok {share.Bill.DueDate:dd.MM.yyyy.}).",
                Values(share.Bill, share.Amount, payer), BillingNotifier.BillsPage, "warning");
            sent++;
        }

        // Company bills nobody has paid: every tenant is reminded, and the landlord when it is late.
        var unpaidCompany = await _db.Bills.Include(b => b.Tenancy).ThenInclude(t => t.Members)
            .Where(b => b.PayTo == BillPayee.Company && b.PaidAt == null && b.CancelledAt == null
                        && b.Tenancy.Status == TenancyStatus.Active
                        && ((b.DueSoonNotifiedAt == null && b.DueDate >= today && b.DueDate <= soonUntil)
                            || (b.OverdueNotifiedAt == null && b.DueDate < today)))
            .ToListAsync();
        foreach (var bill in unpaidCompany)
        {
            var isOverdue = bill.DueDate < today;
            if (isOverdue) { bill.OverdueNotifiedAt = now; bill.DueSoonNotifiedAt ??= now; }
            else bill.DueSoonNotifiedAt = now;

            foreach (var tenant in bill.Tenancy.Members.Where(m => m.LeftDate is null))
                await RemindPayerAsync(tenant.UserId, bill.Tenancy.LandlordUserId, bill, bill.Amount, isOverdue);
            if (isOverdue)
                await _notifier.NotifyAsync(bill.Tenancy.LandlordUserId, 0, "bill_company_overdue", $"Račun nije plaćen: {bill.Title}",
                    $"„{bill.Title}“ ({bill.Amount:N2} RSD) još nije plaćen, rok je bio {bill.DueDate:dd.MM.yyyy.}",
                    Values(bill, bill.Amount, null), BillingNotifier.TenancyPage(bill.TenancyId), "warning");
            sent++;
        }

        await _db.SaveEntitiesAsync();
        return sent;
    }

    private async Task RemindPayerAsync(int payerId, int senderId, Bill bill, decimal amount, bool overdue)
    {
        if (overdue)
            await _notifier.NotifyAsync(payerId, senderId, "bill_overdue", $"Rok je prošao: {bill.Title}",
                $"{amount:N2} RSD je trebalo platiti do {bill.DueDate:dd.MM.yyyy.}",
                Values(bill, amount, null), BillingNotifier.BillsPage, "warning");
        else
            await _notifier.NotifyAsync(payerId, senderId, "bill_due_soon", $"Rok se bliži: {bill.Title}",
                $"{amount:N2} RSD treba platiti do {bill.DueDate:dd.MM.yyyy.}",
                Values(bill, amount, null), BillingNotifier.BillsPage);
        await _notifier.EmailReminderAsync(payerId, bill.Title, amount, bill.DueDate, overdue);
    }

    private static Dictionary<string, string> Values(Bill bill, decimal amount, string? name)
    {
        var values = new Dictionary<string, string>
        {
            ["title"] = bill.Title, ["amount"] = BillingMath.Plain(amount), ["due"] = BillingMath.Plain(bill.DueDate),
        };
        if (name is not null) values["name"] = name;
        return values;
    }

    /// <summary>A clock stopped at one moment: the Serbian calendar day a tenancy was entered on.</summary>
    private sealed class FixedTime(DateTime utc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
    }
}
