import type { Role } from '../../../shared/types/api';

export function getDefaultRouteForRole(role: Role): string {
  switch (role) {
    case 'Admin':
    case 'Monitor':
      return '/monitor';
    case 'Operator':
      return '/viajes';
  }
}
