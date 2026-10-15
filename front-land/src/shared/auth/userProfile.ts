import { User } from '../types/user';

/**
 * The signed-in user is built from the JWT, which carries no picture, birth date or privacy settings.
 * This lays the stored profile over that user so the header, chat and forms see what is really saved.
 */
export const mergeProfile = (user: User, profile: User): User => ({
  ...user,
  phoneNumber: profile.phoneNumber ?? user.phoneNumber,
  profilePicture: profile.profilePicture || undefined,
  // The API returns a full timestamp; <input type="date"> needs yyyy-MM-dd.
  dateOfBirth: profile.dateOfBirth ? profile.dateOfBirth.slice(0, 10) : undefined,
  analyticsConsent: profile.analyticsConsent,
  chatHistoryConsent: profile.chatHistoryConsent,
  profileVisibility: profile.profileVisibility,
  isIncognito: profile.isIncognito,
  twoFactorEnabled: profile.twoFactorEnabled,
  twoFactorRequired: profile.twoFactorRequired,
});

/**
 * What goes into sessionStorage. A picture is normally a short address; one still kept as a base64
 * data URL (from before pictures moved to file storage) can be several MB against a ~5 MB quota,
 * so that one stays out (it is fetched again on every page load anyway).
 */
export const forStorage = (user: User): User => {
  const copy = { ...user };
  if (copy.profilePicture?.startsWith('data:')) delete copy.profilePicture;
  return copy;
};
