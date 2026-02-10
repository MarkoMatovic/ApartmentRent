import { apiClient } from './client';
import { PermissionDto } from '../types/permission';

export const permissionsApi = {
    /**
     * Get all available permissions in the system
     */
    getAllPermissions: async (): Promise<PermissionDto[]> => {
        const response = await apiClient.get('/api/permissions');
        return response.data;
    },

    /**
     * Get permissions for a specific role
     */
    getPermissionsByRole: async (roleId: number): Promise<PermissionDto[]> => {
        const response = await apiClient.get(`/api/permissions/role/${roleId}`);
        return response.data;
    },

    /**
     * Get permissions for the currently authenticated user
     */
    getMyPermissions: async (): Promise<PermissionDto[]> => {
        const response = await apiClient.get('/api/permissions/my-permissions');
        return response.data;
    },
};
