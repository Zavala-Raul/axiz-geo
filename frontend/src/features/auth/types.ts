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
  user: AuthUser;
}

export interface DemoAccountOption {
  email: string;
  fullName: string;
  role: Role;
  badgeText: string;
  description: string;
}
