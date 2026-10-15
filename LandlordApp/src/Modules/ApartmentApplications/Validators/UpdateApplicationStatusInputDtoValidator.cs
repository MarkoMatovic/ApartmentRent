using FluentValidation;
using Lander.src.Common;
using Lander.src.Common.Validation;
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
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.StatusRequired)
            .Must(s => AllowedStatuses.Contains(s)).WithCode(ValidationCodes.StatusInvalid);
    }
}
