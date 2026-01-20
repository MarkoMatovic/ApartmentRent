import apiClient from './client';
import { Apartment, ApartmentDto, GetApartmentDto, ApartmentFilters, ApartmentInputDto, ApartmentUpdateInputDto } from '../types/apartment';

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const apartmentsApi = {
  getAll: async (filters?: ApartmentFilters): Promise<PagedResponse<ApartmentDto>> => {
    const params: Record<string, any> = {};

    if (filters) {
      if (filters.listingType !== undefined) params.listingType = filters.listingType;
      if (filters.city) params.city = filters.city;
      if (filters.minRent) params.minRent = Number(filters.minRent);
      if (filters.maxRent) params.maxRent = Number(filters.maxRent);
      if (filters.numberOfRooms) params.numberOfRooms = Number(filters.numberOfRooms);
      if (filters.apartmentType !== undefined) params.apartmentType = filters.apartmentType;
      if (filters.isFurnished !== undefined) params.isFurnished = filters.isFurnished;
      if (filters.hasParking !== undefined) params.hasParking = filters.hasParking;
      if (filters.hasBalcony !== undefined) params.hasBalcony = filters.hasBalcony;
      if (filters.isPetFriendly !== undefined) params.isPetFriendly = filters.isPetFriendly;
      if (filters.isSmokingAllowed !== undefined) params.isSmokingAllowed = filters.isSmokingAllowed;
      if (filters.isImmediatelyAvailable !== undefined) params.isImmediatelyAvailable = filters.isImmediatelyAvailable;
      if (filters.page) params.page = filters.page;
      if (filters.pageSize) params.pageSize = filters.pageSize;
    }

    const response = await apiClient.get<PagedResponse<ApartmentDto>>(`/api/v1/rent/get-all-apartments`, {
      params,
    });

    return response.data;
  },

  getById: async (id: number): Promise<GetApartmentDto> => {
    const response = await apiClient.get<GetApartmentDto>(`/api/v1/rent/get-apartment`, {
      params: { id },
    });
    return response.data;
  },

  getMyApartments: async (): Promise<ApartmentDto[]> => {
    const response = await apiClient.get<any>(`/api/v1/rent/get-my-apartments`);
    let apartments = response.data?.items || response.data?.Items || response.data;
    if (!Array.isArray(apartments)) {
      apartments = [];
    }
    return apartments;
  },

  create: async (data: ApartmentInputDto): Promise<Apartment> => {
    const response = await apiClient.post<Apartment>(`/api/v1/rent/create-apartment`, data);
    return response.data;
  },

  update: async (id: number, data: ApartmentUpdateInputDto): Promise<ApartmentDto> => {
    const response = await apiClient.put<ApartmentDto>(`/api/v1/rent/update-apartment/${id}`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/v1/rent/delete-apartment/${id}`);
  },

  activate: async (id: number): Promise<void> => {
    await apiClient.post(`/api/v1/rent/activate-apartment/${id}`);
  },

  uploadImages: async (files: File[]): Promise<string[]> => {
    const formData = new FormData();
    files.forEach((file) => {
      formData.append('files', file);
    });

    const response = await apiClient.post<string[]>(`/api/v1/rent/upload-images`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },
};

