// Analytics API Types
export interface AnalyticsSummary {
    totalEvents: number;
    totalApartmentViews: number;
    totalRoommateViews: number;
    totalSearches: number;
    totalContactClicks: number;
    eventsByCategory: Record<string, number>;
    fromDate?: string;
    toDate?: string;
}

export interface TopEntity {
    entityId: number;
    entityType: string;
    viewCount: number;
    entityTitle?: string;
    entityDetails?: string;
}

export interface SearchTerm {
    searchTerm: string;
    searchCount: number;
    lastSearched?: string;
}

export interface EventTrend {
    date: string;
    eventType: string;
    count: number;
}

// ML.NET Price Prediction Types
export interface PricePredictionRequest {
    sizeSquareMeters?: number;
    numberOfRooms?: number;
    isFurnished?: boolean;
    hasBalcony?: boolean;
    hasParking?: boolean;
    hasElevator?: boolean;
    hasAirCondition?: boolean;
    hasInternet?: boolean;
    isPetFriendly?: boolean;
    isSmokingAllowed?: boolean;
    city?: string;
    apartmentType?: number;
}

export interface PricePredictionResponse {
    predictedPrice: number;
    confidenceScore: number;
    message: string;
}

export interface ModelMetrics {
    rSquared: number;
    meanAbsoluteError: number;
    meanSquaredError: number;
    rootMeanSquaredError: number;
    trainingSampleCount: number;
    lastTrainedDate?: string;
}

// Roommate Matching Types
export interface RoommateMatchScore {
    roommateId: number;
    roommate?: any; // RoommateDto
    matchPercentage: number;
    featureScores: Record<string, number>;
    matchQuality: string;
}
