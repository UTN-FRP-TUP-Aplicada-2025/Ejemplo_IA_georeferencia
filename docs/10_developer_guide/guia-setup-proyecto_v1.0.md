# Guía de Setup del Proyecto

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** guia-setup-proyecto_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Propósito

Guía paso a paso para configurar el entorno de desarrollo del proyecto GeoFoto desde cero hasta tener la solución completa ejecutándose localmente (API, Web y Mobile).

---

## 2. Prerequisitos

| Herramienta | Versión mínima | Descarga |
|------------|---------------|----------|
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| Visual Studio 2022 | 17.12+ | Con workloads: ASP.NET, .NET MAUI |
| SQL Server LocalDB | Incluido en VS | O Docker: `mcr.microsoft.com/mssql/server` |
| Git | 2.40+ | https://git-scm.com |
| Android SDK | API 34+ | Incluido en VS con workload MAUI |
| Emulador Android | Pixel 5 API 34 | Android Device Manager en VS |
| Node.js | 18+ (opcional) | Solo si se requiere tooling JS adicional |

---

## 3. Pasos de configuración

### Paso 1 — Clonar el repositorio

```bash
git clone https://github.com/equipo/geofoto.git
cd geofoto
```

### Paso 2 — Verificar .NET SDK

```bash
dotnet --version
# Debe mostrar 10.0.x
```

### Paso 3 — Instalar workloads MAUI

```bash
dotnet workload install maui-android
```

### Paso 4 — Restaurar paquetes NuGet

```bash
dotnet restore GeoFoto.sln
```

### Paso 5 — Configurar base de datos SQL Server

Opción A — LocalDB (Windows):
```bash
# Verificar que LocalDB está disponible
sqllocaldb info
```

Opción B — Docker:
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=GeoFoto2026!" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

> Si se usa Docker, actualizar el connection string en `appsettings.Development.json`.

### Paso 6 — Instalar herramientas EF Core

```bash
dotnet tool install --global dotnet-ef
```

### Paso 7 — Aplicar migraciones

```bash
cd GeoFoto.Api
dotnet ef database update
cd ..
```

### Paso 8 — Compilar la solución

```bash
dotnet build GeoFoto.sln
```

> La compilación debe finalizar sin errores. Warnings no bloqueantes son aceptables.

### Paso 9 — Ejecutar la API

```bash
dotnet run --project GeoFoto.Api
```

Verificar:
- Swagger UI disponible en `https://localhost:5001/swagger`
- `/api/puntos` retorna JSON (array vacío si no hay datos)

### Paso 10 — Ejecutar la Web

En una terminal separada:
```bash
dotnet run --project GeoFoto.Web
```

Verificar:
- Navegar a `https://localhost:5002`
- Se muestra la página con MudAppBar "GeoFoto" y navegación lateral

### Paso 11 — Configurar emulador Android

1. Abrir Visual Studio → Tools → Android Device Manager
2. Crear dispositivo: Pixel 5, API 34, x86_64
3. Iniciar el emulador

### Paso 12 — Ejecutar la app Mobile

1. En Visual Studio, seleccionar GeoFoto.Mobile como proyecto de inicio
2. Seleccionar el emulador Android como target
3. Presionar F5 (Debug)

Verificar:
- La app se instala y abre en el emulador
- Se muestra el mapa (requiere que la API esté ejecutándose)

---

## 4. Estructura de la solución

```text
GeoFoto.sln
├── GeoFoto.Api/              # ASP.NET Core Web API
│   ├── Controllers/          # PuntosController, FotosController, SyncController
│   ├── Data/                 # GeoFotoDbContext, Migrations
│   ├── Services/             # Lógica de negocio del servidor
│   ├── Program.cs            # Entry point con Swagger + CORS
│   └── appsettings.json      # Configuración
│
├── GeoFoto.Shared/           # Razor Class Library (UI compartida)
│   ├── Components/           # Componentes Blazor (.razor)
│   ├── Models/               # DTOs y modelos compartidos
│   ├── wwwroot/              # JS interop (leaflet-interop.js)
│   └── _Imports.razor        # Usings globales
│
├── GeoFoto.Web/              # Blazor Web App (InteractiveServer)
│   ├── Program.cs            # Entry point con InteractiveServer
│   └── appsettings.json      # URL de la API
│
├── GeoFoto.Mobile/           # .NET MAUI Hybrid Android
│   ├── MauiProgram.cs        # Entry point con DI + HttpClient
│   ├── Services/             # LocalDbService, SyncService, ConnectivityService
│   ├── Platforms/Android/    # AndroidManifest.xml, permisos
│   └── Resources/            # Iconos y assets
│
└── GeoFoto.Tests/            # xUnit + Moq
    ├── Unit/                 # Tests unitarios
    └── Integration/          # Tests de integración
```

---

## 5. Variables de entorno y configuración

| Variable / Archivo | Descripción | Ejemplo |
|-------------------|-------------|---------|
| `appsettings.Development.json` (Api) | Connection string SQL Server | `Server=(localdb)\\mssqllocaldb;Database=GeoFoto` |
| `appsettings.json` (Web) | URL de la API | `https://localhost:5001` |
| `MauiProgram.cs` (Mobile) | URL de la API para Android | `https://10.0.2.2:5001` |
| `FileStorage:BasePath` (Api) | Carpeta de fotos subidas | `./uploads` |

---

## 6. Comandos útiles

| Comando | Descripción |
|---------|-------------|
| `dotnet build GeoFoto.sln` | Compilar toda la solución |
| `dotnet run --project GeoFoto.Api` | Ejecutar la API |
| `dotnet run --project GeoFoto.Web` | Ejecutar la Web |
| `dotnet test` | Ejecutar todos los tests |
| `dotnet ef migrations add Nombre --project GeoFoto.Api` | Crear migración |
| `dotnet ef database update --project GeoFoto.Api` | Aplicar migraciones |
| `dotnet publish GeoFoto.Mobile -f net10.0-android -c Debug` | Generar APK Debug |

---

## 7. Solución de problemas comunes

| Problema | Solución |
|----------|---------|
| Error de conexión a SQL Server | Verificar que LocalDB o Docker estén ejecutándose |
| CORS error en Web | Verificar que `AllowedOrigins` incluya `https://localhost:5002` |
| Emulador no conecta a API | Usar `10.0.2.2` en lugar de `localhost` en la config Mobile |
| `dotnet ef` no encontrado | Ejecutar `dotnet tool install --global dotnet-ef` |
| MAUI build falla | Ejecutar `dotnet workload install maui-android` |
| Leaflet no carga en Mobile | Verificar que `leaflet-interop.js` está en wwwroot de Shared |
| Permisos denegados en Android | Verificar AndroidManifest.xml y solicitar permisos en runtime |

---

## 8. Trazabilidad

| Documento | Referencia |
|-----------|-----------|
| Arquitectura | arquitectura-solucion_v1.0.md |
| Entornos y Deploy | entornos-deploy_v1.0.md |
| Pipeline CI/CD | pipeline-ci-cd_v1.0.md |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
