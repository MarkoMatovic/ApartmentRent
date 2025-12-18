import { apiClient } from '@/shared/api/client';
import {
  CreateFavoriteRequest,
  FavoriteResponse,
  CreateReviewRequest,
  ReviewResponse,
  GetReviewByIdRequest,
} from '../types';

const API_V1 = 'api/v1';
const REVIEWS_BASE = `${API_V1}/reviews`;

export const reviewsService = {
  /**
   * Create favorite
   */
  createFavorite: async (data: CreateFavoriteRequest): Promise<FavoriteResponse> => {
    const response = await apiClient.post<FavoriteResponse>(
      `${REVIEWS_BASE}/create-favorite`,
      data
    );
    return response.data;
  },

  /**
   * Create review
   */
  createReview: async (data: CreateReviewRequest): Promise<ReviewResponse> => {
    const response = await apiClient.post<ReviewResponse>(
      `${REVIEWS_BASE}/create-review`,
      data
    );
    return response.data;
  },

  /**
   * Get review by ID
   */
  getReviewById: async (reviewId: number): Promise<ReviewResponse> => {
    const response = await apiClient.get<ReviewResponse>(
      `${REVIEWS_BASE}/get-review-by-id`,
      {
        params: { reviewId },
      }
    );
    return response.data;
  },
};

