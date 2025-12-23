import apiClient from './client';
import { Apartment, ApartmentDto, GetApartmentDto, ApartmentFilters, ApartmentInputDto } from '../types/apartment';

export const apartmentsApi = {
  getAll: async (filters?: ApartmentFilters): Promise<ApartmentDto[]> => {
    const params: Record<string, any> = {};
    
    console.log('[apartmentsApi] Input filters:', filters);
    
    if (filters) {
      if (filters.city) params.city = filters.city;
      if (filters.minRent) params.minRent = Number(filters.minRent);
      if (filters.maxRent) params.maxRent = Number(filters.maxRent);
      if (filters.numberOfRooms) params.numberOfRooms = Number(filters.numberOfRooms);
      if (filters.apartmentType !== undefined && filters.apartmentType !== '') params.apartmentType = filters.apartmentType;
      if (filters.isFurnished !== undefined && filters.isFurnished !== '') params.isFurnished = filters.isFurnished;
      if (filters.hasParking !== undefined) params.hasParking = filters.hasParking;
      if (filters.hasBalcony !== undefined) params.hasBalcony = filters.hasBalcony;
      if (filters.isPetFriendly !== undefined) params.isPetFriendly = filters.isPetFriendly;
      if (filters.isSmokingAllowed !== undefined) params.isSmokingAllowed = filters.isSmokingAllowed;
      if (filters.isImmediatelyAvailable !== undefined) params.isImmediatelyAvailable = filters.isImmediatelyAvailable;
      if (filters.page) params.page = filters.page;
      if (filters.pageSize) params.pageSize = filters.pageSize;
    }
    
    console.log('[apartmentsApi] Sending params:', params);
    
    const response = await apiClient.get<any>(`/api/v1/rent/get-all-apartments`, {
      params,
    });
    
    const apartments = response.data.items || response.data;
    
    console.log('[apartmentsApi] Received apartments count:', apartments.length);
    console.log('[apartmentsApi] First 3 apartments:', apartments.slice(0, 3).map((a: any) => ({ id: a.apartmentId, title: a.title, rent: a.rent })));
    
    return apartments;
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

