import { ApartmentImage } from '../shared/types/apartment';
import { Review } from '../shared/types/review';

// Demo apartment images for testing
export const demoApartmentImages: ApartmentImage[] = [
  {
    imageId: 1,
    apartmentId: 1,
    imageUrl: 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800',
    isPrimary: true,
  },
  {
    imageId: 2,
    apartmentId: 1,
    imageUrl: 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800',
    isPrimary: false,
  },
  {
    imageId: 3,
    apartmentId: 1,
    imageUrl: 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800',
    isPrimary: false,
  },
  {
    imageId: 4,
    apartmentId: 1,
    imageUrl: 'https://images.unsplash.com/photo-1484154218962-a197022b5858?w=800',
    isPrimary: false,
  },
];

// Demo reviews for testing
export const demoReviews: Review[] = [
  {
    reviewId: 1,
    userId: 1,
    apartmentId: 1,
    rating: 5,
    comment: 'Amazing apartment! Very clean, modern, and the location is perfect. The landlord was very responsive and helpful. Highly recommended!',
    createdAt: '2024-01-15T10:30:00Z',
    isAnonymous: false,
    isPublic: true,
    user: {
      firstName: 'John',
      lastName: 'Smith',
      profilePicture: undefined,
    },
  },
  {
    reviewId: 2,
    userId: 2,
    apartmentId: 1,
    rating: 4,
    comment: 'Great place overall. Good amenities and nice neighborhood. Only minor issue was some street noise, but nothing major.',
    createdAt: '2024-01-10T14:20:00Z',
    isAnonymous: false,
    isPublic: true,
    user: {
      firstName: 'Sarah',
      lastName: 'Johnson',
      profilePicture: undefined,
    },
  },
  {
    reviewId: 3,
    userId: 3,
    apartmentId: 1,
    rating: 5,
    comment: 'Perfect apartment for students! Close to university, affordable, and well-maintained.',
    createdAt: '2023-12-20T09:15:00Z',
    isAnonymous: true,
    isPublic: true,
    user: {
      firstName: 'Anonymous',
      lastName: 'User',
    },
  },
  {
    reviewId: 4,
    userId: 4,
    apartmentId: 1,
    rating: 3,
    comment: 'Decent apartment but a bit dated. Could use some renovations.',
    createdAt: '2023-11-05T16:45:00Z',
    isAnonymous: false,
    isPublic: true,
    user: {
      firstName: 'Michael',
      lastName: 'Brown',
      profilePicture: undefined,
    },
  },
];

// You can add these to your apartments for testing:
// apartment.apartmentImages = demoApartmentImages;
// apartment.isLookingForRoommate = true; // to test the blinking badge
// apartment.averageRating = 4.25; // (5 + 4 + 5 + 3) / 4
// apartment.reviewCount = 4;
