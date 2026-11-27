import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  registerAccessTokenProvider,
  registerTokenRefreshHandler,
  registerUnauthorizedHandler,
  SESSION_STORAGE_KEY,
} from '../../../shared/api/httpClient';
import type { Role } from '../../../shared/types/api';
import { authService } from '../services/authService';
import type { AuthUser, LoginRequest, LoginResponse } from '../types';
import { AuthContext, type AuthContextValue } from './AuthContextObject';

interface StoredSession {
  accessToken: string;
  expiresAt: string;
  refreshToken?: string | null;
  user: AuthUser;
}

function readStoredSession(): StoredSession | null {
  try {
    const raw = localStorage.getItem(SESSION_STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as StoredSession;
    if (!parsed.accessToken || !parsed.user || !parsed.expiresAt) {
      return null;
    }
    const isAccessTokenExpired = new Date(parsed.expiresAt).getTime() <= Date.now();
    // If access token expired and there is no refreshToken to rotate, invalidate stored session
    if (isAccessTokenExpired && !parsed.refreshToken) {
      localStorage.removeItem(SESSION_STORAGE_KEY);
      return null;
    }
    return parsed;
  } catch {
    localStorage.removeItem(SESSION_STORAGE_KEY);
    return null;
  }
}

function persistSession(session: LoginResponse | null): void {
  if (!session) {
    localStorage.removeItem(SESSION_STORAGE_KEY);
    return;
  }
  localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [initialSession] = useState<StoredSession | null>(() => readStoredSession());
  const [user, setUser] = useState<AuthUser | null>(initialSession?.user ?? null);
  const [accessToken, setAccessToken] = useState<string | null>(
    initialSession?.accessToken ?? null,
  );
  const [refreshToken, setRefreshToken] = useState<string | null>(
    initialSession?.refreshToken ?? null,
  );
  const [expiresAt, setExpiresAt] = useState<string | null>(initialSession?.expiresAt ?? null);
  const [isLoading, setIsLoading] = useState<boolean>(Boolean(initialSession?.accessToken));

  const applySession = useCallback((session: LoginResponse) => {
    persistSession(session);
    setAccessToken(session.accessToken);
    setRefreshToken(session.refreshToken ?? null);
    setExpiresAt(session.expiresAt);
    setUser(session.user);
  }, []);

  const clearSession = useCallback(() => {
    persistSession(null);
    setUser(null);
    setAccessToken(null);
    setRefreshToken(null);
    setExpiresAt(null);
  }, []);

  useEffect(() => {
    registerAccessTokenProvider(() => accessToken);
    return () => registerAccessTokenProvider(null);
  }, [accessToken]);

  useEffect(() => {
    registerUnauthorizedHandler(() => {
      clearSession();
    });
    return () => registerUnauthorizedHandler(null);
  }, [clearSession]);

  useEffect(() => {
    registerTokenRefreshHandler(async () => {
      if (!refreshToken) {
        return null;
      }
      try {
        const refreshed = await authService.refresh(refreshToken);
        applySession(refreshed);
        return refreshed.accessToken;
      } catch {
        clearSession();
        return null;
      }
    });
    return () => registerTokenRefreshHandler(null);
  }, [refreshToken, applySession, clearSession]);

  useEffect(() => {
    if (!accessToken) {
      setIsLoading(false);
      return;
    }

    let active = true;
    authService
      .getMe(accessToken)
      .then((freshUser) => {
        if (!active) return;
        setUser(freshUser);
        if (expiresAt) {
          persistSession({
            accessToken,
            expiresAt,
            refreshToken,
            user: freshUser,
          });
        }
      })
      .catch(() => {
        if (!active) return;
        clearSession();
      })
      .finally(() => {
        if (active) {
          setIsLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [accessToken, expiresAt, refreshToken, clearSession]);

  const login = useCallback(
    async (credentials: LoginRequest): Promise<AuthUser> => {
      const response = await authService.login(credentials);
      applySession(response);
      return response.user;
    },
    [applySession],
  );

  const logout = useCallback(async (): Promise<void> => {
    const currentRefreshToken = refreshToken;
    await authService.logout(currentRefreshToken);
    clearSession();
  }, [refreshToken, clearSession]);

  const hasRole = useCallback(
    (roles?: Role[]): boolean => {
      if (!user) return false;
      if (!roles || roles.length === 0) return true;
      return roles.includes(user.role);
    },
    [user],
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      accessToken,
      refreshToken,
      expiresAt,
      isAuthenticated: Boolean(user && accessToken),
      isLoading,
      login,
      logout,
      hasRole,
    }),
    [user, accessToken, refreshToken, expiresAt, isLoading, login, logout, hasRole],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
