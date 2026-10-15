import React, { createContext, useContext, useState, useEffect, useRef, ReactNode } from 'react';
import { User, RegisterRequest } from '../types/user';
import { authApi } from '../api/auth';
import { refreshAccessToken } from '../api/client';
import { setAccessToken } from '../api/tokenStore';
import { decodeToken } from '../auth/decodeToken';
import { forStorage, mergeProfile } from '../auth/userProfile';
import { usersApi } from '../api/usersApi';

/** With two-step sign-in the password only opens the code step; the session starts after the code. */
export interface LoginOutcome {
  twoFactorToken?: string;
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<LoginOutcome>;
  /** The code step after login() asked for it. */
  completeTwoFactorLogin: (twoFactorToken: string, code: string) => Promise<void>;
  /** A new access token for this session (two-step sign-in turned on or off). */
  adoptAccessToken: (accessToken: string) => void;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  /** Signs out every device (a lost phone); this one too. */
  logoutEverywhere: () => Promise<void>;
  updateUser: (updatedUser: User) => void;
  isAuthenticated: boolean;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const isTokenExpired = (token: string): boolean => {
  try {
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    return payload.exp != null && payload.exp * 1000 < Date.now();
  } catch {
    return true;
  }
};

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const profileLoadedFor = useRef<number | null>(null);

  // The token has no picture, birth date or privacy settings: load the stored profile once per
  // signed-in user so the header and the chat can show the real avatar.
  useEffect(() => {
    const userId = user?.userId;
    if (!token || !userId || profileLoadedFor.current === userId) return;
    profileLoadedFor.current = userId;

    usersApi
      .getProfile(userId)
      .then((profile) => setUser((prev) => (prev && prev.userId === userId ? mergeProfile(prev, profile) : prev)))
      .catch(() => {
        // The avatar falls back to the initials; try again on the next sign-in.
        profileLoadedFor.current = null;
      });
  }, [token, user?.userId]);

  useEffect(() => {
    const initAuth = async () => {
      // Access token živi samo u memoriji (ne sessionStorage) zbog XSS zaštite.
      // Na svakom page load-u tiho refreshujemo token koristeći httpOnly refresh cookie.
      // Ako refresh cookie ne postoji ili je istekao, korisnik ostaje odjavljen.
      const storedUser = sessionStorage.getItem('user');

      try {
        const newToken = await refreshAccessToken();
        setToken(newToken);

        if (storedUser) {
          try {
            const parsed = JSON.parse(storedUser) as User;
            // A cached user from before roleName was read correctly has none; take it from the fresh token.
            if (!parsed.roleName) parsed.roleName = decodeToken(newToken)?.roleName;
            setUser(parsed);
          } catch {
            const decoded = decodeToken(newToken);
            if (decoded) {
              setUser(decoded);
              sessionStorage.setItem('user', JSON.stringify(forStorage(decoded)));
            }
          }
        } else {
          const decoded = decodeToken(newToken);
          if (decoded) {
            setUser(decoded);
            sessionStorage.setItem('user', JSON.stringify(forStorage(decoded)));
          }
        }

        window.dispatchEvent(new Event('authTokenChanged'));
      } catch {
        // Refresh cookie ne postoji ili je istekao — korisnik nije ulogovan
        setAccessToken(null);
        sessionStorage.removeItem('user');
      } finally {
        setLoading(false);
      }
    };

    initAuth();
  }, []);

  const startSession = (accessToken: string | undefined) => {
    if (!accessToken || accessToken.split('.').length !== 3) {
      throw new Error('Neispravan format tokena sa servera');
    }

    // Token čuvamo samo u memoriji — ne u sessionStorage (S-6 fix)
    setAccessToken(accessToken);
    setToken(accessToken);
    // Note: refresh token je httpOnly cookie, server ga postavlja automatski

    window.dispatchEvent(new Event('authTokenChanged'));

    const decodedUser = decodeToken(accessToken);
    if (decodedUser) {
      setUser(decodedUser);
      sessionStorage.setItem('user', JSON.stringify(forStorage(decodedUser)));
    } else {
      throw new Error('Problem sa dekodiranjem korisničkih podataka');
    }
  };

  const login = async (email: string, password: string): Promise<LoginOutcome> => {
    const tokenResult = await authApi.login({ email, password });
    if (tokenResult?.requiresTwoFactor && tokenResult.twoFactorToken) {
      return { twoFactorToken: tokenResult.twoFactorToken };
    }
    startSession(tokenResult?.accessToken);
    return {};
  };

  const completeTwoFactorLogin = async (twoFactorToken: string, code: string) => {
    const tokenResult = await authApi.loginTwoFactor(twoFactorToken, code);
    startSession(tokenResult.accessToken);
  };

  const adoptAccessToken = (accessToken: string) => {
    setAccessToken(accessToken);
    setToken(accessToken);
    window.dispatchEvent(new Event('authTokenChanged'));
    const decoded = decodeToken(accessToken);
    if (!decoded) return;
    // Only what the token decides; the profile fields (picture, birth date...) stay as they are.
    setUser(prev => {
      const next = prev
        ? { ...prev, roleName: decoded.roleName, permissions: decoded.permissions, twoFactorVerified: decoded.twoFactorVerified }
        : decoded;
      sessionStorage.setItem('user', JSON.stringify(forStorage(next)));
      return next;
    });
  };

  const register = async (data: RegisterRequest) => {
    await authApi.register(data);
    // Registration creates an inactive account — no token issued.
    // User must verify email before they can log in.
  };

  const clearSession = () => {
    setUser(null);
    setToken(null);
    setAccessToken(null);
    profileLoadedFor.current = null;
    sessionStorage.removeItem('user');
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } catch {
      // Ignore logout errors — always clear local state
    } finally {
      clearSession();
    }
  };

  const logoutEverywhere = async () => {
    // Unlike logout, a failure is reported: the user must know the other devices may still be signed in.
    await authApi.logoutEverywhere();
    clearSession();
  };

  const updateUser = (updatedUser: User) => {
    setUser(updatedUser);
    sessionStorage.setItem('user', JSON.stringify(forStorage(updatedUser)));
  };

  const value: AuthContextType = {
    user,
    token,
    login,
    completeTwoFactorLogin,
    adoptAccessToken,
    register,
    logout,
    logoutEverywhere,
    updateUser,
    isAuthenticated: !!token && !isTokenExpired(token),
    loading,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
