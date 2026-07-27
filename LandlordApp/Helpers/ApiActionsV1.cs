namespace Lander.Helpers;

public static class ApiActionsV1
{
    public const string V1 = "api/v1";

    #region UserActions
    public const string Auth = $"{V1}/auth";
    public const string Register = "register";
    public const string Login = "login";
    public const string Logout = "logout";
    public const string DeleteUser = "delete-user/{userGuid}";
    public const string DeactivateUser = "deactivate-user";
    public const string ReactivateUser = "reactivate-user";
    public const string ChangePassword = "change-password";
    public const string UpdateRoommateStatus = "update-roommate-status";
    public const string GetUserProfile = "profile/{userId}";
    public const string UpdateUserProfile = "update-profile/{userId}";
    public const string UpdatePrivacySettings = "update-privacy-settings/{userId}";
    public const string ExportUserData = "export-data/{userId}";
    public const string RefreshToken = "token/refresh";
    public const string SendVerificationEmail = "send-verification-email/{userId}";
    public const string VerifyEmail = "verify-email";
    public const string ForgotPassword = "forgot-password";
    public const string ResetPassword = "reset-password";
    #endregion

    #region ApartmentActions
    public const string Rent = $"{V1}/rent";
    public const string CreateApartment = "create-apartment";
    public const string GetApartment = "get-apartment";
    public const string GetAllApartments = "get-all-apartments";
    public const string GetAllApartmentsKeyset = "keyset";
    public const string GetMyApartments = "get-my-apartments";
    public const string UpdateApartment = "update-apartment/{id}";
    public const string DeleteApartment = "delete-apartment/{id}";
    public const string ActivateApartment = "activate-apartment/{id}";
    public const string UploadImages = "upload-images";
    public const string GetNeighbourhood = "neighbourhood/{apartmentId:int}";
    public const string SemanticSearch = "semantic-search";
    public const string GenerateEmbeddings = "generate-embeddings";
    public const string MigrateImagesToBlob = "admin/migrate-images-to-blob";
    #endregion

    #region NotificationActions
    public const string Notification = $"{V1}/notification";
    public const string SendNotification = "send-notification";
    public const string MarkAsRead = "mark-as-read";
    public const string MarkAllAsRead = "mark-all-as-read";
    public const string GetUserNotifications = "get-user-notifications";
    public const string DeleteNotification = "delete/{id}";
    #endregion

    #region NotificationStream
    public const string NotificationsStream = "api/notifications";
    public const string StreamNotifications = "stream";
    public const string SendTestNotification = "test";
    public const string GetConnectionCount = "connections";
    #endregion

    #region Reviews
    public const string Reviews = $"{V1}/reviews";
    public const string CreateReview = "create-review";
    public const string CreateFavorite = "create-favorite";
    public const string GetReviewById = "get-review-by-id";
    public const string GetReviewsByApartmentId = "apartment/{apartmentId}";
    public const string DeleteReview = "delete-review/{id}";
    public const string DeleteFavorite = "delete-favorite/{id}";
    public const string GetUserFavorites = "favorites/{userId}";
    #endregion

    #region Roommates
    public const string Roommates = $"{V1}/roommates";
    public const string GetAllRoommates = "get-all-roommates";
    public const string GetRoommate = "get-roommate";
    public const string GetRoommateByUserId = "get-roommate-by-user-id";
    public const string CreateRoommate = "create-roommate";
    public const string UpdateRoommate = "update-roommate/{id}";
    public const string DeleteRoommate = "delete-roommate/{id}";
    #endregion

    #region SearchRequests (Gesuche)
    public const string SearchRequests = $"{V1}/search-requests";
    public const string GetAllSearchRequests = "get-all-search-requests";
    public const string GetSearchRequest = "get-search-request";
    public const string GetSearchRequestsByUserId = "get-search-requests-by-user-id";
    public const string CreateSearchRequest = "create-search-request";
    public const string UpdateSearchRequest = "update-search-request/{id}";
    public const string DeleteSearchRequest = "delete-search-request/{id}";
    #endregion

    #region SavedSearches
    public const string SavedSearches = $"{V1}/saved-searches";
    public const string GetSavedSearchesByUserId = "get-saved-searches-by-user-id";
    public const string GetSavedSearch = "get-saved-search";
    public const string CreateSavedSearch = "create-saved-search";
    public const string UpdateSavedSearch = "update-saved-search/{id}";
    public const string DeleteSavedSearch = "delete-saved-search/{id}";
    #endregion

    #region Messages
    public const string Messages = $"{V1}/messages";
    public const string GetConversation = "conversation";
    public const string GetUserConversations = "user/{userId}";
    public const string SendMessage = "send";
    public const string MarkMessageAsRead = "mark-read/{messageId}";
    public const string GetUnreadCount = "unread-count/{userId}";
    public const string UploadMessageFile = "upload";
    public const string DownloadMessageFile = "files/{filename}";
    public const string ArchiveConversation = "archive";
    public const string UnarchiveConversation = "unarchive";
    public const string MuteConversation = "mute";
    public const string UnmuteConversation = "unmute";
    public const string BlockUser = "block";
    public const string UnblockUser = "unblock";
    public const string DeleteConversation = "delete-conversation";
    public const string SearchMessages = "search";
    public const string ReportAbuse = "report";
    #endregion

    #region Appointments
    public const string Appointments = "api/appointments";
    public const string GetMyAppointments = "my-appointments";
    public const string GetLandlordAppointments = "landlord-appointments";
    public const string GetAvailableSlots = "available-slots/{apartmentId}";
    public const string UpdateAppointmentStatus = "{id}/status";
    public const string CancelAppointment = "{id}";
    public const string GetAppointmentById = "{id}";
    public const string GetMyAvailability = "availability";
    public const string SetMyAvailability = "availability";
    #endregion

    #region ApartmentApplications
    public const string Applications = "api/applications";
    public const string GetLandlordApplications = "landlord";
    public const string GetTenantApplications = "tenant";
    public const string UpdateApplicationStatus = "{id}/status";
    public const string CheckApprovalStatus = "check-approval/{apartmentId}";
    #endregion

    #region Payments
    public const string Payments = "api/payments";
    public const string GetSubscriptionPlans = "plans";
    public const string CreatePayment = "create-payment";
    public const string GetMyPaymentStatus = "my-status";
    public const string GetMyOrders = "my-orders";
    public const string CancelAnalytics = "cancel-analytics";
    public const string PaymentCallback = "callback";
    #endregion

    #region Reports
    public const string Reports = $"{V1}/reports";
    public const string ReviewReport = "{reportId}/review";
    public const string ResolveReport = "{reportId}/resolve";
    public const string DeleteReport = "{reportId}";
    #endregion

    #region MachineLearning
    public const string MachineLearning = $"{V1}/ml";
    public const string PredictPrice = "predict-price";
    public const string TrainPriceModel = "train-price-model";
    public const string GetModelMetrics = "model-metrics";
    public const string IsModelTrained = "is-model-trained";
    public const string GetRoommateMatches = "roommate-matches";
    public const string CalculateMatchScore = "match-score";
    #endregion

    #region Analytics
    public const string Analytics = $"{V1}/analytics";
    public const string TrackEvent = "track-event";
    public const string GetAnalyticsSummary = "summary";
    public const string GetTopViewedApartments = "top-apartments";
    public const string GetTopViewedRoommates = "top-roommates";
    public const string GetTopSearchTerms = "top-searches";
    public const string GetEventTrends = "trends";

    // User-specific analytics
    public const string GetUserRoommateSummary = "user-roommate-summary";
    public const string GetUserTopRoommates = "user-top-roommates";
    public const string GetUserSearches = "user-searches";
    public const string GetUserRoommateTrends = "user-roommate-trends";
    public const string GetUserTopApartments = "user-top-apartments";
    public const string GetUserCompleteAnalytics = "user-complete-analytics";
    public const string GetMyViewedApartments = "my-viewed-apartments";
    public const string GetMyApartmentViews = "my-apartment-views";
    public const string GetMyMessagesSent = "my-messages-sent";
    #endregion
}
