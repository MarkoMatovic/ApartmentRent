using FluentAssertions;
using FluentValidation;
using Lander.src.Common.Validation;
using Lander.src.Modules.Analytics.Validators;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Validators;
using Lander.src.Modules.Roommates.Dtos.InputDto;
using Lander.src.Modules.Roommates.Validators;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Validators;

namespace LandlordApp.Tests.Validators;

/// <summary>
/// Validators report a stable code (see <see cref="ValidationCodes"/>) instead of an English
/// sentence; the frontend translates it. These tests keep that contract from drifting.
/// </summary>
public class ValidationCodeTests
{
    // Analytics tracking is fire-and-forget telemetry; no user ever sees its validation messages.
    private static readonly Type[] NotUserFacing = [typeof(TrackEventInputDtoValidator)];

    private static readonly HashSet<string> AllCodes = typeof(ValidationCodes)
        .GetFields()
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToHashSet();

    private static IEnumerable<IValidator> UserFacingValidators() =>
        typeof(UserRegistrationInputDtoValidator).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.BaseType is { IsGenericType: true } b
                        && b.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .Where(t => !NotUserFacing.Contains(t))
            .Select(t => (IValidator)Activator.CreateInstance(t)!);

    private static IEnumerable<(string Validator, string Message, string Code)> AllComponents() =>
        UserFacingValidators().SelectMany(v => v.CreateDescriptor().Rules
            .SelectMany(r => r.Components)
            .Select(c => (v.GetType().Name, c.GetUnformattedErrorMessage(), c.ErrorCode)));

    [Fact]
    public void Reflection_finds_the_validators_and_their_rules()
    {
        // Guards the tests below against passing vacuously if discovery silently finds nothing.
        UserFacingValidators().Count().Should().BeGreaterThanOrEqualTo(10);
        AllComponents().Count().Should().BeGreaterThan(40);
    }

    [Fact]
    public void Every_rule_message_is_a_known_code()
    {
        var offenders = AllComponents()
            .Where(c => !AllCodes.Contains(c.Message))
            .Select(c => $"{c.Validator}: '{c.Message}'")
            .ToList();

        offenders.Should().BeEmpty("rules must use .WithCode(ValidationCodes.X) and X must be defined");
    }

    [Fact]
    public void Error_code_and_message_are_identical()
    {
        var offenders = AllComponents()
            .Where(c => c.Code != c.Message)
            .Select(c => $"{c.Validator}: code '{c.Code}' vs message '{c.Message}'")
            .ToList();

        offenders.Should().BeEmpty();
    }

    [Fact]
    public void Every_defined_code_is_used_by_a_validator()
    {
        var used = AllComponents().Select(c => c.Message).ToHashSet();

        AllCodes.Except(used).Should().BeEmpty("a code nobody raises only adds translation work");
    }

    // ── Behaviour ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Ab1", ValidationCodes.PasswordTooShort)]
    [InlineData("abcdefg1", ValidationCodes.PasswordNeedsUpper)]
    [InlineData("ABCDEFG1", ValidationCodes.PasswordNeedsLower)]
    [InlineData("Abcdefgh", ValidationCodes.PasswordNeedsDigit)]
    [InlineData("", ValidationCodes.PasswordRequired)]
    public void Weak_password_reports_exactly_the_first_broken_rule(string password, string expectedCode)
    {
        var result = new ResetPasswordInputDtoValidator()
            .Validate(new ResetPasswordInputDto { Token = "t", NewPassword = password });

        result.Errors.Should().ContainSingle()
            .Which.Should().Match<FluentValidation.Results.ValidationFailure>(
                e => e.ErrorCode == expectedCode && e.ErrorMessage == expectedCode && e.PropertyName == "NewPassword");
    }

    [Fact]
    public void Strong_password_passes()
    {
        new ResetPasswordInputDtoValidator()
            .Validate(new ResetPasswordInputDto { Token = "t", NewPassword = "Marko1234" })
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Registration_uses_the_same_password_policy()
    {
        var dto = new UserRegistrationInputDto
        {
            Email = "a@b.com", Password = "marko1234", FirstName = "A", LastName = "B"
        };

        new UserRegistrationInputDtoValidator().Validate(dto).Errors
            .Should().ContainSingle(e => e.ErrorCode == ValidationCodes.PasswordNeedsUpper);
    }

    [Fact]
    public void Login_checks_presence_only_not_the_password_policy()
    {
        new LoginUserInputDtoValidator()
            .Validate(new LoginUserInputDto { Email = "a@b.com", Password = "x" })
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_email_reports_a_code_not_a_sentence()
    {
        new LoginUserInputDtoValidator()
            .Validate(new LoginUserInputDto { Email = "not-an-email", Password = "x" })
            .Errors.Should().ContainSingle(e => e.ErrorCode == ValidationCodes.EmailInvalid);
    }

    [Fact]
    public void Report_reason_too_short_is_reported()
    {
        var dto = new ReportMessageDto { MessageId = 1, ReportedUserId = 2, Reason = "short" };

        new ReportMessageDtoValidator().Validate(dto).Errors
            .Should().ContainSingle(e => e.ErrorCode == ValidationCodes.ReasonTooShort);
    }

    [Fact]
    public void Roommate_max_budget_below_min_is_reported()
    {
        var dto = new RoommateInputDto { Bio = "bio", BudgetMin = 500, BudgetMax = 300 };

        new RoommateInputDtoValidator().Validate(dto).Errors
            .Should().ContainSingle(e => e.ErrorCode == ValidationCodes.BudgetMaxBelowMin);
    }
}
