namespace Lander.src.Common.Validation;

/// <summary>
/// Machine-readable validation error codes shared by every module's FluentValidation validators.
/// A validator uses the code as both the error code and the failure *message*, so the standard 400
/// response (<c>errors: { Title: ["TITLE_REQUIRED"] }</c>) carries a stable code that the frontend
/// translates (namespace <c>validation</c> in front-land/src/locales/*/validation.json).
///
/// Adding a code: define it here, use it in a validator, then add the translation to all four
/// languages. A frontend test fails if any code below has no translation.
/// </summary>
public static class ValidationCodes
{
    // ── Users ────────────────────────────────────────────────────────────────
    public const string EmailRequired = "EMAIL_REQUIRED";
    public const string EmailInvalid = "EMAIL_INVALID";
    public const string EmailTooLong = "EMAIL_TOO_LONG";

    public const string PasswordRequired = "PASSWORD_REQUIRED";
    public const string PasswordTooShort = "PASSWORD_TOO_SHORT";
    public const string PasswordNeedsUpper = "PASSWORD_NEEDS_UPPER";
    public const string PasswordNeedsLower = "PASSWORD_NEEDS_LOWER";
    public const string PasswordNeedsDigit = "PASSWORD_NEEDS_DIGIT";
    public const string PasswordSameAsOld = "PASSWORD_SAME_AS_OLD";
    public const string OldPasswordRequired = "OLD_PASSWORD_REQUIRED";

    public const string FirstNameRequired = "FIRST_NAME_REQUIRED";
    public const string FirstNameTooLong = "FIRST_NAME_TOO_LONG";
    public const string LastNameRequired = "LAST_NAME_REQUIRED";
    public const string LastNameTooLong = "LAST_NAME_TOO_LONG";
    public const string PhoneTooLong = "PHONE_TOO_LONG";

    public const string TokenRequired = "TOKEN_REQUIRED";
    public const string TwoFactorCodeRequired = "TWO_FACTOR_CODE_REQUIRED";

    // ── Listings (apartments) ────────────────────────────────────────────────
    public const string TitleRequired = "TITLE_REQUIRED";
    public const string TitleTooLong = "TITLE_TOO_LONG";
    public const string DescriptionRequired = "DESCRIPTION_REQUIRED";
    public const string DescriptionTooLong = "DESCRIPTION_TOO_LONG";
    public const string AddressRequired = "ADDRESS_REQUIRED";
    public const string AddressTooLong = "ADDRESS_TOO_LONG";
    public const string CityRequired = "CITY_REQUIRED";
    public const string CityTooLong = "CITY_TOO_LONG";
    public const string PostalCodeTooLong = "POSTAL_CODE_TOO_LONG";
    public const string RentMustBePositive = "RENT_MUST_BE_POSITIVE";
    public const string PriceMustBePositive = "PRICE_MUST_BE_POSITIVE";
    public const string RoomsMustBePositive = "ROOMS_MUST_BE_POSITIVE";
    public const string SizeMustBePositive = "SIZE_MUST_BE_POSITIVE";
    public const string DepositCannotBeNegative = "DEPOSIT_CANNOT_BE_NEGATIVE";

    // ── Roommates ────────────────────────────────────────────────────────────
    public const string BioRequired = "BIO_REQUIRED";
    public const string BioTooLong = "BIO_TOO_LONG";
    public const string ProfessionTooLong = "PROFESSION_TOO_LONG";
    public const string HobbiesTooLong = "HOBBIES_TOO_LONG";
    public const string BudgetMinMustBePositive = "BUDGET_MIN_MUST_BE_POSITIVE";
    public const string BudgetMaxBelowMin = "BUDGET_MAX_BELOW_MIN";
    public const string PreferredLocationTooLong = "PREFERRED_LOCATION_TOO_LONG";

    // ── Apartment applications ───────────────────────────────────────────────
    public const string ApartmentInvalid = "APARTMENT_INVALID";
    public const string StatusRequired = "STATUS_REQUIRED";
    public const string StatusInvalid = "STATUS_INVALID";

    // ── Messages ─────────────────────────────────────────────────────────────
    public const string ReceiverInvalid = "RECEIVER_INVALID";
    public const string MessageTooLong = "MESSAGE_TOO_LONG";
    public const string MessageEmpty = "MESSAGE_EMPTY";
    public const string MessageIdInvalid = "MESSAGE_ID_INVALID";
    public const string ReportedUserInvalid = "REPORTED_USER_INVALID";
    public const string ReasonRequired = "REASON_REQUIRED";
    public const string ReasonTooShort = "REASON_TOO_SHORT";
    public const string ReasonTooLong = "REASON_TOO_LONG";

    // ── Bills and tenancies ──────────────────────────────────────────────────
    public const string AmountInvalid = "AMOUNT_INVALID";
    public const string DueDayInvalid = "DUE_DAY_INVALID";
    public const string DueDateRequired = "DUE_DATE_REQUIRED";
    public const string StartDateRequired = "START_DATE_REQUIRED";
    public const string BankAccountInvalid = "BANK_ACCOUNT_INVALID";
    public const string PayeeNameRequired = "PAYEE_NAME_REQUIRED";
    public const string PayeeNameTooLong = "PAYEE_NAME_TOO_LONG";
    public const string PaymentCodeInvalid = "PAYMENT_CODE_INVALID";
    public const string PaymentModelInvalid = "PAYMENT_MODEL_INVALID";
    public const string PaymentReferenceInvalid = "PAYMENT_REFERENCE_INVALID";
    public const string TenantsRequired = "TENANTS_REQUIRED";
    public const string BillTitleTooLong = "BILL_TITLE_TOO_LONG";
    public const string BillKindInvalid = "BILL_KIND_INVALID";
    public const string BillPayeeInvalid = "BILL_PAYEE_INVALID";
    public const string UserInvalid = "USER_INVALID";
}
