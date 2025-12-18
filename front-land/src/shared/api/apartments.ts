import apiClient from './client';
import { Apartment, ApartmentDto, GetApartmentDto, ApartmentFilters, ApartmentInputDto } from '../types/apartment';

export const apartmentsApi = {
  getAll: async (filters?: ApartmentFilters): Promise<ApartmentDto[]> => {
    const response = await apiClient.get<ApartmentDto[]>(`/api/v1/rent/get-all-apartments`, {
      params: filters,
    });
    return response.data;
  },

  getById: async (id: number): Promise<GetApartmentDto> => {
    const response = await apiClient.get<GetApartmentDto>(`/api/v1/rent/get-apartment`, {
      params: { id },
    });
    return response.data;
  },

  create: async (data: ApartmentInputDto): Promise<Apartment> => {
    const response = await apiClient.post<Apartment>(`/api/v1/rent/create-apartment`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/v1/rent/delete-apartment/${id}`);
  },

  activate: async (id: number): Promise<void> => {
    await apiClient.post(`/api/v1/rent/activate-apartment/${id}`);
  },
};

