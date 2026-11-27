import type { FormEvent } from 'react';
import type { Role } from '../../../shared/types/api';
import type { DemoAccountOption } from '../types';

export interface LoginFormProps {
  email: string;
  password: string;
  isSubmitting: boolean;
  errorMessage: string | null;
  useMocks: boolean;
  demoAccounts: DemoAccountOption[];
  onEmailChange: (value: string) => void;
  onPasswordChange: (value: string) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onSelectDemoAccount: (account: DemoAccountOption) => void;
}

function getRoleIcon(role: Role): string {
  switch (role) {
    case 'Admin':
      return '🛡️';
    case 'Monitor':
      return '📡';
    case 'Operator':
      return '🚛';
  }
}

export function LoginForm({
  email,
  password,
  isSubmitting,
  errorMessage,
  useMocks,
  demoAccounts,
  onEmailChange,
  onPasswordChange,
  onSubmit,
  onSelectDemoAccount,
}: LoginFormProps) {
  return (
    <div className="login-page">
      <div className="login-container">
        <section className="login-brand-panel" aria-label="Identidad Axiz Geo">
          <div className="brand-badge">
            <span className="brand-orbit-icon" aria-hidden="true">
              ◎
            </span>
            <div>
              <h1 className="brand-title">AXIZ GEO</h1>
              <p className="brand-tagline">Tecnología que orienta</p>
            </div>
          </div>

          <div className="brand-summary">
            <h2>Control Geoespacial de Transporte de Carga</h2>
            <p>
              Plataforma de monitoreo de corredores autorizados, evaluación de geocercas y gestión
              de alertas operativas en <strong>Nuevo Laredo, Tamaulipas</strong>.
            </p>
          </div>

          <ul className="brand-highlights">
            <li>
              <span aria-hidden="true">📍</span>
              <span>
                <strong>Estándar GeoJSON SRID 4326:</strong> Corredores de carga pesada y geocercas
                vectoriales sobre OpenFreeMap.
              </span>
            </li>
            <li>
              <span aria-hidden="true">⏱️</span>
              <span>
                <strong>Horario Local Fronterizo:</strong> Trazabilidad operativa sincronizada en{' '}
                <code>America/Matamoros</code>.
              </span>
            </li>
            <li>
              <span aria-hidden="true">🔔</span>
              <span>
                <strong>Supervisión por Rol:</strong> Accesos diferenciados para Administración,
                Monitoreo C4 y Operadores de Tractocamión.
              </span>
            </li>
          </ul>
        </section>

        <section className="login-form-panel" aria-label="Formulario de inicio de sesión">
          <div className="login-header">
            <h2>Iniciar Sesión</h2>
            <p>Ingresa con tu cuenta institucional para acceder al sistema.</p>
            <div className="mode-pill" role="status">
              <span aria-hidden="true">{useMocks ? '🧪' : '🌐'}</span>
              <span>
                {useMocks
                  ? 'Modo Simulación Activo (VITE_USE_MOCKS=true)'
                  : 'Conectado a API Backend (.NET 10)'}
              </span>
            </div>
          </div>

          {errorMessage && (
            <div className="alert-banner alert-banner--error" role="alert">
              <span className="alert-banner__icon" aria-hidden="true">
                ⚠️
              </span>
              <div className="alert-banner__body">
                <strong>Error de autenticación:</strong> {errorMessage}
              </div>
            </div>
          )}

          <form onSubmit={onSubmit} className="auth-form" noValidate>
            <div className="form-field">
              <label htmlFor="login-email">Correo electrónico</label>
              <input
                id="login-email"
                type="email"
                autoComplete="username"
                required
                placeholder="monitor@axiz.demo"
                value={email}
                onChange={(e) => onEmailChange(e.target.value)}
                disabled={isSubmitting}
              />
            </div>

            <div className="form-field">
              <label htmlFor="login-password">Contraseña</label>
              <input
                id="login-password"
                type="password"
                autoComplete="current-password"
                required
                placeholder="••••••••"
                value={password}
                onChange={(e) => onPasswordChange(e.target.value)}
                disabled={isSubmitting}
              />
            </div>

            <button
              type="submit"
              className="btn btn--primary btn--full"
              disabled={isSubmitting || !email.trim() || !password.trim()}
            >
              <span aria-hidden="true">{isSubmitting ? '⏳' : '🔐'}</span>
              <span>{isSubmitting ? 'Validando credenciales...' : 'Ingresar a Axiz Geo'}</span>
            </button>
          </form>

          <div className="demo-accounts-box">
            <div className="demo-accounts-header">
              <span aria-hidden="true">⚡</span>
              <h3>Cuentas Semilla de Demostración</h3>
            </div>
            <p className="demo-accounts-subtitle">
              Selecciona un perfil para autocompletar credenciales según la matriz de roles del
              contrato de API:
            </p>
            <div className="demo-accounts-grid">
              {demoAccounts.map((account) => {
                const isSelected = email.trim().toLowerCase() === account.email.toLowerCase();
                return (
                  <button
                    key={account.email}
                    type="button"
                    onClick={() => onSelectDemoAccount(account)}
                    className={`demo-account-card ${isSelected ? 'demo-account-card--selected' : ''}`}
                    disabled={isSubmitting}
                  >
                    <div className="demo-account-card__top">
                      <span className={`role-chip role-chip--${account.role.toLowerCase()}`}>
                        <span aria-hidden="true">{getRoleIcon(account.role)}</span>
                        <span>{account.badgeText}</span>
                      </span>
                      <code className="demo-account-card__email">{account.email}</code>
                    </div>
                    <p className="demo-account-card__desc">{account.description}</p>
                  </button>
                );
              })}
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}
