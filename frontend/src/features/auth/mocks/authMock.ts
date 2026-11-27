import { ApiError } from '../../../shared/api/httpClient';
import { toUtcIsoString } from '../../../shared/utils/date';
import type { AuthUser, DemoAccountOption, LoginRequest, LoginResponse } from '../types';

const MOCK_USERS: Record<string, AuthUser> = {
  'admin@axiz.demo': {
    id: '10000000-0000-0000-0000-000000000001',
    email: 'admin@axiz.demo',
    fullName: 'Coordinación General Axiz',
    role: 'Admin',
    operatorId: null,
  },
  'monitor@axiz.demo': {
    id: '10000000-0000-0000-0000-000000000002',
    email: 'monitor@axiz.demo',
    fullName: 'Mónica Treviño',
    role: 'Monitor',
    operatorId: null,
  },
  'operador1@axiz.demo': {
    id: '10000000-0000-0000-0000-000000000003',
    email: 'operador1@axiz.demo',
    fullName: 'Operador Unidad T-101',
    role: 'Operator',
    operatorId: '20000000-0000-0000-0000-000000000101',
  },
  'operador2@axiz.demo': {
    id: '10000000-0000-0000-0000-000000000004',
    email: 'operador2@axiz.demo',
    fullName: 'Operador Unidad T-102',
    role: 'Operator',
    operatorId: '20000000-0000-0000-0000-000000000102',
  },
  'operador3@axiz.demo': {
    id: '10000000-0000-0000-0000-000000000005',
    email: 'operador3@axiz.demo',
    fullName: 'Operador Unidad T-103',
    role: 'Operator',
    operatorId: '20000000-0000-0000-0000-000000000103',
  },
};

export const DEMO_ACCOUNTS: DemoAccountOption[] = [
  {
    email: 'admin@axiz.demo',
    fullName: 'Coordinación General Axiz',
    role: 'Admin',
    badgeText: 'Administrador',
    description: 'Acceso total: catálogos, geocercas, monitoreo, alertas y simulador.',
  },
  {
    email: 'monitor@axiz.demo',
    fullName: 'Mónica Treviño',
    role: 'Monitor',
    badgeText: 'Monitoreo C4',
    description: 'Supervisión de flota en vivo, rutas, geocercas, viajes y atención de alertas.',
  },
  {
    email: 'operador1@axiz.demo',
    fullName: 'Operador Unidad T-101',
    role: 'Operator',
    badgeText: 'Operador (T-101)',
    description: 'Consulta e inicio/cierre de sus viajes asignados y envío de telemetría.',
  },
  {
    email: 'operador2@axiz.demo',
    fullName: 'Operador Unidad T-102',
    role: 'Operator',
    badgeText: 'Operador (T-102)',
    description: 'Unidad Kenworth T680 asignada a corredor Puente III → Oradel.',
  },
  {
    email: 'operador3@axiz.demo',
    fullName: 'Operador Unidad T-103',
    role: 'Operator',
    badgeText: 'Operador (T-103)',
    description: 'Unidad Freightliner Cascadia asignada a corredor Oradel → MX-85.',
  },
];

function encodeMockToken(prefix: string, email: string): string {
  return `${prefix}-${btoa(encodeURIComponent(email))}`;
}

function decodeMockToken(prefix: string, token: string): string | null {
  if (!token.startsWith(`${prefix}-`)) return null;
  try {
    return decodeURIComponent(atob(token.slice(prefix.length + 1)));
  } catch {
    return null;
  }
}

function buildMockLoginResponse(user: AuthUser): LoginResponse {
  const expiresAt = toUtcIsoString(Date.now() + 8 * 60 * 60 * 1000);
  return {
    accessToken: encodeMockToken('mock-jwt', user.email),
    expiresAt,
    refreshToken: encodeMockToken('mock-refresh', user.email),
    user,
  };
}

export async function mockLogin(request: LoginRequest): Promise<LoginResponse> {
  await new Promise((resolve) => setTimeout(resolve, 220));
  const normalizedEmail = request.email.trim().toLowerCase();
  const user = MOCK_USERS[normalizedEmail];

  if (!user || request.password.trim().length < 4) {
    throw new ApiError('Credenciales inválidas. Verifica tu correo y contraseña.', 401, {
      title: 'Unauthorized',
      status: 401,
      detail: 'Credenciales inválidas. Verifica tu correo y contraseña.',
    });
  }

  return buildMockLoginResponse(user);
}

export async function mockRefresh(refreshToken: string): Promise<LoginResponse> {
  await new Promise((resolve) => setTimeout(resolve, 120));
  const email = decodeMockToken('mock-refresh', refreshToken);
  const user = email ? MOCK_USERS[email] : undefined;
  if (!user) {
    throw new ApiError('El refresh token expiró o fue revocado.', 401, {
      title: 'Unauthorized',
      status: 401,
      detail: 'El refresh token expiró o fue revocado.',
    });
  }
  return buildMockLoginResponse(user);
}

export async function mockGetMe(token: string | null): Promise<AuthUser> {
  await new Promise((resolve) => setTimeout(resolve, 100));
  if (!token) {
    throw new ApiError('Sesión no encontrada.', 401);
  }
  const email = decodeMockToken('mock-jwt', token);
  const user = email ? MOCK_USERS[email] : undefined;
  if (!user) {
    throw new ApiError('El token de sesión expiró o no es válido.', 401);
  }
  return user;
}
