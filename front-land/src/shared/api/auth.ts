import apiClient from './client';
import { LoginRequest, RegisterRequest, ForgotPasswordRequest, ResetPasswordRequest, User, PrivacySettings } from '../types/user';

export interface AuthTokenDto {
  accessToken: string;
  refreshToken?: string; // empty string from server — refresh token lives in httpOnly cookie
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

  rotateTokens: async (): Promise<AuthTokenDto> => {
    // Cookie carries the refresh token automatically via withCredentials: true
    const response = await apiClient.post<AuthTokenDto>(`/api/v1/auth/token/refresh`);
    return response.data;
  },

  forgotPassword: async (data: ForgotPasswordRequest): Promise<void> => {
    await apiClient.post(`/api/v1/auth/forgot-password`, data);
  },

  resetPassword: async (data: ResetPasswordRequest): Promise<void> => {
    await apiClient.post(`/api/v1/auth/reset-password`, data);
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

