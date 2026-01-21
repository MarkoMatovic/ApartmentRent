import { apiClient } from './client';
import {
    AnalyticsSummary,
    TopEntity,
    SearchTerm,
    EventTrend,
    PricePredictionRequest,
    PricePredictionResponse,
    ModelMetrics,
    RoommateMatchScore,
} from '../types/analytics';

// Analytics API
export const analyticsApi = {
    trackEvent: async (
        eventType: string,
        eventCategory: string,
        entityId?: number,
        entityType?: string,
        searchQuery?: string,
        metadata?: Record<string, string>
    ): Promise<void> => {
        try {
            await apiClient.post('/api/v1/analytics/track-event', {
                eventType,
                eventCategory,
                entityId,
                entityType,
                searchQuery,
                metadata,
            });
        } catch (error) {
            // Silent fail - analytics should not break the app
        }
    },

    getSummary: async (from?: string, to?: string): Promise<AnalyticsSummary> => {
        const params = new URLSearchParams();
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const url = `/api/v1/analytics/summary?${params}`;
        console.log('🔍 API Call:', url);
        try {
            const response = await apiClient.get(url);
            console.log('✅ API Response:', response.data);
            return response.data;
        } catch (error: any) {
            console.error('❌ API Error:', {
                url,
                status: error.response?.status,
                statusText: error.response?.statusText,
                data: error.response?.data,
                message: error.message
            });
            throw error;
        }
    },

    getTopApartments: async (count: number = 10, from?: string, to?: string): Promise<TopEntity[]> => {
        const params = new URLSearchParams({ count: count.toString() });
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const url = `/api/v1/analytics/top-apartments?${params}`;
        console.log('🔍 API Call:', url);
        try {
            const response = await apiClient.get(url);
            console.log('✅ API Response:', response.data);
            return response.data;
        } catch (error: any) {
            console.error('❌ API Error:', {
                url,
                status: error.response?.status,
                data: error.response?.data
            });
            throw error;
        }
    },

    getTopRoommates: async (count: number = 10, from?: string, to?: string): Promise<TopEntity[]> => {
        const params = new URLSearchParams({ count: count.toString() });
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const response = await apiClient.get(`/api/v1/analytics/top-roommates?${params}`);
        return response.data;
    },

    getTopSearches: async (count: number = 10, from?: string, to?: string): Promise<SearchTerm[]> => {
        const params = new URLSearchParams({ count: count.toString() });
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const response = await apiClient.get(`/api/v1/analytics/top-searches?${params}`);
        return response.data;
    },

    getEventTrends: async (from: string, to: string, eventType?: string): Promise<EventTrend[]> => {
        const params = new URLSearchParams({ from, to });
        if (eventType) params.append('eventType', eventType);
        const response = await apiClient.get(`/api/v1/analytics/trends?${params}`);
        return response.data;
    },

    // User-specific roommate analytics
    getUserRoommateSummary: async (userId: number, from?: string, to?: string): Promise<any> => {
        try {
            const params = new URLSearchParams({ userId: userId.toString() });
            if (from) params.append('from', from);
            if (to) params.append('to', to);
            const response = await apiClient.get(`/api/v1/analytics/user-roommate-summary?${params}`);
            return response.data;
        } catch (error: any) {
            // If endpoint doesn't exist, return empty data structure
            if (error.response?.status === 404 || error.code === 'ERR_NETWORK') {
                return {
                    roommateViews: 0,
                    messagesSent: 0,
                    applicationsSent: 0,
                    searches: 0,
                };
            }
            throw error;
        }
    },

    getUserTopRoommates: async (userId: number, count: number = 10, from?: string, to?: string): Promise<TopEntity[]> => {
        try {
            const params = new URLSearchParams({ userId: userId.toString(), count: count.toString() });
            if (from) params.append('from', from);
            if (to) params.append('to', to);
            const response = await apiClient.get(`/api/v1/analytics/user-top-roommates?${params}`);
            return response.data;
        } catch (error: any) {
            // If endpoint doesn't exist, return empty array
            if (error.response?.status === 404 || error.code === 'ERR_NETWORK') {
                return [];
            }
            throw error;
        }
    },

    getUserSearches: async (userId: number, count: number = 10, from?: string, to?: string): Promise<SearchTerm[]> => {
        try {
            const params = new URLSearchParams({ userId: userId.toString(), count: count.toString() });
            if (from) params.append('from', from);
            if (to) params.append('to', to);
            const response = await apiClient.get(`/api/v1/analytics/user-searches?${params}`);
            return response.data;
        } catch (error: any) {
            // If endpoint doesn't exist, return empty array
            if (error.response?.status === 404 || error.code === 'ERR_NETWORK') {
                return [];
            }
            throw error;
        }
    },

    getUserRoommateTrends: async (userId: number, from?: string, to?: string): Promise<any> => {
        try {
            const params = new URLSearchParams({ userId: userId.toString() });
            if (from) params.append('from', from);
            if (to) params.append('to', to);
            const response = await apiClient.get(`/api/v1/analytics/user-roommate-trends?${params}`);
            return response.data;
        } catch (error: any) {
            // If endpoint doesn't exist, return empty object
            if (error.response?.status === 404 || error.code === 'ERR_NETWORK') {
                return {
                    popularCities: [],
                    averagePrices: [],
                };
            }
            throw error;
        }
    },

    // Personal analytics - new methods
    getMyViewedApartments: async (count: number = 10, from?: string, to?: string): Promise<TopEntity[]> => {
        const params = new URLSearchParams({ count: count.toString() });
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const response = await apiClient.get(`/api/v1/analytics/my-viewed-apartments?${params}`);
        return response.data;
    },

    getMyApartmentViews: async (from?: string, to?: string): Promise<any[]> => {
        const params = new URLSearchParams();
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const url = `/api/v1/analytics/my-apartment-views${params.toString() ? '?' + params : ''}`;
        const response = await apiClient.get(url);
        return response.data;
    },

    getMyMessagesSent: async (from?: string, to?: string): Promise<number> => {
        const params = new URLSearchParams();
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const url = `/api/v1/analytics/my-messages-sent${params.toString() ? '?' + params : ''}`;
        const response = await apiClient.get(url);
        return response.data;
    },

    // Complete user analytics
    getUserTopApartments: async (userId: number, count: number = 10, from?: string, to?: string): Promise<TopEntity[]> => {
        const params = new URLSearchParams({ userId: userId.toString(), count: count.toString() });
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const url = `/api/v1/analytics/user-top-apartments?${params}`;
        console.log('🔍 USER TOP APARTMENTS - URL:', url);
        try {
            const response = await apiClient.get(url);
            console.log('✅ USER TOP APARTMENTS - Response:', response.data);
            return response.data;
        } catch (error: any) {
            console.error('❌ USER TOP APARTMENTS - Error:', error);
            throw error;
        }
    },

    getUserCompleteAnalytics: async (userId: number, from?: string, to?: string): Promise<AnalyticsSummary> => {
        const params = new URLSearchParams({ userId: userId.toString() });
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const url = `/api/v1/analytics/user-complete-analytics?${params}`;
        console.log('🔍 USER COMPLETE ANALYTICS - URL:', url);
        console.log('🔍 USER COMPLETE ANALYTICS - Params:', { userId, from, to });
        try {
            const response = await apiClient.get(url);
            console.log('✅ USER COMPLETE ANALYTICS - Response:', response.data);
            return response.data;
        } catch (error: any) {
            console.error('❌ USER COMPLETE ANALYTICS - Error:', {
                url,
                status: error.response?.status,
                data: error.response?.data,
                message: error.message
            });
            throw error;
        }
    },
};

// ML.NET API
export const mlApi = {
    predictPrice: async (request: PricePredictionRequest): Promise<PricePredictionResponse> => {
        const response = await apiClient.post('/api/v1/ml/predict-price', request);
        return response.data;
    },

    trainPriceModel: async (): Promise<ModelMetrics> => {
        const response = await apiClient.post('/api/v1/ml/train-price-model');
        return response.data;
    },

    getModelMetrics: async (): Promise<ModelMetrics> => {
        const response = await apiClient.get('/api/v1/ml/model-metrics');
        return response.data;
    },

    isModelTrained: async (): Promise<boolean> => {
        const response = await apiClient.get('/api/v1/ml/is-model-trained');
        return response.data.isTrained;
    },

    getRoommateMatches: async (userId: number, topN: number = 10): Promise<RoommateMatchScore[]> => {
        const response = await apiClient.get(`/api/v1/ml/roommate-matches?userId=${userId}&topN=${topN}`);
        return response.data;
    },

    calculateMatchScore: async (userId1: number, userId2: number): Promise<number> => {
        const response = await apiClient.get(`/api/v1/ml/match-score?userId1=${userId1}&userId2=${userId2}`);
        return response.data.matchScore;
    },
};
