# GeoFoto — Registro Georeferenciado de Fotografías

Sistema de registro georeferenciado de fotografías para trabajo de campo. Permite capturar fotos con coordenadas GPS embebidas (EXIF), visualizarlas en un mapa interactivo con markers y registrar notas sobre cada punto geográfico. Construido con **.NET 10**, orientado a una arquitectura **offline-first** con sincronización automática.

GeoFoto opera bajo el principio de que el dispositivo móvil es la fuente de verdad operativa en campo. Todas las operaciones se realizan primero en la base de datos local SQLite del dispositivo y se sincronizan con el servidor central SQL Server cuando hay conexión disponible. La interfaz de usuario se construye con **MudBlazor** como librería de componentes Material Design y **Leaflet.js** para la visualización cartográfica.

---

## Stack Tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend API | ASP.NET Core Web API — .NET 10 — Entity Framework Core — SQL Server |
| Frontend web | Blazor Web App — `@rendermode InteractiveServer` — .NET 10 |
| UI Components | MudBlazor (Material Design para Blazor) |
| App móvil | .NET MAUI Hybrid — BlazorWebView — Android — .NET 10 |
| Componentes compartidos | Razor Class Library — GeoFoto.Shared |
| Mapa | Leaflet.js vía IJSRuntime (JS Interop) |
| Base de datos local | SQLite vía sqlite-net-pcl (GeoFoto.Mobile) |
| Sincronización | Outbox Pattern + SyncService en background |
| Extracción EXIF | MetadataExtractor (NuGet) — server-side |

---

## Estructura de la Solución

```text
GeoFoto.sln
├── GeoFoto.Api        → ASP.NET Core Web API (controladores, EF Core, servicios)
├── GeoFoto.Shared     → Razor Class Library (páginas MudBlazor, servicios HTTP, modelos)
├── GeoFoto.Web        → Blazor Web App InteractiveServer (host web, registra DI)
└── GeoFoto.Mobile     → MAUI Hybrid Android (host móvil, SQLite, SyncService)
```

---

## Estructura de Documentación

```text
docs/
├── 00_contexto/                    — Visión, roadmap, acuerdo de equipo
├── 01_necesidades_negocio/         — Necesidades de negocio (NB-01 a NB-04)
├── 02_especificacion_funcional/    — Casos de uso, reglas de negocio, modelo de datos
├── 03_ux-ui/                       — Wireframes de pantallas (MudBlazor + Leaflet)
├── 05_arquitectura_tecnica/        — Arquitectura, offline-sync, modelo de datos, API REST
├── 06_backlog-tecnico/             — Product Backlog, backlog técnico, DoR, metodología Jira
├── 07_plan-sprint/                 — Sprint plans (01–06)
├── 08_calidad_y_pruebas/           — Estrategia de testing, Definition of Done
├── 09_devops/                      — Pipeline CI/CD, entornos de deploy
└── 10_developer_guide/             — Guía de setup del proyecto
```

---

## Índice de Carpetas

| Carpeta | Responsabilidad | Orden de Lectura |
|---------|----------------|:----------------:|
| [00_contexto](00_contexto/) | Visión, roadmap, acuerdo de equipo | 1 |
| [01_necesidades_negocio](01_necesidades_negocio/) | Necesidades de negocio y trazabilidad | 2 |
| [02_especificacion_funcional](02_especificacion_funcional/) | Casos de uso, reglas de negocio, modelo de datos | 3 |
| [03_ux-ui](03_ux-ui/) | Wireframes de pantallas con componentes MudBlazor | 4 |
| [05_arquitectura_tecnica](05_arquitectura_tecnica/) | Arquitectura, offline-sync, API REST, modelo de datos | 5 |
| [06_backlog-tecnico](06_backlog-tecnico/) | Product Backlog, backlog técnico, DoR, metodología Jira | 6 |
| [07_plan-sprint](07_plan-sprint/) | Planes de iteración Sprint 01 a Sprint 06 | 7 |
| [08_calidad_y_pruebas](08_calidad_y_pruebas/) | Estrategia de testing, Definition of Done | 8 |
| [09_devops](09_devops/) | Pipeline CI/CD, entornos de deploy | 9 |
| [10_developer_guide](10_developer_guide/) | Guía de setup del proyecto | 10 |

---

## Primer Viaje del Desarrollador

1. **Leer la visión del producto** → [vision-producto_v1.0.md](00_contexto/vision-producto_v1.0.md)
2. **Comprender la arquitectura** → [arquitectura-solucion_v1.0.md](05_arquitectura_tecnica/arquitectura-solucion_v1.0.md)
3. **Revisar el modelo de datos** → [modelo-datos-logico_v1.0.md](05_arquitectura_tecnica/modelo-datos-logico_v1.0.md)
4. **Configurar el entorno local** → [guia-setup-proyecto_v1.0.md](10_developer_guide/guia-setup-proyecto_v1.0.md)
5. **Consultar el roadmap y sprints** → [roadmap-producto_v1.0.md](00_contexto/roadmap-producto_v1.0.md)

---

## Requisitos Previos

| Requisito | Versión |
|-----------|---------|
| .NET SDK | 10.0 |
| Visual Studio | 2022 17.x o superior |
| SQL Server | 2019+ o LocalDB |
| Android SDK | API 33+ |
| MAUI Workload | `dotnet workload install maui` |
| Node.js (opcional) | 18+ |

### Comandos de Setup Rápido

```bash
# Clonar repositorio
git clone <url-del-repo> GeoFoto
cd GeoFoto

# Restaurar dependencias
dotnet restore GeoFoto.sln

# Aplicar migrations de EF Core
cd GeoFoto.Api
dotnet ef database update

# Ejecutar API
dotnet run --project GeoFoto.Api

# Ejecutar Web (en otra terminal)
dotnet run --project GeoFoto.Web

# Ejecutar Mobile en emulador Android
dotnet build GeoFoto.Mobile -t:Run -f net10.0-android
```

---

## Referencias Clave

- **Roadmap del producto:** [roadmap-producto_v1.0.md](00_contexto/roadmap-producto_v1.0.md)
- **Product Backlog:** [product-backlog_v1.0.md](06_backlog-tecnico/product-backlog_v1.0.md)
- **API REST:** [api-rest-spec_v1.0.md](05_arquitectura_tecnica/api-rest-spec_v1.0.md)
- **Definition of Done:** [definition-of-done_v1.0.md](08_calidad_y_pruebas/definition-of-done_v1.0.md)
- **Pipeline CI/CD:** [pipeline-ci-cd_v1.0.md](09_devops/pipeline-ci-cd_v1.0.md)
- **Metodología Jira:** [jira-metodologia_v1.0.md](06_backlog-tecnico/jira-metodologia_v1.0.md)

---

## Contribución

1. Fork del repositorio
2. Crear branch descriptivo
3. Implementar cambios
4. Agregar pruebas
5. Asegurar validaciones en verde
6. Crear Pull Request

---

## Licencia

Uso interno / institucional.

---

## Contacto

Equipo de desarrollo del proyecto GeoFoto.

---
