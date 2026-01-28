using FluentValidation;
using Lander.src.Modules.Users.Dtos.InputDto;
namespace Lander.src.Modules.Users.Validators;
public class LoginUserInputDtoValidator : AbstractValidator<LoginUserInputDto>
{
    public LoginUserInputDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
