namespace Lander.src.Modules.Users.Domain.Constants
{
    /// <summary>
    /// Contains all permission constants used throughout the application.
    /// Permissions are organized by functional area.
    /// </summary>
    public static class PermissionConstants
    {
        // ===== APARTMENT MANAGEMENT =====
        public const string ApartmentsCreate = "apartments.create";
        public const string ApartmentsEditOwn = "apartments.edit.own";
        public const string ApartmentsEditAny = "apartments.edit.any";
        public const string ApartmentsDeleteOwn = "apartments.delete.own";
        public const string ApartmentsDeleteAny = "apartments.delete.any";
        public const string ApartmentsViewAll = "apartments.view.all";
        public const string ApartmentsPublish = "apartments.publish";

        // ===== USER MANAGEMENT =====
        public const string UsersViewAll = "users.view.all";
        public const string UsersEditOwn = "users.edit.own";
        public const string UsersEditAny = "users.edit.any";
        public const string UsersDelete = "users.delete";
        public const string UsersBan = "users.ban";
        public const string UsersRolesManage = "users.roles.manage";

        // ===== APPLICATIONS =====
        public const string ApplicationsCreate = "applications.create";
        public const string ApplicationsViewOwn = "applications.view.own";
        public const string ApplicationsViewReceived = "applications.view.received";
        public const string ApplicationsManage = "applications.manage";

        // ===== COMMUNICATION =====
        public const string MessagesSend = "messages.send";
        public const string MessagesViewOwn = "messages.view.own";
        public const string MessagesViewAll = "messages.view.all";
        public const string MessagesDeleteOwn = "messages.delete.own";
        public const string MessagesDeleteAny = "messages.delete.any";

        // ===== REVIEWS =====
        public const string ReviewsCreate = "reviews.create";
        public const string ReviewsEditOwn = "reviews.edit.own";
        public const string ReviewsDeleteOwn = "reviews.delete.own";
        public const string ReviewsDeleteAny = "reviews.delete.any";
        public const string ReviewsModerate = "reviews.moderate";

        // ===== ANALYTICS =====
        public const string AnalyticsViewPersonal = "analytics.view.personal";
        public const string AnalyticsViewLandlord = "analytics.view.landlord";
        public const string AnalyticsViewSystem = "analytics.view.system";
        public const string AnalyticsMlManage = "analytics.ml.manage";

        // ===== ROOMMATES =====
        public const string RoommatesCreate = "roommates.create";
        public const string RoommatesEditOwn = "roommates.edit.own";
        public const string RoommatesSearch = "roommates.search";
        public const string RoommatesMatch = "roommates.match";

        // ===== APPOINTMENTS =====
        public const string AppointmentsCreate = "appointments.create";
        public const string AppointmentsViewOwn = "appointments.view.own";
        public const string AppointmentsViewReceived = "appointments.view.received";
        public const string AppointmentsManage = "appointments.manage";

        // ===== FAVORITES =====
        public const string FavoritesAdd = "favorites.add";
        public const string FavoritesRemove = "favorites.remove";
        public const string FavoritesView = "favorites.view";

        /// <summary>
        /// Gets all permission constants as a list.
        /// </summary>
        public static List<(string Name, string Description)> GetAllPermissions()
        {
            return new List<(string, string)>
            {
                // Apartment Management
                (ApartmentsCreate, "Create new apartment listings"),
                (ApartmentsEditOwn, "Edit own apartment listings"),
                (ApartmentsEditAny, "Edit any apartment listing"),
                (ApartmentsDeleteOwn, "Delete own apartment listings"),
                (ApartmentsDeleteAny, "Delete any apartment listing"),
                (ApartmentsViewAll, "View all apartments including inactive"),
                (ApartmentsPublish, "Publish apartment listings"),

                // User Management
                (UsersViewAll, "View all users in the system"),
                (UsersEditOwn, "Edit own user profile"),
                (UsersEditAny, "Edit any user profile"),
                (UsersDelete, "Delete user accounts"),
                (UsersBan, "Ban/unban users"),
                (UsersRolesManage, "Manage user roles"),

                // Applications
                (ApplicationsCreate, "Submit apartment applications"),
                (ApplicationsViewOwn, "View own applications"),
                (ApplicationsViewReceived, "View received applications (landlord)"),
                (ApplicationsManage, "Accept/reject applications"),

                // Communication
                (MessagesSend, "Send messages to other users"),
                (MessagesViewOwn, "View own messages"),
                (MessagesViewAll, "View all messages (moderation)"),
                (MessagesDeleteOwn, "Delete own messages"),
                (MessagesDeleteAny, "Delete any message"),

                // Reviews
                (ReviewsCreate, "Create reviews"),
                (ReviewsEditOwn, "Edit own reviews"),
                (ReviewsDeleteOwn, "Delete own reviews"),
                (ReviewsDeleteAny, "Delete any review"),
                (ReviewsModerate, "Moderate reviews"),

                // Analytics
                (AnalyticsViewPersonal, "View personal analytics"),
                (AnalyticsViewLandlord, "View landlord analytics"),
                (AnalyticsViewSystem, "View system-wide analytics"),
                (AnalyticsMlManage, "Manage ML models"),

                // Roommates
                (RoommatesCreate, "Create roommate profile"),
                (RoommatesEditOwn, "Edit own roommate profile"),
                (RoommatesSearch, "Search for roommates"),
                (RoommatesMatch, "Access roommate matching"),

                // Appointments
                (AppointmentsCreate, "Schedule apartment viewings"),
                (AppointmentsViewOwn, "View own appointments"),
                (AppointmentsViewReceived, "View received appointment requests"),
                (AppointmentsManage, "Accept/reject appointments"),

                // Favorites
                (FavoritesAdd, "Add apartments to favorites"),
                (FavoritesRemove, "Remove apartments from favorites"),
                (FavoritesView, "View favorite apartments")
            };
        }
    }
}
