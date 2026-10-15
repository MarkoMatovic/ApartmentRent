import { Permission } from './permission';
import { RoleName } from './role';

export interface User {
  userId: number;
  userGuid: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  profilePicture?: string;
  userRoleId?: number;
  userRole?: UserRole;
  roleName?: RoleName;
  permissions?: Permission[];
  dateOfBirth?: string;
  isActive: boolean;
  isLookingForRoommate?: boolean;
  // Premium features (from JWT claims + API)
  hasPersonalAnalytics?: boolean;
  hasLandlordAnalytics?: boolean;
  tokenBalance?: number;
  listingCredits?: number;
  // Timed features (from /api/payments/my-status)
  boostedUntil?: string;
  priorityInboxUntil?: string;
  // Legacy
  subscriptionExpiresAt?: string;
  // Privacy settings
  analyticsConsent?: boolean;
  chatHistoryConsent?: boolean;
  profileVisibility?: boolean;
  isIncognito?: boolean;
  // Rating
  averageRating?: number;
  reviewCount?: number;
  // Two-step sign-in
  /** This session was confirmed with an authenticator code (from the token). */
  twoFactorVerified?: boolean;
  /** From the stored profile. */
  twoFactorEnabled?: boolean;
  /** Some features of the account work only once two-step sign-in is on (from the stored profile). */
  twoFactorRequired?: boolean;
}

export interface PrivacySettings {
  analyticsConsent: boolean;
  chatHistoryConsent: boolean;
  profileVisibility: boolean;
  isIncognito: boolean;
}

export interface UserRole {
  roleId: number;
  roleName: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  dateOfBirth?: string;
  phoneNumber?: string;
  profilePicture?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface AuthResponse {
  token: string;
  user?: User;
}
