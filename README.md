# Axiz Geo

Sistema web de geocercas y monitoreo de rutas para unidades de transporte de carga (Caso de estudio: Nuevo Laredo, Tamaulipas).

Desarrollado como **Proyecto Final — Unidad 6** de Tópicos Avanzados de Programación.

---

## Equipo y Responsabilidades

| Integrante | Rol |
|---|---|
| **Raúl Adriell Zavala Lira** | Backend (`.NET 10` + PostgreSQL/PostGIS) y Administración de GitHub / CI-CD |
| **Gael Onofre García** | Frontend (`React 19` + `TypeScript`) |

---

## Enlaces de Despliegue

- **Frontend (Vercel):** _Pendiente de publicación en primer release_
- **Backend API (Render):** _Pendiente de publicación en primer release_

---

## Stack Tecnológico

- **Frontend:** React 19, TypeScript, Vite, MapLibre GL JS (OpenFreeMap), Terra Draw, TanStack Query.
- **Backend:** ASP.NET Core Web API con .NET 10, Entity Framework Core 10, ASP.NET Core Identity + JWT, NetTopologySuite, SignalR.
- **Base de datos:** PostgreSQL 17 + extensión espacial PostGIS (Neon en producción / Docker en local).
- **Ruteo de carga pesada:** OpenRouteService (`driving-hgv` en `api.heigit.org/openrouteservice`).
- **CI/CD:** GitHub Actions (`.github/workflows/ci.yml`), Vercel y Render.

---

## Estructura del Repositorio

```text
├── .github/workflows/ci.yml     # Pipeline CI para Backend y Frontend
├── backend/                     # Solución ASP.NET Core (.NET 10) en Clean Architecture
│   ├── src/AxizGeo.Domain/
│   ├── src/AxizGeo.Application/
│   ├── src/AxizGeo.Infrastructure/
│   ├── src/AxizGeo.Api/
│   └── tests/AxizGeo.Tests/
├── frontend/                    # SPA en React 19 + TypeScript + Vite
├── docs/                        # Contrato de API, guía de cuentas, diagramas C4 y bitácora
└── docker-compose.yml           # PostgreSQL 17 + PostGIS para desarrollo local
```

---

## Ejecución Local

### Requisitos previos
- **.NET 10 SDK** (`10.0.x`)
- **Node.js** (`v22+`) y `npm`
- **Docker Desktop** (para PostgreSQL + PostGIS local)

### 1. Levantar Base de Datos (PostgreSQL + PostGIS)
```bash
docker compose up -d
```

### 2. Ejecutar Backend (.NET 10 Web API)
```bash
cd backend
dotnet user-secrets --project src/AxizGeo.Api set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=axiz_geo;Username=axiz_user;Password=axiz_dev_password_change_me"
dotnet user-secrets --project src/AxizGeo.Api set "Jwt:Key" "clave-secreta-desarrollo-local-minimo-32-caracteres-2026"
dotnet run --project src/AxizGeo.Api
```
La API quedará disponible en `http://localhost:5080` (Health check en `http://localhost:5080/health`).

### 3. Ejecutar Frontend (React 19 + TypeScript)
```bash
cd frontend
cp .env.example .env
npm install
npm run dev
```
La aplicación web abrirá en `http://localhost:5173`.

---

## Documentación Técnica

- [Diagramas de Arquitectura C4 (Contexto y Contenedores)](./docs/c4-diagrams.md)
- [Contrato de la API (v1)](./docs/api-contract.md)
- [Bitácora de Construcción del Proyecto](./docs/bitacora-desarrollo.md)
- [Configuración de Cuentas y Servicios](./docs/setup-cuentas.md)
