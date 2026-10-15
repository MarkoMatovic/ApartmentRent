using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Validators;

public class ResetPasswordInputDtoValidator : AbstractValidator<ResetPasswordInputDto>
{
    public ResetPasswordInputDtoValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithCode(ValidationCodes.TokenRequired);

        RuleFor(x => x.NewPassword).MustBeValidPassword();
    }
}
