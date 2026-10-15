using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Users.Dtos.InputDto;
namespace Lander.src.Modules.Users.Validators;
public class UserRegistrationInputDtoValidator : AbstractValidator<UserRegistrationInputDto>
{
    public UserRegistrationInputDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.EmailRequired)
            .EmailAddress().WithCode(ValidationCodes.EmailInvalid)
            .MaximumLength(255).WithCode(ValidationCodes.EmailTooLong);

        RuleFor(x => x.Password).MustBeValidPassword();

        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.FirstNameRequired)
            .MaximumLength(100).WithCode(ValidationCodes.FirstNameTooLong);

        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.LastNameRequired)
            .MaximumLength(100).WithCode(ValidationCodes.LastNameTooLong);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithCode(ValidationCodes.PhoneTooLong)
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
