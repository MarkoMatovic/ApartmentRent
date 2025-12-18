// Reviews types matching backend gRPC proto

export interface CreateFavoriteRequest {
  userId: number;
  apartmentId: number;
  createdByGuid: string; // GUID
}

export interface FavoriteResponse {
  favoriteId: number;
  userId: number;
  apartmentId: number;
  createdByGuid: string; // GUID
  createdDate: string; // ISO timestamp
  modifiedByGuid: string; // GUID
  modifiedDate: string; // ISO timestamp
}

export interface CreateReviewRequest {
  tenantId: number;
  landlordId: number;
  rating: number;
  reviewText: string;
  createdByGuid: string; // GUID
}

export interface ReviewResponse {
  reviewId: number;
  tenantId: number;
  landlordId: number;
  rating: number;
  reviewText: string;
  createdByGuid: string; // GUID
  createdDate: string; // ISO timestamp
  modifiedByGuid: string; // GUID
  modifiedDate: string; // ISO timestamp
}

export interface GetReviewByIdRequest {
  reviewId: number;
}

