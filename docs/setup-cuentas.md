# Axiz Geo — Configuración de cuentas y servicios

> **Nunca** pongan llaves, contraseñas o cadenas de conexión en el repo. Van en `dotnet user-secrets` (local), en variables de entorno de Render/Vercel y en *GitHub Secrets*.

| Servicio | Para qué | Estado | Responsable |
|---|---|---|---|
| GitHub | Repo público, PRs, Actions | ✅ Ya existe | Raúl |
| Vercel | Hosting del frontend | ✅ Ya existe | Raúl (invitar a Gael si se usa equipo) |
| Neon | PostgreSQL + PostGIS | ⬜ Crear | Raúl |
| Render | Hosting de la API (.NET 10 en Docker) | ⬜ Crear | Raúl |
| openrouteservice | Rutas para tractocamión | ⬜ Crear | Raúl |

---

## 1. Neon (base de datos)

Plan gratuito (al 26-nov-2026): 1 GB por proyecto, ~100 CU-h/mes, PostGIS soportado. **Se suspende tras 5 min sin uso** (no se puede desactivar en Free); despierta en menos de 1 s.

> La cuenta de Neon está **administrada por Vercel** (integración del Marketplace), por eso Neon muestra `action restricted; reason: "organization is managed by Vercel"`. La base se crea **desde Vercel**, no desde neon.com.

1. Vercel Dashboard → pestaña **Storage** → *Create Database* → **Neon** (Serverless Postgres) → *Continue*.
2. Configuración:
   - Plan: **Free**
   - Region: **Washington, D.C. (iad1 / us-east-1)** → en Render elegir **Virginia** para que queden cerca.
   - Name: `axiz-geo-db`
3. Cuando pregunte a qué proyecto conectarla: **no es necesario conectarla al frontend** (el front no habla con la BD). Si la conectan, Vercel solo agrega variables `DATABASE_URL`/`POSTGRES_*` que el front ignora.
4. En la base creada → **Open in Neon** (abre la consola de Neon ya autenticada) → *SQL Editor* y ejecutar:
   ```sql
   CREATE EXTENSION IF NOT EXISTS postgis;
   SELECT postgis_full_version();
   ```
5. En Vercel → Storage → la base → *.env.local* / *Quickstart*, o en Neon → *Connect*, copiar la cadena (`postgresql://user:pass@host/db?sslmode=require`). Para .NET (Npgsql) convertirla a este formato:
   ```
   Host=ep-xxxx.us-east-2.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=***;SSL Mode=Require;Trust Server Certificate=true
   ```
   - Usar la cadena **sin** `-pooler` para correr migraciones; la de `-pooler` sirve para la app (opcional).
6. Guardarla solo en: Render (`ConnectionStrings__Default`) y en sus `user-secrets` locales si quieren apuntar a Neon desde su máquina.

> En local se usa Docker (`postgis/postgis`), así que Neon solo se necesita para producción.

---

## 2. openrouteservice (rutas de camión)

Plan gratuito (verificado en el dashboard): **Directions V2 = 2,000/día y 40/min**, perfil `driving-hgv`, hasta 50 waypoints por ruta.

Solo se consume al pulsar **"Calcular ruta"** al crear/editar una ruta (no por cada clic ni por cada posición GPS). El resultado se guarda en la BD y no se vuelve a pedir. Uso estimado del proyecto completo: < 100 llamadas.

1. Entrar a https://account.heigit.org (portal de openrouteservice) → registrarse.
2. Confirmar correo → *Dashboard* → *Request a token* / *API Keys* → tipo **Standard (Free)** → nombre `axiz-geo`.
3. Copiar la key. Probarla desde PowerShell (Aduana Puente III → Carretera Nacional MX-85 en Nuevo Laredo) usando el nuevo dominio `api.heigit.org/openrouteservice`:
   ```powershell
   $key = "PEGA_AQUI_TU_KEY"
   $body = '{"coordinates":[[-99.5371,27.5973],[-99.5152,27.4426]],"radiuses":[1000,1000]}'
   Invoke-RestMethod -Method Post -Uri "https://api.heigit.org/openrouteservice/v2/directions/driving-hgv/geojson" `
     -Headers @{ Authorization = $key; "Content-Type" = "application/json" } -Body $body |
     Select-Object -ExpandProperty features | Select-Object -ExpandProperty properties | Select-Object -ExpandProperty summary
   ```
   Debe devolver `distance` y `duration`.
4. Guardarla en: Render (`Ors__ApiKey`) y local con `dotnet user-secrets set "Ors:ApiKey" "<key>"`.
5. **Nunca** en el frontend: la llamada pasa por `POST /api/routes/preview`.

---

## 3. Render (API)

Plan gratuito: sin tarjeta, soporta Docker. **Se duerme tras 15 min sin tráfico** y tarda ~1 min en despertar → antes de exponer/grabar, abrir `/health`.

1. Entrar a https://render.com → *Get Started* con GitHub.
2. Autorizar a Render el acceso **solo al repo** del proyecto (cuando ya exista).
3. Crear el servicio **cuando ya haya `backend/Dockerfile` en `main`** (día del deploy):
   - *New → Web Service* → seleccionar el repo.
   - Language: **Docker** · Root Directory: `backend` · Branch: `main`
   - Region: **Virginia (us-east)** (misma zona que Neon iad1) · Instance type: **Free**
   - Health Check Path: `/health`
   - Environment variables:

     | Key | Valor |
     |---|---|
     | `ASPNETCORE_ENVIRONMENT` | `Production` |
     | `ASPNETCORE_URLS` | `http://+:8080` |
     | `ConnectionStrings__Default` | cadena de Neon |
     | `Jwt__Key` | cadena aleatoria de 64+ caracteres |
     | `Jwt__Issuer` | `axiz-geo-api` |
     | `Jwt__Audience` | `axiz-geo-web` |
     | `Cors__AllowedOrigins` | `https://<proyecto>.vercel.app` |
     | `Ors__ApiKey` | key de openrouteservice |
     | `Seed__DemoPassword` | contraseña de usuarios demo |

   Generar `Jwt__Key` en PowerShell:
   ```powershell
   [Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Max 256 }) -as [byte[]])
   ```
4. *(Opcional, CD desde Actions)* Settings → *Deploy Hook* → copiar URL → GitHub repo → *Settings → Secrets and variables → Actions* → `RENDER_DEPLOY_HOOK`. Luego desactivar *Auto-Deploy* en Render.

---

## 4. Vercel (frontend)

1. *Add New → Project* → importar el repo.
2. Framework: **Vite** · Root Directory: `frontend` · Production Branch: `main`.
3. Environment Variables: `VITE_API_URL = https://<api>.onrender.com/api`.
4. Los PRs generan *Preview Deployments* automáticamente. Ojo: para que los previews hablen con la API, agregar también su dominio a `Cors__AllowedOrigins` o permitir el patrón `https://*-<equipo>.vercel.app` en el backend.
5. Agregar `frontend/vercel.json`:
   ```json
   { "rewrites": [{ "source": "/(.*)", "destination": "/index.html" }] }
   ```

---

## 5. GitHub

1. Crear el repo **público** `axiz-geo` (sin README; lo subimos nosotros).
2. *Settings → Collaborators* → invitar a Gael con permiso **Write**.
3. Tras el primer push de `main` y `develop`: *Settings → Rules → New branch ruleset* para `main` y `develop`:
   - Require a pull request before merging (1 aprobación)
   - Require status checks to pass → `Backend (.NET 10)` y `Frontend (React 19 + TS)`
   - Block force pushes
4. *Settings → Secrets → Actions*: `RENDER_DEPLOY_HOOK` (opcional).
5. *Projects → New project (Board)*: columnas Pendiente / En desarrollo / En revisión / Terminado.

---

## 6. Herramientas locales (ambos)

```powershell
dotnet --list-sdks      # debe incluir 10.0.x
node -v                 # misma versión mayor que el CI y .nvmrc (Raúl tiene v26)
npm -v
docker --version        # Docker Desktop corriendo
git --version
dotnet tool update --global dotnet-ef --version 10.*
```

> **Ojo:** si tienen instalado un SDK preview (ej. .NET 11 RC), `dotnet` lo usará por defecto. El repo incluye `global.json` fijando `10.0.x` para que todos (y el CI) compilen con .NET 10.
