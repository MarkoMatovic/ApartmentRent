export interface Roommate {
  roommateId: number;
  userId: number;
  firstName: string;
  lastName: string;
  email?: string;
  phoneNumber?: string;
  profilePicture?: string;
  dateOfBirth?: string;
  bio?: string;
  hobbies?: string;
  profession?: string;
  smokingAllowed?: boolean;
  petFriendly?: boolean;
  lifestyle?: string; // 'quiet', 'social', 'mixed'
  cleanliness?: string; // 'very clean', 'clean', 'moderate'
  guestsAllowed?: boolean;
  budgetMin?: number;
  budgetMax?: number;
  budgetIncludes?: string;
  availableFrom?: string;
  availableUntil?: string;
  minimumStayMonths?: number;
  maximumStayMonths?: number;
  lookingForRoomType?: string;
  lookingForApartmentType?: string;
  preferredLocation?: string;
  isActive?: boolean;
}

export interface RoommateFilters {
  location?: string;
  minBudget?: number;
  maxBudget?: number;
  smokingAllowed?: boolean;
  petFriendly?: boolean;
  lifestyle?: string;
}

