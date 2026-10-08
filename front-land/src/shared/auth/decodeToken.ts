import { User } from '../types/user';
import { Permission } from '../types/permission';

// TokenProvider adds the role as ClaimTypes.Role, and the token handler writes that under its full URI
// (not "role"). Reading only payload.role therefore always yielded undefined, which is why the UI
// had to guess "admin" from userRoleId === 1 - a database-specific number that is NOT stable
// (a database built from migrations has Tenant = 1).
export const ROLE_CLAIM_URI = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

export const decodeToken = (token: string): User | null => {
  try {
    if (!token || typeof token !== 'string') {
      return null;
    }

    const parts = token.split('.');
    if (parts.length !== 3) {
      return null;
    }

    const base64Url = parts[1];
    if (!base64Url) {
      return null;
    }

    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    const payload = JSON.parse(jsonPayload);

    let permissions: string[] = [];
    if (payload.permission) {
      permissions = Array.isArray(payload.permission)
        ? payload.permission
        : [payload.permission];
    }

    return {
      userId: parseInt(payload.userId || payload.nameid || payload.id) || -1,
      userGuid: payload.sub || payload.nameid || '',
      firstName: payload.given_name || payload.givenName || payload.first_name || '',
      lastName: payload.family_name || payload.familyName || payload.last_name || '',
      email: payload.email || payload.emailaddress || '',
      phoneNumber: payload.phone_number || payload.phone,
      isActive: payload.isActive === 'true' || payload.isActive === true || payload.active === true,
      isLookingForRoommate: payload.isLookingForRoommate === 'true' || payload.isLookingForRoommate === true,
      userRoleId: payload.userRoleId ? parseInt(payload.userRoleId) : undefined,
      roleName: payload[ROLE_CLAIM_URI] || payload.role || payload.roleName,
      permissions: permissions as Permission[],
      hasPersonalAnalytics: payload.hasPersonalAnalytics === 'true' || payload.hasPersonalAnalytics === true,
      hasLandlordAnalytics: payload.hasLandlordAnalytics === 'true' || payload.hasLandlordAnalytics === true,
      subscriptionExpiresAt: payload.subscriptionExpiresAt || undefined,
      tokenBalance: payload.tokenBalance !== undefined ? parseInt(payload.tokenBalance) : 3,
      listingCredits: payload.listingCredits !== undefined ? parseInt(payload.listingCredits) : 0,
      isIncognito: payload.isIncognito === 'true' || payload.isIncognito === true,
    };
  } catch (error) {
    return null;
  }
};
