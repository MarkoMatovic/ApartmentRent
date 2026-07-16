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

// Backend wire format: filters are stored as a JSON string in filtersJson
// (see SavedSearchInputDto / SavedSearchFilters on the server).
interface SavedSearchApiDto {
    savedSearchId: number;
    userId: number;
    name: string;
    searchType: string;
    filtersJson?: string | null;
    emailNotificationsEnabled: boolean;
    isActive: boolean;
    createdDate: string;
}

const toApiInput = (input: SavedSearchInput) => {
    const { searchName, ...filters } = input;
    // Drop undefined/empty values so filtersJson stays compact
    const cleaned = Object.fromEntries(
        Object.entries(filters).filter(([, v]) => v !== undefined && v !== '')
    );
    return {
        name: searchName,
        searchType: 'Apartment',
        filtersJson: Object.keys(cleaned).length > 0 ? JSON.stringify(cleaned) : null,
        emailNotificationsEnabled: true,
    };
};

const fromApiDto = (dto: SavedSearchApiDto): SavedSearch => {
    let filters: Partial<SavedSearchInput> = {};
    if (dto.filtersJson) {
        try {
            filters = JSON.parse(dto.filtersJson);
        } catch {
            // Malformed filters — show the search without filter details
        }
    }
    return {
        savedSearchId: dto.savedSearchId,
        userId: dto.userId,
        searchName: dto.name,
        createdDate: dto.createdDate,
        city: filters.city,
        minRent: filters.minRent,
        maxRent: filters.maxRent,
        numberOfRooms: filters.numberOfRooms,
        apartmentType: filters.apartmentType,
        listingType: filters.listingType,
        isFurnished: filters.isFurnished,
        isPetFriendly: filters.isPetFriendly,
        hasParking: filters.hasParking,
        hasBalcony: filters.hasBalcony,
    };
};

export const savedSearchesApi = {
    // Get all saved searches for a user
    getUserSavedSearches: async (userId: number): Promise<SavedSearch[]> => {
        const response = await apiClient.get(`/api/v1/saved-searches/get-saved-searches-by-user-id`, {
            params: { userId }
        });
        return (response.data as SavedSearchApiDto[]).map(fromApiDto);
    },

    // Get a specific saved search
    getSavedSearch: async (id: number): Promise<SavedSearch> => {
        const response = await apiClient.get(`/api/v1/saved-searches/get-saved-search`, {
            params: { id }
        });
        return fromApiDto(response.data);
    },

    // Create a new saved search
    createSavedSearch: async (input: SavedSearchInput): Promise<SavedSearch> => {
        const response = await apiClient.post('/api/v1/saved-searches/create-saved-search', toApiInput(input));
        return fromApiDto(response.data);
    },

    // Update a saved search
    updateSavedSearch: async (id: number, input: SavedSearchInput): Promise<SavedSearch> => {
        const response = await apiClient.put(`/api/v1/saved-searches/update-saved-search/${id}`, toApiInput(input));
        return fromApiDto(response.data);
    },

    // Delete a saved search
    deleteSavedSearch: async (id: number): Promise<boolean> => {
        const response = await apiClient.delete(`/api/v1/saved-searches/delete-saved-search/${id}`);
        return response.data;
    },
};
