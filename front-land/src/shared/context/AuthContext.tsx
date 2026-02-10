import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { User } from '../types/user';
import { authApi } from '../api/auth';

interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (data: any) => Promise<void>;
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
      userId: parseInt(payload.userId || payload.nameid || payload.id) || 0,
      userGuid: payload.sub || payload.nameid || '',
      firstName: payload.given_name || payload.givenName || payload.first_name || '',
      lastName: payload.family_name || payload.familyName || payload.last_name || '',
      email: payload.email || payload.emailaddress || '',
      phoneNumber: payload.phone_number || payload.phone,
      isActive: payload.isActive === 'true' || payload.isActive === true || payload.active === true,
      isLookingForRoommate: payload.isLookingForRoommate === 'true' || payload.isLookingForRoommate === true,
      userRoleId: payload.userRoleId ? parseInt(payload.userRoleId) : undefined,
      roleName: payload.role || payload.roleName,
      permissions: permissions as any,
      hasPersonalAnalytics: payload.hasPersonalAnalytics === 'true' || payload.hasPersonalAnalytics === true,
      hasLandlordAnalytics: payload.hasLandlordAnalytics === 'true' || payload.hasLandlordAnalytics === true,
      subscriptionExpiresAt: payload.subscriptionExpiresAt || undefined,
    };
  } catch (error) {
    return null;
  }
};

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const initAuth = async () => {
      const storedToken = localStorage.getItem('authToken');
      const storedUser = localStorage.getItem('user');

      if (storedToken) {
        setToken(storedToken);
        if (storedUser) {
          try {
            setUser(JSON.parse(storedUser));
          } catch (e) {
            const decodedUser = decodeToken(storedToken);
            if (decodedUser) {
              setUser(decodedUser);
              localStorage.setItem('user', JSON.stringify(decodedUser));
            }
          }
        } else {
          const decodedUser = decodeToken(storedToken);
          if (decodedUser) {
            setUser(decodedUser);
            localStorage.setItem('user', JSON.stringify(decodedUser));
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

      if (!tokenResult || typeof tokenResult !== 'string' || tokenResult.split('.').length !== 3) {
        throw new Error('Neispravan format tokena sa servera');
      }

      setToken(tokenResult);
      localStorage.setItem('authToken', tokenResult);

      // Pošalji custom event da se osveži broj nepročitanih poruka
      window.dispatchEvent(new Event('authTokenChanged'));

      const decodedUser = decodeToken(tokenResult);
      if (decodedUser) {
        setUser(decodedUser);
        localStorage.setItem('user', JSON.stringify(decodedUser));
      } else {
        throw new Error('Problem sa dekodiranjem korisničkih podataka');
      }
    } catch (error) {
      throw error;
    }
  };

  const register = async (data: any) => {
    try {
      const newUser = await authApi.register(data);
      setUser(newUser);
    } catch (error) {
      throw error;
    }
  };

  const logout = async () => {
    try {
      await authApi.logout();
    } catch (error) {
      // Ignore logout errors
    } finally {
      setUser(null);
      setToken(null);
      localStorage.removeItem('authToken');
      localStorage.removeItem('user');
    }
  };

  const updateUser = (updatedUser: User) => {
    setUser(updatedUser);
    localStorage.setItem('user', JSON.stringify(updatedUser));
  };

  const value: AuthContextType = {
    user,
    token,
    login,
    register,
    logout,
    updateUser,
    isAuthenticated: !!token,
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
