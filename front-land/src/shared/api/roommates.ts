import apiClient from './client';
import { Roommate, RoommateFilters } from '../types/roommate';

export const roommatesApi = {
  getAll: async (filters?: RoommateFilters): Promise<Roommate[]> => {
    const response = await apiClient.get<Roommate[]>(`/api/v1/roommates`, {
      params: filters,
    });
    return response.data;
  },

  getById: async (id: number): Promise<Roommate> => {
    const response = await apiClient.get<Roommate>(`/api/v1/roommates/${id}`);
    return response.data;
  },

  create: async (data: Partial<Roommate>): Promise<Roommate> => {
    const response = await apiClient.post<Roommate>(`/api/v1/roommates`, data);
    return response.data;
  },

  update: async (id: number, data: Partial<Roommate>): Promise<Roommate> => {
    const response = await apiClient.put<Roommate>(`/api/v1/roommates/${id}`, data);
    return response.data;
  },
};

