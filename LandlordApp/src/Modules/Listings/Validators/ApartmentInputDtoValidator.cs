using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Listings.Dtos.InputDto;
namespace Lander.src.Modules.Listings.Validators;
public class ApartmentInputDtoValidator : AbstractValidator<ApartmentInputDto>
{
    public ApartmentInputDtoValidator()
    {
        RuleFor(x => x.Title)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.TitleRequired)
            .MaximumLength(255).WithCode(ValidationCodes.TitleTooLong);

        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.DescriptionRequired)
            .MaximumLength(5000).WithCode(ValidationCodes.DescriptionTooLong);

        RuleFor(x => x.Address)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.AddressRequired)
            .MaximumLength(255).WithCode(ValidationCodes.AddressTooLong);

        RuleFor(x => x.City)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(ValidationCodes.CityRequired)
            .MaximumLength(100).WithCode(ValidationCodes.CityTooLong);

        RuleFor(x => x.PostalCode)
            .MaximumLength(10).WithCode(ValidationCodes.PostalCodeTooLong)
            .When(x => !string.IsNullOrEmpty(x.PostalCode));

        RuleFor(x => x.Rent)
            .GreaterThan(0).WithCode(ValidationCodes.RentMustBePositive)
            .When(x => x.ListingType == Models.ListingType.Rent);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithCode(ValidationCodes.PriceMustBePositive)
            .When(x => x.ListingType == Models.ListingType.Sale);

        RuleFor(x => x.NumberOfRooms)
            .GreaterThan(0).WithCode(ValidationCodes.RoomsMustBePositive)
            .When(x => x.NumberOfRooms.HasValue);

        RuleFor(x => x.SizeSquareMeters)
            .GreaterThan(0).WithCode(ValidationCodes.SizeMustBePositive)
            .When(x => x.SizeSquareMeters.HasValue);

        RuleFor(x => x.DepositAmount)
            .GreaterThanOrEqualTo(0).WithCode(ValidationCodes.DepositCannotBeNegative)
            .When(x => x.DepositAmount.HasValue);
    }
}
