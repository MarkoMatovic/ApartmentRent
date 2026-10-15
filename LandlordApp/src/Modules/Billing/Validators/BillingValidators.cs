using System.Text.RegularExpressions;
using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Billing.Dtos;
using Lander.src.Modules.Billing.Implementation;
using Lander.src.Modules.Billing.Models;

namespace Lander.src.Modules.Billing.Validators;

internal static class BillingRules
{
    /// <summary>Ten million dinars: far above any rent or utility bill, low enough to catch a typo of three zeros.</summary>
    public const decimal MaxAmount = 10_000_000m;

    public static readonly Regex PaymentCode = new(@"^[12]\d{2}$", RegexOptions.Compiled);
    public static readonly Regex PaymentModel = new(@"^\d{2}$", RegexOptions.Compiled);
    public static readonly Regex PaymentReference = new(@"^[0-9][0-9\-]{0,21}$", RegexOptions.Compiled);

    public static IRuleBuilderOptions<T, decimal> ValidAmount<T>(this IRuleBuilder<T, decimal> rule) =>
        rule.Must(a => a > 0 && a <= MaxAmount && decimal.Round(a, 2) == a).WithCode(ValidationCodes.AmountInvalid);

    public static IRuleBuilderOptions<T, string> ValidPayeeName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithCode(ValidationCodes.PayeeNameRequired)
            .MaximumLength(IpsQr.MaxNameLength).WithCode(ValidationCodes.PayeeNameTooLong);

    public static IRuleBuilderOptions<T, string?> ValidAccount<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(SerbianAccount.IsValid).WithCode(ValidationCodes.BankAccountInvalid);
}

public class CreateTenancyInputDtoValidator : AbstractValidator<CreateTenancyInputDto>
{
    public CreateTenancyInputDtoValidator()
    {
        RuleFor(x => x.ApartmentId).GreaterThan(0).WithCode(ValidationCodes.ApartmentInvalid);
        RuleFor(x => x.TenantUserIds).NotEmpty().WithCode(ValidationCodes.TenantsRequired)
            .Must(ids => ids.Count <= 10 && ids.All(id => id > 0)).WithCode(ValidationCodes.UserInvalid);
        RuleFor(x => x.MonthlyRent).ValidAmount();
        RuleFor(x => x.RentDueDay).InclusiveBetween(1, 28).WithCode(ValidationCodes.DueDayInvalid);
        RuleFor(x => x.PayeeName).ValidPayeeName();
        RuleFor(x => x.PayeeAccount).ValidAccount();
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly)).WithCode(ValidationCodes.StartDateRequired);
    }
}

public class UpdateTenancyInputDtoValidator : AbstractValidator<UpdateTenancyInputDto>
{
    public UpdateTenancyInputDtoValidator()
    {
        RuleFor(x => x.MonthlyRent).ValidAmount();
        RuleFor(x => x.RentDueDay).InclusiveBetween(1, 28).WithCode(ValidationCodes.DueDayInvalid);
        RuleFor(x => x.PayeeName).ValidPayeeName();
        RuleFor(x => x.PayeeAccount).ValidAccount();
    }
}

public class AddTenantInputDtoValidator : AbstractValidator<AddTenantInputDto>
{
    public AddTenantInputDtoValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithCode(ValidationCodes.UserInvalid);
    }
}

public class CreateBillInputDtoValidator : AbstractValidator<CreateBillInputDto>
{
    public CreateBillInputDtoValidator()
    {
        RuleFor(x => x.TenancyId).GreaterThan(0).WithCode(ValidationCodes.ApartmentInvalid);
        RuleFor(x => x.Kind).IsInEnum().WithCode(ValidationCodes.BillKindInvalid)
            .NotEqual(BillKind.Rent).WithCode(ValidationCodes.BillKindInvalid); // rent bills are made every month
        RuleFor(x => x.Title).NotEmpty().WithCode(ValidationCodes.TitleRequired)
            .MaximumLength(100).WithCode(ValidationCodes.BillTitleTooLong);
        RuleFor(x => x.Amount).ValidAmount();
        RuleFor(x => x.DueDate).NotEqual(default(DateOnly)).WithCode(ValidationCodes.DueDateRequired);
        RuleFor(x => x.PayTo).IsInEnum().WithCode(ValidationCodes.BillPayeeInvalid);

        When(x => x.PayTo == BillPayee.Company, () =>
        {
            RuleFor(x => x.PayeeName!).ValidPayeeName();
            RuleFor(x => x.PayeeAccount).ValidAccount();
        });
        RuleFor(x => x.PaymentCode!).Matches(BillingRules.PaymentCode).WithCode(ValidationCodes.PaymentCodeInvalid)
            .When(x => !string.IsNullOrEmpty(x.PaymentCode));
        RuleFor(x => x.PaymentModel!).Matches(BillingRules.PaymentModel).WithCode(ValidationCodes.PaymentModelInvalid)
            .When(x => !string.IsNullOrEmpty(x.PaymentModel));
        RuleFor(x => x.PaymentReference!).Matches(BillingRules.PaymentReference).WithCode(ValidationCodes.PaymentReferenceInvalid)
            .When(x => !string.IsNullOrEmpty(x.PaymentReference));
    }
}

public class UpdateBillInputDtoValidator : AbstractValidator<UpdateBillInputDto>
{
    public UpdateBillInputDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithCode(ValidationCodes.TitleRequired)
            .MaximumLength(100).WithCode(ValidationCodes.BillTitleTooLong);
        RuleFor(x => x.Amount).ValidAmount();
        RuleFor(x => x.DueDate).NotEqual(default(DateOnly)).WithCode(ValidationCodes.DueDateRequired);
    }
}

public class PayCompanyBillInputDtoValidator : AbstractValidator<PayCompanyBillInputDto>
{
    public PayCompanyBillInputDtoValidator()
    {
        // Needed only when there are roommates to pay back; the service decides that.
        RuleFor(x => x.PayoutAccount).ValidAccount().When(x => !string.IsNullOrWhiteSpace(x.PayoutAccount));
        RuleFor(x => x.PayoutName!).MaximumLength(IpsQr.MaxNameLength).WithCode(ValidationCodes.PayeeNameTooLong)
            .When(x => x.PayoutName is not null);
    }
}

public class BillingProfileInputDtoValidator : AbstractValidator<BillingProfileInputDto>
{
    public BillingProfileInputDtoValidator()
    {
        RuleFor(x => x.PayoutName).ValidPayeeName();
        RuleFor(x => x.PayoutAccount).ValidAccount();
    }
}
