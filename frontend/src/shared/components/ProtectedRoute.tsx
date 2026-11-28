import type { ReactNode } from 'react';
import { Link, Navigate, Outlet, useLocation } from 'react-router';
import { useAuth } from '../../features/auth/hooks/useAuth';
import { getDefaultRouteForRole } from '../../features/auth/utils/roleRoutes';
import type { Role } from '../types/api';

interface ProtectedRouteProps {
  allowedRoles?: Role[];
  children?: ReactNode;
}

const ROLE_LABELS: Record<Role, string> = {
  Admin: 'Administrador (Admin)',
  Monitor: 'Monitoreo C4 (Monitor)',
  Operator: 'Operador de Transporte (Operator)',
};

export function ProtectedRoute({ allowedRoles, children }: ProtectedRouteProps) {
  const { isAuthenticated, isLoading, user, hasRole } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="loading-screen" role="status" aria-live="polite">
        <div className="loading-card">
          <span className="loading-spinner" aria-hidden="true">
            ◎
          </span>
          <p>Verificando sesión de Axiz Geo...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  if (allowedRoles && !hasRole(allowedRoles)) {
    const homeRoute = getDefaultRouteForRole(user.role);
    return (
      <div className="forbidden-box" role="alert">
        <div className="forbidden-card">
          <div className="status-badge status-badge--danger">
            <span aria-hidden="true">🚫</span>
            <span>HTTP 403 · Acceso Restringido por Rol</span>
          </div>
          <h2>No tienes permisos para acceder a este módulo</h2>
          <p>
            Tu sesión actual tiene el rol <strong>{ROLE_LABELS[user.role]}</strong>. De acuerdo con
            la matriz de permisos del contrato de API, esta vista solo está habilitada para:{' '}
            <strong>{allowedRoles.map((r) => ROLE_LABELS[r]).join(', ')}</strong>.
          </p>
          <div className="forbidden-actions">
            <Link to={homeRoute} className="btn btn--primary">
              <span aria-hidden="true">↩️</span>
              <span>Ir a mi módulo principal</span>
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return children ? <>{children}</> : <Outlet />;
}
