import { BrowserRouter, Navigate, Route, Routes } from 'react-router';
import { AuthProvider } from './features/auth/context/AuthContext';
import { useAuth } from './features/auth/hooks/useAuth';
import { LoginPage } from './features/auth/pages/LoginPage';
import { getDefaultRouteForRole } from './features/auth/utils/roleRoutes';
import { AppLayout } from './shared/components/AppLayout';
import { ProtectedRoute } from './shared/components/ProtectedRoute';
import {
  GeofencesPlaceholderPage,
  HistoryPlaceholderPage,
  MonitorPlaceholderPage,
  OperatorsCatalogPlaceholderPage,
  SimulatorPlaceholderPage,
  TripsPlaceholderPage,
  UnitsCatalogPlaceholderPage,
} from './shared/pages/ModulePlaceholderPages';

function RootRedirect() {
  const { isAuthenticated, user } = useAuth();
  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace />;
  }
  return <Navigate to={getDefaultRouteForRole(user.role)} replace />;
}

export function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route index element={<RootRedirect />} />

              <Route
                path="/monitor"
                element={
                  <ProtectedRoute allowedRoles={['Admin', 'Monitor']}>
                    <MonitorPlaceholderPage />
                  </ProtectedRoute>
                }
              />

              <Route
                path="/geocercas"
                element={
                  <ProtectedRoute allowedRoles={['Admin', 'Monitor', 'Operator']}>
                    <GeofencesPlaceholderPage />
                  </ProtectedRoute>
                }
              />

              <Route
                path="/viajes"
                element={
                  <ProtectedRoute allowedRoles={['Admin', 'Monitor', 'Operator']}>
                    <TripsPlaceholderPage />
                  </ProtectedRoute>
                }
              />

              <Route
                path="/catalogos/unidades"
                element={
                  <ProtectedRoute allowedRoles={['Admin', 'Monitor']}>
                    <UnitsCatalogPlaceholderPage />
                  </ProtectedRoute>
                }
              />

              <Route
                path="/catalogos/operadores"
                element={
                  <ProtectedRoute allowedRoles={['Admin', 'Monitor']}>
                    <OperatorsCatalogPlaceholderPage />
                  </ProtectedRoute>
                }
              />

              <Route
                path="/historial"
                element={
                  <ProtectedRoute allowedRoles={['Admin', 'Monitor']}>
                    <HistoryPlaceholderPage />
                  </ProtectedRoute>
                }
              />

              <Route
                path="/simulador"
                element={
                  <ProtectedRoute allowedRoles={['Admin', 'Operator']}>
                    <SimulatorPlaceholderPage />
                  </ProtectedRoute>
                }
              />

              <Route path="*" element={<RootRedirect />} />
            </Route>
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
