import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { User, RegisterRequest } from '../types/user';
import { Permission } from '../types/permission';
import { authApi } from '../api/auth';
import { apiClient } from '../api/client';
import { setAccessToken } from '../api/tokenStore';

interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
  updateUser: (updatedUser: User) => void;
  isAuthenticated: boolean;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const decodeToken = (token: string): User | null => {
  try {
    if (!token || typeof token !== 'string') {
      return null;
    }

    const parts = token.split('.');
    if (parts.length !== 3) {
      return null;
    }

    const base64Url = parts[1];
    if (!base64Url) {
      return null;
    }

    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    const payload = JSON.parse(jsonPayload);

    let permissions: string[] = [];
    if (payload.permission) {
      permissions = Array.isArray(payload.permission)
        ? payload.permission
        : [payload.permission];
    }

    return {
      userId: parseInt(payload.userId || payload.nameid || payload.id) || -1,
      userGuid: payload.sub || payload.nameid || '',
      firstName: payload.given_name || payload.givenName || payload.first_name || '',
      lastName: payload.family_name || payload.familyName || payload.last_name || '',
      email: payload.email || payload.emailaddress || '',
      phoneNumber: payload.phone_number || payload.phone,
      isActive: payload.isActive === 'true' || payload.isActive === true || payload.active === true,
      isLookingForRoommate: payload.isLookingForRoommate === 'true' || payload.isLookingForRoommate === true,
      userRoleId: payload.userRoleId ? parseInt(payload.userRoleId) : undefined,
      roleName: payload.role || payload.roleName,
      permissions: permissions as Permission[],
      hasPersonalAnalytics: payload.hasPersonalAnalytics === 'true' || payload.hasPersonalAnalytics === true,
      hasLandlordAnalytics: payload.hasLandlordAnalytics === 'true' || payload.hasLandlordAnalytics === true,
      subscriptionExpiresAt: payload.subscriptionExpiresAt || undefined,
      tokenBalance: payload.tokenBalance !== undefined ? parseInt(payload.tokenBalance) : 3,
      isIncognito: payload.isIncognito === 'true' || payload.isIncognito === true,
    };
  } catch (error) {
    return null;
  }
};

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

  useEffect(() => {
    const initAuth = async () => {
      // Access token živi samo u memoriji (ne sessionStorage) zbog XSS zaštite.
      // Na svakom page load-u tiho refreshujemo token koristeći httpOnly refresh cookie.
      // Ako refresh cookie ne postoji ili je istekao, korisnik ostaje odjavljen.
      const storedUser = sessionStorage.getItem('user');

      try {
        const { data } = await apiClient.post<{ accessToken: string }>('/api/v1/auth/token/refresh');

        const newToken = data.accessToken;
        setAccessToken(newToken);
        setToken(newToken);

        if (storedUser) {
          try {
            setUser(JSON.parse(storedUser));
          } catch {
            const decoded = decodeToken(newToken);
            if (decoded) {
              setUser(decoded);
              sessionStorage.setItem('user', JSON.stringify(decoded));
            }
          }
        } else {
          const decoded = decodeToken(newToken);
          if (decoded) {
            setUser(decoded);
            sessionStorage.setItem('user', JSON.stringify(decoded));
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

  const login = async (email: string, password: string) => {
    const tokenResult = await authApi.login({ email, password });

    if (!tokenResult?.accessToken || tokenResult.accessToken.split('.').length !== 3) {
      throw new Error('Neispravan format tokena sa servera');
    }

    // Token čuvamo samo u memoriji — ne u sessionStorage (S-6 fix)
    setAccessToken(tokenResult.accessToken);
    setToken(tokenResult.accessToken);
    // Note: refresh token je httpOnly cookie, server ga postavlja automatski

    window.dispatchEvent(new Event('authTokenChanged'));

    const decodedUser = decodeToken(tokenResult.accessToken);
    if (decodedUser) {
      setUser(decodedUser);
      sessionStorage.setItem('user', JSON.stringify(decodedUser));
    } else {
      throw new Error('Problem sa dekodiranjem korisničkih podataka');
    }
  };

  const register = async (data: RegisterRequest) => {
    await authApi.register(data);
    // Registration creates an inactive account — no token issued.
    // User must verify email before they can log in.
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } catch {
      // Ignore logout errors — always clear local state
    } finally {
      setUser(null);
      setToken(null);
      setAccessToken(null);
      sessionStorage.removeItem('user');
    }
  };

  const updateUser = (updatedUser: User) => {
    setUser(updatedUser);
    sessionStorage.setItem('user', JSON.stringify(updatedUser));
  };

  const value: AuthContextType = {
    user,
    token,
    login,
    register,
    logout,
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
