# Prompt para Copilot — Docker + Railway para GeoFoto

---

## Instrucción Principal

Sos un DevOps engineer senior trabajando en el proyecto **GeoFoto**. Necesito que generes los siguientes archivos para containerizar y desplegar la solución. Respetá la estructura de documentación existente, el estilo de redacción de los documentos actuales (encabezados con metadata de proyecto, tablas markdown, secciones numeradas, control de cambios al final) y las convenciones del proyecto.

---

## Contexto del Proyecto

**GeoFoto** es un sistema de registro georeferenciado de fotografías offline-first construido con .NET 10. La solución tiene 4 proyectos:

```text
GeoFoto.sln
├── GeoFoto.Api        → ASP.NET Core Web API (.NET 10, EF Core, SQL Server)
│   ├── Controllers/   → PuntosController, FotosController, SyncController
│   ├── Data/          → GeoFotoDbContext, Migrations
│   ├── Services/
│   ├── Program.cs     → Entry point con Swagger + CORS
│   └── appsettings.json
├── GeoFoto.Shared     → Razor Class Library (páginas MudBlazor, servicios HTTP, modelos)
├── GeoFoto.Web        → Blazor Web App InteractiveServer (.NET 10)
│   ├── Program.cs     → Entry point con InteractiveServer + MudBlazor
│   └── appsettings.json → contiene URL de la API
└── GeoFoto.Mobile     → .NET MAUI Hybrid Android (no se containeriza)
```

**Stack relevante:**
- .NET 10 SDK
- SQL Server 2022 (imagen `mcr.microsoft.com/mssql/server:2022-latest`)
- Entity Framework Core con migrations code-first
- MudBlazor como librería UI
- Leaflet.js para mapas
- Puertos actuales: API en 5000/5001, Web en 5002
- Connection string dev: `Server=(localdb)\\mssqllocaldb;Database=GeoFoto;Trusted_Connection=true`
- La Web consume la API vía HttpClient configurado en `appsettings.json`
- Las fotos se almacenan en `./uploads/{puntoId}/{fotoId}.jpg`

**Estructura de documentación existente:**
```text
docs/
├── 09_devops/
│   ├── pipeline-ci-cd_v1.0.md        (ya existe)
│   └── entornos-deploy_v1.0.md       (ya existe)
├── 10_developer_guide/
│   └── guia-setup-proyecto_v1.0.md   (ya existe)
scripts/
├── 06_demo_completa.bat              (ya existe)
└── 07_full_rebuild.bat               (ya existe)
```

---

## Entregables Solicitados

### 1. Dockerfiles (3 archivos en la raíz del proyecto)

#### 1.1 `Dockerfile.api` (para GeoFoto.Api)
- Base: `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime) y `mcr.microsoft.com/dotnet/sdk:10.0` (build)
- Multi-stage build: restore → build → publish → runtime
- Exponer puertos 8080 (HTTP)
- Copiar GeoFoto.Shared como dependencia (la API referencia a Shared)
- Configurar `ASPNETCORE_ENVIRONMENT=Production`
- Crear directorio `/app/uploads` para almacenamiento de fotos con volumen
- Healthcheck con `curl` o `wget` al endpoint `/health` (agregar nota de que se debe implementar)
- El `ENTRYPOINT` debe ser `["dotnet", "GeoFoto.Api.dll"]`

#### 1.2 `Dockerfile.web` (para GeoFoto.Web)
- Base: mismas imágenes .NET 10
- Multi-stage build
- Exponer puerto 8080
- Copiar GeoFoto.Shared como dependencia (Web referencia a Shared)
- La URL de la API se debe inyectar por variable de entorno `API_BASE_URL`
- Configurar `ASPNETCORE_ENVIRONMENT=Production`
- `ENTRYPOINT`: `["dotnet", "GeoFoto.Web.dll"]`

#### 1.3 `docker-compose.yml` (orquestación completa)
- **Servicio `sqlserver`:**
  - Imagen: `mcr.microsoft.com/mssql/server:2022-latest`
  - Variables: `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD` (desde `.env`)
  - Puerto: 1433:1433
  - Volumen persistente para datos: `sqlserver-data:/var/opt/mssql`
  - Healthcheck con `/opt/mssql-tools18/bin/sqlcmd`
- **Servicio `geofoto-api`:**
  - Build desde `Dockerfile.api`
  - Puerto: 5000:8080
  - Depende de `sqlserver` (condition: service_healthy)
  - Variables de entorno para connection string apuntando al servicio sqlserver: `ConnectionStrings__DefaultConnection=Server=sqlserver;Database=GeoFoto;User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=true`
  - Variable `APPLY_MIGRATIONS=true` (para ejecutar migrations al iniciar)
  - Volumen para uploads: `uploads-data:/app/uploads`
- **Servicio `geofoto-web`:**
  - Build desde `Dockerfile.web`
  - Puerto: 5002:8080
  - Depende de `geofoto-api`
  - Variable: `API_BASE_URL=http://geofoto-api:8080`
- **Archivo `.env.example`** con las variables requeridas:
  ```
  SA_PASSWORD=GeoFoto2026!
  ASPNETCORE_ENVIRONMENT=Production
  ```
- **Volúmenes nombrados:** `sqlserver-data`, `uploads-data`
- **Red:** `geofoto-network` (bridge)

---

### 2. Documento de Dockerización — `docs/09_devops/dockerizacion_v1.0.md`

Crear este documento siguiendo el estilo de los documentos existentes del proyecto (ver formato de `pipeline-ci-cd_v1.0.md` y `entornos-deploy_v1.0.md`). Debe incluir:

**Encabezado estándar:**
```markdown
# Dockerización de la Solución — GeoFoto

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** dockerizacion_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-14  
**Autor:** Equipo Técnico
```

**Secciones requeridas:**

1. **Propósito** — Breve descripción de por qué se containeriza la solución
2. **Arquitectura de contenedores** — Diagrama ASCII mostrando los 3 servicios (sqlserver, api, web) y sus conexiones, puertos y volúmenes
3. **Prerequisitos** — Tabla con Docker Engine 24+, Docker Compose v2+, recursos mínimos (4GB RAM para SQL Server)
4. **Estructura de archivos Docker** — Tabla mostrando cada Dockerfile, docker-compose.yml, .env.example y su ubicación
5. **Imágenes Docker** — Tabla detallando cada imagen: nombre, base image, puerto expuesto, tamaño estimado
6. **Variables de entorno** — Tabla completa de todas las variables de entorno por servicio
7. **Volúmenes persistentes** — Tabla con cada volumen, su propósito y ruta dentro del contenedor
8. **Comandos de operación** — Tabla con comandos para: build, start, stop, logs, rebuild, limpiar volúmenes, ejecutar migrations manualmente, acceder a SQL Server desde el contenedor
9. **Flujo de primer inicio** — Pasos numerados de qué ocurre al hacer `docker compose up` por primera vez (pull images → create network → start sqlserver → wait healthy → start api + migrations → start web)
10. **Troubleshooting** — Tabla con problemas comunes: SQL Server no inicia (poca RAM), API no conecta a DB (healthcheck), Web no conecta a API (red), migrations fallan (DB no ready)
11. **Trazabilidad** — Referencias a `entornos-deploy_v1.0.md`, `pipeline-ci-cd_v1.0.md`, `guia-setup-proyecto_v1.0.md`
12. **Control de Cambios** — Tabla estándar

---

### 3. Documento de Deploy en Railway — `docs/09_devops/deploy-railway_v1.0.md`

Mismo formato de encabezado. Secciones:

1. **Propósito** — Desplegar GeoFoto en Railway como entorno de staging/producción
2. **¿Por qué Railway?** — Breve justificación (soporte Docker, deploy desde GitHub, bases de datos gestionadas, networking interno, plan gratuito/trial disponible)
3. **Arquitectura en Railway** — Diagrama ASCII mostrando los 3 servicios en Railway con su networking interno (private networking entre servicios, public domain solo para Web)
4. **Prerequisitos** — Cuenta en Railway, CLI de Railway instalado, repositorio en GitHub conectado
5. **Paso a paso — Base de datos SQL Server en Railway:**
   - Opción A: Plugin de base de datos PostgreSQL como alternativa (Railway no tiene plugin nativo de SQL Server) → explicar que requeriría migrar el provider de EF Core a Npgsql
   - Opción B: Deploy de SQL Server como servicio Docker custom en Railway desde la imagen `mcr.microsoft.com/mssql/server:2022-latest` → variables necesarias, volumen persistente, limitaciones de RAM en Railway
   - Recomendar Opción B para mantener compatibilidad con SQL Server, con nota sobre los requisitos de RAM (mínimo 2GB, verificar plan de Railway)
6. **Paso a paso — Deploy de GeoFoto.Api en Railway:**
   - Crear servicio desde Dockerfile
   - Configurar root directory si es monorepo
   - Variables de entorno requeridas (connection string con referencia al servicio SQL Server interno `${{sqlserver.RAILWAY_PRIVATE_DOMAIN}}`)
   - Health check path: `/health`
   - Internal networking: el API no necesita dominio público, se accede internamente
7. **Paso a paso — Deploy de GeoFoto.Web en Railway:**
   - Crear servicio desde Dockerfile
   - Variable `API_BASE_URL` apuntando al dominio privado interno del API
   - Generar dominio público para la Web (es el único servicio público)
   - Configurar custom domain si se desea
8. **Variables de entorno en Railway** — Tabla completa con todas las variables, indicando cuáles usar `${{service.variable}}` para referencia interna
9. **Networking y dominios** — Explicar private networking de Railway, cuál servicio es público (Web), cuál es interno (API, SQL Server)
10. **Volumes y persistencia** — Cómo configurar persistent volumes en Railway para SQL Server data y uploads de fotos
11. **CI/CD con Railway** — Cómo Railway auto-deploya desde push a `main` en GitHub, y cómo integrarlo con el pipeline de GitHub Actions existente
12. **Costos estimados** — Tabla orientativa de consumo de recursos y costos en Railway (Trial/Hobby/Pro)
13. **Limitaciones y consideraciones** — SQL Server requiere mucha RAM, alternativa PostgreSQL, límites de storage en volúmenes de Railway, cold starts
14. **Troubleshooting Railway** — Problemas comunes: deploy falla por memoria, SQL Server crashea (OOM killer), networking entre servicios, logs en Railway dashboard
15. **Trazabilidad y Control de Cambios**

---

### 4. Scripts de Docker — en carpeta `scripts/`

#### 4.1 `scripts/docker-build.sh` (Linux/Mac) + `scripts/docker-build.bat` (Windows)

Script para construir las imágenes Docker localmente:
- Recibir como parámetro opcional el tag de versión (default: `latest`)
- Construir `geofoto-api:{tag}` desde `Dockerfile.api`
- Construir `geofoto-web:{tag}` desde `Dockerfile.web`
- Mostrar resumen con nombre y tamaño de cada imagen construida
- Colores en la salida (verde OK, rojo error) — para .bat usar `color` y para .sh usar ANSI
- Validar que Docker esté instalado y corriendo antes de empezar
- Medir y mostrar el tiempo total de build
- Formato de salida similar a los scripts existentes del proyecto (`07_full_rebuild.bat`): con fases, `[OK]`, `[ERROR]`, `[INFO]`

#### 4.2 `scripts/docker-publish.sh` (Linux/Mac) + `scripts/docker-publish.bat` (Windows)

Script para publicar las imágenes a un registry:
- Recibir como parámetros: registry URL (default: `ghcr.io`), namespace/owner, tag de versión
- Hacer `docker tag` de cada imagen con el formato `{registry}/{namespace}/geofoto-api:{tag}`
- Hacer `docker push` de cada imagen
- Verificar que el usuario esté autenticado en el registry (`docker login`) antes de pushear
- Mostrar resumen de las imágenes publicadas con su URL completa
- Mismo formato de salida que los scripts existentes del proyecto

#### 4.3 `scripts/docker-compose-up.sh` (Linux/Mac) + `scripts/docker-compose-up.bat` (Windows)

Script simplificado para levantar todo el entorno con docker compose:
- Copiar `.env.example` a `.env` si `.env` no existe (con warning)
- Ejecutar `docker compose up --build -d`
- Esperar a que los 3 servicios estén healthy (polling con timeout de 120 segundos)
- Mostrar URLs de acceso: API (localhost:5000/swagger), Web (localhost:5002)
- Mostrar estado final de los contenedores con `docker compose ps`

---

### 5. Actualizar `docs/09_devops/entornos-deploy_v1.0.md`

Agregar una nueva sección **2.4 Docker Compose (local)** y **2.5 Railway (staging)** en la sección de Entornos existente, con tablas que sigan el mismo formato de las secciones 2.1, 2.2, 2.3. También agregar en la sección de evolución futura la referencia a Railway como plataforma de staging.

---

### 6. Actualizar `docs/README.md`

Agregar en la sección de documentación la referencia a los nuevos documentos:
- `dockerizacion_v1.0.md` en la fila de `09_devops`
- `deploy-railway_v1.0.md` en la fila de `09_devops`

---

## Reglas de Estilo

1. Todos los documentos `.md` deben seguir el formato de encabezado estándar del proyecto (Proyecto, Documento, Versión, Estado, Fecha, Autor)
2. Usar tablas markdown para información estructurada
3. Diagramas en ASCII art con bloques `┌─┐│└─┘` o bloques ```text
4. Secciones numeradas
5. Control de cambios al final de cada documento
6. Los scripts `.bat` deben seguir el estilo de `07_full_rebuild.bat`: con `setlocal enabledelayedexpansion`, fases numeradas, mensajes `[OK]`/`[ERROR]`/`[INFO]`/`[WARN]`, título con `title`, colores con `color`
7. Los scripts `.sh` deben tener `#!/bin/bash`, `set -e`, funciones para colores, y mismo estilo de fases y mensajes
8. Todos los scripts deben ser idempotentes (se pueden ejecutar múltiples veces sin problemas)
9. El `docker-compose.yml` debe incluir comentarios explicativos en cada servicio
10. Los Dockerfiles deben incluir `LABEL` con metadata del proyecto (maintainer, version, description)

---

## Archivos a Generar (resumen)

```text
# Raíz del proyecto
Dockerfile.api
Dockerfile.web
docker-compose.yml
.env.example
.dockerignore

# Documentación
docs/09_devops/dockerizacion_v1.0.md          (NUEVO)
docs/09_devops/deploy-railway_v1.0.md         (NUEVO)
docs/09_devops/entornos-deploy_v1.0.md        (ACTUALIZAR secciones 2.4 y 2.5)
docs/README.md                                 (ACTUALIZAR tabla de carpetas)

# Scripts
scripts/docker-build.sh                        (NUEVO)
scripts/docker-build.bat                       (NUEVO)
scripts/docker-publish.sh                      (NUEVO)
scripts/docker-publish.bat                     (NUEVO)
scripts/docker-compose-up.sh                   (NUEVO)
scripts/docker-compose-up.bat                  (NUEVO)
```

Generá todos los archivos completos, sin placeholders ni `// TODO`. Cada archivo debe estar listo para usar.
