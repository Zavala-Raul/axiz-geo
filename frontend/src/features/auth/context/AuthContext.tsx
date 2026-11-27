import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import {
  registerAccessTokenProvider,
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
    if (new Date(parsed.expiresAt).getTime() <= Date.now()) {
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
  const [expiresAt, setExpiresAt] = useState<string | null>(initialSession?.expiresAt ?? null);
  const [isLoading, setIsLoading] = useState<boolean>(Boolean(initialSession?.accessToken));

  const clearSession = useCallback(() => {
    persistSession(null);
    setUser(null);
    setAccessToken(null);
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
          persistSession({ accessToken, expiresAt, user: freshUser });
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
  }, [accessToken, expiresAt, clearSession]);

  const login = useCallback(async (credentials: LoginRequest): Promise<AuthUser> => {
    const response = await authService.login(credentials);
    persistSession(response);
    setAccessToken(response.accessToken);
    setExpiresAt(response.expiresAt);
    setUser(response.user);
    return response.user;
  }, []);

  const logout = useCallback(async (): Promise<void> => {
    await authService.logout();
    clearSession();
  }, [clearSession]);

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
      expiresAt,
      isAuthenticated: Boolean(user && accessToken),
      isLoading,
      login,
      logout,
      hasRole,
    }),
    [user, accessToken, expiresAt, isLoading, login, logout, hasRole],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
