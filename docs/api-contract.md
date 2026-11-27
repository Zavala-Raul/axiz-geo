# Axiz Geo — Contrato de la API (v1)

Acuerdo entre **Frontend (Gael)** y **Backend (Raúl)**. Cualquier cambio a este documento se hace por PR y lo aprueban ambos.
Cuando la API esté arriba, la fuente de verdad será el OpenAPI (`/openapi/v1.json`, UI en `/scalar`) y los tipos de TypeScript se generan desde ahí.

---

## 1. Convenciones generales

| Tema | Regla |
|---|---|
| Base URL | Local: `http://localhost:5080/api` · Producción: `https://<api>.onrender.com/api` · En el front: `import.meta.env.VITE_API_URL` |
| Formato | JSON, `Content-Type: application/json` |
| Nombres JSON | `camelCase` |
| IDs | `uuid` (string) |
| Fechas | ISO 8601 **en UTC**, con `Z`: `"2026-12-01T18:30:00Z"`. El front convierte a `America/Matamoros` solo para mostrar |
| Coordenadas | **GeoJSON**, orden **`[longitud, latitud]`**. SRID 4326. Ej. Nuevo Laredo: `[-99.5075, 27.4763]` |
| Enums | Como **string** (`"InProgress"`), nunca números |
| Distancias / velocidades | Metros (`...Meters`), km/h (`speedKmh`), grados (`heading`, 0 = norte) |
| Idioma | Código, rutas y campos en inglés. Textos visibles al usuario (UI y `message` de alertas) en español |
| Autenticación | Header `Authorization: Bearer <accessToken>` en todo excepto `/auth/login` y `/health` |
| Paginación | Query `?page=1&pageSize=20` (máx. 100). Respuesta envuelta en `PagedResult<T>` |
| Ordenamiento / filtros | Query string, ej. `?status=New&sort=-createdAt` (`-` = descendente) |
| Borrado | Lógico para catálogos (`isActive = false`); los listados por defecto solo devuelven activos (`?includeInactive=true` para todos) |

### 1.1 Tipos comunes

```ts
type Uuid = string;
type IsoDate = string; // UTC

type GeoJsonPoint      = { type: "Point";      coordinates: [number, number] };
type GeoJsonLineString = { type: "LineString"; coordinates: [number, number][] };
type GeoJsonPolygon    = { type: "Polygon";    coordinates: [number, number][][] }; // primer anillo = exterior, cerrado (primer punto = último)

type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};
```

### 1.2 Enums

| Enum | Valores |
|---|---|
| `Role` | `Admin`, `Monitor`, `Operator` |
| `TripStatus` | `Scheduled`, `InProgress`, `Completed`, `Cancelled` |
| `GeofenceType` | `Circle`, `Polygon` |
| `GeofenceKind` | `Allowed` (zona segura/permitida), `Restricted` (prohibida) |
| `AlertType` | `Deviation` (fuera de corredor), `NoSignal`, `UnplannedStop`, `RestrictedZone` |
| `AlertStatus` | `New`, `InReview`, `Resolved`, `Dismissed` |
| `AlertSeverity` | `Low`, `Medium`, `High` |
| `LiveStatus` (estado calculado de la unidad en el mapa) | `OnRoute`, `Stopped`, `Deviated`, `NoSignal`, `Idle` (sin viaje activo) |

> **Rutas vs. geocercas:** una **Ruta** es el corredor autorizado (línea + margen en metros) y se asigna a un viaje. Una **Geocerca** es una zona (círculo o polígono) permitida o restringida, independiente del viaje.

---

## 2. Errores

Todas las respuestas de error usan **ProblemDetails** (RFC 9457):

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "Uno o más campos no son válidos.",
  "errors": {
    "economicNumber": ["El número económico es obligatorio."],
    "geometry": ["El polígono debe estar cerrado."]
  },
  "traceId": "00-abc..."
}
```

| Código | Cuándo |
|---|---|
| `200 OK` | Lectura o actualización con cuerpo |
| `201 Created` | Creación (incluye header `Location` y el recurso en el cuerpo) |
| `204 No Content` | Borrado o acción sin cuerpo |
| `400 Bad Request` | Validación (`errors` por campo) |
| `401 Unauthorized` | Sin token o token expirado → el front redirige a `/login` |
| `403 Forbidden` | Rol sin permiso, u operador intentando acceder a un viaje que no es suyo |
| `404 Not Found` | Recurso no existe |
| `409 Conflict` | Regla de negocio: transición de estado inválida, unidad ya tiene viaje activo, duplicado |
| `429 Too Many Requests` | Rate limit (login) |
| `500` | Error no controlado (sin detalles internos en producción) |

---

## 3. Autenticación — `/api/auth`

### `POST /api/auth/login` · público
```json
// Request
{ "email": "monitor@axiz.demo", "password": "Demo123!" }

// 200 Response
{
  "accessToken": "eyJhbGciOi...",
  "expiresAt": "2026-12-01T20:30:00Z",
  "user": {
    "id": "8b1f...", "email": "monitor@axiz.demo", "fullName": "Mónica Treviño",
    "role": "Monitor", "operatorId": null
  }
}
```
`401` si las credenciales son inválidas (mensaje genérico, sin decir cuál campo falló). `429` tras varios intentos.

### `GET /api/auth/me` · autenticado
Devuelve el mismo objeto `user`. El front lo usa al recargar la página para validar el token.

### `POST /api/auth/refresh` · *(NICE)* cookie httpOnly `axiz_refresh`
Respuesta igual a login. Si no se implementa, el token dura ~8 h.

### `POST /api/auth/logout` · autenticado → `204`

**Claims del JWT:** `sub` (userId), `email`, `name`, `role`, `operatorId` (si aplica).

---

## 4. Matriz de permisos

| Recurso | Admin | Monitor | Operator |
|---|:-:|:-:|:-:|
| Usuarios | CRUD | — | — |
| Unidades, Operadores | CRUD | Leer | — |
| Rutas, Geocercas | CRUD | CRUD | Leer (solo la ruta de su viaje) |
| Viajes | CRUD | CRUD | Leer los suyos, iniciar/finalizar los suyos |
| Telemetría (envío) | ✔ (simulador) | — | ✔ (solo su viaje activo) |
| Flota en vivo, historial | ✔ | ✔ | — |
| Alertas | ✔ | ✔ | — |
| Hub SignalR | ✔ | ✔ | — |

---

## 5. Catálogos

### 5.1 Unidades — `/api/units` (Admin escribe; Admin/Monitor leen)

```ts
type UnitDto = {
  id: Uuid;
  economicNumber: string;   // "T-101" (único)
  plates: string;           // "XY-1234-A"
  brand: string;            // "Kenworth"
  model: string;            // "T680"
  year: number;
  heightMeters?: number;    // para ruteo de camión
  weightTons?: number;
  isActive: boolean;
};
type CreateUnitRequest = Omit<UnitDto, "id" | "isActive">;
type UpdateUnitRequest = Omit<UnitDto, "id">;
```

| Método | Ruta | Respuesta |
|---|---|---|
| GET | `/api/units?search=&page=&pageSize=` | `PagedResult<UnitDto>` |
| GET | `/api/units/{id}` | `UnitDto` |
| POST | `/api/units` | `201 UnitDto` · `409` si `economicNumber` duplicado |
| PUT | `/api/units/{id}` | `UnitDto` |
| DELETE | `/api/units/{id}` | `204` (lógico) · `409` si tiene viaje en curso |

### 5.2 Operadores — `/api/operators`

```ts
type OperatorDto = {
  id: Uuid;
  fullName: string;
  licenseNumber: string;
  phone: string;
  email: string;            // usuario con rol Operator asociado
  userId: Uuid;
  isActive: boolean;
};
type CreateOperatorRequest = {
  fullName: string; licenseNumber: string; phone: string;
  email: string; password: string;  // crea el usuario Operator en Identity
};
type UpdateOperatorRequest = { fullName: string; licenseNumber: string; phone: string; isActive: boolean };
```
Mismos endpoints que unidades (`GET` paginado, `GET {id}`, `POST`, `PUT`, `DELETE`).

### 5.3 Usuarios — `/api/users` *(NICE, Admin)*
`GET` paginado, `POST { email, fullName, password, role }`, `PUT {id} { fullName, role, isActive }`.

---

## 6. Rutas (corredores) — `/api/routes`

```ts
type RouteDto = {
  id: Uuid;
  name: string;                       // "Puente III → Parque Industrial Oradel"
  waypoints: GeoJsonPoint[];          // puntos que el usuario clickeó (origen, intermedios, destino)
  path: GeoJsonLineString;            // trazo calculado para camión
  corridorMeters: number;             // margen permitido a cada lado, ej. 150
  distanceMeters: number;
  durationSeconds: number;
  isActive: boolean;
  createdAt: IsoDate;
  createdBy: string;                  // nombre
};
```

| Método | Ruta | Body / Respuesta |
|---|---|---|
| POST | `/api/routes/preview` | Body: `{ waypoints: GeoJsonPoint[], unitId?: Uuid }` → `{ path, distanceMeters, durationSeconds }`. Llama a openrouteservice `driving-hgv` (usa alto/peso de la unidad si se envía). `502` si el proveedor falla |
| GET | `/api/routes` | `PagedResult<RouteDto>` |
| GET | `/api/routes/{id}` | `RouteDto` |
| POST | `/api/routes` | Body: `{ name, waypoints, path, corridorMeters }` → `201 RouteDto` |
| PUT | `/api/routes/{id}` | Igual que POST → `RouteDto` (se registra quién cambió la ruta) |
| DELETE | `/api/routes/{id}` | `204` · `409` si un viaje en curso la usa |

Validaciones: `corridorMeters` entre 25 y 2000; `waypoints` 2–50; `path` con ≥ 2 puntos.

---

## 7. Geocercas (zonas) — `/api/geofences`

```ts
type GeofenceDto = {
  id: Uuid;
  name: string;                       // "Patio Aduana Colombia"
  type: "Circle" | "Polygon";
  kind: "Allowed" | "Restricted";
  geometry: GeoJsonPoint | GeoJsonPolygon;  // Point (centro) si Circle, Polygon si Polygon
  radiusMeters?: number;              // obligatorio si Circle
  color?: string;                     // "#213D71"
  isActive: boolean;
  createdAt: IsoDate;
};
type UpsertGeofenceRequest = Omit<GeofenceDto, "id" | "createdAt">;
```

| Método | Ruta | Respuesta |
|---|---|---|
| GET | `/api/geofences?kind=&includeInactive=` | `GeofenceDto[]` (sin paginar; son pocas y se pintan todas) |
| GET | `/api/geofences/{id}` | `GeofenceDto` |
| POST | `/api/geofences` | `201 GeofenceDto` |
| PUT | `/api/geofences/{id}` | `GeofenceDto` |
| DELETE | `/api/geofences/{id}` | `204` |

Validaciones: polígono válido y cerrado, 3–200 vértices; `radiusMeters` 10–20000.

**Ejemplo (creada con clics en el mapa):**
```json
{
  "name": "Zona restringida Centro",
  "type": "Polygon",
  "kind": "Restricted",
  "geometry": {
    "type": "Polygon",
    "coordinates": [[[-99.5100,27.4990],[-99.5020,27.4990],[-99.5020,27.4930],[-99.5100,27.4930],[-99.5100,27.4990]]]
  },
  "isActive": true
}
```

---

## 8. Viajes — `/api/trips`

```ts
type TripDto = {
  id: Uuid;
  unit: { id: Uuid; economicNumber: string };
  operator: { id: Uuid; fullName: string };
  route: { id: Uuid; name: string };
  status: TripStatus;
  scheduledStart: IsoDate;
  startedAt?: IsoDate;
  endedAt?: IsoDate;
  lastPositionAt?: IsoDate;
  notes?: string;
};
type CreateTripRequest = { unitId: Uuid; operatorId: Uuid; routeId: Uuid; scheduledStart: IsoDate; notes?: string };
```

| Método | Ruta | Rol | Respuesta |
|---|---|---|---|
| GET | `/api/trips?status=&unitId=&operatorId=&from=&to=&page=` | Admin/Monitor | `PagedResult<TripDto>` |
| GET | `/api/trips/mine` | Operator | `TripDto[]` (sus viajes `Scheduled`/`InProgress`) |
| GET | `/api/trips/{id}` | todos (Operator solo el suyo) | `TripDto` + `routeDetail: RouteDto` |
| POST | `/api/trips` | Admin/Monitor | `201 TripDto` |
| POST | `/api/trips/{id}/start` | Operator dueño / Admin | `TripDto` · `409` si no está `Scheduled` o la unidad/operador ya tiene uno `InProgress` |
| POST | `/api/trips/{id}/finish` | Operator dueño / Admin | `TripDto` · `409` si no está `InProgress` |
| POST | `/api/trips/{id}/cancel` | Admin/Monitor | `TripDto` |

---

## 9. Telemetría — `/api/telemetry`

### `POST /api/telemetry/batch` · Operator (su viaje activo) / Admin
Siempre se envía un **lote** (1 o más posiciones). El simulador acumula cuando está "sin señal" y luego reenvía.

```ts
type PositionInput = {
  id: Uuid;               // generado por el CLIENTE (crypto.randomUUID()) → idempotencia
  tripId: Uuid;
  location: GeoJsonPoint;
  accuracyMeters: number;
  speedKmh?: number;
  heading?: number;
  capturedAt: IsoDate;    // momento real de captura (no el de envío)
  source: "Simulator" | "BrowserGps";
};
type TelemetryBatchRequest = { positions: PositionInput[] }; // máx. 500

type TelemetryBatchResponse = {
  accepted: number;
  duplicates: number;     // ids ya existentes (se ignoran sin error)
  rejected: { id: Uuid; reason: string }[];
  alertsRaised: number;
};
```
- `200` aunque haya duplicados. `403` si el `tripId` no pertenece al operador. `409` si el viaje no está `InProgress`.
- El servidor asigna `receivedAt`. Si `receivedAt - capturedAt > 2 min`, la posición se marca `isLate = true` y las alertas que genere se marcan como históricas.

---

## 10. Monitoreo — `/api/fleet`, `/api/units/{id}/track`

### `GET /api/fleet/live` · Admin/Monitor
Última posición de cada unidad activa (con o sin viaje).

```ts
type LiveUnitDto = {
  unitId: Uuid;
  economicNumber: string;
  operatorName?: string;
  tripId?: Uuid;
  routeName?: string;
  liveStatus: LiveStatus;
  location?: GeoJsonPoint;
  speedKmh?: number;
  heading?: number;
  accuracyMeters?: number;
  capturedAt?: IsoDate;     // el front muestra "hace X" con esto
  openAlerts: number;
};
// Response: LiveUnitDto[]
```

### `GET /api/units/{unitId}/track?from=&to=` · Admin/Monitor
Rango máximo: 7 días. Por defecto, últimas 12 h.

```ts
type TrackDto = {
  unitId: Uuid;
  from: IsoDate;
  to: IsoDate;
  path: GeoJsonLineString;               // simplificado si hay muchos puntos
  points: {
    location: GeoJsonPoint;
    capturedAt: IsoDate;
    speedKmh?: number;
    accuracyMeters: number;
    insideCorridor?: boolean;
  }[];
  trips: { id: Uuid; routeId: Uuid; routeName: string; startedAt?: IsoDate; endedAt?: IsoDate }[];
  events: {
    type: "TripStarted" | "TripFinished" | "Alert" | "Stop";
    at: IsoDate;
    location?: GeoJsonPoint;
    alertId?: Uuid;
    description: string;
  }[];
};
```
El front pinta el corredor de las rutas en `trips` (con `GET /api/routes/{id}`) debajo del `path`.

### `GET /api/dashboard/summary` · Admin/Monitor
```json
{ "activeTrips": 3, "unitsOnRoute": 2, "unitsDeviated": 1, "unitsNoSignal": 0, "openAlerts": 2 }
```

---

## 11. Alertas — `/api/alerts`

```ts
type AlertDto = {
  id: Uuid;
  type: AlertType;
  severity: AlertSeverity;
  status: AlertStatus;
  message: string;                 // en español, ej. "Unidad T-101 fuera del corredor autorizado"
  unit: { id: Uuid; economicNumber: string };
  trip: { id: Uuid; routeName: string };
  operatorName: string;
  location?: GeoJsonPoint;
  geofenceId?: Uuid;
  isHistorical: boolean;           // generada por posiciones sincronizadas tarde
  createdAt: IsoDate;
  attendedBy?: string;
  attendedAt?: IsoDate;
  resolution?: string;
};
```

| Método | Ruta | Respuesta |
|---|---|---|
| GET | `/api/alerts?status=New,InReview&type=&unitId=&from=&to=&page=` | `PagedResult<AlertDto>` (orden: más recientes primero) |
| GET | `/api/alerts/{id}` | `AlertDto` |
| POST | `/api/alerts/{id}/review` | → `InReview` · `AlertDto` |
| POST | `/api/alerts/{id}/resolve` | Body `{ resolution: string }` (obligatorio, ≥ 5 caracteres) → `Resolved` |
| POST | `/api/alerts/{id}/dismiss` | Body `{ resolution: string }` → `Dismissed` (falsa alarma) |

Transiciones inválidas → `409 Conflict`. Máquina de estados:
`New → InReview → Resolved | Dismissed` y `New → Dismissed`.

---

## 12. Tiempo real — SignalR `/hubs/fleet`

- Conexión: `new HubConnectionBuilder().withUrl(`${API_ORIGIN}/hubs/fleet`, { accessTokenFactory: () => token }).withAutomaticReconnect()`
- Solo Admin/Monitor. Al conectar, el servidor los agrega al grupo `monitors`.

| Evento (servidor → cliente) | Payload | Uso en el front |
|---|---|---|
| `UnitUpdated` | `LiveUnitDto` | Mover el marcador / cambiar color |
| `AlertRaised` | `AlertDto` | Toast + badge + agregar a la bandeja |
| `AlertUpdated` | `AlertDto` | Actualizar estado en la bandeja |

**Plan B** si SignalR falla: `GET /api/fleet/live` y `GET /api/alerts?status=New,InReview` cada 10 s.

---

## 13. Salud — `GET /health` · público
`200 { "status": "Healthy", "database": "Healthy" }`. Sirve para despertar la API en Render y para demostrar la conexión a la BD.

---

## 14. Usuarios de demo (seed)

| Email | Rol | Notas |
|---|---|---|
| `admin@axiz.demo` | Admin | |
| `monitor@axiz.demo` | Monitor | |
| `operador1@axiz.demo` | Operator | Unidad T-101 |
| `operador2@axiz.demo` | Operator | Unidad T-102 |
| `operador3@axiz.demo` | Operator | Unidad T-103 |

Contraseña de demo: se define en la variable de entorno `Seed__DemoPassword` (no se versiona) y se comparte en el README para la evaluación.

---

## 15. Mock para el frontend mientras el backend avanza

Gael puede usar **MSW** (`msw`) con handlers que devuelvan exactamente los tipos de este documento. Carpeta sugerida: `frontend/src/mocks/`. Activarlo con `VITE_USE_MOCKS=true`. Al conectar con la API real solo se cambia la variable.
