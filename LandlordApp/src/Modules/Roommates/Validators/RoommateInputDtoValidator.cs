using FluentValidation;
using Lander.src.Modules.Roommates.Dtos.InputDto;
namespace Lander.src.Modules.Roommates.Validators;
public class RoommateInputDtoValidator : AbstractValidator<RoommateInputDto>
{
    public RoommateInputDtoValidator()
    {
        RuleFor(x => x.Bio)
            .NotEmpty().WithMessage("Bio is required")
            .MaximumLength(5000).WithMessage("Bio cannot exceed 5000 characters");
        RuleFor(x => x.Profession)
            .MaximumLength(100).WithMessage("Profession cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Profession));
        RuleFor(x => x.Hobbies)
            .MaximumLength(500).WithMessage("Hobbies cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Hobbies));
        RuleFor(x => x.BudgetMin)
            .GreaterThan(0).WithMessage("Minimum budget must be greater than 0")
            .When(x => x.BudgetMin.HasValue);
        RuleFor(x => x.BudgetMax)
            .GreaterThan(x => x.BudgetMin ?? 0).WithMessage("Maximum budget must be greater than minimum budget")
            .When(x => x.BudgetMax.HasValue && x.BudgetMin.HasValue);
        RuleFor(x => x.PreferredLocation)
            .MaximumLength(255).WithMessage("Preferred location cannot exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.PreferredLocation));
    }
}
