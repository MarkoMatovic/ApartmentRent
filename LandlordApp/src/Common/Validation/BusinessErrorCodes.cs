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
}
