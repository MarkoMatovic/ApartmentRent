import axios from 'axios';
import { ApartmentApplication, CreateApplicationDto, UpdateApplicationStatusDto } from '../types/application';

const API_URL = 'https://localhost:7092/api/applications';

export const applicationsApi = {
    applyForApartment: async (data: CreateApplicationDto): Promise<ApartmentApplication> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.post(API_URL, data, {
            headers: { Authorization: `Bearer ${token}` }
        });
        return response.data;
    },

    getLandlordApplications: async (): Promise<ApartmentApplication[]> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.get(`${API_URL}/landlord`, {
            headers: { Authorization: `Bearer ${token}` }
        });
        return response.data;
    },

    getTenantApplications: async (): Promise<ApartmentApplication[]> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.get(`${API_URL}/tenant`, {
            headers: { Authorization: `Bearer ${token}` }
        });
        return response.data;
    },

    updateStatus: async (id: number, data: UpdateApplicationStatusDto): Promise<ApartmentApplication> => {
        const token = localStorage.getItem('authToken');
        const response = await axios.put(`${API_URL}/${id}/status`, data, {
            headers: { Authorization: `Bearer ${token}` }
        });
        return response.data;
    }
};
