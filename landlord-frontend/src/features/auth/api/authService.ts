import { apiClient, setAuthToken, clearAuthToken } from '@/shared/api/client';
import {
  UserRegistrationInputDto,
  UserRegistrationDto,
  LoginUserInputDto,
  LoginResponse,
  ChangePasswordInputDto,
  DeactivateUserInputDto,
  ReactivateUserInputDto,
  DeleteUserInputDto,
} from '../types';

const API_V1 = 'api/v1';
const AUTH_BASE = `${API_V1}/auth`;

export const authService = {
  /**
   * Register a new user
   */
  register: async (data: UserRegistrationInputDto): Promise<UserRegistrationDto> => {
    const response = await apiClient.post<UserRegistrationDto>(
      `${AUTH_BASE}/register`,
      data
    );
    return response.data;
  },

  /**
   * Login user and store JWT token
   */
  login: async (data: LoginUserInputDto): Promise<string> => {
    const response = await apiClient.post<LoginResponse>(
      `${AUTH_BASE}/login`,
      data
    );
    const token = response.data;
    setAuthToken(token);
    return token;
  },

  /**
   * Logout user
   */
  logout: async (): Promise<void> => {
    try {
      await apiClient.post(`${AUTH_BASE}/logout`);
    } finally {
      clearAuthToken();
    }
  },

  /**
   * Change user password
   */
  changePassword: async (data: ChangePasswordInputDto): Promise<void> => {
    await apiClient.post(`${AUTH_BASE}/change-password`, data);
  },

  /**
   * Deactivate user
   */
  deactivateUser: async (data: DeactivateUserInputDto): Promise<void> => {
    await apiClient.post(`${AUTH_BASE}/deactivate-user`, data);
  },

  /**
   * Reactivate user
   */
  reactivateUser: async (data: ReactivateUserInputDto): Promise<void> => {
    await apiClient.post(`${AUTH_BASE}/reactivate-user`, data);
  },

  /**
   * Delete user
   */
  deleteUser: async (data: DeleteUserInputDto): Promise<void> => {
    await apiClient.delete(`${AUTH_BASE}/delete-user`, { data });
  },
};

