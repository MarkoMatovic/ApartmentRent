import { apiClient } from './client';

export interface SavedSearch {
    savedSearchId: number;
    userId: number;
    searchName: string;
    city?: string;
    minRent?: number;
    maxRent?: number;
    numberOfRooms?: number;
    apartmentType?: number;
    listingType?: number;
    isFurnished?: boolean;
    isPetFriendly?: boolean;
    hasParking?: boolean;
    hasBalcony?: boolean;
    createdDate: string;
}

export interface SavedSearchInput {
    searchName: string;
    city?: string;
    minRent?: number;
    maxRent?: number;
    numberOfRooms?: number;
    apartmentType?: number;
    listingType?: number;
    isFurnished?: boolean;
    isPetFriendly?: boolean;
    hasParking?: boolean;
    hasBalcony?: boolean;
}

export const savedSearchesApi = {
    // Get all saved searches for a user
    getUserSavedSearches: async (userId: number): Promise<SavedSearch[]> => {
        const response = await apiClient.get(`/api/v1/saved-searches/get-saved-searches-by-user-id`, {
            params: { userId }
        });
        return response.data;
    },

    // Get a specific saved search
    getSavedSearch: async (id: number): Promise<SavedSearch> => {
        const response = await apiClient.get(`/api/v1/saved-searches/get-saved-search`, {
            params: { id }
        });
        return response.data;
    },

    // Create a new saved search
    createSavedSearch: async (input: SavedSearchInput): Promise<SavedSearch> => {
        const response = await apiClient.post('/api/v1/saved-searches/create-saved-search', input);
        return response.data;
    },

    // Update a saved search
    updateSavedSearch: async (id: number, input: SavedSearchInput): Promise<SavedSearch> => {
        const response = await apiClient.put(`/api/v1/saved-searches/update-saved-search/${id}`, input);
        return response.data;
    },

    // Delete a saved search
    deleteSavedSearch: async (id: number): Promise<boolean> => {
        const response = await apiClient.delete(`/api/v1/saved-searches/delete-saved-search/${id}`);
        return response.data;
    },
};
