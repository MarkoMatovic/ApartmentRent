import axios from 'axios';
import type {
    AppointmentDto,
    CreateAppointmentDto,
    AvailableSlotDto,
    UpdateAppointmentStatusDto,
    LandlordAvailabilityDto,
    SetAvailabilityDto,
} from '../types/appointment';

const API_URL = 'https://localhost:7092/api/appointments';

export const appointmentsApi = {
    create: async (data: CreateAppointmentDto): Promise<AppointmentDto> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.post(API_URL, data, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    getMyAppointments: async (): Promise<AppointmentDto[]> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.get(`${API_URL}/my-appointments`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    getLandlordAppointments: async (): Promise<AppointmentDto[]> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.get(`${API_URL}/landlord-appointments`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    getAvailableSlots: async (apartmentId: number, date: string): Promise<AvailableSlotDto[]> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.get(`${API_URL}/available-slots/${apartmentId}`, {
            params: { date },
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    updateStatus: async (id: number, data: UpdateAppointmentStatusDto): Promise<AppointmentDto> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.put(`${API_URL}/${id}/status`, data, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    cancel: async (id: number): Promise<void> => {
        const token = localStorage.getItem('authToken');
        await axios.delete(`${API_URL}/${id}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
    },

    getById: async (id: number): Promise<AppointmentDto> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.get(`${API_URL}/${id}`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    getMyAvailability: async (): Promise<LandlordAvailabilityDto[]> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.get(`${API_URL}/availability`, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },

    setMyAvailability: async (data: SetAvailabilityDto): Promise<LandlordAvailabilityDto[]> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.put(`${API_URL}/availability`, data, {
            headers: { Authorization: `Bearer ${token}` },
        });
        return response.data;
    },
};
