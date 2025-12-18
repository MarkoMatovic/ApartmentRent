export interface Review {
  reviewId: number;
  userId: number;
  apartmentId?: number;
  rating: number; // 1-5
  comment?: string;
  createdAt: string;
  user?: {
    firstName: string;
    lastName: string;
    profilePicture?: string;
  };
}

export interface CreateReviewRequest {
  apartmentId: number;
  rating: number;
  comment?: string;
}

