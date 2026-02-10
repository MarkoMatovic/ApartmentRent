import apiClient from './client';
import { LoginRequest, RegisterRequest, ForgotPasswordRequest, ResetPasswordRequest, User } from '../types/user';

export const authApi = {
  login: async (data: LoginRequest): Promise<string> => {
    const response = await apiClient.post<string>(`/api/v1/auth/login`, data);
    return response.data;
  },

  register: async (data: RegisterRequest): Promise<User> => {
    const response = await apiClient.post<User>(`/api/v1/auth/register`, data);
    return response.data;
  },

  logout: async (): Promise<void> => {
    await apiClient.post(`/api/v1/auth/logout`);
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

  updatePrivacySettings: async (userId: number, settings: any): Promise<any> => {
    const response = await apiClient.put<any>(`/api/v1/auth/update-privacy-settings/${userId}`, settings);
    return response.data;
  },

  exportUserData: async (userId: number): Promise<any> => {
    const response = await apiClient.get<any>(`/api/v1/auth/export-data/${userId}`);
    return response.data;
  },

  deleteUser: async (data: { userGuid: string }): Promise<void> => {
    await apiClient.delete('/api/v1/auth/delete-account', { data });
  },
};

