using FluentValidation;
using Lander.src.Common;
using Lander.src.Modules.ApartmentApplications.Dtos.InputDto;

namespace Lander.src.Modules.ApartmentApplications.Validators;

public class UpdateApplicationStatusInputDtoValidator : AbstractValidator<UpdateApplicationStatusInputDto>
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ApplicationStatuses.Pending,
        ApplicationStatuses.Approved,
        ApplicationStatuses.Rejected
    };

    public UpdateApplicationStatusInputDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required.")
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage("Status must be one of: Pending, Approved, Rejected.");
    }
}
