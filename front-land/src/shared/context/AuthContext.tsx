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

// Helper function to decode JWT token
const decodeToken = (token: string): User | null => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    const payload = JSON.parse(jsonPayload);

    // Map JWT claims to User object
    return {
      userId: parseInt(payload.userId) || 0,
      userGuid: payload.sub || '',
      firstName: payload.given_name || '',
      lastName: payload.family_name || '',
      email: payload.email || '',
      phoneNumber: payload.phone_number,
      isActive: payload.isActive === 'true' || payload.isActive === true,
      isLookingForRoommate: payload.isLookingForRoommate === 'true' || payload.isLookingForRoommate === true,
      userRoleId: payload.userRoleId ? parseInt(payload.userRoleId) : undefined,
    };
  } catch (error) {
    console.error('Failed to decode token:', error);
    return null;
  }
};

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Load user and token from localStorage on mount
    const storedToken = localStorage.getItem('authToken');
    const storedUser = localStorage.getItem('user');

    if (storedToken) {
      setToken(storedToken);
      if (storedUser) {
        setUser(JSON.parse(storedUser));
      } else {
        // Decode token to get user data
        const decodedUser = decodeToken(storedToken);
        if (decodedUser) {
          setUser(decodedUser);
          localStorage.setItem('user', JSON.stringify(decodedUser));
        }
      }
    }
    setLoading(false);
  }, []);

  const login = async (email: string, password: string) => {
    try {
      const token = await authApi.login({ email, password });
      setToken(token);
      localStorage.setItem('authToken', token);

      // Decode token to extract user data
      const decodedUser = decodeToken(token);
      if (decodedUser) {
        setUser(decodedUser);
        localStorage.setItem('user', JSON.stringify(decodedUser));
      }
    } catch (error) {
      throw error;
    }
  };

  const register = async (data: any) => {
    try {
      const newUser = await authApi.register(data);
      setUser(newUser);
      // After registration, you might want to auto-login
    } catch (error) {
      throw error;
    }
  };

  const logout = async () => {
    try {
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

