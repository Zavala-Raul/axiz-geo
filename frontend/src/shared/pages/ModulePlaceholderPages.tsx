import { ModuleOverviewCard } from '../components/ModuleOverviewCard';

export function MonitorPlaceholderPage() {
  return (
    <ModuleOverviewCard
      title="Panel de Monitoreo en Vivo y Bandeja de Alertas"
      subtitle="Supervisión en tiempo real de unidades activas en Nuevo Laredo mediante SignalR (/hubs/fleet) con respaldo por polling cada 10 s."
      badgeIcon="📡"
      badgeText="Módulo de Monitoreo C4"
      featureBranch="feature/frontend-live-fleet-and-alerts"
      showLiveLegend
      highlights={[
        'Visualización de unidades con estado calculado (OnRoute, Stopped, Deviated, NoSignal, Idle) acompañado siempre de ícono y etiqueta.',
        'Antigüedad del último reporte GPS en formato relativo ("hace 45 s") y marca de tiempo en America/Matamoros.',
        'Bandeja de alertas con máquina de estados (New → InReview → Resolved / Dismissed) y motivo obligatorio (≥ 5 caracteres).',
      ]}
      endpoints={[
        {
          method: 'GET',
          path: '/api/fleet/live',
          description: 'Última posición, velocidad, rumbo y alertas abiertas de cada unidad activa.',
        },
        {
          method: 'GET',
          path: '/api/dashboard/summary',
          description: 'Resumen de viajes activos, unidades en ruta, desviadas, sin señal y alertas abiertas.',
        },
        {
          method: 'GET',
          path: '/api/alerts?status=New,InReview',
          description: 'Listado paginado de alertas operativas ordenadas por fecha más reciente.',
        },
        {
          method: 'POST',
          path: '/api/alerts/{id}/resolve',
          description: 'Transición a estado Resolved registrando motivo de resolución.',
        },
      ]}
    />
  );
}

export function GeofencesPlaceholderPage() {
  return (
    <ModuleOverviewCard
      title="Mapa de Rutas (Corredores) y Geocercas Vectoriales"
      subtitle="Cartografía vectorial con MapLibre GL JS sobre OpenFreeMap centrada en Nuevo Laredo [-99.5075, 27.4763] y dibujo interactivo con Terra Draw."
      badgeIcon="🗺️"
      badgeText="Cartografía y Geocercas"
      featureBranch="feature/frontend-map-and-geofences"
      highlights={[
        'Mapa base libre de llaves propietarias (https://tiles.openfreemap.org/styles/liberty).',
        'Dibujo de geocercas circulares y poligonales clasificadas como Permitidas (Allowed) o Restringidas (Restricted).',
        'Trazado de corredores para tractocamión con waypoints y margen configurable (25 m a 2000 m).',
      ]}
      endpoints={[
        {
          method: 'POST',
          path: '/api/routes/preview',
          description: 'Cálculo de trazo driving-hgv con OpenRouteService considerando altura y peso del tractocamión.',
        },
        {
          method: 'GET',
          path: '/api/routes',
          description: 'Consulta paginada de corredores autorizados.',
        },
        {
          method: 'GET',
          path: '/api/geofences',
          description: 'Listado completo de zonas circulares y poligonales en formato GeoJSON.',
        },
      ]}
    />
  );
}

export function TripsPlaceholderPage() {
  return (
    <ModuleOverviewCard
      title="Gestión y Despacho de Viajes"
      subtitle="Asignación de tractocamión, operador y corredor autorizado con control de ciclo de vida (Scheduled → InProgress → Completed / Cancelled)."
      badgeIcon="🧭"
      badgeText="Operación de Viajes"
      featureBranch="feature/frontend-catalogs-and-trips"
      highlights={[
        'Creación y consulta de viajes para Administradores y Monitores C4.',
        'Vista dedicada (/api/trips/mine) para que cada Operador inicie y finalice únicamente sus viajes asignados.',
        'Validación de conflictos HTTP 409 cuando una unidad u operador ya cuenta con un viaje en curso.',
      ]}
      endpoints={[
        {
          method: 'GET',
          path: '/api/trips',
          description: 'Listado paginado con filtros por estado, unidad, operador y rango de fechas.',
        },
        {
          method: 'GET',
          path: '/api/trips/mine',
          description: 'Viajes Scheduled o InProgress pertenecientes al operador autenticado.',
        },
        {
          method: 'POST',
          path: '/api/trips/{id}/start',
          description: 'Inicia formalmente el viaje y habilita la recepción de telemetría.',
        },
        {
          method: 'POST',
          path: '/api/trips/{id}/finish',
          description: 'Finaliza el viaje activo registrando marca de tiempo UTC.',
        },
      ]}
    />
  );
}

export function UnitsCatalogPlaceholderPage() {
  return (
    <ModuleOverviewCard
      title="Catálogo de Unidades (Tractocamiones)"
      subtitle="Administración de flota con número económico único (T-101, T-102, T-103), placas y dimensiones físicas para ruteo de carga pesada."
      badgeIcon="🚛"
      badgeText="Catálogo de Flota"
      featureBranch="feature/frontend-catalogs-and-trips"
      highlights={[
        'Escritura habilitada para rol Admin y consulta para Admin / Monitor.',
        'Dimensiones heightMeters y weightTons integradas con el cálculo de ruta driving-hgv.',
        'Borrado lógico (isActive = false) protegido contra eliminación si la unidad tiene viaje en curso.',
      ]}
      endpoints={[
        {
          method: 'GET',
          path: '/api/units?search=&page=1&pageSize=20',
          description: 'Consulta paginada de unidades activas.',
        },
        {
          method: 'POST',
          path: '/api/units',
          description: 'Alta de unidad (valida unicidad de economicNumber).',
        },
        {
          method: 'PUT',
          path: '/api/units/{id}',
          description: 'Actualización de datos técnicos y estado activo.',
        },
      ]}
    />
  );
}

export function OperatorsCatalogPlaceholderPage() {
  return (
    <ModuleOverviewCard
      title="Catálogo de Operadores"
      subtitle="Registro de operadores de transporte vinculados con su cuenta de acceso (rol Operator) y número de licencia federal."
      badgeIcon="🪪"
      badgeText="Catálogo de Personal"
      featureBranch="feature/frontend-catalogs-and-trips"
      highlights={[
        'Creación simultánea del perfil operativo y su usuario de autenticación con rol Operator.',
        'Búsqueda por nombre, licencia o correo electrónico.',
        'Control de estado activo/inactivo con protección de viajes en curso.',
      ]}
      endpoints={[
        {
          method: 'GET',
          path: '/api/operators?search=&page=1&pageSize=20',
          description: 'Listado paginado de operadores.',
        },
        {
          method: 'POST',
          path: '/api/operators',
          description: 'Registro de nuevo operador y credenciales asociadas.',
        },
        {
          method: 'PUT',
          path: '/api/operators/{id}',
          description: 'Actualización de nombre, licencia, teléfono y estatus.',
        },
      ]}
    />
  );
}

export function HistoryPlaceholderPage() {
  return (
    <ModuleOverviewCard
      title="Historial y Auditoría de Recorridos"
      subtitle="Consulta de trayectoria histórica por unidad con selectores rápidos de 1h, 6h, 12h y 24h superpuesta al corredor autorizado."
      badgeIcon="🕒"
      badgeText="Auditoría de Rutas"
      featureBranch="feature/frontend-history-and-simulator"
      highlights={[
        'Comparación visual entre el corredor autorizado de la ruta y la traza real reportada por la unidad.',
        'Inspección punto por punto con indicador insideCorridor, velocidad y precisión GPS.',
        'Línea de tiempo de eventos del viaje (TripStarted, Alert, Stop, TripFinished) formateada en America/Matamoros.',
      ]}
      endpoints={[
        {
          method: 'GET',
          path: '/api/units/{unitId}/track?from=&to=',
          description: 'Devuelve traza LineString, puntos detallados, viajes asociados y eventos del periodo.',
        },
        {
          method: 'GET',
          path: '/api/routes/{id}',
          description: 'Obtiene la geometría y margen del corredor autorizado para pintarlo debajo del recorrido.',
        },
      ]}
    />
  );
}

export function SimulatorPlaceholderPage() {
  return (
    <ModuleOverviewCard
      title="Simulador de Telemetría GPS y Escenarios Operativos"
      subtitle="Generación interactiva de lotes de posiciones con @turf/turf para demostrar recorrido normal, desvío fuera de corredor y pérdida/recuperación de señal."
      badgeIcon="🎛️"
      badgeText="Simulador de Demostración"
      featureBranch="feature/frontend-history-and-simulator"
      highlights={[
        'Idempotencia garantizada mediante UUID generado en el cliente (crypto.randomUUID()) por cada punto GPS.',
        'Cola local offline para simular pérdida de señal y reenvío posterior en lote conservando capturedAt original.',
        'Detección automática de posiciones históricas cuando receivedAt - capturedAt supera 2 minutos.',
      ]}
      endpoints={[
        {
          method: 'POST',
          path: '/api/telemetry/batch',
          description: 'Envío de lote de hasta 500 posiciones GPS con respuesta de aceptados, duplicados y alertas generadas.',
        },
        {
          method: 'GET',
          path: '/api/trips/mine',
          description: 'Selección del viaje activo sobre el cual emitir la telemetría simulada.',
        },
      ]}
    />
  );
}
