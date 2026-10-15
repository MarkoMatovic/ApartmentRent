import { describe, expect, it } from 'vitest';
import type { User } from '../types/user';
import { forStorage, mergeProfile } from './userProfile';

const base = { userId: 7, userGuid: 'g', firstName: 'Ana', lastName: 'Anić', email: 'a@a.rs', isActive: true } as User;

describe('mergeProfile', () => {
  it('adds the stored picture, birth date (date part only) and privacy settings to the token user', () => {
    const merged = mergeProfile(base, {
      ...base,
      profilePicture: 'data:image/png;base64,AAAA',
      dateOfBirth: '1995-04-12T00:00:00',
      profileVisibility: true,
      isIncognito: false,
      analyticsConsent: true,
      chatHistoryConsent: false,
    });

    expect(merged.profilePicture).toBe('data:image/png;base64,AAAA');
    expect(merged.dateOfBirth).toBe('1995-04-12');
    expect(merged.profileVisibility).toBe(true);
    expect(merged.chatHistoryConsent).toBe(false);
    expect(merged.firstName).toBe('Ana');
  });

  it('keeps the phone from the token when the profile has none, and treats an empty picture as none', () => {
    const merged = mergeProfile({ ...base, phoneNumber: '+381' }, { ...base, profilePicture: '' });

    expect(merged.phoneNumber).toBe('+381');
    expect(merged.profilePicture).toBeUndefined();
  });
});

describe('forStorage', () => {
  it('leaves an inline picture out of what goes to sessionStorage and does not touch the original', () => {
    const user = { ...base, profilePicture: 'data:image/png;base64,AAAA' };

    expect(forStorage(user).profilePicture).toBeUndefined();
    expect(forStorage(user).firstName).toBe('Ana');
    expect(user.profilePicture).toBe('data:image/png;base64,AAAA');
  });

  it('keeps the address of a stored picture', () => {
    const url = 'https://landlanderstore.blob.core.windows.net/avatars/a/b.webp';

    expect(forStorage({ ...base, profilePicture: url }).profilePicture).toBe(url);
  });
});
