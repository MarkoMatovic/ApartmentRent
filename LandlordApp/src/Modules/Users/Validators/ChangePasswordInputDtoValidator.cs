using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Validators;

public class ChangePasswordInputDtoValidator : AbstractValidator<ChangePasswordInputDto>
{
    public ChangePasswordInputDtoValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithCode(ValidationCodes.OldPasswordRequired);

        RuleFor(x => x.NewPassword)
            .MustBeValidPassword()
            .NotEqual(x => x.OldPassword).WithCode(ValidationCodes.PasswordSameAsOld);
    }
}
