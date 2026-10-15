import apiClient, { refreshAccessToken } from './client';
import { LoginRequest, RegisterRequest, ForgotPasswordRequest, ResetPasswordRequest, User, PrivacySettings } from '../types/user';

export interface AuthTokenDto {
  accessToken: string;
  refreshToken?: string; // empty string from server — refresh token lives in httpOnly cookie
  /** The password was right; the account also wants the code from the authenticator app. */
  requiresTwoFactor?: boolean;
  /** Hand it to loginTwoFactor together with the code (valid for a few minutes). */
  twoFactorToken?: string;
}

export interface TwoFactorSetup {
  /** The key as text (Base32), for typing it into the app. */
  secret: string;
  setupUri: string;
  qrCodeDataUrl: string;
}

export interface TwoFactorEnabled {
  /** Shown once: each one can replace a code from the app a single time. */
  recoveryCodes: string[];
  /** This session now counts as confirmed. */
  accessToken: string;
}

export const authApi = {
  login: async (data: LoginRequest): Promise<AuthTokenDto> => {
    const response = await apiClient.post<AuthTokenDto>(`/api/v1/auth/login`, data);
    return response.data;
  },

  register: async (data: RegisterRequest): Promise<User> => {
    const response = await apiClient.post<User>(`/api/v1/auth/register`, data);
    return response.data;
  },

  logout: async (): Promise<void> => {
    // Refresh token is sent automatically via httpOnly cookie — no body payload needed
    await apiClient.post(`/api/v1/auth/logout`);
  },

  /** The code step of signing in: the app's code or a recovery code. */
  loginTwoFactor: async (twoFactorToken: string, code: string): Promise<AuthTokenDto> => {
    const response = await apiClient.post<AuthTokenDto>(`/api/v1/auth/two-factor/login`, { twoFactorToken, code });
    return response.data;
  },

  twoFactorSetup: async (): Promise<TwoFactorSetup> => {
    const response = await apiClient.post<TwoFactorSetup>(`/api/v1/auth/two-factor/setup`);
    return response.data;
  },

  twoFactorEnable: async (code: string): Promise<TwoFactorEnabled> => {
    const response = await apiClient.post<TwoFactorEnabled>(`/api/v1/auth/two-factor/enable`, { code });
    return response.data;
  },

  /** Resolves to a new access token for this session, which no longer counts as confirmed. */
  twoFactorDisable: async (code: string): Promise<string> => {
    const response = await apiClient.post<AuthTokenDto>(`/api/v1/auth/two-factor/disable`, { code });
    return response.data.accessToken;
  },

  /** Ends the sessions of every device of this user, this one included. */
  logoutEverywhere: async (): Promise<void> => {
    await apiClient.post(`/api/v1/auth/logout-everywhere`);
  },

  rotateTokens: async (): Promise<AuthTokenDto> => {
    // Shares a refresh that is already running instead of sending a second one.
    return { accessToken: await refreshAccessToken() };
  },

  forgotPassword: async (data: ForgotPasswordRequest): Promise<void> => {
    await apiClient.post(`/api/v1/auth/forgot-password`, data);
  },

  resetPassword: async (data: ResetPasswordRequest): Promise<void> => {
    await apiClient.post(`/api/v1/auth/reset-password`, data);
  },

  verifyEmail: async (token: string): Promise<void> => {
    await apiClient.get(`/api/v1/auth/verify-email`, { params: { token } });
  },

  updateRoommateStatus: async (userGuid: string, isLookingForRoommate: boolean): Promise<void> => {
    await apiClient.post(`/api/v1/auth/update-roommate-status`, { userGuid, isLookingForRoommate });
  },

  deactivateUser: async (userGuid: string): Promise<void> => {
    await apiClient.post(`/api/v1/auth/deactivate-user`, { userGuid });
  },

  updatePrivacySettings: async (userId: number, settings: PrivacySettings): Promise<User> => {
    const response = await apiClient.put<User>(`/api/v1/auth/update-privacy-settings/${userId}`, settings);
    return response.data;
  },

  exportUserData: async (userId: number): Promise<User> => {
    const response = await apiClient.get<User>(`/api/v1/auth/export-data/${userId}`);
    return response.data;
  },

  deleteUser: async (data: { userGuid: string }): Promise<void> => {
    await apiClient.delete('/api/v1/auth/delete-user', { data });
  },

  changePassword: async (data: { userId: string; oldPassword: string; newPassword: string }): Promise<void> => {
    await apiClient.post('/api/v1/auth/change-password', data);
  },
};

