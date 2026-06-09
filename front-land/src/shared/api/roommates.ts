import apiClient from './client';
import { Roommate, RoommateFilters } from '../types/roommate';

export interface RoommateInputDto {
  bio?: string;
  hobbies?: string;
  profession?: string;
  smokingAllowed?: boolean;
  petFriendly?: boolean;
  lifestyle?: string;
  cleanliness?: string;
  guestsAllowed?: boolean;
  budgetMin?: number;
  budgetMax?: number;
  budgetIncludes?: string;
  availableFrom?: string;
  availableUntil?: string;
  minimumStayMonths?: number;
  maximumStayMonths?: number;
  lookingForRoomType?: string;
  lookingForApartmentType?: string;
  preferredLocation?: string;
  // New fields
  gender?: number;
  languages?: string;
  workSchedule?: number;
  musicFriendly?: boolean;
}

export const roommatesApi = {
  getAll: async (filters?: RoommateFilters): Promise<Roommate[]> => {
    const params: Record<string, any> = {};
    if (filters?.location)            params.location = filters.location;
    if (filters?.minBudget)           params.minBudget = filters.minBudget;
    if (filters?.maxBudget)           params.maxBudget = filters.maxBudget;
    if (filters?.smokingAllowed !== undefined) params.smokingAllowed = filters.smokingAllowed;
    if (filters?.petFriendly !== undefined)    params.petFriendly = filters.petFriendly;
    if (filters?.lifestyle)           params.lifestyle = filters.lifestyle;
    if (filters?.profession)          params.profession = filters.profession;
    if (filters?.availableFrom)       params.availableFrom = filters.availableFrom;
    if (filters?.stayDuration)        params.stayDuration = filters.stayDuration;
    if (filters?.apartmentId)         params.apartmentId = filters.apartmentId;
    if (filters?.workSchedule !== undefined) params.workSchedule = filters.workSchedule;
    if (filters?.gender !== undefined)       params.gender = filters.gender;

    const response = await apiClient.get<any>(`/api/v1/roommates/get-all-roommates`, { params });
    return Array.isArray(response.data) ? response.data : (response.data?.items ?? []);
  },

  getById: async (id: number): Promise<Roommate> => {
    const response = await apiClient.get<Roommate>(`/api/v1/roommates/get-roommate`, { params: { id } });
    return response.data;
  },

  getByUserId: async (userId: number): Promise<Roommate> => {
    const response = await apiClient.get<Roommate>(`/api/v1/roommates/get-roommate-by-user-id`, { params: { userId } });
    return response.data;
  },

  create: async (data: RoommateInputDto): Promise<Roommate> => {
    const response = await apiClient.post<Roommate>(`/api/v1/roommates/create-roommate`, data);
    return response.data;
  },

  update: async (id: number, data: RoommateInputDto): Promise<Roommate> => {
    const response = await apiClient.put<Roommate>(`/api/v1/roommates/update-roommate/${id}`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/v1/roommates/delete-roommate/${id}`);
  },
};
