using Lander.src.Modules.Billing.Models;

namespace Lander.src.Modules.Billing.Implementation;

/// <summary>
/// The monthly rent bill of a tenancy appears <see cref="BillingCalendar.RentLeadDays"/> days before
/// its due date and is split equally among the tenants. A tenancy entered today with a start date in
/// the past gets no bills for the months before today: nobody wants a year of rent billed at once.
/// </summary>
public static class RentBills
{
    /// <summary>The rent bills that should exist by <paramref name="today"/> and do not yet; they are added to the context, not saved.</summary>
    public static List<Bill> AddMissing(
        BillingContext db, Tenancy tenancy, IReadOnlySet<string> existingPeriods, DateOnly today, DateOnly createdOn, DateTime nowUtc)
    {
        var made = new List<Bill>();
        if (tenancy.Status != TenancyStatus.Active) return made;

        var tenants = tenancy.Members.Where(m => m.LeftDate is null).Select(m => m.UserId).OrderBy(id => id).ToList();
        if (tenants.Count == 0) return made;

        var firstDue = tenancy.StartDate > createdOn ? tenancy.StartDate : createdOn;
        var lastVisible = today.AddDays(BillingCalendar.RentLeadDays);

        for (var month = new DateOnly(firstDue.Year, firstDue.Month, 1); month <= lastVisible; month = month.AddMonths(1))
        {
            var due = BillingCalendar.RentDueDate(month.Year, month.Month, tenancy.RentDueDay);
            var period = BillingCalendar.Period(month.Year, month.Month);
            if (due < firstDue || due > lastVisible) continue;
            if (tenancy.EndDate is DateOnly end && due > end) continue;
            if (existingPeriods.Contains(period)) continue;

            var bill = new Bill
            {
                TenancyId = tenancy.TenancyId,
                Tenancy = tenancy,
                Kind = BillKind.Rent,
                Title = $"Kirija {month.Month:D2}/{month.Year}",
                Amount = tenancy.MonthlyRent,
                DueDate = due,
                Period = period,
                PayTo = BillPayee.Landlord,
                PayeeName = tenancy.PayeeName,
                PayeeAccount = tenancy.PayeeAccount,
                PaymentCode = "289",
                PaymentModel = "00",
                PaymentReference = $"{tenancy.TenancyId}-{month.Year:D4}{month.Month:D2}",
                Purpose = $"Kirija {month.Month:D2}/{month.Year}",
                CreatedByUserId = tenancy.LandlordUserId,
                CreatedDate = nowUtc,
            };
            var parts = BillingMath.Split(tenancy.MonthlyRent, tenants.Count);
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
            db.Bills.Add(bill);
            made.Add(bill);
        }
        return made;
    }
}
