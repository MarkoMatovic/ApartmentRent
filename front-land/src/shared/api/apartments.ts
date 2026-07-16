import apiClient from './client';
import { ApartmentDto, GetApartmentDto, ApartmentFilters, ApartmentInputDto, ApartmentUpdateInputDto, UploadedImage } from '../types/apartment';

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// The backend serializes enums as string names (JsonStringEnumConverter) —
// normalize them back to the backend's numeric values on every read.
const LISTING_TYPE_VALUES: Record<string, number> = { Rent: 1, Sale: 2 };
// Mirrors ApartmentType in LandlordApp/src/Modules/Listings/Models/ApartmentType.cs
const APARTMENT_TYPE_VALUES: Record<string, number> = {
  Studio: 0, OneRoom: 1, TwoRoom: 2, ThreeRoom: 3, FourRoom: 4, House: 5,
};

const normalizeApartment = <T extends { listingType?: any; apartmentType?: any }>(a: T): T => ({
  ...a,
  listingType: typeof a.listingType === 'string'
    ? (LISTING_TYPE_VALUES[a.listingType] ?? a.listingType)
    : a.listingType,
  apartmentType: typeof a.apartmentType === 'string'
    ? (APARTMENT_TYPE_VALUES[a.apartmentType] ?? a.apartmentType)
    : a.apartmentType,
});

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
      if (filters.availableFrom) params.availableFrom = filters.availableFrom;
      if (filters.page) params.page = filters.page;
      if (filters.pageSize) params.pageSize = filters.pageSize;
    }

    const response = await apiClient.get<PagedResponse<ApartmentDto>>(`/api/v1/rent/get-all-apartments`, {
      params,
    });

    return { ...response.data, items: (response.data.items ?? []).map(normalizeApartment) };
  },

  getById: async (id: number): Promise<GetApartmentDto> => {
    const response = await apiClient.get<GetApartmentDto>(`/api/v1/rent/get-apartment`, {
      params: { id },
    });
    return normalizeApartment(response.data);
  },

  getMyApartments: async (): Promise<ApartmentDto[]> => {
    const response = await apiClient.get<any>(`/api/v1/rent/get-my-apartments`);
    let apartments = response.data?.items || response.data?.Items || response.data;
    if (!Array.isArray(apartments)) {
      apartments = [];
    }
    return apartments.map(normalizeApartment);
  },

  create: async (data: ApartmentInputDto): Promise<GetApartmentDto> => {
    const response = await apiClient.post<GetApartmentDto>(`/api/v1/rent/create-apartment`, data);
    return normalizeApartment(response.data);
  },

  update: async (id: number, data: ApartmentUpdateInputDto): Promise<ApartmentDto> => {
    const response = await apiClient.put<ApartmentDto>(`/api/v1/rent/update-apartment/${id}`, data);
    return normalizeApartment(response.data);
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/v1/rent/delete-apartment/${id}`);
  },

  activate: async (id: number): Promise<void> => {
    await apiClient.put(`/api/v1/rent/activate-apartment/${id}`);
  },

  uploadImages: async (files: File[]): Promise<UploadedImage[]> => {
    const formData = new FormData();
    files.forEach((file) => {
      formData.append('files', file);
    });

    const response = await apiClient.post<UploadedImage[]>(`/api/v1/rent/upload-images`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },
};

