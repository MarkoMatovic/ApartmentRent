namespace Lander.src.Common.Validation;

/// <summary>
/// Codes for <see cref="Lander.src.Common.Exceptions.BusinessRuleException"/>. Like
/// <see cref="ValidationCodes"/> each one needs a translation in the frontend `validation`
/// namespace (a frontend test fails if it is missing).
/// </summary>
public static class BusinessErrorCodes
{
    public const string IncorrectOldPassword = "INCORRECT_OLD_PASSWORD";
    public const string InsufficientTokens = "INSUFFICIENT_TOKENS";
    public const string MessagingNotAllowed = "MESSAGING_NOT_ALLOWED";
    public const string InvalidFileUrl = "INVALID_FILE_URL";
    public const string TimeSlotTaken = "TIME_SLOT_TAKEN";
    public const string PairingWithSelf = "PAIRING_WITH_SELF";
    public const string PairingNotPending = "PAIRING_NOT_PENDING";
    public const string PairingNotActive = "PAIRING_NOT_ACTIVE";
    public const string PairingRequired = "PAIRING_REQUIRED";
    /// <summary>The person you want to pair with already has a roommate.</summary>
    public const string RoommateAlreadyTaken = "ROOMMATE_ALREADY_TAKEN";
    /// <summary>The signed-in user already has a roommate.</summary>
    public const string YouAlreadyHaveRoommate = "YOU_ALREADY_HAVE_ROOMMATE";
    public const string GroupChatNotAllowed = "GROUP_CHAT_NOT_ALLOWED";
    /// <summary>The profile picture is not a PNG, JPEG, WebP, GIF or BMP image.</summary>
    public const string InvalidProfilePicture = "INVALID_PROFILE_PICTURE";
    public const string ProfilePictureTooLarge = "PROFILE_PICTURE_TOO_LARGE";
    public const string InvalidTwoFactorCode = "INVALID_TWO_FACTOR_CODE";
    public const string TwoFactorAlreadyEnabled = "TWO_FACTOR_ALREADY_ENABLED";
    public const string TwoFactorNotEnabled = "TWO_FACTOR_NOT_ENABLED";
    /// <summary>Turning it on needs the setup (QR code) first.</summary>
    public const string TwoFactorSetupRequired = "TWO_FACTOR_SETUP_REQUIRED";
    /// <summary>The code step came too late (or with a token that is not ours): sign in again.</summary>
    public const string TwoFactorExpired = "TWO_FACTOR_EXPIRED";

    // ── Bills and tenancies ──────────────────────────────────────────────────
    /// <summary>The apartment already has an active tenancy.</summary>
    public const string TenancyAlreadyActive = "TENANCY_ALREADY_ACTIVE";
    public const string TenancyNotActive = "TENANCY_NOT_ACTIVE";
    /// <summary>Only people with an approved application for the apartment (or their roommate) can be added.</summary>
    public const string TenantNotEligible = "TENANT_NOT_ELIGIBLE";
    public const string TenantAlreadyInTenancy = "TENANT_ALREADY_IN_TENANCY";
    public const string LandlordCannotBeTenant = "LANDLORD_CANNOT_BE_TENANT";
    /// <summary>The bill has payments already: it can no longer be changed or deleted.</summary>
    public const string BillHasPayments = "BILL_HAS_PAYMENTS";
    public const string BillAlreadyPaid = "BILL_ALREADY_PAID";
    public const string BillNotPaid = "BILL_NOT_PAID";
    /// <summary>Only the landlord can ask to be paid; tenants enter bills that are paid to a company.</summary>
    public const string OnlyLandlordCanBePaid = "ONLY_LANDLORD_CAN_BE_PAID";
    public const string CompanyPayeeRequired = "COMPANY_PAYEE_REQUIRED";
    public const string PaymentAlreadyConfirmed = "PAYMENT_ALREADY_CONFIRMED";
    public const string PaymentNotMarked = "PAYMENT_NOT_MARKED";
    /// <summary>Someone else changed the payment a moment ago: reload and try again.</summary>
    public const string PaymentChanged = "PAYMENT_CHANGED";
    /// <summary>The roommates pay their part back to the payer, so the payer's account is needed.</summary>
    public const string PayoutAccountRequired = "PAYOUT_ACCOUNT_REQUIRED";
    public const string NoTenants = "NO_TENANTS";
}
