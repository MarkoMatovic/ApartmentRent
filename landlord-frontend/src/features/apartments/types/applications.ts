// Apartment Application types matching backend DTOs

export interface ApartmentApplicationDto {
  applicationId: number;
  userId: number;
  apartmentId: number;
  applicationDate: string; // ISO date string
  status: string;
  createdByGuid?: string; // GUID
  createdDate: string; // ISO date string
  modifiedByGuid?: string; // GUID
  modifiedDate?: string; // ISO date string
}

// Extended DTO with apartment details (for display)
export interface ApartmentApplicationWithDetailsDto extends ApartmentApplicationDto {
  apartment?: {
    apartmentId: number;
    title: string;
    address: string;
    city: string;
    rent: number;
  };
}

