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
// i18n keys in the "roommates" namespace — render with t(GENDER_KEYS[g]) so the
// labels follow the selected language instead of being hardcoded Serbian.
export const GENDER_KEYS: Record<number, string> = {
  0: 'genderNotSay', 1: 'genderMale', 2: 'genderFemale', 3: 'genderNonBinary', 4: 'genderOther',
};
export const SCHEDULE_KEYS: Record<number, string> = {
  0: 'scheduleFlexible', 1: 'scheduleMorning', 2: 'scheduleEvening', 3: 'scheduleNight',
};
export const SCHEDULE_ICONS: Record<number, string> = {
  0: '🔄', 1: '🌅', 2: '🌇', 3: '🌙',
};
export const LIFESTYLE_KEYS: Record<string, string> = {
  quiet: 'lifestyleQuiet', social: 'lifestyleSocial', mixed: 'lifestyleMixed',
};
export const LIFESTYLE_ICONS: Record<string, string> = {
  quiet: '📚', social: '🎉', mixed: '☕',
};
export const CLEANLINESS_KEYS: Record<string, string> = {
  veryClean: 'cleanlinessVeryClean', clean: 'cleanlinessClean', moderate: 'cleanlinessModerate',
};

export const getAge = (dateOfBirth?: string): number | null => {
  if (!dateOfBirth) return null;
  const birth = new Date(dateOfBirth);
  const today = new Date();
  let age = today.getFullYear() - birth.getFullYear();
  if (today.getMonth() < birth.getMonth() ||
      (today.getMonth() === birth.getMonth() && today.getDate() < birth.getDate())) age--;
  // Backend serializes an unset DateOfBirth as 0001-01-01 which would render
  // as an absurd "age" (e.g. 2025) — treat implausible values as unknown.
  if (age < 14 || age > 100) return null;
  return age;
};

export const formatAvailableFrom = (date?: string): string => {
  if (!date) return 'Odmah';
  const d = new Date(date);
  const now = new Date();
  if (d <= now) return 'Odmah';
  return d.toLocaleDateString('sr-RS', { day: 'numeric', month: 'short', year: 'numeric' });
};
