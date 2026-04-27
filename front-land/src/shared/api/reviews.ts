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
    const response = await apiClient.get<Review[]>(`/api/v1/reviews/apartment/${apartmentId}`);
    return response.data;
  },

  deleteReview: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/v1/reviews/delete-review/${id}`);
  },

  createFavorite: async (userId: number, apartmentId: number): Promise<any> => {
    const response = await apiClient.post(`/api/v1/reviews/create-favorite`, { userId, apartmentId });
    return response.data;
  },

  deleteFavorite: async (id: number): Promise<void> => {
    await apiClient.delete(`/api/v1/reviews/delete-favorite/${id}`);
  },

  getFavoritesByUserId: async (userId: number): Promise<any[]> => {
    const response = await apiClient.get(`/api/v1/reviews/favorites/${userId}`);
    return response.data;
  },
};

