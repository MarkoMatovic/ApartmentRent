import { apiClient } from '@/shared/api/client';
import { ApartmentApplicationDto, ApartmentApplicationWithDetailsDto } from '../types/applications';

const API_V1 = 'api/v1';
// Note: This endpoint doesn't exist in backend yet, but structure is ready
const APPLICATIONS_BASE = `${API_V1}/applications`;

export const applicationsService = {
  /**
   * Get user's apartment applications
   * Note: Backend endpoint needs to be created
   */
  getUserApplications: async (userId: number): Promise<ApartmentApplicationWithDetailsDto[]> => {
    // This will fail until backend endpoint is created
    // For now, return empty array to show empty state
    try {
      const response = await apiClient.get<ApartmentApplicationWithDetailsDto[]>(
        `${APPLICATIONS_BASE}/user/${userId}`
      );
      return response.data;
    } catch (error) {
      // Return empty array if endpoint doesn't exist
      console.warn('Applications endpoint not available:', error);
      return [];
    }
  },

  /**
   * Get applications for an apartment
   */
  getApartmentApplications: async (apartmentId: number): Promise<ApartmentApplicationDto[]> => {
    try {
      const response = await apiClient.get<ApartmentApplicationDto[]>(
        `${APPLICATIONS_BASE}/apartment/${apartmentId}`
      );
      return response.data;
    } catch (error) {
      console.warn('Apartment applications endpoint not available:', error);
      return [];
    }
  },

  /**
   * Cancel an application
   */
  cancelApplication: async (applicationId: number): Promise<void> => {
    await apiClient.delete(`${APPLICATIONS_BASE}/${applicationId}`);
  },
};

