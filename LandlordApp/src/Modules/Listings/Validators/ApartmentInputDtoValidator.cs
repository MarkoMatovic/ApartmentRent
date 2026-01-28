using FluentValidation;
using Lander.src.Modules.Listings.Dtos.InputDto;
namespace Lander.src.Modules.Listings.Validators;
public class ApartmentInputDtoValidator : AbstractValidator<ApartmentInputDto>
{
    public ApartmentInputDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters");
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters");
        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .MaximumLength(255).WithMessage("Address cannot exceed 255 characters");
        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters");
        RuleFor(x => x.PostalCode)
            .MaximumLength(10).WithMessage("Postal code cannot exceed 10 characters")
            .When(x => !string.IsNullOrEmpty(x.PostalCode));
        RuleFor(x => x.Rent)
            .GreaterThan(0).WithMessage("Rent must be greater than 0")
            .When(x => x.ListingType == Models.ListingType.Rent);
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .When(x => x.ListingType == Models.ListingType.Sale);
        RuleFor(x => x.NumberOfRooms)
            .GreaterThan(0).WithMessage("Number of rooms must be at least 1")
            .When(x => x.NumberOfRooms.HasValue);
        RuleFor(x => x.SizeSquareMeters)
            .GreaterThan(0).WithMessage("Size must be greater than 0")
            .When(x => x.SizeSquareMeters.HasValue);
        RuleFor(x => x.DepositAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Deposit amount cannot be negative")
            .When(x => x.DepositAmount.HasValue);
    }
}
