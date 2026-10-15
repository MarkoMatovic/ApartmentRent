using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Communication.Dtos.InputDto;
namespace Lander.src.Modules.Communication.Validators;
public class SendMessageInputDtoValidator : AbstractValidator<SendMessageInputDto>
{
    public SendMessageInputDtoValidator()
    {
        RuleFor(x => x.ReceiverId)
            .GreaterThan(0).WithCode(ValidationCodes.ReceiverInvalid);

        RuleFor(x => x.MessageText)
            .MaximumLength(2000).WithCode(ValidationCodes.MessageTooLong);

        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.MessageText) || !string.IsNullOrEmpty(x.FileUrl))
            .WithCode(ValidationCodes.MessageEmpty)
            .OverridePropertyName("MessageText");
    }
}
