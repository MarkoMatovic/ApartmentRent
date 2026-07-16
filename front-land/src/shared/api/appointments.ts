import apiClient from './client';
import type {
    AppointmentDto,
    CreateAppointmentDto,
    AvailableSlotDto,
    UpdateAppointmentStatusDto,
    LandlordAvailabilityDto,
    SetAvailabilityDto,
} from '../types/appointment';

// The backend serializes enums as string names (JsonStringEnumConverter),
// while our types use numeric values — normalize on every read.
const STATUS_NAMES = ['Pending', 'Confirmed', 'Cancelled', 'Completed', 'Rejected'];
const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

const toStatusNumber = (status: number | string): number =>
    typeof status === 'number' ? status : Math.max(0, STATUS_NAMES.indexOf(status));

const toDayNumber = (day: number | string): number =>
    typeof day === 'number' ? day : Math.max(0, DAY_NAMES.indexOf(day));

const normalizeAppointment = (dto: AppointmentDto): AppointmentDto => ({
    ...dto,
    status: toStatusNumber(dto.status as unknown as number | string),
});

const normalizeAvailability = (dto: LandlordAvailabilityDto): LandlordAvailabilityDto => ({
    ...dto,
    dayOfWeek: toDayNumber(dto.dayOfWeek as unknown as number | string),
});

export const appointmentsApi = {
    create: async (data: CreateAppointmentDto): Promise<AppointmentDto> => {
        const response = await apiClient.post('/api/appointments', data);
        return normalizeAppointment(response.data);
    },

    getMyAppointments: async (): Promise<AppointmentDto[]> => {
        const response = await apiClient.get('/api/appointments/my-appointments');
        return (response.data as AppointmentDto[]).map(normalizeAppointment);
    },

    getLandlordAppointments: async (): Promise<AppointmentDto[]> => {
        const response = await apiClient.get('/api/appointments/landlord-appointments');
        return (response.data as AppointmentDto[]).map(normalizeAppointment);
    },

    getAvailableSlots: async (apartmentId: number, date: string): Promise<AvailableSlotDto[]> => {
        const response = await apiClient.get(`/api/appointments/available-slots/${apartmentId}`, {
            params: { date },
        });
        return response.data;
    },

    updateStatus: async (id: number, data: UpdateAppointmentStatusDto): Promise<AppointmentDto> => {
        const response = await apiClient.put(`/api/appointments/${id}/status`, data);
        return normalizeAppointment(response.data);
    },

    cancel: async (id: number): Promise<void> => {
        await apiClient.delete(`/api/appointments/${id}`);
    },

    getById: async (id: number): Promise<AppointmentDto> => {
        const response = await apiClient.get(`/api/appointments/${id}`);
        return normalizeAppointment(response.data);
    },

    getMyAvailability: async (): Promise<LandlordAvailabilityDto[]> => {
        const response = await apiClient.get('/api/appointments/availability');
        return (response.data as LandlordAvailabilityDto[]).map(normalizeAvailability);
    },

    setMyAvailability: async (data: SetAvailabilityDto): Promise<LandlordAvailabilityDto[]> => {
        const response = await apiClient.put('/api/appointments/availability', data);
        return (response.data as LandlordAvailabilityDto[]).map(normalizeAvailability);
    },
};
