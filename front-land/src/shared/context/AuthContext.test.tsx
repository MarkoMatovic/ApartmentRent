import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, render, waitFor } from '@testing-library/react';
import type { User } from '../types/user';

const api = vi.hoisted(() => ({
  refresh: vi.fn(),
  login: vi.fn(),
  logout: vi.fn(),
  logoutEverywhere: vi.fn(),
  getProfile: vi.fn(),
}));

vi.mock('../api/client', () => ({
  refreshAccessToken: () => api.refresh('/api/v1/auth/token/refresh').then((r: { data: { accessToken: string } }) => r.data.accessToken),
}));
vi.mock('../api/auth', () => ({
  authApi: { login: api.login, logout: api.logout, logoutEverywhere: api.logoutEverywhere, register: vi.fn() },
}));
vi.mock('../api/tokenStore', () => ({ setAccessToken: vi.fn() }));
vi.mock('../api/usersApi', () => ({ usersApi: { getProfile: api.getProfile } }));

import { AuthProvider, useAuth } from './AuthContext';

const PICTURE = 'data:image/png;base64,' + 'A'.repeat(400);

const jwt = (claims: Record<string, unknown>) =>
  `h.${btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + 3600, ...claims }))}.s`;

const tokenFor = (userId: number, firstName: string) =>
  jwt({ userId: String(userId), given_name: firstName, family_name: 'Test', email: `${firstName}@test.com` });

let auth: ReturnType<typeof useAuth>;
const Capture = () => {
  auth = useAuth();
  return null;
};
const mount = () => render(<AuthProvider><Capture /></AuthProvider>);

const profile = (userId: number, over: Partial<User> = {}): User => ({
  userId, userGuid: 'g', firstName: 'Marko', lastName: 'Matovic', email: 'm@test.com', isActive: true,
  profilePicture: PICTURE, dateOfBirth: '1992-06-09T00:00:00', phoneNumber: '0637721040',
  profileVisibility: true, isIncognito: false, analyticsConsent: true, chatHistoryConsent: true,
  ...over,
});

beforeEach(() => {
  vi.clearAllMocks();
  sessionStorage.clear();
});

describe('AuthProvider - the stored profile (picture, birth date, privacy)', () => {
  it('loads the profile after a page load that restores the session, so the picture is available', async () => {
    api.refresh.mockResolvedValue({ data: { accessToken: tokenFor(7, 'Marko') } });
    api.getProfile.mockResolvedValue(profile(7));

    mount();

    await waitFor(() => expect(auth.user?.profilePicture).toBe(PICTURE));
    expect(api.getProfile).toHaveBeenCalledWith(7);
    expect(auth.user?.dateOfBirth).toBe('1992-06-09');
    expect(auth.user?.phoneNumber).toBe('0637721040');
    expect(auth.user?.firstName).toBe('Marko');
  });

  it('loads it after signing in, too', async () => {
    api.refresh.mockRejectedValue(new Error('no session'));
    api.login.mockResolvedValue({ accessToken: tokenFor(8, 'Ana') });
    api.getProfile.mockResolvedValue(profile(8, { firstName: 'Ana' }));
    mount();
    await waitFor(() => expect(auth.loading).toBe(false));
    expect(auth.user).toBeNull();

    await act(async () => { await auth.login('ana@test.com', 'secret'); });

    await waitFor(() => expect(auth.user?.profilePicture).toBe(PICTURE));
    expect(api.getProfile).toHaveBeenCalledWith(8);
  });

  it('keeps the picture out of sessionStorage (it can be several MB), but keeps the rest', async () => {
    api.refresh.mockResolvedValue({ data: { accessToken: tokenFor(7, 'Marko') } });
    api.getProfile.mockResolvedValue(profile(7));
    mount();
    await waitFor(() => expect(auth.user?.profilePicture).toBe(PICTURE));

    act(() => auth.updateUser({ ...auth.user!, phoneNumber: '0641112222' }));

    const stored = sessionStorage.getItem('user')!;
    expect(stored).not.toContain('base64');
    expect(JSON.parse(stored)).toMatchObject({ userId: 7, phoneNumber: '0641112222' });
    expect(auth.user?.profilePicture).toBe(PICTURE); // still there in memory
  });

  it('asks for the profile once per signed-in user, not on every change of the user', async () => {
    api.refresh.mockResolvedValue({ data: { accessToken: tokenFor(7, 'Marko') } });
    api.getProfile.mockResolvedValue(profile(7));
    mount();
    await waitFor(() => expect(auth.user?.profilePicture).toBe(PICTURE));

    act(() => auth.updateUser({ ...auth.user!, phoneNumber: '1' }));
    act(() => auth.updateUser({ ...auth.user!, phoneNumber: '2' }));

    expect(api.getProfile).toHaveBeenCalledTimes(1);
  });

  it('still works when the profile cannot be loaded, and tries again at the next sign-in', async () => {
    api.refresh.mockRejectedValue(new Error('no session'));
    api.login.mockResolvedValue({ accessToken: tokenFor(9, 'Petar') });
    api.getProfile.mockRejectedValueOnce(new Error('network')).mockResolvedValue(profile(9, { firstName: 'Petar' }));
    mount();
    await waitFor(() => expect(auth.loading).toBe(false));

    await act(async () => { await auth.login('p@test.com', 'secret'); });
    await waitFor(() => expect(api.getProfile).toHaveBeenCalledTimes(1));
    expect(auth.user?.firstName).toBe('Petar');          // the token user is usable without a picture
    expect(auth.user?.profilePicture).toBeUndefined();
    expect(auth.isAuthenticated).toBe(true);

    await act(async () => { await auth.logout(); });
    await act(async () => { await auth.login('p@test.com', 'secret'); });

    await waitFor(() => expect(auth.user?.profilePicture).toBe(PICTURE));
    expect(api.getProfile).toHaveBeenCalledTimes(2);
  });

  it('does not show one user the picture of the previous one', async () => {
    api.refresh.mockRejectedValue(new Error('no session'));
    api.login
      .mockResolvedValueOnce({ accessToken: tokenFor(7, 'Marko') })
      .mockResolvedValueOnce({ accessToken: tokenFor(8, 'Ana') });
    api.logout.mockResolvedValue(undefined);
    api.getProfile.mockImplementation(async (id: number) =>
      profile(id, { firstName: id === 7 ? 'Marko' : 'Ana', profilePicture: id === 7 ? PICTURE : undefined }));
    mount();
    await waitFor(() => expect(auth.loading).toBe(false));

    await act(async () => { await auth.login('m@test.com', 'x'); });
    await waitFor(() => expect(auth.user?.profilePicture).toBe(PICTURE));
    await act(async () => { await auth.logout(); });
    expect(auth.user).toBeNull();

    await act(async () => { await auth.login('a@test.com', 'x'); });
    await waitFor(() => expect(api.getProfile).toHaveBeenCalledWith(8));
    await waitFor(() => expect(auth.user?.userId).toBe(8));
    expect(auth.user?.profilePicture).toBeUndefined();
  });
});

describe('AuthProvider - signing out every device', () => {
  it('ends the session here too once the server has signed out every device', async () => {
    api.refresh.mockResolvedValue({ data: { accessToken: tokenFor(7, 'Marko') } });
    api.getProfile.mockResolvedValue(profile(7));
    api.logoutEverywhere.mockResolvedValue(undefined);
    mount();
    await waitFor(() => expect(auth.isAuthenticated).toBe(true));

    await act(async () => { await auth.logoutEverywhere(); });

    expect(api.logoutEverywhere).toHaveBeenCalledTimes(1);
    expect(api.logout).not.toHaveBeenCalled();
    expect(auth.user).toBeNull();
    expect(sessionStorage.getItem('user')).toBeNull();
  });

  it('reports a failure and stays signed in, because the other devices may still be signed in', async () => {
    api.refresh.mockResolvedValue({ data: { accessToken: tokenFor(7, 'Marko') } });
    api.getProfile.mockResolvedValue(profile(7));
    api.logoutEverywhere.mockRejectedValue(new Error('network'));
    mount();
    await waitFor(() => expect(auth.isAuthenticated).toBe(true));

    await act(async () => {
      await expect(auth.logoutEverywhere()).rejects.toThrow('network');
    });

    expect(auth.user?.userId).toBe(7);
  });
});
