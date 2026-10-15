using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Communication.Dtos.InputDto;

namespace Lander.src.Modules.Communication.Validators;

public class OpenChatGroupInputDtoValidator : AbstractValidator<OpenChatGroupInputDto>
{
    public OpenChatGroupInputDtoValidator()
    {
        RuleFor(x => x.OtherUserId)
            .GreaterThan(0).WithCode(ValidationCodes.ReceiverInvalid);
    }
}

public class SendGroupMessageInputDtoValidator : AbstractValidator<SendGroupMessageInputDto>
{
    public SendGroupMessageInputDtoValidator()
    {
        RuleFor(x => x.MessageText)
            .Cascade(CascadeMode.Stop)
            .Must(text => !string.IsNullOrWhiteSpace(text)).WithCode(ValidationCodes.MessageEmpty)
            .MaximumLength(2000).WithCode(ValidationCodes.MessageTooLong);
    }
}
