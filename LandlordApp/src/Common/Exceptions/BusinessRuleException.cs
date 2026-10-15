namespace Lander.src.Common.Exceptions;

/// <summary>
/// The caller broke a business rule (not a bug, and not malformed input): wrong current password,
/// not enough tokens, a booked time slot... It maps to HTTP 422 and carries a stable code from
/// <see cref="Lander.src.Common.Validation.BusinessErrorCodes"/> that the frontend translates.
/// </summary>
public class BusinessRuleException : Exception
{
    public string Code { get; }

    public BusinessRuleException(string code) : base(code)
    {
        Code = code;
    }
}
