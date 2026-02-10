import { Apartment } from './apartment';
import { User } from './user';

export interface ApartmentApplication {
    applicationId: number;
    userId: number;
    apartmentId: number;
    applicationDate: string;
    status: string;
    createdDate: string;
    apartment?: Apartment;
    user?: User; // Tenant details
}

export interface CreateApplicationDto {
    apartmentId: number;
}

export interface UpdateApplicationStatusDto {
    status: string;
}
