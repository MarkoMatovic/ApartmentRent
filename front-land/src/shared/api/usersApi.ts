import apiClient from './client';
import { User, PrivacySettings } from '../types/user';

export const usersApi = {
    getProfile: async (userId: number): Promise<User> => {
        const response = await apiClient.get<User>(`/api/v1/auth/profile/${userId}`);
        return response.data;
    },

    updateProfile: async (userId: number, data: Partial<User>): Promise<User> => {
        const response = await apiClient.put<User>(`/api/v1/auth/update-profile/${userId}`, data);
        return response.data;
    },

    updatePrivacySettings: async (userId: number, settings: PrivacySettings): Promise<User> => {
        const response = await apiClient.put<User>(`/api/v1/auth/update-privacy-settings/${userId}`, settings);
        return response.data;
    },
};
