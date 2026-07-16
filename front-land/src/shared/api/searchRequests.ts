import { apiClient } from './client';

export enum SearchRequestType {
    LookingForApartment = 0,
    LookingForRoommate = 1,
}

export interface SearchRequest {
    searchRequestId: number;
    userId: number;
    userName?: string;
    profilePicture?: string;
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

// Backend wire format (SearchRequestDto / SearchRequestInputDto):
// budget maps to budgetMax, preferredMoveInDate to availableFrom (DateOnly "yyyy-MM-dd"),
// and the user's name arrives as firstName/lastName.
interface SearchRequestApiDto {
    searchRequestId: number;
    userId: number;
    firstName?: string;
    lastName?: string;
    profilePicture?: string;
    requestType: SearchRequestType;
    title: string;
    description?: string;
    city?: string;
    budgetMin?: number;
    budgetMax?: number;
    availableFrom?: string;
    isActive: boolean;
    createdDate: string;
}

const toApiInput = (input: SearchRequestInput) => ({
    requestType: input.requestType,
    title: input.title,
    description: input.description,
    city: input.city || undefined,
    budgetMax: input.budget,
    availableFrom: input.preferredMoveInDate || undefined,
});

// Backend serializes enums as string names — normalize to our numeric enum.
const REQUEST_TYPE_NAMES = ['LookingForApartment', 'LookingForRoommate'];
const toRequestType = (value: SearchRequestType | string): SearchRequestType =>
    typeof value === 'number' ? value : Math.max(0, REQUEST_TYPE_NAMES.indexOf(value));

const fromApiDto = (dto: SearchRequestApiDto): SearchRequest => ({
    searchRequestId: dto.searchRequestId,
    userId: dto.userId,
    userName: [dto.firstName, dto.lastName].filter(Boolean).join(' ') || undefined,
    profilePicture: dto.profilePicture,
    requestType: toRequestType(dto.requestType),
    title: dto.title,
    description: dto.description ?? '',
    city: dto.city,
    budget: dto.budgetMax ?? dto.budgetMin,
    preferredMoveInDate: dto.availableFrom,
    isActive: dto.isActive,
    createdDate: dto.createdDate,
});

export const searchRequestsApi = {
    // Get all search requests with optional filters
    getAllSearchRequests: async (filters?: SearchRequestFilters): Promise<PagedSearchRequests> => {
        const params = new URLSearchParams();
        if (filters?.requestType !== undefined) params.append('requestType', filters.requestType.toString());
        if (filters?.city) params.append('city', filters.city);
        if (filters?.minBudget) params.append('minBudget', filters.minBudget.toString());
        if (filters?.maxBudget) params.append('maxBudget', filters.maxBudget.toString());
        if (filters?.page) params.append('page', filters.page.toString());
        if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

        const response = await apiClient.get(`/api/v1/search-requests/get-all-search-requests?${params.toString()}`);
        const data = response.data;
        return { ...data, items: (data.items ?? []).map(fromApiDto) };
    },

    // Get a specific search request
    getSearchRequest: async (id: number): Promise<SearchRequest> => {
        const response = await apiClient.get(`/api/v1/search-requests/get-search-request`, {
            params: { id }
        });
        return fromApiDto(response.data);
    },

    // Get search requests by user
    getUserSearchRequests: async (userId: number): Promise<SearchRequest[]> => {
        const response = await apiClient.get(`/api/v1/search-requests/get-search-requests-by-user-id`, {
            params: { userId }
        });
        return (response.data as SearchRequestApiDto[]).map(fromApiDto);
    },

    // Create a new search request
    createSearchRequest: async (input: SearchRequestInput): Promise<SearchRequest> => {
        const response = await apiClient.post('/api/v1/search-requests/create-search-request', toApiInput(input));
        return fromApiDto(response.data);
    },

    // Update a search request
    updateSearchRequest: async (id: number, input: SearchRequestInput): Promise<SearchRequest> => {
        const response = await apiClient.put(`/api/v1/search-requests/update-search-request/${id}`, toApiInput(input));
        return fromApiDto(response.data);
    },

    // Delete a search request
    deleteSearchRequest: async (id: number): Promise<boolean> => {
        const response = await apiClient.delete(`/api/v1/search-requests/delete-search-request/${id}`);
        return response.data;
    },
};
