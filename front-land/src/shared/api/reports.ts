import { apiClient } from './client';
import { ReportedMessage } from '../types/message';

export const reportsApi = {
    /**
     * Get all abuse reports (Admin only)
     */
    getAllReports: async (status?: string): Promise<ReportedMessage[]> => {
        const params = status ? { status } : {};
        const response = await apiClient.get('/api/v1/reports', { params });
        return response.data;
    },

    /**
     * Review a report (Admin only)
     */
    reviewReport: async (reportId: number, adminId: number, adminNotes?: string): Promise<void> => {
        await apiClient.put(`/api/v1/reports/${reportId}/review?adminId=${adminId}`, { adminNotes });
    },

    /**
     * Resolve a report (Admin only)
     */
    resolveReport: async (reportId: number, adminId: number, adminNotes?: string): Promise<void> => {
        await apiClient.put(`/api/v1/reports/${reportId}/resolve?adminId=${adminId}`, { adminNotes });
    },

    /**
     * Delete a report (Admin only)
     */
    deleteReport: async (reportId: number): Promise<void> => {
        await apiClient.delete(`/api/v1/reports/${reportId}`);
    },
};
