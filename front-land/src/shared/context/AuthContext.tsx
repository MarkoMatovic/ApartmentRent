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
  console.log('Attempting to decode token:', token);
  try {
    if (!token || typeof token !== 'string') {
      console.error('Token is null or not a string');
      return null;
    }

    const parts = token.split('.');
    if (parts.length !== 3) {
      console.warn('Invalid token format - expected 3 parts, got:', parts.length);
      return null;
    }

    const base64Url = parts[1];
    if (!base64Url) {
      console.error('Token payload part is missing');
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
    console.log('Decoded Token Payload Successfully:', payload);

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
    };
  } catch (error) {
    console.error('CRITICAL: Error decoding token:', error);
    return null;
  }
};

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const initAuth = async () => {
      console.log('Initializing Auth State...');
      const storedToken = localStorage.getItem('authToken');
      const storedUser = localStorage.getItem('user');

      if (storedToken) {
        console.log('Found stored token');
        setToken(storedToken);
        if (storedUser) {
          try {
            console.log('Found stored user data');
            setUser(JSON.parse(storedUser));
          } catch (e) {
            console.error('Error parsing stored user:', e);
            const decodedUser = decodeToken(storedToken);
            if (decodedUser) {
              setUser(decodedUser);
              localStorage.setItem('user', JSON.stringify(decodedUser));
            }
          }
        } else {
          console.log('No stored user data, decoding from token...');
          const decodedUser = decodeToken(storedToken);
          if (decodedUser) {
            setUser(decodedUser);
            localStorage.setItem('user', JSON.stringify(decodedUser));
          }
        }
      }
      setLoading(false);
    };

    initAuth();
  }, []);

  const login = async (email: string, password: string) => {
    try {
      console.log('Logging in user:', email);
      const tokenResult = await authApi.login({ email, password });

      if (!tokenResult || typeof tokenResult !== 'string' || tokenResult.split('.').length !== 3) {
        console.error('Backend returned invalid token:', tokenResult);
        throw new Error('Neispravan format tokena sa servera');
      }

      setToken(tokenResult);
      localStorage.setItem('authToken', tokenResult);

      const decodedUser = decodeToken(tokenResult);
      if (decodedUser) {
        console.log('User logged in and decoded:', decodedUser);
        setUser(decodedUser);
        localStorage.setItem('user', JSON.stringify(decodedUser));
      } else {
        console.error('Failed to decode valid looking token');
        throw new Error('Problem sa dekodiranjem korisničkih podataka');
      }
    } catch (error) {
      console.error('Login function error:', error);
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
      console.log('Logging out...');
      await authApi.logout();
    } catch (error) {
      console.error('Logout error:', error);
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
