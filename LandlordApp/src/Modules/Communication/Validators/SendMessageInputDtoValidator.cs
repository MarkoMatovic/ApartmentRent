using FluentValidation;
using Lander.src.Modules.Communication.Dtos.InputDto;
namespace Lander.src.Modules.Communication.Validators;
public class SendMessageInputDtoValidator : AbstractValidator<SendMessageInputDto>
{
    public SendMessageInputDtoValidator()
    {
        RuleFor(x => x.ReceiverId)
            .GreaterThan(0).WithMessage("Receiver ID must be valid");
        RuleFor(x => x.MessageText)
            .NotEmpty().WithMessage("Message text is required")
            .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters");
    }
}
