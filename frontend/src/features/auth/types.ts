import type { IsoDate, Role, Uuid } from '../../shared/types/api';

export interface AuthUser {
  id: Uuid;
  email: string;
  fullName: string;
  role: Role;
  operatorId: Uuid | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: IsoDate;
  refreshToken?: string | null;
  user: AuthUser;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface LogoutRequest {
  refreshToken?: string | null;
}

export interface DemoAccountOption {
  email: string;
  fullName: string;
  role: Role;
  badgeText: string;
  description: string;
}
