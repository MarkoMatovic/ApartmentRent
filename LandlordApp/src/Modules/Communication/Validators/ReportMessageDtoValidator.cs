using FluentValidation;
using Lander.src.Modules.Communication.Dtos.InputDto;

namespace Lander.src.Modules.Communication.Validators;

public class ReportMessageDtoValidator : AbstractValidator<ReportMessageDto>
{
    public ReportMessageDtoValidator()
    {
        RuleFor(x => x.MessageId)
            .GreaterThan(0)
            .WithMessage("MessageId must be greater than 0");

        RuleFor(x => x.ReportedUserId)
            .GreaterThan(0)
            .WithMessage("ReportedUserId must be greater than 0");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MinimumLength(10)
            .WithMessage("Reason must be at least 10 characters")
            .MaximumLength(1000)
            .WithMessage("Reason cannot exceed 1000 characters");
    }
}
