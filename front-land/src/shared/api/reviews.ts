import apiClient from './client';
import { Review, CreateReviewRequest } from '../types/review';

export const reviewsApi = {
  create: async (data: CreateReviewRequest): Promise<Review> => {
    const response = await apiClient.post<Review>(`/api/v1/reviews/create-review`, data);
    return response.data;
  },

  getById: async (id: number): Promise<Review> => {
    const response = await apiClient.get<Review>(`/api/v1/reviews/get-review-by-id`, {
      params: { reviewId: id },
    });
    return response.data;
  },

  getByApartmentId: async (apartmentId: number): Promise<Review[]> => {
    // This endpoint might need to be added to backend
    const response = await apiClient.get<Review[]>(`/api/v1/reviews/apartment/${apartmentId}`);
    return response.data;
  },
};

