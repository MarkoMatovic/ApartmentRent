using System.Globalization;
using Lander.src.Modules.Billing.Dtos;
using Lander.src.Modules.Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace Lander.src.Modules.Billing.Implementation;

/// <summary>Names and pictures of the people on bills, read in one query per request.</summary>
public sealed class BillingPeople
{
    private readonly UsersContext _users;

    public BillingPeople(UsersContext users)
    {
        _users = users;
    }

    public async Task<Dictionary<int, BillingPersonDto>> LoadAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new();
        return await _users.Users.AsNoTracking()
            .Where(u => ids.Contains(u.UserId))
            .Select(u => new BillingPersonDto
            {
                UserId = u.UserId,
                Name = (u.FirstName + " " + u.LastName).Trim(),
                ProfilePicture = u.ProfilePicture,
            })
            .ToDictionaryAsync(p => p.UserId);
    }

    public static BillingPersonDto Of(Dictionary<int, BillingPersonDto> people, int userId) =>
        people.TryGetValue(userId, out var person) ? person : new BillingPersonDto { UserId = userId, Name = "—" };
}

public static class BillingMath
{
    /// <summary>
    /// Splits an amount into <paramref name="parts"/> equal parts in para: the parts always add up to the
    /// amount, the first ones getting the leftover para (100,01 among 3 → 33,34 + 33,34 + 33,33).
    /// </summary>
    public static List<decimal> Split(decimal amount, int parts)
    {
        if (parts <= 0) throw new ArgumentOutOfRangeException(nameof(parts));
        var totalPara = (long)decimal.Round(amount * 100, 0, MidpointRounding.AwayFromZero);
        var basePara = totalPara / parts;
        var leftover = totalPara - basePara * parts;
        return Enumerable.Range(0, parts)
            .Select(i => (basePara + (i < leftover ? 1 : 0)) / 100m)
            .ToList();
    }

    /// <summary>For notification values: "1234.5" (the browser writes it the user's way).</summary>
    public static string Plain(decimal amount) => amount.ToString("0.##", CultureInfo.InvariantCulture);

    public static string Plain(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public static class BillState
{
    public static bool HasPayments(Bill bill) =>
        bill.PaidAt is not null || bill.Shares.Any(s => s.Status != ShareStatus.Unpaid);

    public static string Status(Bill bill)
    {
        if (bill.CancelledAt is not null) return "cancelled";
        if (bill.PayTo == BillPayee.Company && bill.PaidAt is null) return "open";
        if (bill.Shares.All(s => s.Status == ShareStatus.Confirmed)) return "paid";
        if (bill.PayTo == BillPayee.Company || bill.Shares.Any(s => s.Status != ShareStatus.Unpaid)) return "partly_paid";
        return "open";
    }

    public static bool IsOverdue(Bill bill, DateOnly today)
    {
        var status = Status(bill);
        return status is "open" or "partly_paid" && bill.DueDate < today;
    }
}

public static class BillingMapper
{
    public static TenancyDto ToDto(Tenancy tenancy, int viewerId, Dictionary<int, BillingPersonDto> people, int openBills)
    {
        return new TenancyDto
        {
            TenancyId = tenancy.TenancyId,
            ApartmentId = tenancy.ApartmentId,
            ApartmentTitle = tenancy.ApartmentTitle,
            Landlord = BillingPeople.Of(people, tenancy.LandlordUserId),
            IsLandlord = tenancy.LandlordUserId == viewerId,
            MonthlyRent = tenancy.MonthlyRent,
            RentDueDay = tenancy.RentDueDay,
            PayeeName = tenancy.PayeeName,
            PayeeAccount = SerbianAccount.Format(tenancy.PayeeAccount),
            StartDate = tenancy.StartDate,
            EndDate = tenancy.EndDate,
            Status = BillingNames.Of(tenancy.Status),
            OpenBills = openBills,
            Members = tenancy.Members
                .OrderBy(m => m.LeftDate.HasValue).ThenBy(m => m.JoinedDate)
                .Select(m =>
                {
                    var person = BillingPeople.Of(people, m.UserId);
                    return new TenancyMemberDto
                    {
                        UserId = m.UserId,
                        Name = person.Name,
                        ProfilePicture = person.ProfilePicture,
                        JoinedDate = m.JoinedDate,
                        LeftDate = m.LeftDate,
                    };
                })
                .ToList(),
        };
    }

    public static BillDto ToDto(Bill bill, int viewerId, Dictionary<int, BillingPersonDto> people, DateOnly today)
    {
        var tenancy = bill.Tenancy;
        var isLandlord = tenancy.LandlordUserId == viewerId;
        var isActiveMember = tenancy.Status == TenancyStatus.Active
            && tenancy.Members.Any(m => m.UserId == viewerId && m.LeftDate is null);
        var cancelled = bill.CancelledAt is not null;
        var hasPayments = BillState.HasPayments(bill);

        return new BillDto
        {
            BillId = bill.BillId,
            TenancyId = bill.TenancyId,
            ApartmentTitle = tenancy.ApartmentTitle,
            TenantCount = tenancy.Members.Count(m => m.LeftDate is null),
            Kind = BillingNames.Of(bill.Kind),
            Title = bill.Title,
            Amount = bill.Amount,
            DueDate = bill.DueDate,
            Period = bill.Period,
            PayTo = BillingNames.Of(bill.PayTo),
            PayeeName = bill.PayeeName,
            PayeeAccount = SerbianAccount.Format(bill.PayeeAccount),
            PaymentCode = bill.PaymentCode,
            PaymentModel = bill.PaymentModel,
            PaymentReference = bill.PaymentReference,
            Purpose = bill.Purpose,
            CreatedBy = BillingPeople.Of(people, bill.CreatedByUserId),
            CreatedDate = bill.CreatedDate,
            Status = BillState.Status(bill),
            IsOverdue = BillState.IsOverdue(bill, today),
            PaidBy = bill.PaidByUserId is int paidBy ? BillingPeople.Of(people, paidBy) : null,
            PaidAt = bill.PaidAt,
            CanPayCompany = !cancelled && bill.PayTo == BillPayee.Company && bill.PaidAt is null && isActiveMember,
            CanUndoCompanyPayment = !cancelled && bill.PaidByUserId == viewerId
                && bill.Shares.All(s => s.Status == ShareStatus.Unpaid),
            CanEdit = !cancelled && !hasPayments
                && (isLandlord || (bill.Kind != BillKind.Rent && bill.CreatedByUserId == viewerId && isActiveMember)),
            Shares = bill.Shares
                .OrderBy(s => s.PayerUserId)
                .Select(s => ToDto(s, viewerId, people, cancelled))
                .ToList(),
        };
    }

    public static BillShareDto ToDto(BillShare share, int viewerId, Dictionary<int, BillingPersonDto> people, bool cancelled) => new()
    {
        BillShareId = share.BillShareId,
        Payer = BillingPeople.Of(people, share.PayerUserId),
        Receiver = BillingPeople.Of(people, share.ReceiverUserId),
        Amount = share.Amount,
        Status = BillingNames.Of(share.Status),
        MarkedPaidAt = share.MarkedPaidAt,
        ConfirmedAt = share.ConfirmedAt,
        CanMarkPaid = !cancelled && share.PayerUserId == viewerId && share.Status == ShareStatus.Unpaid,
        CanConfirm = !cancelled && share.ReceiverUserId == viewerId && share.Status != ShareStatus.Confirmed,
    };

    /// <summary>Everyone a list of bills mentions.</summary>
    public static IEnumerable<int> PeopleOf(IEnumerable<Bill> bills) =>
        bills.SelectMany(b => new[] { b.CreatedByUserId, b.Tenancy.LandlordUserId }
            .Concat(b.PaidByUserId is int p ? new[] { p } : Array.Empty<int>())
            .Concat(b.Shares.SelectMany(s => new[] { s.PayerUserId, s.ReceiverUserId })));
}
