export enum AppointmentStatus {
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
    Completed = 3,
    Rejected = 4,
}

export interface AppointmentDto {
    appointmentId: number;
    appointmentGuid: string;
    apartmentId: number;
    apartmentTitle?: string;
    apartmentAddress?: string;
    tenantId: number;
    tenantName?: string;
    tenantEmail?: string;
    landlordId: number;
    landlordName?: string;
    landlordEmail?: string;
    appointmentDate: string;
    duration: string;
    status: AppointmentStatus;
    tenantNotes?: string;
    landlordNotes?: string;
    createdDate: string;
}

export interface CreateAppointmentDto {
    apartmentId: number;
    appointmentDate: string;
    tenantNotes?: string;
}

export interface AvailableSlotDto {
    startTime: string;
    endTime: string;
    isAvailable: boolean;
}

export interface UpdateAppointmentStatusDto {
    status: AppointmentStatus;
    landlordNotes?: string;
}
