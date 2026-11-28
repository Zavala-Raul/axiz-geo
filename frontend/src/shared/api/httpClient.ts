import type { ProblemDetails } from '../types/api';

export const API_BASE_URL = (import.meta.env.VITE_API_URL ?? 'http://localhost:5080/api').replace(
  /\/+$/,
  '',
);

export const API_ORIGIN = API_BASE_URL.replace(/\/api$/, '');

export const USE_MOCKS = String(import.meta.env.VITE_USE_MOCKS).toLowerCase() === 'true';

export const SESSION_STORAGE_KEY = 'axiz_geo_session_v1';

export class ApiError extends Error {
  public readonly status: number;
  public readonly problem?: ProblemDetails;

  constructor(message: string, status: number, problem?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

type UnauthorizedHandler = () => void;
type TokenRefreshHandler = () => Promise<string | null>;

let unauthorizedHandler: UnauthorizedHandler | null = null;
let accessTokenProvider: (() => string | null) | null = null;
let tokenRefreshHandler: TokenRefreshHandler | null = null;
let inflightRefreshPromise: Promise<string | null> | null = null;

export function registerUnauthorizedHandler(handler: UnauthorizedHandler | null): void {
  unauthorizedHandler = handler;
}

export function registerAccessTokenProvider(provider: (() => string | null) | null): void {
  accessTokenProvider = provider;
}

/**
 * Registers the callback that attempts a single POST /api/auth/refresh when a 401 is intercepted.
 * Must return the new accessToken if rotation succeeds, or null if no refreshToken exists / refresh fails.
 */
export function registerTokenRefreshHandler(handler: TokenRefreshHandler | null): void {
  tokenRefreshHandler = handler;
}

async function tryRefreshAccessTokenOnce(): Promise<string | null> {
  if (!tokenRefreshHandler) {
    return null;
  }
  if (!inflightRefreshPromise) {
    inflightRefreshPromise = tokenRefreshHandler()
      .catch(() => null)
      .finally(() => {
        inflightRefreshPromise = null;
      });
  }
  return inflightRefreshPromise;
}

export interface RequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown;
  skipAuth?: boolean;
  /** Internal flag to prevent infinite 401 refresh loops */
  _isRetryAfterRefresh?: boolean;
}

async function request<T>(endpoint: string, options: RequestOptions = {}): Promise<T> {
  const {
    body,
    skipAuth = false,
    _isRetryAfterRefresh = false,
    headers: customHeaders,
    ...rest
  } = options;

  const headers = new Headers(customHeaders);
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json');
  }

  if (body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json');
  }

  if (!skipAuth && accessTokenProvider) {
    const token = accessTokenProvider();
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }
  }

  const url = endpoint.startsWith('http')
    ? endpoint
    : `${API_BASE_URL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

  const response = await fetch(url, {
    ...rest,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  // 401 Unauthorized Interceptor: attempt single dynamic token refresh if eligible
  if (response.status === 401 && !skipAuth && !_isRetryAfterRefresh) {
    const refreshedAccessToken = await tryRefreshAccessTokenOnce();
    if (refreshedAccessToken) {
      return request<T>(endpoint, {
        ...options,
        _isRetryAfterRefresh: true,
      });
    }

    if (unauthorizedHandler) {
      unauthorizedHandler();
    }
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get('content-type') ?? '';
  const isJson =
    contentType.includes('application/json') || contentType.includes('application/problem+json');

  if (!response.ok) {
    let problem: ProblemDetails | undefined;
    if (isJson) {
      try {
        problem = (await response.json()) as ProblemDetails;
      } catch {
        problem = undefined;
      }
    }

    if (response.status === 401 && !skipAuth && _isRetryAfterRefresh && unauthorizedHandler) {
      unauthorizedHandler();
    }

    const message =
      problem?.detail ??
      problem?.title ??
      (response.status === 401
        ? 'Credenciales inválidas o sesión expirada.'
        : response.status === 403
          ? 'No tienes permisos para realizar esta acción.'
          : response.status === 429
            ? 'Demasiados intentos. Espera un momento antes de volver a intentar.'
            : `Error HTTP ${response.status}`);

    throw new ApiError(message, response.status, problem);
  }

  if (!isJson) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const httpClient = {
  get: <T>(endpoint: string, options?: RequestOptions) =>
    request<T>(endpoint, { ...options, method: 'GET' }),
  post: <T>(endpoint: string, body?: unknown, options?: RequestOptions) =>
    request<T>(endpoint, { ...options, method: 'POST', body }),
  put: <T>(endpoint: string, body?: unknown, options?: RequestOptions) =>
    request<T>(endpoint, { ...options, method: 'PUT', body }),
  delete: <T>(endpoint: string, options?: RequestOptions) =>
    request<T>(endpoint, { ...options, method: 'DELETE' }),
};
