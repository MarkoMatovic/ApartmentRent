using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.ApartmentApplications.Dtos.InputDto;
namespace Lander.src.Modules.ApartmentApplications.Validators;
public class CreateApplicationInputDtoValidator : AbstractValidator<CreateApplicationInputDto>
{
    public CreateApplicationInputDtoValidator()
    {
        RuleFor(x => x.ApartmentId)
            .GreaterThan(0).WithCode(ValidationCodes.ApartmentInvalid);
    }
}
