export interface Roommate {
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  profilePicture?: string;
  bio?: string;
  budget?: number;
  preferences?: RoommatePreferences;
  availability?: {
    availableFrom?: string;
    availableUntil?: string;
  };
}

export interface RoommatePreferences {
  smokingAllowed?: boolean;
  petFriendly?: boolean;
  lifestyle?: string; // 'quiet', 'social', 'mixed'
  cleanliness?: string; // 'very clean', 'clean', 'moderate'
  guestsAllowed?: boolean;
}

export interface RoommateFilters {
  location?: string;
  minBudget?: number;
  maxBudget?: number;
  smokingAllowed?: boolean;
  petFriendly?: boolean;
  lifestyle?: string;
}

