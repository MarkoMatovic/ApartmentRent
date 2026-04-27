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
            .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters");
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.MessageText) || !string.IsNullOrEmpty(x.FileUrl))
            .WithMessage("Message must contain text or a file attachment")
            .OverridePropertyName("MessageText");
    }
}
