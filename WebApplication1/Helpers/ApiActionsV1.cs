using Microsoft.Identity.Client;

namespace Lander.Helpers;

public static class ApiActionsV1
{
    public const string V1 = "api/v1";

    #region UserActions
    public const string Auth = $"{V1}/auth";
    public const string Register = "register";
    public const string Login = "login";
    public const string Logout = "logout";
    public const string DeleteUser = "delete-user";
    public const string DeactivateUser = "deactivate-user";
    public const string ReactivateUser = "reactivate-user";
    public const string ChangePassword = "change-password";
    #endregion

    #region ApartmentActions
    public const string Rent = $"{V1}/rent";
    public const string CreateApartment = "create-apartment";
    public const string GetApartment = "get-apartment";
    public const string GetAllApartments = "get-all-apartments";
    public const string DeleteApartment = "delete-apartment/{id}";
    public const string ActivateApartment = "activate-apartment/{id}";
    #endregion

    #region NotificationActions
    public const string Notification = $"{V1}/notification";
    public const string SendNotification = "send-notification";
    public const string MarkAsRead = "mark-as-read";
    public const string GetUserNotifications = "get-user-notifications";
    #endregion

    #region Reviews
    public const string Reviews = $"{V1}/reviews";
    public const string CreateReview = "create-review";
    public const string CreateFavorite = "create-favorite";
    public const string GetReviewById = "get-review-by-id";
    #endregion

    #region Sms
    public const string Sms = $"{V1}/sms";
    public const string SendSms = "send";
    #endregion
}
