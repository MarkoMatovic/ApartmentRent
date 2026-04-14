using FluentValidation;
using Lander.src.Modules.ApartmentApplications.Dtos.InputDto;

namespace Lander.src.Modules.ApartmentApplications.Validators;

public class CreateApplicationInputDtoValidator : AbstractValidator<CreateApplicationInputDto>
{
    public CreateApplicationInputDtoValidator()
    {
        RuleFor(x => x.ApartmentId)
            .GreaterThan(0)
            .WithMessage("ApartmentId must be a valid positive integer.");
    }
}
