import { apiClient } from './client';

export enum SearchRequestType {
    LookingForApartment = 0,
    LookingForRoommate = 1,
}

export interface SearchRequest {
    searchRequestId: number;
    userId: number;
    userName?: string;
    userEmail?: string;
    userPhone?: string;
    requestType: SearchRequestType;
    title: string;
    description: string;
    city?: string;
    budget?: number;
    preferredMoveInDate?: string;
    isActive: boolean;
    createdDate: string;
}

export interface SearchRequestInput {
    requestType: SearchRequestType;
    title: string;
    description: string;
    city?: string;
    budget?: number;
    preferredMoveInDate?: string;
}

export interface SearchRequestFilters {
    requestType?: SearchRequestType;
    city?: string;
    minBudget?: number;
    maxBudget?: number;
    page?: number;
    pageSize?: number;
}

export interface PagedSearchRequests {
    items: SearchRequest[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}

export const searchRequestsApi = {
    // Get all search requests with optional filters
    getAllSearchRequests: async (filters?: SearchRequestFilters): Promise<PagedSearchRequests | SearchRequest[]> => {
        const params = new URLSearchParams();
        if (filters?.requestType !== undefined) params.append('requestType', filters.requestType.toString());
        if (filters?.city) params.append('city', filters.city);
        if (filters?.minBudget) params.append('minBudget', filters.minBudget.toString());
        if (filters?.maxBudget) params.append('maxBudget', filters.maxBudget.toString());
        if (filters?.page) params.append('page', filters.page.toString());
        if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

        const response = await apiClient.get(`/api/v1/search-requests/get-all-search-requests?${params.toString()}`);
        return response.data;
    },

    // Get a specific search request
    getSearchRequest: async (id: number): Promise<SearchRequest> => {
        const response = await apiClient.get(`/api/v1/search-requests/get-search-request`, {
            params: { id }
        });
        return response.data;
    },

    // Get search requests by user
    getUserSearchRequests: async (userId: number): Promise<SearchRequest[]> => {
        const response = await apiClient.get(`/api/v1/search-requests/get-search-requests-by-user-id`, {
            params: { userId }
        });
        return response.data;
    },

    // Create a new search request
    createSearchRequest: async (input: SearchRequestInput): Promise<SearchRequest> => {
        const response = await apiClient.post('/api/v1/search-requests/create-search-request', input);
        return response.data;
    },

    // Update a search request
    updateSearchRequest: async (id: number, input: SearchRequestInput): Promise<SearchRequest> => {
        const response = await apiClient.put(`/api/v1/search-requests/update-search-request/${id}`, input);
        return response.data;
    },

    // Delete a search request
    deleteSearchRequest: async (id: number): Promise<boolean> => {
        const response = await apiClient.delete(`/api/v1/search-requests/delete-search-request/${id}`);
        return response.data;
    },
};
