export enum ApartmentType {
  Studio = 0,
  OneRoom = 1,
  TwoRoom = 2,
  ThreeRoom = 3,
  FourRoom = 4,
  House = 5,
}

export interface Apartment {
  apartmentId: number;
  landlordId?: number;
  title: string;
  description?: string;
  rent: number;
  address: string;
  city?: string;
  postalCode?: string;
  availableFrom?: string;
  availableUntil?: string;
  numberOfRooms?: number;
  rentIncludeUtilities?: boolean;
  latitude?: number;
  longitude?: number;
  sizeSquareMeters?: number;
  apartmentType: ApartmentType;
  isFurnished: boolean;
  hasBalcony: boolean;
  hasElevator: boolean;
  hasParking: boolean;
  hasInternet: boolean;
  hasAirCondition: boolean;
  isPetFriendly: boolean;
  isSmokingAllowed: boolean;
  depositAmount?: number;
  minimumStayMonths?: number;
  maximumStayMonths?: number;
  isImmediatelyAvailable: boolean;
  apartmentImages?: ApartmentImage[];
  isActive?: boolean;
}

export interface ApartmentImage {
  imageId: number;
  apartmentId: number;
  imageUrl: string;
  isPrimary: boolean;
}

export interface ApartmentDto {
  apartmentId: number;
  title: string;
  rent: number;
  address: string;
  city: string;
  latitude?: number;
  longitude?: number;
  sizeSquareMeters?: number;
  apartmentType?: ApartmentType;
  isFurnished?: boolean;
  isImmediatelyAvailable?: boolean;
}

export interface GetApartmentDto {
  apartmentId: number;
  title: string;
  description: string;
  rent: number;
  address: string;
  city: string;
  postalCode: string;
  availableFrom: string;
  availableUntil: string;
  numberOfRooms: number;
  rentIncludeUtilities: boolean;
  latitude?: number;
  longitude?: number;
  sizeSquareMeters?: number;
  apartmentType: ApartmentType;
  isFurnished: boolean;
  hasBalcony: boolean;
  hasElevator: boolean;
  hasParking: boolean;
  hasInternet: boolean;
  hasAirCondition: boolean;
  isPetFriendly: boolean;
  isSmokingAllowed: boolean;
  depositAmount?: number;
  minimumStayMonths?: number;
  maximumStayMonths?: number;
  isImmediatelyAvailable: boolean;
}

export interface ApartmentFilters {
  city?: string;
  minPrice?: number;
  maxPrice?: number;
  numberOfRooms?: number;
  apartmentType?: ApartmentType;
  isFurnished?: boolean;
  hasParking?: boolean;
  hasBalcony?: boolean;
  isPetFriendly?: boolean;
  moveInDate?: string;
  moveOutDate?: string;
}

export interface ApartmentSearchParams {
  location?: string;
  moveIn?: string;
  moveOut?: string;
  [key: string]: string | undefined;
}

export interface ApartmentInputDto {
  title: string;
  description?: string;
  rent: number;
  address: string;
  city?: string;
  postalCode?: string;
  availableFrom?: string;
  availableUntil?: string;
  numberOfRooms?: number;
  rentIncludeUtilities?: boolean;
  latitude?: number;
  longitude?: number;
  sizeSquareMeters?: number;
  apartmentType: ApartmentType;
  isFurnished: boolean;
  hasBalcony: boolean;
  hasElevator: boolean;
  hasParking: boolean;
  hasInternet: boolean;
  hasAirCondition: boolean;
  isPetFriendly: boolean;
  isSmokingAllowed: boolean;
  depositAmount?: number;
  minimumStayMonths?: number;
  maximumStayMonths?: number;
  isImmediatelyAvailable: boolean;
}
