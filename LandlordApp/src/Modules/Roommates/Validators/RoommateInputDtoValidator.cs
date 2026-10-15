using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Roommates.Dtos.InputDto;
namespace Lander.src.Modules.Roommates.Validators;
public class RoommateInputDtoValidator : AbstractValidator<RoommateInputDto>
{
    public RoommateInputDtoValidator()
    {
        RuleFor(x => x.Bio)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.BioRequired)
            .MaximumLength(5000).WithCode(ValidationCodes.BioTooLong);

        RuleFor(x => x.Profession)
            .MaximumLength(100).WithCode(ValidationCodes.ProfessionTooLong)
            .When(x => !string.IsNullOrEmpty(x.Profession));

        RuleFor(x => x.Hobbies)
            .MaximumLength(500).WithCode(ValidationCodes.HobbiesTooLong)
            .When(x => !string.IsNullOrEmpty(x.Hobbies));

        RuleFor(x => x.BudgetMin)
            .GreaterThan(0).WithCode(ValidationCodes.BudgetMinMustBePositive)
            .When(x => x.BudgetMin.HasValue);

        RuleFor(x => x.BudgetMax)
            .GreaterThan(x => x.BudgetMin ?? 0).WithCode(ValidationCodes.BudgetMaxBelowMin)
            .When(x => x.BudgetMax.HasValue && x.BudgetMin.HasValue);

        RuleFor(x => x.PreferredLocation)
            .MaximumLength(255).WithCode(ValidationCodes.PreferredLocationTooLong)
            .When(x => !string.IsNullOrEmpty(x.PreferredLocation));
    }
}
