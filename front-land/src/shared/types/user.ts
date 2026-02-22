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
  hasPersonalAnalytics?: boolean;
  hasLandlordAnalytics?: boolean;
  subscriptionExpiresAt?: string;
  analyticsConsent?: boolean;
  chatHistoryConsent?: boolean;
  profileVisibility?: boolean;
  averageRating?: number;
  reviewCount?: number;
}

export interface PrivacySettings {
  analyticsConsent: boolean;
  chatHistoryConsent: boolean;
  profileVisibility: boolean;
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

