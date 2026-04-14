# Acuerdo de Equipo (Team Charter / Working Agreements)

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** acuerdo-equipo_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-13
**Autor:** Equipo Técnico

---

# 1. Propósito

Establecer los acuerdos de trabajo, roles y compromisos del equipo de desarrollo de GeoFoto. Este documento sirve como referencia compartida para las normas y prácticas que el equipo se compromete a seguir durante todo el ciclo de vida del proyecto.

---

# 2. Equipo y Roles Scrum

| Rol | Persona | Responsabilidades en GeoFoto |
|-----|---------|------------------------------|
| Product Owner | [Pendiente de asignar] | Priorización del backlog, validación de demos, aceptación de historias |
| Scrum Master | [Pendiente de asignar] | Facilitador del proceso Scrum, remoción de impedimentos, métricas de velocidad |
| Equipo de Desarrollo | [Pendiente de completar] | Desarrollo full-stack (.NET, Blazor, MAUI), testing, documentación |

### Miembros del equipo de desarrollo

| Nombre | Especialización | Componentes principales |
|--------|----------------|------------------------|
| [Nombre 1] | Backend / API | GeoFoto.Api, EF Core, SQL Server |
| [Nombre 2] | Frontend / Blazor | GeoFoto.Shared, MudBlazor, Leaflet.js |
| [Nombre 3] | Mobile / MAUI | GeoFoto.Mobile, SQLite, SyncService |

### Responsabilidades específicas de GeoFoto

- **API y persistencia:** Controladores REST, Entity Framework Core, migrations, extracción EXIF con MetadataExtractor.
- **UI compartida:** Páginas y componentes Blazor con MudBlazor en GeoFoto.Shared, integración Leaflet.js vía IJSRuntime.
- **App móvil:** MAUI Hybrid con BlazorWebView, SQLite vía sqlite-net-pcl, SyncService, ConnectivityService.
- **Sincronización:** Implementación del Outbox Pattern, resolución Last-Write-Wins, gestión de SyncQueue.
- **Testing:** Tests unitarios con xUnit, tests de integración del motor de sync, verificación en modo avión.

---

# 3. Cadencia de Ceremonias

| Ceremonia | Cuándo | Duración | Notas |
|-----------|--------|----------|-------|
| Sprint Planning | Día 1 de cada sprint | 2 horas | Todo el equipo Scrum. Se comprometen US con IDs Jira (GEO-USXX) |
| Daily Scrum | Diario | 15 minutos | Formato async o sincrónico según acuerdo del equipo |
| Sprint Review | Último día del sprint | 1 hora | Demo ejecutable siguiendo el walkthrough del plan de sprint |
| Sprint Retrospective | Después del Review | 1 hora | Solo el equipo Scrum |
| Backlog Refinement | Semanal, mitad del sprint | 1 hora | Preparación de ítems para el sprint siguiente |

**Duración del sprint:** 2 semanas.

---

# 4. Acuerdos de Trabajo

## 4.1 Branching Strategy

- **Modelo:** GitFlow
- **Branches principales:** `main` (producción), `develop` (integración)
- **Branches de trabajo:** `feature/*`, `bugfix/*`, `hotfix/*`
- **Convención de nombres:** `feature/GEO-USXX-descripcion-corta`
- **Ejemplo:** `feature/GEO-US04-upload-fotos-mapa`

## 4.2 Commits

- **Formato:** Conventional Commits
- **Estructura:** `tipo(alcance): descripción`
- **Tipos permitidos:** `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`
- **Alcance:** ID Jira de la historia (GEO-USXX) o componente (api, shared, mobile)
- **Ejemplo:** `feat(GEO-US04): agrega endpoint POST /api/fotos/upload`
- **Ejemplo:** `test(GEO-US19): agrega tests de integración para SyncService`

## 4.3 Code Review

- Mínimo **1 aprobación** antes de merge a `develop`.
- Las revisiones se realizan en un máximo de **24 horas hábiles**.
- El autor del PR es responsable de resolver los comentarios.
- El PR debe incluir en el título el ID Jira: `GEO-US04: Upload de fotos con extracción EXIF`.

## 4.4 Comunicación

- **Canal principal:** [Canal Slack/Teams — pendiente de definir]
- **Bloqueos:** Comunicar inmediatamente al equipo, no esperar al Daily.
- **Decisiones técnicas:** Documentar cuando sean significativas y actualizar la documentación en `docs/`.

## 4.5 Documentación

- Todo cambio relevante se refleja en `docs/`.
- La documentación se actualiza como parte del Definition of Done.
- Los IDs Jira son la fuente de verdad para trazabilidad en commits, PRs y documentación.

---

# 5. Definition of Done

Ver [definition-of-done_v1.0.md](../08_calidad_y_pruebas/definition-of-done_v1.0.md)

---

# 6. Definition of Ready

Ver [definition-of-ready_v1.0.md](../06_backlog-tecnico/definition-of-ready_v1.0.md)

---

# 7. Nomenclatura Jira

La nomenclatura Jira es la fuente de verdad para toda trazabilidad en el proyecto.

| Tipo | Formato | Ejemplo |
|------|---------|---------|
| Épica | GEO-EXX | GEO-E01, GEO-E02 |
| Historia de usuario | GEO-USXX | GEO-US01, GEO-US04 |
| Tarea técnica | GEO-TXX | GEO-T01, GEO-T41 |
| Subtarea | GEO-TXX-N | GEO-T01-1, GEO-T41-2 |
| Bug | GEO-BUGXX | GEO-BUG01 |

Todos los commits, PRs, planes de sprint y documentos de backlog referencian estos IDs.

---

# 8. Herramientas

| Categoría | Herramienta | Notas |
|-----------|------------|-------|
| IDE | Visual Studio 2022 / VS Code | Con extensión MAUI y MudBlazor |
| Repositorio | Git | Hosted en GitHub |
| CI/CD | GitHub Actions | Pipeline de build, test y APK |
| Tracking | Jira | Proyecto GeoFoto, prefijo GEO |
| UI Components | MudBlazor | Instalado en GeoFoto.Shared |
| Mapas | Leaflet.js | Vía JS Interop (IJSRuntime) |
| Base de datos servidor | SQL Server | Con EF Core |
| Base de datos local | SQLite | Vía sqlite-net-pcl en MAUI |
| Testing | xUnit + Moq | SQLite in-memory para tests |

---

# 9. Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |

---

**Fin del documento**
