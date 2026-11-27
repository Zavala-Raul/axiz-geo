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
- **`.github/pull_request_template.md`**: Checklist de calidad y seguridad para las revisiones cruzadas de Pull Requests entre Raúl y Gael.

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
