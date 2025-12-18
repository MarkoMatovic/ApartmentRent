import axios, { AxiosInstance, AxiosError, InternalAxiosRequestConfig } from 'axios';
import { ApiError } from '../types/api';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5197';

// Create axios instance
export const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add JWT token
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('auth_token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    // TEMPORARY: Auth disabled for development - bypassing 401 redirect
    // if (error.response?.status === 401) {
    //   // Unauthorized - clear token and redirect to login
    //   localStorage.removeItem('auth_token');
    //   window.location.href = '/login';
    // }
    
    const apiError: ApiError = {
      message: (error.response?.data as any)?.message || error.message || 'An error occurred',
      statusCode: error.response?.status,
    };
    
    return Promise.reject(apiError);
  }
);

// Helper function to get token
export const getAuthToken = (): string | null => {
  return localStorage.getItem('auth_token');
};

// Helper function to set token
export const setAuthToken = (token: string): void => {
  localStorage.setItem('auth_token', token);
};

// Helper function to clear token
export const clearAuthToken = (): void => {
  localStorage.removeItem('auth_token');
};

