import { createContext } from 'react';
import type { Role } from '../../../shared/types/api';
import type { AuthUser, LoginRequest } from '../types';

export interface AuthContextValue {
  user: AuthUser | null;
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<AuthUser>;
  logout: () => Promise<void>;
  hasRole: (roles?: Role[]) => boolean;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
