import axios from 'axios';
import { User, PrivacySettings } from '../types/user';

const API_URL = import.meta.env.VITE_API_URL || 'https://localhost:7092';

export const usersApi = {
    getProfile: async (userId: number): Promise<User> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.get(`${API_URL}/api/v1/auth/profile/${userId}`, {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });
        return response.data;
    },

    updateProfile: async (userId: number, data: Partial<User>): Promise<User> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.put(`${API_URL}/api/users/${userId}/profile`, data, {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });
        return response.data;
    },

    updatePrivacySettings: async (userId: number, settings: PrivacySettings): Promise<User> => {
        const token = localStorage.getItem('token');
        const response = await axios.put(`${API_URL}/api/users/${userId}/privacy`, settings, {
            headers: {
                Authorization: `Bearer ${token}`,
            },
        });
        return response.data;
    },
};
