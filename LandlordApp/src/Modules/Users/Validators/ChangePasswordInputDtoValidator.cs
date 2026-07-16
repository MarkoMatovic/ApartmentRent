using FluentValidation;
using Lander.src.Modules.Users.Dtos.InputDto;

namespace Lander.src.Modules.Users.Validators;

public class ChangePasswordInputDtoValidator : AbstractValidator<ChangePasswordInputDto>
{
    public ChangePasswordInputDtoValidator()
    {
        RuleFor(x => x.OldPassword).NotEmpty().WithMessage("Old password is required.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .NotEqual(x => x.OldPassword).WithMessage("New password must differ from the current password.");
    }
}
