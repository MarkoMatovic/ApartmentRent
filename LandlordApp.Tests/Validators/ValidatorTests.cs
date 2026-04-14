using FluentAssertions;
using FluentValidation;
using Lander.src.Modules.Analytics.Dtos.InputDto;
using Lander.src.Modules.Analytics.Validators;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Validators;
using Lander.src.Modules.Listings.Dtos.InputDto;
using Lander.src.Modules.Listings.Models;
using Lander.src.Modules.Listings.Validators;
using Lander.src.Modules.Roommates.Dtos.InputDto;
using Lander.src.Modules.Roommates.Validators;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Validators;

namespace LandlordApp.Tests.Validators;

// ─────────────────────────────────────────────────────────────────
// UserRegistrationInputDtoValidator
// ─────────────────────────────────────────────────────────────────
public class UserRegistrationInputDtoValidatorTests
{
    private readonly UserRegistrationInputDtoValidator _validator = new();

    private static UserRegistrationInputDto Valid() => new()
    {
        Email = "test@example.com",
        Password = "Password1",
        FirstName = "Marko",
        LastName = "Markovic"
    };

    [Fact]
    public void ValidInput_ShouldPass()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyEmail_ShouldFail()
    {
        var dto = Valid(); dto.Email = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFail()
    {
        var dto = Valid(); dto.Email = "not-an-email";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void TooLongEmail_ShouldFail()
    {
        var dto = Valid(); dto.Email = new string('a', 250) + "@x.com";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void ShortPassword_ShouldFail()
    {
        var dto = Valid(); dto.Password = "Ab1";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void PasswordMissingUppercase_ShouldFail()
    {
        var dto = Valid(); dto.Password = "password1";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void PasswordMissingLowercase_ShouldFail()
    {
        var dto = Valid(); dto.Password = "PASSWORD1";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void PasswordMissingDigit_ShouldFail()
    {
        var dto = Valid(); dto.Password = "PasswordOnly";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void EmptyFirstName_ShouldFail()
    {
        var dto = Valid(); dto.FirstName = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void TooLongFirstName_ShouldFail()
    {
        var dto = Valid(); dto.FirstName = new string('a', 101);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void EmptyLastName_ShouldFail()
    {
        var dto = Valid(); dto.LastName = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "LastName");
    }

    [Fact]
    public void TooLongPhoneNumber_ShouldFail()
    {
        var dto = Valid(); dto.PhoneNumber = new string('0', 21);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Fact]
    public void NullPhoneNumber_ShouldPass()
    {
        var dto = Valid(); dto.PhoneNumber = null;
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }
}

// ─────────────────────────────────────────────────────────────────
// LoginUserInputDtoValidator
// ─────────────────────────────────────────────────────────────────
public class LoginUserInputDtoValidatorTests
{
    private readonly LoginUserInputDtoValidator _validator = new();

    private static LoginUserInputDto Valid() => new()
    {
        Email = "user@example.com",
        Password = "anypass"
    };

    [Fact]
    public void ValidInput_ShouldPass()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyEmail_ShouldFail()
    {
        var dto = Valid(); dto.Email = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmailFormat_ShouldFail()
    {
        var dto = Valid(); dto.Email = "notanemail";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmptyPassword_ShouldFail()
    {
        var dto = Valid(); dto.Password = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}

// ─────────────────────────────────────────────────────────────────
// ApartmentInputDtoValidator
// ─────────────────────────────────────────────────────────────────
public class ApartmentInputDtoValidatorTests
{
    private readonly ApartmentInputDtoValidator _validator = new();

    private static ApartmentInputDto Valid() => new()
    {
        Title = "Stan u centru",
        Description = "Lep stan",
        Address = "Knez Mihailova 1",
        City = "Beograd",
        Rent = 500,
        ListingType = ListingType.Rent
    };

    [Fact]
    public void ValidInput_ShouldPass()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyTitle_ShouldFail()
    {
        var dto = Valid(); dto.Title = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void TooLongTitle_ShouldFail()
    {
        var dto = Valid(); dto.Title = new string('a', 256);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void EmptyDescription_ShouldFail()
    {
        var dto = Valid(); dto.Description = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void EmptyAddress_ShouldFail()
    {
        var dto = Valid(); dto.Address = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Address");
    }

    [Fact]
    public void EmptyCity_ShouldFail()
    {
        var dto = Valid(); dto.City = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "City");
    }

    [Fact]
    public void ZeroRentForRentalListing_ShouldFail()
    {
        var dto = Valid(); dto.Rent = 0; dto.ListingType = ListingType.Rent;
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Rent");
    }

    [Fact]
    public void NegativeRentForRentalListing_ShouldFail()
    {
        var dto = Valid(); dto.Rent = -100; dto.ListingType = ListingType.Rent;
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Rent");
    }

    [Fact]
    public void NegativeDepositAmount_ShouldFail()
    {
        var dto = Valid(); dto.DepositAmount = -1;
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "DepositAmount");
    }

    [Fact]
    public void ZeroNumberOfRooms_ShouldFail()
    {
        var dto = Valid(); dto.NumberOfRooms = 0;
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "NumberOfRooms");
    }

    [Fact]
    public void NullPostalCode_ShouldPass()
    {
        var dto = Valid(); dto.PostalCode = null;
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void TooLongPostalCode_ShouldFail()
    {
        var dto = Valid(); dto.PostalCode = "12345678901";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "PostalCode");
    }
}

// ─────────────────────────────────────────────────────────────────
// RoommateInputDtoValidator
// ─────────────────────────────────────────────────────────────────
public class RoommateInputDtoValidatorTests
{
    private readonly RoommateInputDtoValidator _validator = new();

    private static RoommateInputDto Valid() => new()
    {
        Bio = "Traži cimera u mirnom kraju."
    };

    [Fact]
    public void ValidInput_ShouldPass()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyBio_ShouldFail()
    {
        var dto = Valid(); dto.Bio = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Bio");
    }

    [Fact]
    public void TooLongBio_ShouldFail()
    {
        var dto = Valid(); dto.Bio = new string('a', 5001);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Bio");
    }

    [Fact]
    public void TooLongProfession_ShouldFail()
    {
        var dto = Valid(); dto.Profession = new string('a', 101);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Profession");
    }

    [Fact]
    public void BudgetMaxLessThanBudgetMin_ShouldFail()
    {
        var dto = Valid(); dto.BudgetMin = 500; dto.BudgetMax = 300;
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "BudgetMax");
    }

    [Fact]
    public void BudgetMaxGreaterThanBudgetMin_ShouldPass()
    {
        var dto = Valid(); dto.BudgetMin = 300; dto.BudgetMax = 600;
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ZeroBudgetMin_ShouldFail()
    {
        var dto = Valid(); dto.BudgetMin = 0;
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "BudgetMin");
    }
}

// ─────────────────────────────────────────────────────────────────
// SendMessageInputDtoValidator
// ─────────────────────────────────────────────────────────────────
public class SendMessageInputDtoValidatorTests
{
    private readonly SendMessageInputDtoValidator _validator = new();

    private static SendMessageInputDto Valid() => new()
    {
        ReceiverId = 5,
        MessageText = "Zdravo!"
    };

    [Fact]
    public void ValidInput_ShouldPass()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ZeroReceiverId_ShouldFail()
    {
        var dto = Valid(); dto.ReceiverId = 0;
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "ReceiverId");
    }

    [Fact]
    public void NegativeReceiverId_ShouldFail()
    {
        var dto = Valid(); dto.ReceiverId = -1;
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "ReceiverId");
    }

    [Fact]
    public void EmptyMessageText_ShouldFail()
    {
        var dto = Valid(); dto.MessageText = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "MessageText");
    }

    [Fact]
    public void TooLongMessageText_ShouldFail()
    {
        var dto = Valid(); dto.MessageText = new string('a', 2001);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "MessageText");
    }
}

// ─────────────────────────────────────────────────────────────────
// ReportMessageDtoValidator
// ─────────────────────────────────────────────────────────────────
public class ReportMessageDtoValidatorTests
{
    private readonly ReportMessageDtoValidator _validator = new();

    private static ReportMessageDto Valid() => new()
    {
        MessageId = 1,
        ReportedUserId = 2,
        Reason = "Ovo je neprikladan sadrzaj."
    };

    [Fact]
    public void ValidInput_ShouldPass()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ZeroMessageId_ShouldFail()
    {
        var dto = Valid(); dto.MessageId = 0;
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "MessageId");
    }

    [Fact]
    public void ZeroReportedUserId_ShouldFail()
    {
        var dto = Valid(); dto.ReportedUserId = 0;
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "ReportedUserId");
    }

    [Fact]
    public void EmptyReason_ShouldFail()
    {
        var dto = Valid(); dto.Reason = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void TooShortReason_ShouldFail()
    {
        var dto = Valid(); dto.Reason = "Kratko";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Reason");
    }

    [Fact]
    public void TooLongReason_ShouldFail()
    {
        var dto = Valid(); dto.Reason = new string('a', 1001);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Reason");
    }
}

// ─────────────────────────────────────────────────────────────────
// TrackEventInputDtoValidator
// ─────────────────────────────────────────────────────────────────
public class TrackEventInputDtoValidatorTests
{
    private readonly TrackEventInputDtoValidator _validator = new();

    private static TrackEventInputDto Valid() => new()
    {
        EventType = "click",
        EventCategory = "apartment"
    };

    [Fact]
    public void ValidInput_ShouldPass()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyEventType_ShouldFail()
    {
        var dto = Valid(); dto.EventType = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "EventType");
    }

    [Fact]
    public void TooLongEventType_ShouldFail()
    {
        var dto = Valid(); dto.EventType = new string('a', 101);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "EventType");
    }

    [Fact]
    public void EmptyEventCategory_ShouldFail()
    {
        var dto = Valid(); dto.EventCategory = "";
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "EventCategory");
    }

    [Fact]
    public void TooLongEventCategory_ShouldFail()
    {
        var dto = Valid(); dto.EventCategory = new string('a', 101);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "EventCategory");
    }

    [Fact]
    public void TooLongEntityType_ShouldFail()
    {
        var dto = Valid(); dto.EntityType = new string('a', 51);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "EntityType");
    }

    [Fact]
    public void NullEntityType_ShouldPass()
    {
        var dto = Valid(); dto.EntityType = null;
        _validator.Validate(dto).IsValid.Should().BeTrue();
    }

    [Fact]
    public void TooLongSearchQuery_ShouldFail()
    {
        var dto = Valid(); dto.SearchQuery = new string('a', 501);
        _validator.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "SearchQuery");
    }
}
