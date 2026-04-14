# Estrategia de Deploy y Entornos

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** entornos-deploy_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Propósito

Definir la estrategia de despliegue y los entornos del proyecto GeoFoto para la versión 1.0. En esta versión, el foco es desarrollo local y generación de artifacts para distribución manual.

---

## 2. Entornos

### 2.1 Desarrollo local

| Componente | Entorno | URL / Acceso |
|-----------|---------|-------------|
| GeoFoto.Api | localhost | `https://localhost:5001` |
| GeoFoto.Web | localhost | `https://localhost:5002` |
| GeoFoto.Mobile | Emulador Android / dispositivo físico USB | N/A |
| SQL Server | LocalDB o Docker | `Server=(localdb)\\mssqllocaldb;Database=GeoFoto` |

### 2.2 CI/CD (GitHub Actions)

| Componente | Entorno | Ubicación |
|-----------|---------|-----------|
| Build + Tests | ubuntu-latest runner | GitHub Actions |
| APK Debug | Artifact generado | GitHub Actions artifacts |
| Test Report | Artifact generado | GitHub Actions artifacts |

### 2.3 Entornos futuros (v2.0+)

| Entorno | Plataforma | Descripción |
|---------|-----------|-------------|
| Staging | Azure App Service | API + Web pre-producción |
| Production | Azure App Service | API + Web producción |
| Android Release | Google Play (internal track) | APK firmado |

---

## 3. Estrategia de deploy por componente

### 3.1 GeoFoto.Api

```text
┌──────────┐    ┌──────────┐    ┌──────────┐
│ Desarrollo│───►│   CI     │───►│ Artifact │
│  local   │    │ (build + │    │ (publish │
│ dotnet   │    │  test)   │    │  output) │
│ run      │    │          │    │          │
└──────────┘    └──────────┘    └──────────┘
```

| Aspecto | Valor |
|---------|-------|
| Comando local | `dotnet run --project GeoFoto.Api` |
| Build CI | `dotnet build --configuration Release` |
| Puerto HTTPS | 5001 |
| Puerto HTTP | 5000 |
| Swagger | Habilitado en Development |
| CORS | Permitir localhost:5002 (Web) |

### 3.2 GeoFoto.Web

| Aspecto | Valor |
|---------|-------|
| Comando local | `dotnet run --project GeoFoto.Web` |
| Render mode | InteractiveServer |
| Puerto HTTPS | 5002 |
| API base URL | Configurable en appsettings.json |

### 3.3 GeoFoto.Mobile

| Aspecto | Valor |
|---------|-------|
| Desarrollo | Emulador Android o dispositivo USB |
| Build debug | `dotnet build -f net10.0-android` |
| Publish APK | `dotnet publish -f net10.0-android -c Debug` |
| CI artifact | APK Debug en GitHub Actions |
| Distribución v1.0 | Instalación manual del APK |
| API URL en Mobile | Configurable en MauiProgram.cs |

---

## 4. Configuración por entorno

### 4.1 appsettings.Development.json (Api)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GeoFoto;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "AllowedOrigins": ["https://localhost:5002"],
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "FileStorage": {
    "BasePath": "./uploads"
  }
}
```

### 4.2 Configuración Mobile

```csharp
// MauiProgram.cs
builder.Services.AddHttpClient("GeoFotoApi", client =>
{
    client.BaseAddress = new Uri("https://10.0.2.2:5001"); // Android emulator → localhost
});
```

---

## 5. Base de datos

### 5.1 SQL Server (servidor)

| Aspecto | Valor |
|---------|-------|
| Provider | SQL Server LocalDB (dev) |
| Migraciones | EF Core Code-First |
| Aplicar migraciones | `dotnet ef database update` |
| Crear migración | `dotnet ef migrations add NombreMigration --project GeoFoto.Api` |
| Seed de datos | Método `SeedData()` en DbContext |

### 5.2 SQLite (mobile)

| Aspecto | Valor |
|---------|-------|
| Provider | sqlite-net-pcl |
| Ubicación BD | `FileSystem.AppDataDirectory/geofoto.db3` |
| Creación de tablas | `CreateTableAsync<T>()` al iniciar app |
| Sin migraciones | Modelo simple, tablas se recrean si no existen |

---

## 6. Almacenamiento de archivos

| Entorno | Ubicación de fotos |
|---------|-------------------|
| Servidor (dev) | `./uploads/{puntoId}/{fotoId}.jpg` |
| Mobile (local) | `FileSystem.AppDataDirectory/fotos/{puntoId}/{fotoId}.jpg` |
| Servidor (futuro) | Azure Blob Storage |

---

## 7. Versionado de releases

| Versión | Contenido | Criterio de release |
|---------|----------|-------------------|
| v1.0-alpha | Sprint 01–03 (online funcional) | CRUD completo + app Android |
| v1.0-beta | Sprint 04–05 (offline + sync) | Motor offline-first funcional |
| v1.0 | Sprint 06 (calidad + CI/CD) | Tests + pipeline + performance |

---

## 8. Checklist de deploy

### Para desarrollo local

- [ ] SQL Server LocalDB o Docker ejecutándose
- [ ] `dotnet ef database update` ejecutado
- [ ] `dotnet run --project GeoFoto.Api` funcional
- [ ] `dotnet run --project GeoFoto.Web` funcional
- [ ] CORS configurado para localhost:5002
- [ ] Swagger accesible en /swagger

### Para mobile

- [ ] Emulador Android o dispositivo USB conectado
- [ ] API URL apuntando a 10.0.2.2:5001 (emulador) o IP local (dispositivo)
- [ ] Permisos aceptados (cámara, ubicación, internet)

---

## 9. Trazabilidad

| Documento | Referencia |
|-----------|-----------|
| Pipeline CI/CD | pipeline-ci-cd_v1.0.md |
| Arquitectura | arquitectura-solucion_v1.0.md |
| Setup del proyecto | guia-setup-proyecto_v1.0.md |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
