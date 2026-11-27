# Bitácora de Construcción — Axiz Geo

Este documento explica **paso a paso qué se va construyendo en el proyecto, por qué se tomó cada decisión técnica y cómo encaja con los requisitos de evaluación** (React 19 + TypeScript, ASP.NET Core Web API con .NET 10, PostgreSQL, GitHub Actions, ramas `main`/`develop`/`feature/*`, diagramas C4 y patrones de diseño).

---

## Fase 1 — Inicialización del Repositorio, Entorno y CI/CD (27 de noviembre de 2026)

### 1.1 Control de versiones de SDK (`global.json` y `.nvmrc`)
- **`global.json`**: En la máquina de desarrollo coexisten el SDK estable de `.NET 10` (`10.0.403`) y una versión preliminar de `.NET 11 RC`. Sin `global.json`, el comando `dotnet` elegiría por defecto `.NET 11`, rompiendo la compatibilidad con el requisito académico de **.NET 10** y con el contenedor de producción. Se fijó `"version": "10.0.100"` con `"rollForward": "latestFeature"` y `"allowPrerelease": false`.
- **`.nvmrc`**: Define Node.js LTS (`22`) como referencia para alinear el entorno con el runner de GitHub Actions.

### 1.2 Seguridad de credenciales (`.gitignore` y `user-secrets`)
El documento de requisitos exige mantener **credenciales y cadenas de conexión fuera del repositorio**:
- `.gitignore` excluye `.env`, `.env.*` (permitiendo únicamente `.env.example`), `appsettings.Local.json`, `bin/`, `obj/`, `node_modules/` y `dist/`.
- Se inicializó `dotnet user-secrets` en `AxizGeo.Api.csproj` para que la cadena de conexión a PostgreSQL/Neon, la llave JWT y la API key de OpenRouteService vivan fuera del árbol de Git durante el desarrollo local.

### 1.3 Base de datos local con soporte geoespacial (`docker-compose.yml`)
Se configuró un contenedor basado en `postgis/postgis:17-3.5` en el puerto `5432` con healthcheck integrado. Permite desarrollar localmente sobre **PostgreSQL 17 + PostGIS**, idéntico al entorno productivo en **Neon**.

### 1.4 Integración Continua (`.github/workflows/ci.yml` y plantilla de PR)
Para cumplir con la validación automática al integrar cambios hacia `develop` y `main`:
- **Job `Backend (.NET 10)`**: Instala el SDK `10.0.x`, restaura paquetes NuGet, compila `AxizGeo.sln` en modo `Release` y ejecuta las pruebas unitarias con `dotnet test`.
- **Job `Frontend (React 19 + TS)`**: Configura Node, instala dependencias limpias con `npm ci` (con caché de `package-lock.json`), verifica tipos con `tsc -b`, ejecuta ESLint y valida la compilación SPA con `npm run build`.
- **`.github/pull_request_template.md`**: Checklist de calidad y seguridad para las revisiones cruzadas de Pull Requests entre los integrantes del equipo.

---

## Fase 2 — Estructura Base del Backend en .NET 10 (`backend/`)

Se creó la solución `backend/AxizGeo.sln` dividida en **5 proyectos** siguiendo **Clean Architecture**:

| Proyecto | Responsabilidad | Archivos iniciales |
|---|---|---|
| `AxizGeo.Domain` | Núcleo de negocio sin dependencias externas | `Enums/DomainEnums.cs` (roles, estados de viaje/alerta, tipos de geocerca) y `States/AlertState.cs` (**Patrón State** para controlar transiciones válidas de una alerta: `New` → `InReview` → `Resolved` / `Dismissed`). |
| `AxizGeo.Application` | Casos de uso y contratos desacoplados | `Abstractions/IRepository.cs` (**Patrones Repository y Unit of Work**). |
| `AxizGeo.Infrastructure` | Acceso a datos (EF Core + PostGIS) e integraciones externas | Referencia hacia `Domain` y `Application` lista para implementar `AppDbContext`, repositorios y el `OpenRouteServiceAdapter`. |
| `AxizGeo.Api` | Capa de presentación HTTP REST | `Program.cs` con serialización de enums como strings (`camelCase`), manejo estandarizado de errores (`AddProblemDetails`), política CORS configurable (`Cors:AllowedOrigins`), OpenAPI y endpoint `/health`. |
| `AxizGeo.Tests` | Pruebas automatizadas (`xUnit`) | `AlertStateTests.cs` verificando transiciones válidas e inválidas del patrón **State**. |

Además se incluyó `backend/Dockerfile` (multi-stage con `dotnet/sdk:10.0` y `dotnet/aspnet:10.0` exponiendo el puerto `8080`) listo para desplegar en **Render**.

---

## Fase 3 — Estructura Base del Frontend en React 19 + TypeScript (`frontend/`)

Se construyó la SPA en `frontend/` cumpliendo estrictamente con **React 19** (`^19.0.0`) y **TypeScript** (`~5.7.3`) sobre **Vite 6**:
- `src/App.tsx`: Shell inicial con la paleta institucional de Axiz Geo (`#213D71`, `#AEAEB0`, `#CBC4B1`) y verificación en vivo contra el endpoint `/health` del backend usando `VITE_API_URL`.
- `vercel.json`: Regla `rewrites` hacia `/index.html` para que React Router funcione sin errores `404` al recargar rutas en **Vercel**.
- `.env.example`: Plantilla documentada de variables de entorno del cliente.

---

## Fase 4 — Diagramas C4 Iniciales (`docs/c4-diagrams.md`)

Se documentaron los diagramas **C4 Nivel 1 (Contexto)** y **C4 Nivel 2 (Contenedores)** en Mermaid para mostrar los actores del sistema (Personal de Monitoreo y Operadores), la relación entre la SPA en React 19, la Web API en .NET 10, PostgreSQL/PostGIS y los servicios externos (OpenRouteService y OpenFreeMap).

---

## Fase 5 — Goal 1: Modelo de Dominio, Persistencia PostGIS, Repositorios y Seed (`feature/backend-domain-and-db`)

### 5.1 Entidades de Dominio (`AxizGeo.Domain/Entities`)
Se implementaron todas las entidades del modelo operativo cuidando las reglas de negocio e invariantes:
- **`Unit`**: Tractocamiones con número económico único (`T-101`, etc.), placas, marca, modelo y dimensiones (`HeightMeters`, `WeightTons`) empleadas en el ruteo de carga pesada.
- **`Operator`**: Operadores vinculados 1 a 1 con una cuenta `ApplicationUser` de rol `Operator` y licencia federal única.
- **`GeoRoute`**: Corredores autorizados con `Waypoints` (`MultiPoint`, SRID 4326), `Path` (`LineString`, SRID 4326) y margen de tolerancia en metros (`CorridorMeters`).
- **`Geofence`**: Zonas permitidas (`Allowed`) o restringidas (`Restricted`) almacenadas como `Geometry` (`Point` + `RadiusMeters` para círculos, `Polygon` para polígonos).
- **`Trip`**: Ciclo de vida del viaje (`Scheduled` → `InProgress` → `Completed` / `Cancelled`) con métodos de dominio (`Start`, `Finish`, `Cancel`) que lanzan `InvalidTripTransitionException` ante cambios de estado inválidos.
- **`Position`**: Reportes de telemetría con UUID generado por el cliente (`ValueGeneratedNever`) para garantizar **idempotencia** cuando el simulador/móvil sincroniza lotes pendientes tras perder señal, conservando tanto `CapturedAt` como `ReceivedAt`.
- **`Alert`**: Integrada directamente con el **Patrón State** (`AlertState`), exigiendo motivo de al menos 5 caracteres al resolver (`Resolve`) o descartar (`Dismiss`).

### 5.2 Persistencia con Entity Framework Core 10 + PostGIS (`AxizGeo.Infrastructure/Persistence`)
- **`AppDbContext`**: Extiende `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` para unificar autenticación basada en roles con las tablas operativas en PostgreSQL.
- **Extensión e índices espaciales**: Se declaró `builder.HasPostgresExtension("postgis")`, columnas `geography (..., 4326)` (que calculan distancias reales en metros sobre el esferoide WGS84) e índices espaciales **`GIST`** sobre `geofences.Geometry` y `positions.Location`, además de índices compuestos en `(TripId, CapturedAt)` para acelerar consultas de historial de 12 horas.
- **Enumeraciones legibles**: Todos los enums se persisten como `string` (`HasConversion<string>()`) cumpliendo el contrato de API y facilitando la auditoría en base de datos.

### 5.3 Patrones Repository y Unit of Work (`AxizGeo.Application` + `AxizGeo.Infrastructure`)
- **Problema que resuelven**: Evitan acoplar los servicios de aplicación y el motor de reglas directamente a `AppDbContext`/SQL, permiten escribir pruebas unitarias rápidas con dobles de prueba y agrupan múltiples cambios (ej. guardar posición + levantar alerta + actualizar `LastPositionAt` del viaje) en una única transacción atómica (`IUnitOfWork.SaveChangesAsync`).
- Se implementó `Repository<T>` genérico junto con repositorios especializados de dominio (`UnitRepository`, `OperatorRepository`, `RouteRepository`, `GeofenceRepository`, `TripRepository`, `PositionRepository`, `AlertRepository`) y `UnitOfWork`.

### 5.4 Migración Inicial y Datos Semilla (`Migrations/` y `DbSeeder.cs`)
- Se generó la migración `InitialCreate` con todas las tablas de ASP.NET Core Identity y del dominio espacial de Axiz Geo.
- Se creó `DbSeeder` idempotente que inicializa los 3 roles (`Admin`, `Monitor`, `Operator`), las 5 cuentas demo (leyendo la contraseña desde `Seed:DemoPassword` fuera del código fuente), 3 tractocamiones, 3 operadores, 2 rutas reales en **Nuevo Laredo** (*Puente III Comercio Mundial → Parque Industrial Oradel* y *Oradel → MX-85*) y 2 geocercas operativas.

---

## Fase 6 — Goal 2: Autenticación JWT por Roles, Manejo Centralizado de Errores y Catálogos (`feature/backend-auth-and-catalogs`)

### 6.1 Seguridad con ASP.NET Core Identity + JWT Bearer (`AxizGeo.Infrastructure/Identity` y `AuthController`)
- **Flujo de inicio de sesión (`POST /api/auth/login`)**: Valida formato de credenciales con FluentValidation, verifica existencia, estado activo (`IsActive`) y bloqueo temporal por intentos fallidos (`IsLockedOutAsync` / `AccessFailedAsync`), y emite un token JWT firmado con `HMAC-SHA256` (`JwtTokenGenerator`).
- **Claims de autorización**: El JWT incorpora `sub` (`userId`), `email`, `name`, `role` (`Admin`, `Monitor`, `Operator`) y `operatorId` (cuando el usuario corresponde a un operador de unidad), permitiendo validar en siguientes fases que un operador solo interactúe con su propio viaje activo.
- **Protección contra fuerza bruta**: Se configuró `AddRateLimiter` con una ventana fija de máximo 10 intentos por minuto por IP en `/api/auth/login` devolviendo `429 Too Many Requests`.
- **Compatibilidad anticipada con WebSockets**: En `JwtBearerEvents.OnMessageReceived` se habilitó la lectura de `?access_token=` exclusivamente en `/hubs/fleet` para autenticar conexiones de **SignalR**.
- **Validación estricta de secretos en producción**: En entornos no locales (`!IsDevelopment()`), la API exige que `Jwt:Key` provenga de variables de entorno, impidiendo arrancar con claves por defecto.

### 6.2 Manejo estandarizado de errores RFC 9457 (`GlobalExceptionHandler`)
Se implementó `IExceptionHandler` para traducir de forma uniforme las excepciones de dominio y aplicación a respuestas **ProblemDetails** en JSON (`camelCase`):
- `ValidationException` (FluentValidation) → `400 Bad Request` con diccionario `errors` por campo.
- `UnauthorizedAppException` → `401 Unauthorized`.
- `ForbiddenException` → `403 Forbidden`.
- `NotFoundException` → `404 Not Found`.
- `ConflictException`, `InvalidAlertTransitionException`, `InvalidTripTransitionException` → `409 Conflict`.

### 6.3 CRUD de Catálogos con Patrón DTO y Validadores (`UnitsController` y `OperatorsController`)
- **Unidades (`/api/units`)**: Consulta paginada con búsqueda por número económico, placas, marca o modelo (`Admin,Monitor`), alta, edición y baja lógica (`Admin`). Valida dimensiones físicas (`HeightMeters`, `WeightTons`), unicidad de `EconomicNumber` e impide desactivar una unidad que tenga un viaje `InProgress` (`409 Conflict`).
- **Operadores (`/api/operators`)**: Consulta paginada (`Admin,Monitor`), alta (`Admin`) aprovisionando automáticamente su cuenta `ApplicationUser` con rol `Operator` mediante el puerto desacoplado `IIdentityOperatorProvisioner`, edición sincronizada y baja lógica protegida si existe viaje activo.

---

## Fase 7 — Autenticación por Roles, Interceptor de Refresh Token, Layout Institucional y Ruteo Protegido en Frontend (`feature/frontend-auth-and-layout`)

### 7.1 Arquitectura Modular por Features, Fechas UTC y Cliente HTTP con Refresh Token (`src/shared/`)
- **`src/shared/types/api.ts`**: Definición estricta en TypeScript de todos los DTOs, enumeraciones como strings (`Role`, `TripStatus`, `GeofenceType`, `GeofenceKind`, `AlertType`, `AlertStatus`, `AlertSeverity`, `LiveStatus`), geometrías GeoJSON (`[longitud, latitud]` SRID 4326) y errores `ProblemDetails` (RFC 9457) alineados con `docs/api-contract.md`.
- **`src/shared/utils/date.ts`**: Conversión estandarizada de fechas salientes en ISO 8601 UTC (`toUtcIsoString()`) y formateo de marcas de tiempo entrantes (`Z`) hacia la zona horaria fronteriza `America/Matamoros`, además del cálculo de antigüedad relativa (`"hace 45 s"`).
- **`src/shared/api/httpClient.ts`**: Cliente HTTP centralizado que adjunta `Authorization: Bearer <accessToken>`, procesa errores `ProblemDetails` e implementa un **interceptor automático de `401 Unauthorized`**: si existe `refreshToken` en sesión, intenta una sola vez `POST /api/auth/refresh` (deduplicando peticiones concurrentes en vuelo), actualiza `{ accessToken, expiresAt, refreshToken }` en el store y reintenta la petición original automáticamente; si no hay `refreshToken` o la rotación responde `401`, limpia la sesión y redirige a `/login`.

### 7.2 Módulo de Autenticación y Patrones en Frontend (`src/features/auth/`)
- **Separación Container / Presentational**: `LoginPage.tsx` (contenedor con lógica de estado, redirección por rol y manejo de errores) desacoplado de `LoginForm.tsx` (componente presentacional puro con selector rápido de las 5 cuentas semilla de demostración y logotipo oficial `axiz_logo.png`).
- **Custom Hook y Context (`AuthContext` + `useAuth`)**: Gestión centralizada de sesión (`{ accessToken, expiresAt, refreshToken, user }`) persistida en `localStorage`, registro del manejador de rotación de token ante el `httpClient`, revalidación con `GET /api/auth/me`, envío de `{ refreshToken }` en `POST /api/auth/logout` y verificación de permisos por rol (`hasRole`).
- **Conmutación limpia Mock / API Real (`VITE_USE_MOCKS`)**: `authService.ts` alterna de forma transparente entre `/api/auth/login`, `/api/auth/refresh`, `/api/auth/me` y `/api/auth/logout` del backend .NET 10 y `authMock.ts` sin acoplar los componentes de UI.

### 7.3 Layout Institucional Axiz Geo y Guard de Rutas (`ProtectedRoute` y `AppLayout`)
- **`ProtectedRoute.tsx`**: Guard de navegación en React Router que protege rutas autenticadas y valida la matriz de permisos (`Admin`, `Monitor`, `Operator`), mostrando una vista explícita `HTTP 403 · Acceso Restringido por Rol` cuando un perfil intenta ingresar a un módulo no autorizado.
- **`AppLayout.tsx`**: Shell principal con la identidad visual de Axiz Geo (`#213D71` azul profundo, `#AEAEB0` gris claro, `#CBC4B1` crema arena), menú lateral filtrado dinámicamente según el rol activo, reloj en vivo en `America/Matamoros`, indicador de salud del backend (`/health` o modo simulación) y etiquetas de estado accesibles que combinan siempre ícono y texto.
