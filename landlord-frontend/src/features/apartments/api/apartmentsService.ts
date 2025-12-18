import { apiClient } from '@/shared/api/client';
import { ApartmentDto, GetApartmentDto, ApartmentInputDto } from '../types';

const API_V1 = 'api/v1';
const RENT_BASE = `${API_V1}/rent`;

export const apartmentsService = {
  /**
   * Get all apartments
   */
  getAllApartments: async (): Promise<ApartmentDto[]> => {
    const response = await apiClient.get<ApartmentDto[]>(
      `${RENT_BASE}/get-all-apartments`
    );
    return response.data;
  },

  /**
   * Get apartment by ID
   */
  getApartment: async (id: number): Promise<GetApartmentDto> => {
    const response = await apiClient.get<GetApartmentDto>(
      `${RENT_BASE}/get-apartment`,
      {
        params: { id },
      }
    );
    return response.data;
  },

  /**
   * Create a new apartment
   */
  createApartment: async (data: ApartmentInputDto): Promise<ApartmentDto> => {
    const response = await apiClient.post<ApartmentDto>(
      `${RENT_BASE}/create-apartment`,
      data
    );
    return response.data;
  },

  /**
   * Delete apartment (Admin only)
   */
  deleteApartment: async (id: number): Promise<boolean> => {
    const response = await apiClient.delete<boolean>(
      `${RENT_BASE}/delete-apartment/${id}`
    );
    return response.data;
  },

  /**
   * Activate apartment
   */
  activateApartment: async (id: number): Promise<void> => {
    await apiClient.put(`${RENT_BASE}/activate-apartment/${id}`);
  },
};

