using FluentValidation;

namespace Lander.src.Common.Validation;

/// <summary>
/// Helpers so a validator states a rule and its code once, instead of repeating
/// <c>.WithErrorCode(x).WithMessage(x)</c> on every rule.
/// </summary>
public static class ValidationRuleExtensions
{
    public const int PasswordMinLength = 8;

    /// <summary>Sets <paramref name="code"/> as both the error code and the failure message.</summary>
    public static IRuleBuilderOptions<T, TProperty> WithCode<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule, string code)
        => rule.WithErrorCode(code).WithMessage(code);

    /// <summary>
    /// The single definition of the password policy, shared by registration, reset and change
    /// password. Rules run in order and stop at the first failure so the user fixes one thing
    /// at a time.
    /// </summary>
    public static IRuleBuilderOptions<T, string> MustBeValidPassword<T>(
        this IRuleBuilderInitial<T, string> rule, string requiredCode = ValidationCodes.PasswordRequired)
    {
        return rule
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithCode(requiredCode)
            .MinimumLength(PasswordMinLength).WithCode(ValidationCodes.PasswordTooShort)
            .Matches(@"[A-Z]").WithCode(ValidationCodes.PasswordNeedsUpper)
            .Matches(@"[a-z]").WithCode(ValidationCodes.PasswordNeedsLower)
            .Matches(@"[0-9]").WithCode(ValidationCodes.PasswordNeedsDigit);
    }
}
