using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Validators;

public class TwoFactorLoginInputDtoValidator : AbstractValidator<TwoFactorLoginInputDto>
{
    public TwoFactorLoginInputDtoValidator()
    {
        RuleFor(x => x.TwoFactorToken)
            .NotEmpty().WithCode(ValidationCodes.TokenRequired)
            .MaximumLength(2000).WithCode(ValidationCodes.TokenRequired);

        RuleFor(x => x.Code)
            .NotEmpty().WithCode(ValidationCodes.TwoFactorCodeRequired)
            .MaximumLength(20).WithCode(ValidationCodes.TwoFactorCodeRequired);
    }
}

public class TwoFactorCodeInputDtoValidator : AbstractValidator<TwoFactorCodeInputDto>
{
    public TwoFactorCodeInputDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithCode(ValidationCodes.TwoFactorCodeRequired)
            .MaximumLength(20).WithCode(ValidationCodes.TwoFactorCodeRequired);
    }
}
