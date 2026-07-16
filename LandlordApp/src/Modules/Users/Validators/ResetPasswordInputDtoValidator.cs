using FluentValidation;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Validators;

public class ResetPasswordInputDtoValidator : AbstractValidator<ResetPasswordInputDto>
{
    public ResetPasswordInputDtoValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Reset token is required.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
