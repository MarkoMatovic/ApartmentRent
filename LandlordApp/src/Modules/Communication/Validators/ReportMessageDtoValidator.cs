using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Communication.Dtos.InputDto;
namespace Lander.src.Modules.Communication.Validators;
public class ReportMessageDtoValidator : AbstractValidator<ReportMessageDto>
{
    public ReportMessageDtoValidator()
    {
        RuleFor(x => x.MessageId)
            .GreaterThan(0).WithCode(ValidationCodes.MessageIdInvalid);

        RuleFor(x => x.ReportedUserId)
            .GreaterThan(0).WithCode(ValidationCodes.ReportedUserInvalid);

        RuleFor(x => x.Reason)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.ReasonRequired)
            .MinimumLength(10).WithCode(ValidationCodes.ReasonTooShort)
            .MaximumLength(1000).WithCode(ValidationCodes.ReasonTooLong);
    }
}
