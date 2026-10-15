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
/** The part of the roommate profile that comes with a match. */
export interface MatchedRoommate {
    roommateId: number;
    userId: number;
    firstName: string;
    lastName?: string;
    profilePicture?: string | null;
    profession?: string | null;
    preferredLocation?: string | null;
}

/** What GET /api/v1/ml/roommate-matches returns for each candidate (RoommateMatchScoreDto). */
export interface RoommateMatchScore {
    roommateId: number;
    roommate?: MatchedRoommate;
    /** 0-100 */
    matchPercentage: number;
    /** Budget, Lifestyle, Preferences and Location, each 0-100. */
    featureScores?: Partial<Record<'Budget' | 'Lifestyle' | 'Preferences' | 'Location', number>>;
    matchQuality?: string;
}
