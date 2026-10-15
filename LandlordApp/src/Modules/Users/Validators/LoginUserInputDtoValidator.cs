using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Users.Dtos.InputDto;
namespace Lander.src.Modules.Users.Validators;
public class LoginUserInputDtoValidator : AbstractValidator<LoginUserInputDto>
{
    public LoginUserInputDtoValidator()
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.EmailRequired)
            .EmailAddress().WithCode(ValidationCodes.EmailInvalid);

        // Login only checks presence: the password policy applies when a password is created,
        // and revealing the policy here would say nothing about whether the credentials are right.
        RuleFor(x => x.Password)
            .NotEmpty().WithCode(ValidationCodes.PasswordRequired);
    }
}
