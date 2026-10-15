using Lander.src.Modules.Billing.Models;

namespace Lander.src.Modules.Billing.Dtos;

public class BillingPersonDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
}

public class TenancyMemberDto : BillingPersonDto
{
    public DateTime JoinedDate { get; set; }
    public DateTime? LeftDate { get; set; }
}

public class TenancyDto
{
    public int TenancyId { get; set; }
    public int ApartmentId { get; set; }
    public string ApartmentTitle { get; set; } = string.Empty;
    public BillingPersonDto Landlord { get; set; } = new();
    /// <summary>The signed-in user rents this apartment out (and so sets the rent and confirms payments).</summary>
    public bool IsLandlord { get; set; }
    public decimal MonthlyRent { get; set; }
    public int RentDueDay { get; set; }
    public string PayeeName { get; set; } = string.Empty;
    /// <summary>Written the usual way, 160-0000000123456-78.</summary>
    public string PayeeAccount { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<TenancyMemberDto> Members { get; set; } = new();
    /// <summary>Unpaid or not yet confirmed bills of the tenancy.</summary>
    public int OpenBills { get; set; }
}

public class TenancyCandidateDto : BillingPersonDto
{
    /// <summary>"application" (approved application for this apartment) or "roommate" (roommate of such an applicant).</summary>
    public string Reason { get; set; } = string.Empty;
}

public class BillShareDto
{
    public int BillShareId { get; set; }
    public BillingPersonDto Payer { get; set; } = new();
    public BillingPersonDto Receiver { get; set; } = new();
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? MarkedPaidAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public bool CanMarkPaid { get; set; }
    public bool CanConfirm { get; set; }
}

public class BillDto
{
    public int BillId { get; set; }
    public int TenancyId { get; set; }
    public string ApartmentTitle { get; set; } = string.Empty;
    /// <summary>Tenants living there now: a company bill is split among them.</summary>
    public int TenantCount { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public string? Period { get; set; }
    public string PayTo { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
    public string PaymentCode { get; set; } = string.Empty;
    public string? PaymentModel { get; set; }
    public string? PaymentReference { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public BillingPersonDto CreatedBy { get; set; } = new();
    public DateTime CreatedDate { get; set; }

    /// <summary>"open", "partly_paid", "paid" (everything confirmed).</summary>
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }

    // Company bill: paid once, by one tenant.
    public BillingPersonDto? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public bool CanPayCompany { get; set; }
    public bool CanUndoCompanyPayment { get; set; }

    public bool CanEdit { get; set; }
    public List<BillShareDto> Shares { get; set; } = new();
}

/// <summary>One thing the user has to pay: a share, or a whole company bill nobody has paid yet.</summary>
public class PaymentDueDto
{
    /// <summary>"share" or "company_bill".</summary>
    public string Type { get; set; } = string.Empty;
    public int? BillShareId { get; set; }
    public BillDto Bill { get; set; } = new();
    public decimal Amount { get; set; }
    public BillingPersonDto? Receiver { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class MyBillsDto
{
    /// <summary>What the user still has to pay (marked ones included, until they are confirmed).</summary>
    public List<PaymentDueDto> ToPay { get; set; } = new();
    /// <summary>Payments to the user waiting for "arrived" / "did not arrive".</summary>
    public List<BillShareWithBillDto> ToConfirm { get; set; } = new();
}

public class BillShareWithBillDto
{
    public BillShareDto Share { get; set; } = new();
    public BillDto Bill { get; set; } = new();
}

public class BillingSummaryDto
{
    public int ToPay { get; set; }
    public int ToConfirm { get; set; }
    public decimal AmountToPay { get; set; }
}

/// <summary>Everything a payment order needs, the IPS QR code for a banking app, and the same as text.</summary>
public class PaymentInstructionsDto
{
    public string PayeeName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RSD";
    public string PaymentCode { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string? PaymentModel { get; set; }
    public string? PaymentReference { get; set; }
    public string QrPayload { get; set; } = string.Empty;
    public string QrCodeDataUrl { get; set; } = string.Empty;
}

public class BillingProfileDto
{
    public string? PayoutName { get; set; }
    public string? PayoutAccount { get; set; }
}

public class BillPageDto
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<BillDto> Bills { get; set; } = new();
}

public static class BillingNames
{
    public static string Of(TenancyStatus status) => status == TenancyStatus.Active ? "active" : "ended";

    public static string Of(ShareStatus status) => status switch
    {
        ShareStatus.Unpaid => "unpaid",
        ShareStatus.MarkedPaid => "marked_paid",
        _ => "confirmed",
    };

    public static string Of(BillKind kind) => kind switch
    {
        BillKind.Rent => "rent",
        BillKind.Electricity => "electricity",
        BillKind.Water => "water",
        BillKind.Infostan => "infostan",
        BillKind.Internet => "internet",
        BillKind.Heating => "heating",
        BillKind.Gas => "gas",
        _ => "other",
    };

    public static string Of(BillPayee payee) => payee == BillPayee.Landlord ? "landlord" : "company";
}
