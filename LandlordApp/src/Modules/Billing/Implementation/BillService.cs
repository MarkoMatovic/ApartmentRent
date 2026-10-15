using Lander.src.Common.Exceptions;
using Lander.src.Common.Validation;
using Lander.src.Modules.Billing.Dtos;
using Lander.src.Modules.Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Billing.Implementation;

public interface IBillService
{
    Task<BillPageDto> GetTenancyBillsAsync(int userId, int tenancyId, int page, int pageSize);
    Task<BillDto> GetAsync(int userId, int billId);
    Task<MyBillsDto> GetMineAsync(int userId);
    Task<BillingSummaryDto> GetSummaryAsync(int userId);

    Task<BillDto> CreateAsync(int userId, CreateBillInputDto input);
    Task<BillDto> UpdateAsync(int userId, int billId, UpdateBillInputDto input);
    /// <summary>Calls the bill off (kept in the history as cancelled).</summary>
    Task CancelAsync(int userId, int billId);

    Task<PaymentInstructionsDto> GetCompanyPaymentAsync(int userId, int billId);
    Task<BillDto> PayCompanyBillAsync(int userId, int billId, PayCompanyBillInputDto input);
    Task<BillDto> UndoCompanyPaymentAsync(int userId, int billId);

    Task<PaymentInstructionsDto> GetSharePaymentAsync(int userId, int shareId);
    Task<BillDto> MarkSharePaidAsync(int userId, int shareId);
    Task<BillDto> UnmarkShareAsync(int userId, int shareId);
    Task<BillDto> ConfirmShareAsync(int userId, int shareId);
    Task<BillDto> RejectShareAsync(int userId, int shareId);

    Task<BillingProfileDto> GetProfileAsync(int userId);
    Task<BillingProfileDto> SaveProfileAsync(int userId, BillingProfileInputDto input);
}

public sealed class BillService : IBillService
{
    public const int MaxPageSize = 50;

    private readonly BillingContext _db;
    private readonly BillingPeople _people;
    private readonly BillingNotifier _notifier;
    private readonly TimeProvider _time;

    public BillService(BillingContext db, BillingPeople people, BillingNotifier notifier, TimeProvider time)
    {
        _db = db;
        _people = people;
        _notifier = notifier;
        _time = time;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;
    private DateOnly Today => BillingCalendar.Today(_time);

    private IQueryable<Bill> Bills(bool tracking = false)
    {
        var query = _db.Bills
            .Include(b => b.Shares)
            .Include(b => b.Tenancy).ThenInclude(t => t.Members)
            .AsSplitQuery();
        return tracking ? query : query.AsNoTracking();
    }

    // ── reading ──────────────────────────────────────────────────────────────

    public async Task<BillPageDto> GetTenancyBillsAsync(int userId, int tenancyId, int page, int pageSize)
    {
        var tenancy = await _db.Tenancies.AsNoTracking().Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.TenancyId == tenancyId)
            ?? throw new NotFoundException("Tenancy", tenancyId);
        TenancyService.EnsureCanView(tenancy, userId);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var query = Bills().Where(b => b.TenancyId == tenancyId);
        var total = await query.CountAsync();
        var bills = await query
            .OrderByDescending(b => b.DueDate).ThenByDescending(b => b.BillId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return new BillPageDto { TotalCount = total, Page = page, PageSize = pageSize, Bills = await ToDtosAsync(bills, userId) };
    }

    public async Task<BillDto> GetAsync(int userId, int billId)
    {
        var bill = await Bills().FirstOrDefaultAsync(b => b.BillId == billId) ?? throw new NotFoundException("Bill", billId);
        TenancyService.EnsureCanView(bill.Tenancy, userId);
        return await ToDtoAsync(bill, userId);
    }

    public async Task<MyBillsDto> GetMineAsync(int userId)
    {
        var myShares = await Bills()
            .Where(b => b.CancelledAt == null && b.Shares.Any(s =>
                (s.PayerUserId == userId && s.Status != ShareStatus.Confirmed)
                || (s.ReceiverUserId == userId && s.Status == ShareStatus.MarkedPaid)))
            .ToListAsync();
        var companyBills = await Bills()
            .Where(b => b.CancelledAt == null && b.PayTo == BillPayee.Company && b.PaidAt == null
                        && b.Tenancy.Status == TenancyStatus.Active
                        && b.Tenancy.Members.Any(m => m.UserId == userId && m.LeftDate == null))
            .ToListAsync();

        var all = myShares.Concat(companyBills).DistinctBy(b => b.BillId).ToList();
        var people = await _people.LoadAsync(BillingMapper.PeopleOf(all));
        var today = Today;
        var result = new MyBillsDto();

        foreach (var bill in myShares)
        {
            var dto = BillingMapper.ToDto(bill, userId, people, today);
            foreach (var share in bill.Shares.Where(s => s.PayerUserId == userId && s.Status != ShareStatus.Confirmed))
            {
                result.ToPay.Add(new PaymentDueDto
                {
                    Type = "share",
                    BillShareId = share.BillShareId,
                    Bill = dto,
                    Amount = share.Amount,
                    Receiver = BillingPeople.Of(people, share.ReceiverUserId),
                    Status = BillingNames.Of(share.Status),
                });
            }
            foreach (var share in bill.Shares.Where(s => s.ReceiverUserId == userId && s.Status == ShareStatus.MarkedPaid))
            {
                result.ToConfirm.Add(new BillShareWithBillDto
                {
                    Share = BillingMapper.ToDto(share, userId, people, cancelled: false),
                    Bill = dto,
                });
            }
        }
        foreach (var bill in companyBills)
        {
            result.ToPay.Add(new PaymentDueDto
            {
                Type = "company_bill",
                Bill = BillingMapper.ToDto(bill, userId, people, today),
                Amount = bill.Amount,
                Status = "unpaid",
            });
        }

        result.ToPay = result.ToPay.OrderBy(p => p.Bill.DueDate).ThenBy(p => p.Bill.BillId).ToList();
        result.ToConfirm = result.ToConfirm.OrderBy(c => c.Share.MarkedPaidAt).ToList();
        return result;
    }

    public async Task<BillingSummaryDto> GetSummaryAsync(int userId)
    {
        var shares = await _db.BillShares.AsNoTracking()
            .Where(s => s.Bill.CancelledAt == null && s.PayerUserId == userId && s.Status == ShareStatus.Unpaid)
            .Select(s => s.Amount)
            .ToListAsync();
        var companyBills = await _db.Bills.AsNoTracking()
            .Where(b => b.CancelledAt == null && b.PayTo == BillPayee.Company && b.PaidAt == null
                        && b.Tenancy.Status == TenancyStatus.Active
                        && b.Tenancy.Members.Any(m => m.UserId == userId && m.LeftDate == null))
            .Select(b => b.Amount)
            .ToListAsync();
        var toConfirm = await _db.BillShares.AsNoTracking()
            .CountAsync(s => s.Bill.CancelledAt == null && s.ReceiverUserId == userId && s.Status == ShareStatus.MarkedPaid);

        return new BillingSummaryDto
        {
            ToPay = shares.Count + companyBills.Count,
            AmountToPay = shares.Sum() + companyBills.Sum(),
            ToConfirm = toConfirm,
        };
    }

    // ── entering bills ───────────────────────────────────────────────────────

    public async Task<BillDto> CreateAsync(int userId, CreateBillInputDto input)
    {
        var tenancy = await _db.Tenancies.Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.TenancyId == input.TenancyId)
            ?? throw new NotFoundException("Tenancy", input.TenancyId);
        var isLandlord = tenancy.LandlordUserId == userId;
        var isMember = tenancy.Members.Any(m => m.UserId == userId && m.LeftDate is null);
        if (!isLandlord && !isMember) throw new ForbiddenException("You are not part of this tenancy.");
        if (tenancy.Status != TenancyStatus.Active) throw new BusinessRuleException(BusinessErrorCodes.TenancyNotActive);
        if (input.PayTo == BillPayee.Landlord && !isLandlord) throw new BusinessRuleException(BusinessErrorCodes.OnlyLandlordCanBePaid);

        var tenants = tenancy.Members.Where(m => m.LeftDate is null).Select(m => m.UserId).OrderBy(id => id).ToList();
        if (tenants.Count == 0) throw new BusinessRuleException(BusinessErrorCodes.NoTenants);

        var toLandlord = input.PayTo == BillPayee.Landlord;
        var title = input.Title.Trim();
        var bill = new Bill
        {
            TenancyId = tenancy.TenancyId,
            Tenancy = tenancy,
            Kind = input.Kind,
            Title = title,
            Amount = input.Amount,
            DueDate = input.DueDate,
            PayTo = input.PayTo,
            PayeeName = toLandlord ? tenancy.PayeeName : input.PayeeName!.Trim(),
            PayeeAccount = toLandlord ? tenancy.PayeeAccount : SerbianAccount.Normalize(input.PayeeAccount)!,
            PaymentCode = string.IsNullOrEmpty(input.PaymentCode) ? "289" : input.PaymentCode,
            PaymentModel = string.IsNullOrEmpty(input.PaymentReference) ? null : (string.IsNullOrEmpty(input.PaymentModel) ? "00" : input.PaymentModel),
            PaymentReference = string.IsNullOrEmpty(input.PaymentReference) ? null : input.PaymentReference,
            Purpose = title.Length > 35 ? title[..35] : title,
            CreatedByUserId = userId,
            CreatedDate = Now,
        };
        if (toLandlord) AddLandlordShares(bill, tenancy, tenants);

        _db.Bills.Add(bill);
        await _db.SaveEntitiesAsync();

        await NotifyNewBillAsync(bill, userId);
        return await GetAsync(userId, bill.BillId);
    }

    public async Task<BillDto> UpdateAsync(int userId, int billId, UpdateBillInputDto input)
    {
        var bill = await LoadEditableAsync(userId, billId);
        var title = input.Title.Trim();
        var dueChanged = bill.DueDate != input.DueDate;
        bill.Title = title;
        if (bill.Kind != BillKind.Rent) bill.Purpose = title.Length > 35 ? title[..35] : title;
        bill.Amount = input.Amount;
        bill.DueDate = input.DueDate;
        if (dueChanged)
        {
            // A new due date earns new reminders.
            bill.DueSoonNotifiedAt = null;
            bill.OverdueNotifiedAt = null;
            foreach (var share in bill.Shares) { share.DueSoonNotifiedAt = null; share.OverdueNotifiedAt = null; }
        }
        var shares = bill.Shares.OrderBy(s => s.PayerUserId).ToList();
        if (shares.Count > 0)
        {
            var parts = BillingMath.Split(bill.Amount, shares.Count);
            for (var i = 0; i < shares.Count; i++) shares[i].Amount = parts[i];
        }
        await SaveAsync();

        var editor = await _notifier.NameOfAsync(userId);
        foreach (var share in shares.Where(s => s.PayerUserId != userId))
            await _notifier.NotifyAsync(share.PayerUserId, userId, "bill_changed", "Račun je izmenjen",
                $"{editor} je izmenio/la račun „{bill.Title}“. Vaš deo: {share.Amount:N2} RSD, rok {bill.DueDate:dd.MM.yyyy.}",
                new Dictionary<string, string>
                {
                    ["name"] = editor, ["title"] = bill.Title,
                    ["amount"] = BillingMath.Plain(share.Amount), ["due"] = BillingMath.Plain(bill.DueDate),
                },
                BillingNotifier.BillsPage);
        return await GetAsync(userId, billId);
    }

    public async Task CancelAsync(int userId, int billId)
    {
        var bill = await LoadEditableAsync(userId, billId);
        bill.CancelledAt = Now;
        await SaveAsync();
    }

    // ── a company bill: one payment to the company, then the roommates pay back ──

    public async Task<PaymentInstructionsDto> GetCompanyPaymentAsync(int userId, int billId)
    {
        var bill = await Bills().FirstOrDefaultAsync(b => b.BillId == billId) ?? throw new NotFoundException("Bill", billId);
        TenancyService.EnsureCanView(bill.Tenancy, userId);
        if (bill.PayTo != BillPayee.Company) throw new NotFoundException("Company payment", billId);

        return Instructions(new IpsPayment(
            bill.PayeeAccount, bill.PayeeName, bill.Amount, bill.PaymentCode, bill.Purpose,
            bill.PaymentModel, bill.PaymentReference, await _notifier.NameOfAsync(userId)));
    }

    public async Task<BillDto> PayCompanyBillAsync(int userId, int billId, PayCompanyBillInputDto input)
    {
        var bill = await Bills(tracking: true).FirstOrDefaultAsync(b => b.BillId == billId) ?? throw new NotFoundException("Bill", billId);
        var tenancy = bill.Tenancy;
        if (!tenancy.Members.Any(m => m.UserId == userId && m.LeftDate is null) || tenancy.Status != TenancyStatus.Active)
            throw new ForbiddenException("Only a tenant of the apartment can pay this bill.");
        if (bill.PayTo != BillPayee.Company) throw new BusinessRuleException(BusinessErrorCodes.CompanyPayeeRequired);
        if (bill.CancelledAt is not null) throw new NotFoundException("Bill", billId);
        if (bill.PaidAt is not null) throw new BusinessRuleException(BusinessErrorCodes.BillAlreadyPaid);

        var tenants = tenancy.Members.Where(m => m.LeftDate is null).Select(m => m.UserId).OrderBy(id => id).ToList();
        var others = tenants.Where(id => id != userId).ToList();
        var payerName = await _notifier.NameOfAsync(userId);

        if (others.Count > 0)
        {
            var (name, account) = await PayoutOfAsync(userId, input, payerName);
            var parts = BillingMath.Split(bill.Amount, tenants.Count);
            for (var i = 0; i < tenants.Count; i++)
            {
                if (tenants[i] == userId) continue; // the payer's own part is already paid
                bill.Shares.Add(new BillShare
                {
                    PayerUserId = tenants[i],
                    ReceiverUserId = userId,
                    Amount = parts[i],
                    ReceiverName = name,
                    ReceiverAccount = account,
                    Status = ShareStatus.Unpaid,
                });
            }
        }
        bill.PaidByUserId = userId;
        bill.PaidAt = Now;
        await SaveAsync();

        var values = new Dictionary<string, string>
        {
            ["name"] = payerName, ["title"] = bill.Title, ["amount"] = BillingMath.Plain(bill.Amount),
        };
        foreach (var share in bill.Shares.Where(s => s.ReceiverUserId == userId))
            await _notifier.NotifyAsync(share.PayerUserId, userId, "bill_company_paid", "Račun je plaćen",
                $"{payerName} je platio/la „{bill.Title}“ ({bill.Amount:N2} RSD). Vaš deo: {share.Amount:N2} RSD.",
                new Dictionary<string, string>(values) { ["share"] = BillingMath.Plain(share.Amount) },
                BillingNotifier.BillsPage);
        if (tenancy.LandlordUserId != userId)
            await _notifier.NotifyAsync(tenancy.LandlordUserId, userId, "bill_company_paid_landlord", "Račun je plaćen",
                $"{payerName} je platio/la „{bill.Title}“ ({bill.Amount:N2} RSD).",
                values, BillingNotifier.TenancyPage(tenancy.TenancyId), "success");

        return await GetAsync(userId, billId);
    }

    public async Task<BillDto> UndoCompanyPaymentAsync(int userId, int billId)
    {
        var bill = await Bills(tracking: true).FirstOrDefaultAsync(b => b.BillId == billId) ?? throw new NotFoundException("Bill", billId);
        TenancyService.EnsureCanView(bill.Tenancy, userId);
        if (bill.PaidAt is null) throw new BusinessRuleException(BusinessErrorCodes.BillNotPaid);
        if (bill.PaidByUserId != userId) throw new ForbiddenException("Only whoever paid the bill can undo it.");
        if (bill.Shares.Any(s => s.Status != ShareStatus.Unpaid)) throw new BusinessRuleException(BusinessErrorCodes.BillHasPayments);

        _db.BillShares.RemoveRange(bill.Shares);
        bill.Shares.Clear();
        bill.PaidByUserId = null;
        bill.PaidAt = null;
        await SaveAsync();
        return await GetAsync(userId, billId);
    }

    // ── shares: "paid" by the payer, "arrived" / "did not arrive" by the receiver ──

    public async Task<PaymentInstructionsDto> GetSharePaymentAsync(int userId, int shareId)
    {
        var share = await _db.BillShares.AsNoTracking().Include(s => s.Bill)
            .FirstOrDefaultAsync(s => s.BillShareId == shareId)
            ?? throw new NotFoundException("Payment", shareId);
        if (share.PayerUserId != userId && share.ReceiverUserId != userId)
            throw new ForbiddenException("This payment is not yours.");

        var bill = share.Bill;
        var reimbursement = bill.PayTo == BillPayee.Company;
        var purpose = reimbursement ? $"Udeo: {bill.Title}" : bill.Purpose;
        return Instructions(new IpsPayment(
            share.ReceiverAccount, share.ReceiverName, share.Amount, reimbursement ? "289" : bill.PaymentCode,
            purpose.Length > 35 ? purpose[..35] : purpose,
            reimbursement ? null : bill.PaymentModel, reimbursement ? null : bill.PaymentReference,
            await _notifier.NameOfAsync(share.PayerUserId)));
    }

    public async Task<BillDto> MarkSharePaidAsync(int userId, int shareId)
    {
        var (bill, share) = await LoadShareAsync(shareId);
        if (share.PayerUserId != userId) throw new ForbiddenException("Only the payer can say the money is sent.");
        if (share.Status == ShareStatus.Confirmed) throw new BusinessRuleException(BusinessErrorCodes.PaymentAlreadyConfirmed);
        if (share.Status == ShareStatus.MarkedPaid) return await GetAsync(userId, bill.BillId);

        share.Status = ShareStatus.MarkedPaid;
        share.MarkedPaidAt = Now;
        await SaveAsync();

        var payer = await _notifier.NameOfAsync(userId);
        await _notifier.NotifyAsync(share.ReceiverUserId, userId, "bill_marked_paid", "Stigla je uplata?",
            $"{payer} kaže da je uplatio/la {share.Amount:N2} RSD za „{bill.Title}“. Potvrdite kad novac stigne.",
            new Dictionary<string, string> { ["name"] = payer, ["title"] = bill.Title, ["amount"] = BillingMath.Plain(share.Amount) },
            BillingNotifier.BillsPage);
        return await GetAsync(userId, bill.BillId);
    }

    public async Task<BillDto> UnmarkShareAsync(int userId, int shareId)
    {
        var (bill, share) = await LoadShareAsync(shareId);
        if (share.PayerUserId != userId) throw new ForbiddenException("Only the payer can take it back.");
        if (share.Status == ShareStatus.Confirmed) throw new BusinessRuleException(BusinessErrorCodes.PaymentAlreadyConfirmed);
        if (share.Status != ShareStatus.MarkedPaid) throw new BusinessRuleException(BusinessErrorCodes.PaymentNotMarked);

        share.Status = ShareStatus.Unpaid;
        share.MarkedPaidAt = null;
        await SaveAsync();
        return await GetAsync(userId, bill.BillId);
    }

    public async Task<BillDto> ConfirmShareAsync(int userId, int shareId)
    {
        var (bill, share) = await LoadShareAsync(shareId);
        if (share.ReceiverUserId != userId) throw new ForbiddenException("Only the receiver can confirm the money arrived.");
        if (share.Status == ShareStatus.Confirmed) throw new BusinessRuleException(BusinessErrorCodes.PaymentAlreadyConfirmed);

        // The money can be seen on the account before the payer says anything: confirming an unmarked part is fine.
        share.Status = ShareStatus.Confirmed;
        share.ConfirmedAt = Now;
        share.MarkedPaidAt ??= share.ConfirmedAt;
        await SaveAsync();

        var receiver = await _notifier.NameOfAsync(userId);
        await _notifier.NotifyAsync(share.PayerUserId, userId, "bill_confirmed", "Uplata je potvrđena",
            $"{receiver} je potvrdio/la da je primio/la {share.Amount:N2} RSD za „{bill.Title}“.",
            new Dictionary<string, string> { ["name"] = receiver, ["title"] = bill.Title, ["amount"] = BillingMath.Plain(share.Amount) },
            BillingNotifier.BillsPage, "success");
        return await GetAsync(userId, bill.BillId);
    }

    public async Task<BillDto> RejectShareAsync(int userId, int shareId)
    {
        var (bill, share) = await LoadShareAsync(shareId);
        if (share.ReceiverUserId != userId) throw new ForbiddenException("Only the receiver can say the money did not arrive.");
        if (share.Status == ShareStatus.Confirmed) throw new BusinessRuleException(BusinessErrorCodes.PaymentAlreadyConfirmed);
        if (share.Status != ShareStatus.MarkedPaid) throw new BusinessRuleException(BusinessErrorCodes.PaymentNotMarked);

        share.Status = ShareStatus.Unpaid;
        share.MarkedPaidAt = null;
        await SaveAsync();

        var receiver = await _notifier.NameOfAsync(userId);
        await _notifier.NotifyAsync(share.PayerUserId, userId, "bill_rejected", "Uplata nije stigla",
            $"{receiver} još ne vidi vašu uplatu od {share.Amount:N2} RSD za „{bill.Title}“. Proverite nalog u banci.",
            new Dictionary<string, string> { ["name"] = receiver, ["title"] = bill.Title, ["amount"] = BillingMath.Plain(share.Amount) },
            BillingNotifier.BillsPage, "warning");
        return await GetAsync(userId, bill.BillId);
    }

    // ── where roommates pay back ─────────────────────────────────────────────

    public async Task<BillingProfileDto> GetProfileAsync(int userId)
    {
        var profile = await _db.BillingProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
        return new BillingProfileDto
        {
            PayoutName = profile?.PayoutName,
            PayoutAccount = profile is null ? null : SerbianAccount.Format(profile.PayoutAccount),
        };
    }

    public async Task<BillingProfileDto> SaveProfileAsync(int userId, BillingProfileInputDto input)
    {
        await UpsertProfileAsync(userId, input.PayoutName.Trim(), SerbianAccount.Normalize(input.PayoutAccount)!);
        await _db.SaveEntitiesAsync();
        return await GetProfileAsync(userId);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void AddLandlordShares(Bill bill, Tenancy tenancy, List<int> tenants)
    {
        var parts = BillingMath.Split(bill.Amount, tenants.Count);
        for (var i = 0; i < tenants.Count; i++)
        {
            bill.Shares.Add(new BillShare
            {
                PayerUserId = tenants[i],
                ReceiverUserId = tenancy.LandlordUserId,
                Amount = parts[i],
                ReceiverName = tenancy.PayeeName,
                ReceiverAccount = tenancy.PayeeAccount,
                Status = ShareStatus.Unpaid,
            });
        }
    }

    private async Task<(string Name, string Account)> PayoutOfAsync(int userId, PayCompanyBillInputDto input, string fallbackName)
    {
        var given = SerbianAccount.Normalize(input.PayoutAccount);
        if (given is not null)
        {
            var name = string.IsNullOrWhiteSpace(input.PayoutName) ? fallbackName : input.PayoutName.Trim();
            await UpsertProfileAsync(userId, name, given);
            return (name, given);
        }
        var saved = await _db.BillingProfiles.FirstOrDefaultAsync(p => p.UserId == userId)
            ?? throw new BusinessRuleException(BusinessErrorCodes.PayoutAccountRequired);
        return (saved.PayoutName, saved.PayoutAccount);
    }

    private async Task UpsertProfileAsync(int userId, string name, string account)
    {
        var profile = await _db.BillingProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile is null)
        {
            profile = new BillingProfile { UserId = userId };
            _db.BillingProfiles.Add(profile);
        }
        profile.PayoutName = name.Length > 70 ? name[..70] : name;
        profile.PayoutAccount = account;
        profile.ModifiedDate = Now;
    }

    private async Task<Bill> LoadEditableAsync(int userId, int billId)
    {
        var bill = await Bills(tracking: true).FirstOrDefaultAsync(b => b.BillId == billId) ?? throw new NotFoundException("Bill", billId);
        var tenancy = bill.Tenancy;
        var isLandlord = tenancy.LandlordUserId == userId;
        var isCreatorMember = bill.Kind != BillKind.Rent && bill.CreatedByUserId == userId
            && tenancy.Members.Any(m => m.UserId == userId && m.LeftDate is null);
        if (!isLandlord && !isCreatorMember) throw new ForbiddenException("Only the landlord or whoever entered the bill can change it.");
        if (bill.CancelledAt is not null) throw new NotFoundException("Bill", billId);
        if (BillState.HasPayments(bill)) throw new BusinessRuleException(BusinessErrorCodes.BillHasPayments);
        return bill;
    }

    private async Task<(Bill Bill, BillShare Share)> LoadShareAsync(int shareId)
    {
        var billId = await _db.BillShares.AsNoTracking()
            .Where(s => s.BillShareId == shareId)
            .Select(s => (int?)s.BillId)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Payment", shareId);
        var bill = await Bills(tracking: true).FirstAsync(b => b.BillId == billId);
        if (bill.CancelledAt is not null) throw new NotFoundException("Payment", shareId);
        return (bill, bill.Shares.First(s => s.BillShareId == shareId));
    }

    /// <summary>Saves; another request having changed the same payment a moment ago is told as "reload and try again".</summary>
    private async Task SaveAsync()
    {
        try
        {
            await _db.SaveEntitiesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleException(BusinessErrorCodes.PaymentChanged);
        }
    }

    private async Task NotifyNewBillAsync(Bill bill, int creatorId)
    {
        var creator = await _notifier.NameOfAsync(creatorId);
        var tenancy = bill.Tenancy;
        var due = BillingMath.Plain(bill.DueDate);
        if (bill.PayTo == BillPayee.Landlord)
        {
            foreach (var share in bill.Shares)
                await _notifier.NotifyAsync(share.PayerUserId, creatorId, "bill_new", $"Novi račun: {bill.Title}",
                    $"Vaš deo: {share.Amount:N2} RSD, rok {bill.DueDate:dd.MM.yyyy.}",
                    new Dictionary<string, string>
                    {
                        ["name"] = creator, ["title"] = bill.Title, ["amount"] = BillingMath.Plain(share.Amount), ["due"] = due,
                    },
                    BillingNotifier.BillsPage);
            return;
        }

        var recipients = tenancy.Members.Where(m => m.LeftDate is null).Select(m => m.UserId)
            .Append(tenancy.LandlordUserId)
            .Where(id => id != creatorId)
            .Distinct();
        foreach (var recipient in recipients)
            await _notifier.NotifyAsync(recipient, creatorId, "bill_company_new", $"Novi račun: {bill.Title}",
                $"{creator} je uneo/la račun od {bill.Amount:N2} RSD ({bill.PayeeName}), rok {bill.DueDate:dd.MM.yyyy.}",
                new Dictionary<string, string>
                {
                    ["name"] = creator, ["title"] = bill.Title, ["amount"] = BillingMath.Plain(bill.Amount),
                    ["due"] = due, ["payee"] = bill.PayeeName,
                },
                recipient == tenancy.LandlordUserId ? BillingNotifier.TenancyPage(tenancy.TenancyId) : BillingNotifier.BillsPage);
    }

    private static PaymentInstructionsDto Instructions(IpsPayment payment)
    {
        var payload = IpsQr.Payload(payment);
        return new PaymentInstructionsDto
        {
            PayeeName = payment.PayeeName,
            PayeeAccount = SerbianAccount.Format(payment.PayeeAccount),
            Amount = payment.Amount,
            PaymentCode = payment.PaymentCode,
            Purpose = payment.Purpose,
            PaymentModel = payment.PaymentModel,
            PaymentReference = payment.PaymentReference,
            QrPayload = payload,
            QrCodeDataUrl = IpsQr.QrCodeDataUrl(payload),
        };
    }

    private async Task<BillDto> ToDtoAsync(Bill bill, int userId) => (await ToDtosAsync(new List<Bill> { bill }, userId))[0];

    private async Task<List<BillDto>> ToDtosAsync(List<Bill> bills, int userId)
    {
        var people = await _people.LoadAsync(BillingMapper.PeopleOf(bills));
        var today = Today;
        return bills.Select(b => BillingMapper.ToDto(b, userId, people, today)).ToList();
    }
}
