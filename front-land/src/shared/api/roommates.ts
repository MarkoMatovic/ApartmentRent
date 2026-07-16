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

// The backend serializes enums as string names (JsonStringEnumConverter),
// while our types use numeric values — normalize on every read.
const GENDER_NAMES = ['PreferNotToSay', 'Male', 'Female', 'NonBinary', 'Other'];
const SCHEDULE_NAMES = ['Flexible', 'Morning', 'Evening', 'Night'];

const normalizeRoommate = (r: Roommate): Roommate => ({
  ...r,
  gender: typeof r.gender === 'string'
    ? (Math.max(0, GENDER_NAMES.indexOf(r.gender)) as Roommate['gender'])
    : r.gender,
  workSchedule: typeof r.workSchedule === 'string'
    ? (Math.max(0, SCHEDULE_NAMES.indexOf(r.workSchedule)) as Roommate['workSchedule'])
    : r.workSchedule,
});

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
    const items: Roommate[] = Array.isArray(response.data) ? response.data : (response.data?.items ?? []);
    return items.map(normalizeRoommate);
  },

  getById: async (id: number): Promise<Roommate> => {
    const response = await apiClient.get<Roommate>(`/api/v1/roommates/get-roommate`, { params: { id } });
    return normalizeRoommate(response.data);
  },

  getByUserId: async (userId: number): Promise<Roommate> => {
    const response = await apiClient.get<Roommate>(`/api/v1/roommates/get-roommate-by-user-id`, { params: { userId } });
    return normalizeRoommate(response.data);
  },

  create: async (data: RoommateInputDto): Promise<Roommate> => {
    const response = await apiClient.post<Roommate>(`/api/v1/roommates/create-roommate`, data);
    return normalizeRoommate(response.data);
  },

  update: async (id: number, data: RoommateInputDto): Promise<Roommate> => {
    const response = await apiClient.put<Roommate>(`/api/v1/roommates/update-roommate/${id}`, data);
    return normalizeRoommate(response.data);
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/v1/roommates/delete-roommate/${id}`);
  },
};
