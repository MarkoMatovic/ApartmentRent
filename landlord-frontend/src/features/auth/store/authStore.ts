import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import { AuthState } from '../types/auth';
import { authService } from '../api/authService';
import { LoginUserInputDto, UserRegistrationInputDto } from '../types';
import { setAuthToken, clearAuthToken, getAuthToken } from '@/shared/api/client';
import { decodeJwt, isTokenExpired } from '@/shared/utils/jwt';

interface AuthStore extends AuthState {
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  register: (data: UserRegistrationInputDto) => Promise<void>;
  isLoading: boolean;
  error: string | null;
  initialize: () => void;
}

export const useAuthStore = create<AuthStore>()(
  persist(
    (set) => ({
      token: null,
      isAuthenticated: false,
      userId: null,
      userGuid: null,
      isLoading: false,
      error: null,

      initialize: () => {
        const token = getAuthToken();
        if (token && !isTokenExpired(token)) {
          const payload = decodeJwt(token);
          set({
            token,
            isAuthenticated: true,
            userGuid: payload?.sub || null,
            // Note: userId is not in JWT, would need to fetch from backend
            // For now, it will be null and can be set when needed
          });
        } else if (token) {
          // Token expired, clear it
          clearAuthToken();
          set({
            token: null,
            isAuthenticated: false,
            userId: null,
            userGuid: null,
          });
        }
      },

      login: async (email: string, password: string) => {
        set({ isLoading: true, error: null });
        try {
          const token = await authService.login({ email, password });
          const payload = decodeJwt(token);
          set({
            token,
            isAuthenticated: true,
            userGuid: payload?.sub || null,
            // Note: userId is not in JWT, would need to fetch from backend
            // For now, it will be null and can be set when needed
            isLoading: false,
          });
        } catch (error: any) {
          set({ error: error.message || 'Login failed', isLoading: false });
          throw error;
        }
      },

      logout: async () => {
        set({ isLoading: true });
        try {
          await authService.logout();
        } catch (error) {
          // Even if logout fails on server, clear local state
          console.error('Logout error:', error);
        } finally {
          clearAuthToken();
          set({
            token: null,
            isAuthenticated: false,
            userId: null,
            userGuid: null,
            isLoading: false,
          });
        }
      },

      register: async (data: UserRegistrationInputDto) => {
        set({ isLoading: true, error: null });
        try {
          await authService.register(data);
          set({ isLoading: false });
        } catch (error: any) {
          set({ error: error.message || 'Registration failed', isLoading: false });
          throw error;
        }
      },
    }),
    {
      name: 'auth-storage',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        token: state.token,
        isAuthenticated: state.isAuthenticated,
        userId: state.userId,
        userGuid: state.userGuid,
      }),
    }
  )
);

