import { useAuth } from '../../features/auth/hooks/useAuth';
import type { LiveStatus, Role } from '../types/api';
import { formatMatamorosDateTime, NUEVO_LAREDO_CENTER } from '../utils/date';

export interface ModuleSectionProps {
  title: string;
  subtitle: string;
  badgeIcon: string;
  badgeText: string;
  featureBranch: string;
  endpoints: { method: string; path: string; description: string }[];
  highlights: string[];
  showLiveLegend?: boolean;
}

const LIVE_STATUS_LEGEND: {
  status: LiveStatus;
  icon: string;
  label: string;
  className: string;
}[] = [
  {
    status: 'OnRoute',
    icon: '🟢',
    label: 'En Ruta (OnRoute)',
    className: 'status-badge--success',
  },
  {
    status: 'Stopped',
    icon: '⏸️',
    label: 'Detenida (Stopped)',
    className: 'status-badge--warning',
  },
  {
    status: 'Deviated',
    icon: '🚨',
    label: 'Fuera de Corredor (Deviated)',
    className: 'status-badge--danger',
  },
  {
    status: 'NoSignal',
    icon: '📵',
    label: 'Sin Señal GPS (NoSignal)',
    className: 'status-badge--neutral',
  },
  {
    status: 'Idle',
    icon: '🅿️',
    label: 'Sin Viaje Activo (Idle)',
    className: 'status-badge--sand',
  },
];

function getPermissionNote(role: Role): { icon: string; text: string } {
  switch (role) {
    case 'Admin':
      return {
        icon: '🛡️',
        text: 'Permisos de Administrador: Lectura y escritura completa (CRUD), gestión de alertas y acceso al simulador.',
      };
    case 'Monitor':
      return {
        icon: '📡',
        text: 'Permisos de Monitor C4: Lectura de unidades/operadores, CRUD de rutas/geocercas/viajes, monitoreo en vivo y atención de alertas.',
      };
    case 'Operator':
      return {
        icon: '🚛',
        text: 'Permisos de Operador: Consulta de ruta asignada, inicio/finalización de sus viajes y envío de lotes de telemetría.',
      };
  }
}

export function ModuleOverviewCard({
  title,
  subtitle,
  badgeIcon,
  badgeText,
  featureBranch,
  endpoints,
  highlights,
  showLiveLegend = false,
}: ModuleSectionProps) {
  const { user } = useAuth();
  const permissionNote = user ? getPermissionNote(user.role) : null;

  return (
    <div className="module-view">
      <section className="module-hero">
        <div className="module-hero__header">
          <div className="status-badge status-badge--navy">
            <span aria-hidden="true">{badgeIcon}</span>
            <span>{badgeText}</span>
          </div>
          <code className="branch-tag">{featureBranch}</code>
        </div>

        <h1 className="module-hero__title">{title}</h1>
        <p className="module-hero__subtitle">{subtitle}</p>

        {permissionNote && (
          <div className="permission-banner" role="note">
            <span aria-hidden="true">{permissionNote.icon}</span>
            <span>{permissionNote.text}</span>
          </div>
        )}
      </section>

      {showLiveLegend && (
        <section className="module-card">
          <h2 className="module-card__title">
            Estándar de Accesibilidad Visual — Estados con Ícono + Etiqueta
          </h2>
          <p className="module-card__desc">
            Ningún estado operativo ni alerta depende únicamente del color:
          </p>
          <div className="status-legend-grid">
            {LIVE_STATUS_LEGEND.map((item) => (
              <div key={item.status} className={`status-badge ${item.className}`}>
                <span aria-hidden="true">{item.icon}</span>
                <span>{item.label}</span>
              </div>
            ))}
          </div>
        </section>
      )}

      <div className="module-grid">
        <section className="module-card">
          <h2 className="module-card__title">Capacidades del Módulo</h2>
          <ul className="module-list">
            {highlights.map((item) => (
              <li key={item}>
                <span aria-hidden="true">✔</span>
                <span>{item}</span>
              </li>
            ))}
          </ul>
          <div className="module-meta-footer">
            <span>
              <strong>Coordenadas base (SRID 4326):</strong>{' '}
              <code>[{NUEVO_LAREDO_CENTER.join(', ')}]</code>
            </span>
            <span>
              <strong>Última sincronización UI:</strong>{' '}
              {formatMatamorosDateTime(new Date().toISOString())}
            </span>
          </div>
        </section>

        <section className="module-card">
          <h2 className="module-card__title">Contrato de API Vinculado (v1)</h2>
          <div className="endpoints-list">
            {endpoints.map((ep) => (
              <div key={`${ep.method}-${ep.path}`} className="endpoint-item">
                <div className="endpoint-item__top">
                  <span className={`http-method http-method--${ep.method.toLowerCase()}`}>
                    {ep.method}
                  </span>
                  <code className="endpoint-path">{ep.path}</code>
                </div>
                <p className="endpoint-desc">{ep.description}</p>
              </div>
            ))}
          </div>
        </section>
      </div>
    </div>
  );
}
