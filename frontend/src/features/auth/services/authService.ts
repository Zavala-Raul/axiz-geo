import { httpClient, USE_MOCKS } from '../../../shared/api/httpClient';
import { mockGetMe, mockLogin, mockRefresh } from '../mocks/authMock';
import type { AuthUser, LoginRequest, LoginResponse } from '../types';

export const authService = {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    if (USE_MOCKS) {
      return mockLogin(credentials);
    }
    return httpClient.post<LoginResponse>('/auth/login', credentials, { skipAuth: true });
  },

  async refresh(refreshToken: string): Promise<LoginResponse> {
    if (USE_MOCKS) {
      return mockRefresh(refreshToken);
    }
    return httpClient.post<LoginResponse>(
      '/auth/refresh',
      { refreshToken },
      { skipAuth: true },
    );
  },

  async getMe(currentToken: string | null): Promise<AuthUser> {
    if (USE_MOCKS) {
      return mockGetMe(currentToken);
    }
    return httpClient.get<AuthUser>('/auth/me');
  },

  async logout(refreshToken?: string | null): Promise<void> {
    if (USE_MOCKS) {
      return;
    }
    try {
      await httpClient.post<void>(
        '/auth/logout',
        refreshToken ? { refreshToken } : undefined,
      );
    } catch {
      // Local session cleanup must always succeed even if server token already expired
    }
  },
};
