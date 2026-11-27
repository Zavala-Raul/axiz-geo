import { useEffect, useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router';
import { useAuth } from '../../features/auth/hooks/useAuth';
import { API_ORIGIN, USE_MOCKS } from '../api/httpClient';
import type { HealthResponse, Role } from '../types/api';
import { formatMatamorosTime, MATAMOROS_TIMEZONE, NUEVO_LAREDO_CENTER } from '../utils/date';

interface NavItem {
  to: string;
  label: string;
  icon: string;
  section: 'operacion' | 'catalogos' | 'herramientas';
  roles: Role[];
}

const NAV_ITEMS: NavItem[] = [
  {
    to: '/monitor',
    label: 'Monitoreo y Alertas',
    icon: '📡',
    section: 'operacion',
    roles: ['Admin', 'Monitor'],
  },
  {
    to: '/geocercas',
    label: 'Rutas y Geocercas',
    icon: '🗺️',
    section: 'operacion',
    roles: ['Admin', 'Monitor', 'Operator'],
  },
  {
    to: '/viajes',
    label: 'Viajes Activos',
    icon: '🧭',
    section: 'operacion',
    roles: ['Admin', 'Monitor', 'Operator'],
  },
  {
    to: '/historial',
    label: 'Historial (12h / 24h)',
    icon: '🕒',
    section: 'operacion',
    roles: ['Admin', 'Monitor'],
  },
  {
    to: '/catalogos/unidades',
    label: 'Unidades (Tractos)',
    icon: '🚛',
    section: 'catalogos',
    roles: ['Admin', 'Monitor'],
  },
  {
    to: '/catalogos/operadores',
    label: 'Operadores',
    icon: '🪪',
    section: 'catalogos',
    roles: ['Admin', 'Monitor'],
  },
  {
    to: '/simulador',
    label: 'Simulador GPS',
    icon: '🎛️',
    section: 'herramientas',
    roles: ['Admin', 'Operator'],
  },
];

function getRoleMeta(role: Role): { icon: string; label: string; className: string } {
  switch (role) {
    case 'Admin':
      return { icon: '🛡️', label: 'Administrador', className: 'role-chip--admin' };
    case 'Monitor':
      return { icon: '📡', label: 'Monitor C4', className: 'role-chip--monitor' };
    case 'Operator':
      return { icon: '🚛', label: 'Operador', className: 'role-chip--operator' };
  }
}

export function AppLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const [apiHealth, setApiHealth] = useState<{ ok: boolean; label: string }>(() =>
    USE_MOCKS
      ? { ok: true, label: 'Mocks Activos (Contrato v1)' }
      : { ok: false, label: 'Verificando API...' },
  );
  const [localClock, setLocalClock] = useState<string>(() =>
    formatMatamorosTime(new Date().toISOString()),
  );

  useEffect(() => {
    const interval = window.setInterval(() => {
      setLocalClock(formatMatamorosTime(new Date().toISOString()));
    }, 1000);
    return () => window.clearInterval(interval);
  }, []);

  useEffect(() => {
    if (USE_MOCKS) {
      setApiHealth({ ok: true, label: 'Mocks Activos (Contrato v1)' });
      return;
    }

    const controller = new AbortController();
    fetch(`${API_ORIGIN}/health`, { signal: controller.signal })
      .then(async (res) => {
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = (await res.json()) as HealthResponse;
        setApiHealth({
          ok: true,
          label: `API .NET 10 (${data.status})`,
        });
      })
      .catch((err: unknown) => {
        if (err instanceof DOMException && err.name === 'AbortError') return;
        setApiHealth({
          ok: false,
          label: 'API sin conexión (localhost:5080)',
        });
      });

    return () => controller.abort();
  }, []);

  if (!user) return null;

  const roleMeta = getRoleMeta(user.role);
  const visibleItems = NAV_ITEMS.filter((item) => item.roles.includes(user.role));
  const operationItems = visibleItems.filter((item) => item.section === 'operacion');
  const catalogItems = visibleItems.filter((item) => item.section === 'catalogos');
  const toolItems = visibleItems.filter((item) => item.section === 'herramientas');

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="layout-shell">
      <aside className="sidebar" aria-label="Navegación principal">
        <div className="sidebar__brand">
          <img
            src="/axiz_logo.png"
            alt="Logotipo oficial Axiz Geo"
            className="sidebar__logo-img"
          />
          <div>
            <div className="sidebar__title">AXIZ GEO</div>
            <div className="sidebar__subtitle">Sistema Telemático · C4</div>
          </div>
        </div>

        <div className="sidebar__coords" title="Centro geográfico SRID 4326 [lng, lat]">
          <span aria-hidden="true">🌐</span>
          <span>
            GeoJSON <code>[{NUEVO_LAREDO_CENTER.join(', ')}]</code>
          </span>
        </div>

        <nav className="sidebar__nav">
          {operationItems.length > 0 && (
            <div className="nav-group">
              <div className="nav-group__title">Operación Geoespacial</div>
              {operationItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    `nav-link ${isActive ? 'nav-link--active' : ''}`
                  }
                >
                  <span className="nav-link__icon" aria-hidden="true">
                    {item.icon}
                  </span>
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          )}

          {catalogItems.length > 0 && (
            <div className="nav-group">
              <div className="nav-group__title">Catálogos Operativos</div>
              {catalogItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    `nav-link ${isActive ? 'nav-link--active' : ''}`
                  }
                >
                  <span className="nav-link__icon" aria-hidden="true">
                    {item.icon}
                  </span>
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          )}

          {toolItems.length > 0 && (
            <div className="nav-group">
              <div className="nav-group__title">Telemetría y Pruebas</div>
              {toolItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  className={({ isActive }) =>
                    `nav-link ${isActive ? 'nav-link--active' : ''}`
                  }
                >
                  <span className="nav-link__icon" aria-hidden="true">
                    {item.icon}
                  </span>
                  <span>{item.label}</span>
                </NavLink>
              ))}
            </div>
          )}
        </nav>

        <div className="sidebar__footer">
          <div className="sidebar__user-card">
            <div className="sidebar__user-header">
              <span className={`role-chip ${roleMeta.className}`}>
                <span aria-hidden="true">{roleMeta.icon}</span>
                <span>{roleMeta.label}</span>
              </span>
            </div>
            <div className="sidebar__user-name">{user.fullName}</div>
            <div className="sidebar__user-email">{user.email}</div>
          </div>

          <button type="button" onClick={handleLogout} className="btn btn--logout">
            <span aria-hidden="true">🚪</span>
            <span>Cerrar sesión</span>
          </button>
        </div>
      </aside>

      <div className="layout-main">
        <header className="topbar">
          <div className="topbar__left">
            <span className="topbar__region-badge">
              <span aria-hidden="true">📍</span>
              <span>Corredor Fronterizo Nuevo Laredo, Tam.</span>
            </span>
          </div>

          <div className="topbar__right">
            <div
              className="status-badge status-badge--neutral"
              title={`Zona horaria operativa: ${MATAMOROS_TIMEZONE}`}
            >
              <span aria-hidden="true">🕒</span>
              <span>
                {localClock} ({MATAMOROS_TIMEZONE})
              </span>
            </div>

            <div
              className={`status-badge ${
                apiHealth.ok ? 'status-badge--success' : 'status-badge--warning'
              }`}
              role="status"
            >
              <span aria-hidden="true">{apiHealth.ok ? '🟢' : '🟠'}</span>
              <span>{apiHealth.label}</span>
            </div>
          </div>
        </header>

        <main className="page-container">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
