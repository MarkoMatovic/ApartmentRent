namespace Lander.src.Modules.Billing.Models;

public enum TenancyStatus
{
    Active = 0,
    Ended = 1,
}

/// <summary>
/// An apartment rented out to one or more people on the site: the rent, when it is due and where it is
/// paid. Monthly rent bills are made from it; utility bills belong to it. The money never goes through
/// the site: every payment is a bank transfer (IPS QR) straight to the one who receives it.
/// </summary>
public class Tenancy
{
    public int TenancyId { get; set; }
    public int ApartmentId { get; set; }
    public int LandlordUserId { get; set; }
    /// <summary>The listing title when the tenancy was made: bills stay readable if the listing goes.</summary>
    public string ApartmentTitle { get; set; } = null!;

    /// <summary>Dinars: IPS payments are only in RSD.</summary>
    public decimal MonthlyRent { get; set; }
    /// <summary>Day of the month the rent is due, 1–28 (so that every month has it).</summary>
    public int RentDueDay { get; set; }
    /// <summary>Who receives the rent, as the bank knows them (shown in the payer's banking app).</summary>
    public string PayeeName { get; set; } = null!;
    /// <summary>18 digits, without dashes.</summary>
    public string PayeeAccount { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public TenancyStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }

    public List<TenancyMember> Members { get; set; } = new();
    public List<Bill> Bills { get; set; } = new();
}

/// <summary>A tenant of a tenancy. Leaving keeps the row (with the date) so the history stays.</summary>
public class TenancyMember
{
    public int TenancyMemberId { get; set; }
    public int TenancyId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedDate { get; set; }
    public DateTime? LeftDate { get; set; }

    public Tenancy Tenancy { get; set; } = null!;
}

public enum BillKind
{
    Rent = 0,
    Electricity = 1,
    Water = 2,
    Infostan = 3,
    Internet = 4,
    Heating = 5,
    Gas = 6,
    Other = 9,
}

public enum BillPayee
{
    /// <summary>Every tenant pays their part to the landlord (rent, or utilities the landlord pays and passes on).</summary>
    Landlord = 0,
    /// <summary>Paid in one payment straight to the company (EPS, Infostan...); the roommates then pay back whoever paid.</summary>
    Company = 1,
}

public class Bill
{
    public int BillId { get; set; }
    public int TenancyId { get; set; }
    public BillKind Kind { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    /// <summary>"2026-10" for the rent of October 2026: one rent bill per month.</summary>
    public string? Period { get; set; }

    public BillPayee PayTo { get; set; }
    public string PayeeName { get; set; } = null!;
    public string PayeeAccount { get; set; } = null!;
    /// <summary>"Šifra plaćanja", three digits.</summary>
    public string PaymentCode { get; set; } = "289";
    /// <summary>"Model" of the reference ("97", "00"...), when the bill has one.</summary>
    public string? PaymentModel { get; set; }
    /// <summary>"Poziv na broj".</summary>
    public string? PaymentReference { get; set; }
    /// <summary>"Svrha plaćanja", at most 35 characters.</summary>
    public string Purpose { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public DateTime CreatedDate { get; set; }

    // A company bill is paid once, by one tenant.
    public int? PaidByUserId { get; set; }
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Called off (a month without rent, a bill entered by mistake). Kept, not deleted: the history shows
    /// it, and the monthly job does not make the same month's rent again.
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    // Reminders for a company bill nobody has paid yet (shares keep their own).
    public DateTime? DueSoonNotifiedAt { get; set; }
    public DateTime? OverdueNotifiedAt { get; set; }

    public Tenancy Tenancy { get; set; } = null!;
    public List<BillShare> Shares { get; set; } = new();
}

public enum ShareStatus
{
    Unpaid = 0,
    /// <summary>The payer says the money is sent.</summary>
    MarkedPaid = 1,
    /// <summary>The receiver says the money arrived.</summary>
    Confirmed = 2,
}

/// <summary>
/// What one person owes on a bill and to whom: a tenant's part of the rent to the landlord, or a
/// roommate's part of a company bill to the tenant who paid it.
/// </summary>
public class BillShare
{
    public int BillShareId { get; set; }
    public int BillId { get; set; }
    public int PayerUserId { get; set; }
    public int ReceiverUserId { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Where the money goes, as when the share was made (the QR code is built from it).</summary>
    public string ReceiverName { get; set; } = null!;
    public string ReceiverAccount { get; set; } = null!;

    public ShareStatus Status { get; set; }
    public DateTime? MarkedPaidAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? DueSoonNotifiedAt { get; set; }
    public DateTime? OverdueNotifiedAt { get; set; }

    public Bill Bill { get; set; } = null!;
}

/// <summary>The account a user is paid back to (roommates returning their part of a bill), remembered for next time.</summary>
public class BillingProfile
{
    public int UserId { get; set; }
    public string PayoutName { get; set; } = null!;
    public string PayoutAccount { get; set; } = null!;
    public DateTime ModifiedDate { get; set; }
}
