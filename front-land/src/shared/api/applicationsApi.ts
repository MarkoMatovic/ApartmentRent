import { apiClient } from './client';
import { ApartmentApplication, CreateApplicationDto, UpdateApplicationStatusDto } from '../types/application';

export const applicationsApi = {
    applyForApartment: async (data: CreateApplicationDto): Promise<ApartmentApplication> => {
        const response = await apiClient.post('/api/applications', data);
        return response.data;
    },

    getLandlordApplications: async (): Promise<ApartmentApplication[]> => {
        const response = await apiClient.get('/api/applications/landlord');
        return response.data;
    },

    getTenantApplications: async (): Promise<ApartmentApplication[]> => {
        const response = await apiClient.get('/api/applications/tenant');
        return response.data;
    },

    updateStatus: async (id: number, data: UpdateApplicationStatusDto): Promise<ApartmentApplication> => {
        const response = await apiClient.put(`/api/applications/${id}/status`, data);
        return response.data;
    },

    checkApprovalStatus: async (apartmentId: number): Promise<{ hasApprovedApplication: boolean; applicationStatus?: string; applicationId?: number }> => {
        const response = await apiClient.get(`/api/applications/check-approval/${apartmentId}`);
        return response.data;
    }
};
