using Lander.src.Modules.Billing.Models;

namespace Lander.src.Modules.Billing.Dtos;

public class CreateTenancyInputDto
{
    public int ApartmentId { get; set; }
    public List<int> TenantUserIds { get; set; } = new();
    public decimal MonthlyRent { get; set; }
    public int RentDueDay { get; set; }
    public string PayeeName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
}

public class UpdateTenancyInputDto
{
    public decimal MonthlyRent { get; set; }
    public int RentDueDay { get; set; }
    public string PayeeName { get; set; } = string.Empty;
    public string PayeeAccount { get; set; } = string.Empty;
}

public class AddTenantInputDto
{
    public int UserId { get; set; }
}

public class CreateBillInputDto
{
    public int TenancyId { get; set; }
    public BillKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public BillPayee PayTo { get; set; }
    // Only for PayTo = Company: the company's details as printed on the bill.
    public string? PayeeName { get; set; }
    public string? PayeeAccount { get; set; }
    public string? PaymentCode { get; set; }
    public string? PaymentModel { get; set; }
    public string? PaymentReference { get; set; }
}

public class UpdateBillInputDto
{
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
}

/// <summary>"I paid the whole company bill": where the roommates pay their part back.</summary>
public class PayCompanyBillInputDto
{
    public string? PayoutName { get; set; }
    public string? PayoutAccount { get; set; }
}

public class BillingProfileInputDto
{
    public string PayoutName { get; set; } = string.Empty;
    public string PayoutAccount { get; set; } = string.Empty;
}
