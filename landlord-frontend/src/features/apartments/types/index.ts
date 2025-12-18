// Apartment types matching backend DTOs

export interface ApartmentDto {
  apartmentId: number;
  title: string;
  rent: number;
  address: string;
  city: string;
}

export interface GetApartmentDto {
  apartmentId: number;
  title: string;
  description: string;
  rent: number;
  address: string;
  city: string;
  postalCode: string;
  availableFrom: string; // DateOnly from backend
  availableUntil: string; // DateOnly from backend
  numberOfRooms: number;
  rentIncludeUtilities: boolean;
}

export interface ApartmentInputDto {
  title: string;
  description: string;
  rent: number;
  address: string;
  city: string;
  postalCode: string;
  availableFrom: string; // DateOnly format: YYYY-MM-DD
  availableUntil: string; // DateOnly format: YYYY-MM-DD
  numberOfRooms: number;
  rentIncludeUtilities: boolean;
  imageUrls: string[];
}

