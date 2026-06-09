export type RoommateGender = 0 | 1 | 2 | 3 | 4; // 0=PreferNotToSay,1=Male,2=Female,3=NonBinary,4=Other
export type WorkSchedule  = 0 | 1 | 2 | 3;       // 0=Flexible,1=Morning,2=Evening,3=Night

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
  lifestyle?: string;      // 'quiet' | 'social' | 'mixed'
  cleanliness?: string;    // 'veryClean' | 'clean' | 'moderate'
  guestsAllowed?: boolean;
  budgetMin?: number;
  budgetMax?: number;
  budgetIncludes?: string;
  availableFrom?: string;  // DateOnly from backend → "YYYY-MM-DD"
  availableUntil?: string;
  minimumStayMonths?: number;
  maximumStayMonths?: number;
  lookingForRoomType?: string;
  lookingForApartmentType?: string;
  preferredLocation?: string;
  lookingForApartmentId?: number;
  isActive?: boolean;
  // New fields
  gender?: RoommateGender;
  languages?: string;      // comma-separated, e.g. "Srpski,Engleski"
  workSchedule?: WorkSchedule;
  musicFriendly?: boolean;
  // Client-side only
  matchScore?: number;     // 0–100 from ML
  user?: any;
}

export interface RoommateFilters {
  location?: string;
  minBudget?: number;
  maxBudget?: number;
  smokingAllowed?: boolean;
  petFriendly?: boolean;
  lifestyle?: string;
  profession?: string;
  availableFrom?: string;
  stayDuration?: number;
  apartmentId?: number;
  gender?: RoommateGender;
  workSchedule?: WorkSchedule;
}

// ── Display helpers ────────────────────────────────────────────────────────────
export const GENDER_LABELS: Record<number, string> = {
  0: 'Neodređeno', 1: 'Muško', 2: 'Žensko', 3: 'Nebinarno', 4: 'Ostalo',
};
export const SCHEDULE_LABELS: Record<number, string> = {
  0: 'Fleksibilno', 1: 'Jutarnji tip', 2: 'Večernji tip', 3: 'Noćni tip',
};
export const SCHEDULE_ICONS: Record<number, string> = {
  0: '🔄', 1: '🌅', 2: '🌇', 3: '🌙',
};
export const LIFESTYLE_LABELS: Record<string, string> = {
  quiet: 'Miran', social: 'Društven', mixed: 'Mešovit',
};
export const LIFESTYLE_ICONS: Record<string, string> = {
  quiet: '📚', social: '🎉', mixed: '☕',
};
export const CLEANLINESS_LABELS: Record<string, string> = {
  veryClean: 'Veoma uredno', clean: 'Uredno', moderate: 'Umereno',
};

export const getAge = (dateOfBirth?: string): number | null => {
  if (!dateOfBirth) return null;
  const birth = new Date(dateOfBirth);
  const today = new Date();
  let age = today.getFullYear() - birth.getFullYear();
  if (today.getMonth() < birth.getMonth() ||
      (today.getMonth() === birth.getMonth() && today.getDate() < birth.getDate())) age--;
  return age;
};

export const formatAvailableFrom = (date?: string): string => {
  if (!date) return 'Odmah';
  const d = new Date(date);
  const now = new Date();
  if (d <= now) return 'Odmah';
  return d.toLocaleDateString('sr-RS', { day: 'numeric', month: 'short', year: 'numeric' });
};
