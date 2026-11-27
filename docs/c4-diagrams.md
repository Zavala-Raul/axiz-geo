# Diagramas de Arquitectura C4 — Axiz Geo

De acuerdo con los requisitos de arquitectura del proyecto final, se presentan los diagramas **C4 de Contexto (Nivel 1)** y **C4 de Contenedores (Nivel 2)**.

---

## Nivel 1 — Diagrama de Contexto del Sistema

Muestra quiénes utilizan **Axiz Geo** y con qué sistemas externos interactúa.

```mermaid
C4Context
    title Diagrama de Contexto (C4 Nivel 1) — Axiz Geo

    Person(monitor, "Personal de Monitoreo / Operaciones", "Administra unidades, rutas y geocercas; supervisa la flota en el mapa y atiende alertas.")
    Person(operator, "Operador de Transporte", "Inicia/finaliza viajes asignados y reporta telemetría GPS desde vista web móvil o simulador.")

    System(axizGeo, "Axiz Geo", "Sistema web para monitoreo de rutas de carga, evaluación de geocercas y gestión de alertas operativas.")

    System_Ext(ors, "OpenRouteService API (HeiGIT)", "Calcula trazos de rutas para vehículo pesado (perfil driving-hgv).")
    System_Ext(ofm, "OpenFreeMap Tiles", "Provee las teselas vectoriales del mapa base en el navegador.")

    Rel(monitor, axizGeo, "Supervisa flota, configura geocercas y resuelve alertas", "HTTPS")
    Rel(operator, axizGeo, "Inicia viaje y envía lotes de posiciones", "HTTPS")
    Rel(axizGeo, ors, "Solicita cálculo de ruta para tractocamión", "HTTPS / JSON")
    Rel(monitor, ofm, "Carga mapa interactivo desde la SPA", "HTTPS")
```

---

## Nivel 2 — Diagrama de Contenedores

Detalla cómo se relacionan el **Frontend (SPA React 19 + TypeScript)**, el **Backend (ASP.NET Core Web API .NET 10)**, la **Base de Datos (PostgreSQL + PostGIS)** y los servicios externos.

```mermaid
C4Container
    title Diagrama de Contenedores (C4 Nivel 2) — Axiz Geo

    Person(monitor, "Personal de Monitoreo / Admin", "Supervisa operaciones en navegador web.")
    Person(operator, "Operador / Simulador", "Reporta posiciones de viaje activo.")

    System_Boundary(axizBoundary, "Sistema Axiz Geo") {
        Container(spa, "SPA Frontend", "React 19, TypeScript, Vite, MapLibre GL, Terra Draw", "Interfaz de usuario única (Vercel): mapa en vivo, dibujo de geocercas, historial de 12 h, bandeja de alertas y simulador.")
        Container(api, "Backend Web API", "ASP.NET Core (.NET 10), EF Core 10, Identity/JWT, NetTopologySuite, SignalR", "Gestiona autenticación por roles, catálogos, motor de reglas/geocercas, ingesta de telemetría y notificaciones (Render Docker).")
        ContainerDb(db, "Base de Datos Geoespacial", "PostgreSQL 17 + PostGIS", "Persiste usuarios, unidades, operadores, rutas, geocercas, viajes, posiciones históricas y alertas (Neon).")
    }

    System_Ext(ors, "OpenRouteService (HeiGIT)", "API REST driving-hgv para cálculo de corredores de tractocamión.")
    System_Ext(ofm, "OpenFreeMap", "Servidor público de teselas vectoriales.")

    Rel(monitor, spa, "Usa panel de monitoreo y catálogos", "HTTPS")
    Rel(operator, spa, "Usa vista de viaje / simulador de telemetría", "HTTPS")
    Rel(spa, ofm, "Descarga estilos y teselas del mapa", "HTTPS")
    Rel(spa, api, "Consume endpoints REST autenticados", "HTTPS / JSON + JWT")
    Rel(api, spa, "Notifica posiciones y alertas en tiempo real", "WebSockets / SignalR")
    Rel(api, db, "Lee y escribe entidades y consultas espaciales", "TCP / SSL (Npgsql)")
    Rel(api, ors, "Calcula trazo autorizado al crear ruta", "HTTPS / JSON")
```
