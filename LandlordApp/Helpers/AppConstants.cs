namespace Lander.Helpers;

/// <summary>Role name constants — use instead of inline string literals.</summary>
public static class RoleConstants
{
    public const string Guest            = "Guest";
    public const string Tenant           = "Tenant";
    public const string Landlord         = "Landlord";
    public const string TenantLandlord   = "TenantLandlord";
    public const string PremiumTenant    = "Premium Tenant";
    public const string PremiumLandlord  = "Premium Landlord";
    public const string Broker           = "Broker";
    public const string Admin            = "Admin";
}

/// <summary>Subscription plan ID constants — must match Monri:Plans config keys.</summary>
public static class PlanConstants
{
    public const string PersonalAnalytics  = "personal_analytics";
    public const string LandlordAnalytics  = "landlord_analytics";
}

/// <summary>JWT claim name constants.</summary>
public static class ClaimConstants
{
    public const string UserId                 = "userId";
    public const string IsActive               = "isActive";
    public const string IsLookingForRoommate   = "isLookingForRoommate";
    public const string HasPersonalAnalytics   = "hasPersonalAnalytics";
    public const string HasLandlordAnalytics   = "hasLandlordAnalytics";
    public const string TokenBalance           = "tokenBalance";
    public const string IsIncognito            = "isIncognito";
    public const string UserRoleId             = "userRoleId";
    public const string PhoneNumber            = "phone_number";
    public const string Permission             = "permission";
}
