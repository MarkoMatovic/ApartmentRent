using FluentValidation;
using Lander.src.Modules.Analytics.Dtos.InputDto;
namespace Lander.src.Modules.Analytics.Validators;
public class TrackEventInputDtoValidator : AbstractValidator<TrackEventInputDto>
{
    public TrackEventInputDtoValidator()
    {
        RuleFor(x => x.EventType)
            .NotEmpty().WithMessage("Event type is required")
            .MaximumLength(100).WithMessage("Event type cannot exceed 100 characters");
        RuleFor(x => x.EventCategory)
            .NotEmpty().WithMessage("Event category is required")
            .MaximumLength(100).WithMessage("Event category cannot exceed 100 characters");
        RuleFor(x => x.EntityType)
            .MaximumLength(50).WithMessage("Entity type cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.EntityType));
        RuleFor(x => x.SearchQuery)
            .MaximumLength(500).WithMessage("Search query cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.SearchQuery));
    }
}
