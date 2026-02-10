import { apiClient } from './client';

// Price Prediction Types
export interface PricePredictionRequest {
    city: string;
    numberOfRooms: number;
    sizeSquareMeters: number;
    apartmentType: number;
    isFurnished: boolean;
    hasBalcony: boolean;
    hasParking: boolean;
    hasElevator: boolean;
}

export interface PricePredictionResponse {
    predictedPrice: number;
    confidence?: number;
    modelVersion?: string;
}

// Model Metrics Types
export interface ModelMetrics {
    rSquared: number;
    meanAbsoluteError: number;
    rootMeanSquaredError: number;
    trainedDate?: string;
    sampleCount?: number;
}

// Roommate Matching Types
export interface RoommateMatchScore {
    userId: number;
    userName: string;
    matchScore: number;
    compatibilityFactors?: {
        lifestyle?: number;
        cleanliness?: number;
        socialPreference?: number;
        budget?: number;
    };
}

export const machineLearningApi = {
    // Price Prediction
    predictPrice: async (request: PricePredictionRequest): Promise<PricePredictionResponse> => {
        const response = await apiClient.post('/api/v1/ml/predict-price', request);
        return response.data;
    },

    // Train ML Model (Admin only)
    trainModel: async (): Promise<ModelMetrics> => {
        const response = await apiClient.post('/api/v1/ml/train-model');
        return response.data;
    },

    // Get Model Metrics (Admin only)
    getModelMetrics: async (): Promise<ModelMetrics> => {
        const response = await apiClient.get('/api/v1/ml/model-metrics');
        return response.data;
    },

    // Check if model is trained
    isModelTrained: async (): Promise<boolean> => {
        const response = await apiClient.get('/api/v1/ml/is-model-trained');
        return response.data.isTrained;
    },

    // Roommate Matching
    getRoommateMatches: async (userId: number, topN: number = 10): Promise<RoommateMatchScore[]> => {
        const response = await apiClient.get(`/api/v1/ml/roommate-matches?userId=${userId}&topN=${topN}`);
        return response.data;
    },

    // Calculate match score between two users
    calculateMatchScore: async (userId1: number, userId2: number): Promise<number> => {
        const response = await apiClient.get(`/api/v1/ml/calculate-match-score?userId1=${userId1}&userId2=${userId2}`);
        return response.data.matchScore;
    },
};
