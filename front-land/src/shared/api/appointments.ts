import apiClient from './client';
import type {
    AppointmentDto,
    CreateAppointmentDto,
    AvailableSlotDto,
    UpdateAppointmentStatusDto,
    LandlordAvailabilityDto,
    SetAvailabilityDto,
} from '../types/appointment';

export const appointmentsApi = {
    create: async (data: CreateAppointmentDto): Promise<AppointmentDto> => {
        const response = await apiClient.post('/api/appointments', data);
        return response.data;
    },

    getMyAppointments: async (): Promise<AppointmentDto[]> => {
        const response = await apiClient.get('/api/appointments/my-appointments');
        return response.data;
    },

    getLandlordAppointments: async (): Promise<AppointmentDto[]> => {
        const response = await apiClient.get('/api/appointments/landlord-appointments');
        return response.data;
    },

    getAvailableSlots: async (apartmentId: number, date: string): Promise<AvailableSlotDto[]> => {
        const response = await apiClient.get(`/api/appointments/available-slots/${apartmentId}`, {
            params: { date },
        });
        return response.data;
    },

    updateStatus: async (id: number, data: UpdateAppointmentStatusDto): Promise<AppointmentDto> => {
        const response = await apiClient.put(`/api/appointments/${id}/status`, data);
        return response.data;
    },

    cancel: async (id: number): Promise<void> => {
        await apiClient.delete(`/api/appointments/${id}`);
    },

    getById: async (id: number): Promise<AppointmentDto> => {
        const response = await apiClient.get(`/api/appointments/${id}`);
        return response.data;
    },

    getMyAvailability: async (): Promise<LandlordAvailabilityDto[]> => {
        const response = await apiClient.get('/api/appointments/availability');
        return response.data;
    },

    setMyAvailability: async (data: SetAvailabilityDto): Promise<LandlordAvailabilityDto[]> => {
        const response = await apiClient.put('/api/appointments/availability', data);
        return response.data;
    },
};
