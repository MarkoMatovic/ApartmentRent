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
    getSummary: async (from?: string, to?: string): Promise<AnalyticsSummary> => {
        const params = new URLSearchParams();
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const response = await apiClient.get(`/api/v1/analytics/summary?${params}`);
        return response.data;
    },

    getTopApartments: async (count: number = 10, from?: string, to?: string): Promise<TopEntity[]> => {
        const params = new URLSearchParams({ count: count.toString() });
        if (from) params.append('from', from);
        if (to) params.append('to', to);
        const response = await apiClient.get(`/api/v1/analytics/top-apartments?${params}`);
        return response.data;
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
