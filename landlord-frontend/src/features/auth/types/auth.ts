// Auth state and context types

export interface AuthState {
  token: string | null;
  isAuthenticated: boolean;
  userId: number | null;
  userGuid: string | null;
}

export interface AuthContextType extends AuthState {
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  register: (data: import('./index').UserRegistrationInputDto) => Promise<void>;
  isLoading: boolean;
}

