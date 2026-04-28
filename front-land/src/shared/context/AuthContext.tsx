import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { User, RegisterRequest } from '../types/user';
import { Permission } from '../types/permission';
import { authApi } from '../api/auth';

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

    // Extract permissions from claims (can be array or single value)
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
      const storedToken = sessionStorage.getItem('authToken');
      const storedUser = sessionStorage.getItem('user');

      if (storedToken && !isTokenExpired(storedToken)) {
        setToken(storedToken);
        if (storedUser) {
          try {
            setUser(JSON.parse(storedUser));
          } catch (e) {
            const decodedUser = decodeToken(storedToken);
            if (decodedUser) {
              setUser(decodedUser);
              sessionStorage.setItem('user', JSON.stringify(decodedUser));
            }
          }
        } else {
          const decodedUser = decodeToken(storedToken);
          if (decodedUser) {
            setUser(decodedUser);
            sessionStorage.setItem('user', JSON.stringify(decodedUser));
            // Pošalji custom event da se osveži broj nepročitanih poruka
            window.dispatchEvent(new Event('authTokenChanged'));
          }
        }
      }
      setLoading(false);
    };

    initAuth();
  }, []);

  const login = async (email: string, password: string) => {
    try {
      const tokenResult = await authApi.login({ email, password });

      if (!tokenResult?.accessToken || tokenResult.accessToken.split('.').length !== 3) {
        throw new Error('Neispravan format tokena sa servera');
      }

      setToken(tokenResult.accessToken);
      sessionStorage.setItem('authToken', tokenResult.accessToken);
      // Note: refresh token is stored in httpOnly cookie by the server — not handled here

      // Pošalji custom event da se osveži broj nepročitanih poruka
      window.dispatchEvent(new Event('authTokenChanged'));

      const decodedUser = decodeToken(tokenResult.accessToken);
      if (decodedUser) {
        setUser(decodedUser);
        sessionStorage.setItem('user', JSON.stringify(decodedUser));
      } else {
        throw new Error('Problem sa dekodiranjem korisničkih podataka');
      }
    } catch (error) {
      throw error;
    }
  };

  const register = async (data: RegisterRequest) => {
    await authApi.register(data);
    // Registration creates an inactive account — no token issued.
    // User must verify email before they can log in.
  };

  const logout = async () => {
    try {
      // Refresh token is sent automatically via httpOnly cookie; no need to read it from storage
      await authApi.logout();
    } catch (error) {
      // Ignore logout errors — always clear local state
    } finally {
      setUser(null);
      setToken(null);
      sessionStorage.removeItem('authToken');
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
