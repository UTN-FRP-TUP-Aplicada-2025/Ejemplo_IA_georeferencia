# Metodología Jira

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** jira-metodologia_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Propósito

Este documento establece cómo se configura y opera Jira para el seguimiento del proyecto GeoFoto. Define la estructura del proyecto en Jira, la nomenclatura de todos los ítems, el flujo de trabajo, las convenciones de commits y ramas, y los procedimientos operativos del equipo en cada ceremonia Scrum.

---

## 2. Configuración del Proyecto en Jira

| Parámetro | Valor |
|-----------|-------|
| Tipo de proyecto | Scrum |
| Nombre del proyecto | GeoFoto |
| Clave del proyecto (Project Key) | GEO |
| Metodología | Scrum con sprints de 2 semanas |
| Duración de sprint | 14 días calendario |
| Story Points | Habilitados (escala Fibonacci: 1, 2, 3, 5, 8, 13, 21) |
| Componentes | Api / Shared / Web / Mobile / DevOps / Testing |
| Versiones (Releases) | v1.0-alpha (Sprint 01–03), v1.0-beta (Sprint 04–05), v1.0 (Sprint 06) |

---

## 3. Jerarquía de Ítems en Jira

### 3.1 Diagrama de jerarquía

```
Épica (GEO-Exx)
  └── Historia de Usuario (GEO-USxx)
        └── Tarea Técnica (GEO-Txx)
              └── Subtarea (GEO-Txx-N)
  Bug (GEO-BUGxx)  ← puede colgar de una Historia o ser independiente
```

### 3.2 Tipos de ítem

| Tipo en Jira | Prefijo | Ejemplo | Descripción |
|-------------|---------|---------|-------------|
| Epic | GEO-E + 2 dígitos | GEO-E01 | Capacidad grande de negocio que abarca semanas o meses de trabajo |
| Story | GEO-US + 2 dígitos | GEO-US04 | Funcionalidad entregable dentro de un sprint |
| Task | GEO-T + 2 dígitos | GEO-T12 | Tarea técnica hija de una Story |
| Sub-task | GEO-T + 2 dígitos + -N | GEO-T12-1 | Subtarea dentro de una Task (cuando se requiere descomposición adicional) |
| Bug | GEO-BUG + 2 dígitos | GEO-BUG01 | Defecto encontrado en testing o producción |

---

## 4. Épicas del Proyecto

### GEO-E01 — Fundaciones del proyecto

| Campo | Valor |
|-------|-------|
| ID Jira | GEO-E01 |
| Color sugerido | Azul |
| Descripción | Configuración inicial de la solución .NET 10 con los 4 proyectos (Api, Web, Shared, Mobile), base de datos SQL Server con EF Core y layout MudBlazor compartido. Establece la infraestructura base sobre la que se construye el resto del sistema. |
| Historias | GEO-US01 (5 pts), GEO-US02 (8 pts), GEO-US03 (3 pts) |
| Sprint(s) | Sprint 01 |

### GEO-E02 — Captura y visualización online

| Campo | Valor |
|-------|-------|
| ID Jira | GEO-E02 |
| Color sugerido | Verde |
| Descripción | Flujo completo de captura de fotos desde la web con extracción EXIF, visualización de markers en mapa Leaflet.js, popup de detalle, edición de puntos, listado con filtros y eliminación con confirmación. Constituye el CRUD online del sistema. |
| Historias | GEO-US04 (8 pts), GEO-US05 (5 pts), GEO-US06 (5 pts), GEO-US07 (3 pts), GEO-US08 (3 pts) |
| Sprint(s) | Sprint 02, Sprint 03 |

### GEO-E03 — App móvil MAUI Android

| Campo | Valor |
|-------|-------|
| ID Jira | GEO-E03 |
| Color sugerido | Naranja |
| Descripción | Configuración de GeoFoto.Mobile como app .NET MAUI Hybrid con BlazorWebView, integración del mapa Leaflet en WebView Android, permisos del sistema operativo y captura fotográfica con cámara nativa vía MediaPicker. |
| Historias | GEO-US09 (8 pts), GEO-US10 (8 pts) |
| Sprint(s) | Sprint 03 |

### GEO-E04 — Motor offline-first (SQLite)

| Campo | Valor |
|-------|-------|
| ID Jira | GEO-E04 |
| Color sugerido | Rojo |
| Descripción | Persistencia local con SQLite (sqlite-net-pcl), tabla SyncQueue para el Outbox Pattern, escritura offline-first de puntos y fotos, indicador de pendientes con MudBadge, y experiencia transparente offline/online con IConnectivityService. |
| Historias | GEO-US11 (13 pts), GEO-US12 (5 pts), GEO-US13 (5 pts) |
| Sprint(s) | Sprint 04 |

### GEO-E05 — Motor de sincronización

| Campo | Valor |
|-------|-------|
| ID Jira | GEO-E05 |
| Color sugerido | Morado |
| Descripción | Motor de sincronización automática con push queue FIFO, pull delta, backoff exponencial (5s→30s→5min), sincronización manual, historial de operaciones y resolución de conflictos por Last-Write-Wins comparando timestamps UpdatedAt. |
| Historias | GEO-US14 (13 pts), GEO-US15 (5 pts), GEO-US16 (5 pts), GEO-US17 (8 pts) |
| Sprint(s) | Sprint 05 |

### GEO-E06 — Calidad, UX y deploy productivo

| Campo | Valor |
|-------|-------|
| ID Jira | GEO-E06 |
| Color sugerido | Gris |
| Descripción | Pipeline CI/CD con GitHub Actions (build, test, APK), suite de tests xUnit para el motor de sync con cobertura ≥ 85%, marker clustering con Leaflet.markercluster, paginación y optimización de queries para 100+ puntos. |
| Historias | GEO-US18 (8 pts), GEO-US19 (8 pts), GEO-US20 (5 pts) |
| Sprint(s) | Sprint 06 |

---

## 5. Carga de Historias de Usuario en Jira

### 5.1 Campos a completar

| Campo Jira | Valor a ingresar | Ejemplo |
|-----------|-----------------|---------|
| Issue Type | Story | Story |
| Summary | Título corto orientado al usuario | "Subir fotos desde la web y verlas como markers en el mapa" |
| Description | Formato "Como…, quiero…, para…" + criterios BDD | Ver template abajo |
| Epic Link | ID de la épica padre | GEO-E02 |
| Sprint | Nombre del sprint asignado | GEO Sprint 02 |
| Story Points | Estimación en Fibonacci | 8 |
| Priority | Highest / High / Medium / Low | Highest |
| Components | Componente(s) del sistema afectados | Api, Shared |
| Fix Version | Release objetivo | v1.0-alpha |
| Labels | Prioridad MoSCoW | Must |

### 5.2 Template de Description para una Story

```
*Como* [rol], *quiero* [acción], *para* [beneficio].

*Criterios de Aceptación:*

*CA-01 — [Título del criterio]:*
*Dado* [contexto]
*Cuando* [acción]
*Entonces* [resultado esperado]

*CA-02 — [Título del criterio]:*
*Dado* [contexto]
*Cuando* [acción]
*Entonces* [resultado esperado]

*Notas técnicas:*
- [Restricción o consideración técnica relevante]
```

### 5.3 Ejemplo completado: GEO-US04

```
*Como* supervisor, *quiero* subir fotos desde la web y verlas como markers
en el mapa, *para* registrar puntos desde escritorio.

*Criterios de Aceptación:*

*CA-01 — Carga de fotografía con geolocalización:*
*Dado* que el supervisor se encuentra en la pantalla del mapa en la
aplicación web
*Cuando* seleccione una ubicación en el mapa y suba una fotografía
(JPEG/PNG, máx. 10 MB)
*Entonces* se creará un punto geográfico con las coordenadas seleccionadas
y la foto se almacenará asociada al punto.

*CA-02 — Visualización de markers en el mapa:*
*Dado* que existen puntos geográficos con fotografías registradas en la
base de datos
*Cuando* se cargue la vista de mapa
*Entonces* cada punto se representará como un marker en la posición
correspondiente del mapa Leaflet.

*CA-03 — Validación de formato y tamaño de archivo:*
*Dado* que el supervisor intenta subir un archivo
*Cuando* el archivo no sea JPEG ni PNG, o supere los 10 MB
*Entonces* se mostrará un mensaje de error descriptivo y la operación se
rechazará sin crear el punto.

*Notas técnicas:*
- Endpoint: POST /api/puntos con multipart form-data
- Extracción de coordenadas EXIF con MetadataExtractor
- Componentes: MudFileUpload, Leaflet.js vía IJSRuntime
- Límite: 10 MB por archivo, formatos JPEG y PNG
```

---

## 6. Carga de Tareas Técnicas en Jira

### 6.1 Campos a completar

| Campo Jira | Valor a ingresar |
|-----------|-----------------|
| Issue Type | Task |
| Summary | Descripción técnica concreta (verbo + objeto) |
| Parent | ID de la Story padre |
| Original Estimate | Horas estimadas (ej: 3h) |
| Components | Componente del sistema (Api, Shared, Web, Mobile, DevOps, Testing) |
| Assignee | Desarrollador responsable |
| Priority | Heredado de la Story o ajustado según criticidad |

### 6.2 Ejemplo completado: GEO-T13

| Campo | Valor |
|-------|-------|
| Issue Type | Task |
| Summary | GEO-T13 — Crear SubirFotos.razor con MudFileUpload |
| Parent | GEO-US04 |
| Original Estimate | 3h |
| Components | Shared |
| Priority | Highest |

**Description:**

```
Crear el componente SubirFotos.razor en GeoFoto.Shared con las siguientes
características:
- MudFileUpload para selección de archivos múltiples (JPEG/PNG)
- Validación de formato y tamaño (máx. 10 MB) en cliente
- Previsualización de imágenes seleccionadas
- Envío a POST /api/fotos/upload vía HttpClient
- Feedback visual con MudProgressLinear durante la subida
- MudSnackbar con resultado de la operación

Criterio de Done: el componente compila, permite seleccionar y subir fotos,
y muestra errores de validación cuando corresponde.
```

---

## 7. Configuración de Sprints en Jira

| Nombre en Jira | Sprint Goal | Fechas | Stories asignadas | Pts |
|---------------|-------------|--------|-------------------|-----|
| GEO Sprint 01 | Infraestructura base funcional: solución .NET 10, BD SQL Server con migraciones aplicadas, layout MudBlazor operativo | 2026-04-20 → 2026-05-03 | GEO-US01, GEO-US02, GEO-US03 | 16 |
| GEO Sprint 02 | Flujo completo online: subir fotos con EXIF, ver markers en mapa Leaflet, popup de detalle, lista de puntos | 2026-05-04 → 2026-05-17 | GEO-US04, GEO-US05, GEO-US06, GEO-US07 | 21 |
| GEO Sprint 03 | CRUD completo + app Android funcional: eliminación de puntos con confirmación, app MAUI con mapa y captura desde cámara | 2026-05-18 → 2026-05-31 | GEO-US08, GEO-US09, GEO-US10 | 19 |
| GEO Sprint 04 | Motor offline-first: datos en SQLite, indicador de pendientes, experiencia transparente offline/online | 2026-06-01 → 2026-06-14 | GEO-US11, GEO-US12, GEO-US13 | 23 |
| GEO Sprint 05 | Motor de sincronización completo: sync automática, sync manual, historial de operaciones, conflictos Last-Write-Wins | 2026-06-15 → 2026-06-28 | GEO-US14, GEO-US15, GEO-US16, GEO-US17 | 31 |
| GEO Sprint 06 | Producto listo para entrega: pipeline CI/CD, tests del motor de sync con cobertura ≥ 85%, performance con 100+ puntos | 2026-06-29 → 2026-07-12 | GEO-US18, GEO-US19, GEO-US20 | 21 |

**Velocidad promedio planificada:** 21.8 pts/sprint  
**Total del proyecto:** 131 story points en 6 sprints (12 semanas)

---

## 8. Flujo de Trabajo (Workflow)

### 8.1 Diagrama de estados

```
To Do → In Progress → In Review → Done
            ↓               ↓
          Blocked        Rejected → In Progress
```

### 8.2 Definición de estados

| Estado | Quién lo mueve | Condición |
|--------|---------------|-----------|
| To Do | PO / SM | Al crear el ítem o al inicio del sprint |
| In Progress | Desarrollador | Al empezar a trabajar en la tarea |
| Blocked | Desarrollador | Al encontrar un impedimento — notificar inmediatamente al SM |
| In Review | Desarrollador | Al abrir el Pull Request en GitHub |
| Rejected | Reviewer | Si el PR no cumple el DoD — vuelve a In Progress con comentarios |
| Done | Reviewer / SM | PR mergeado a `develop` + criterios de aceptación verificados |

### 8.3 Reglas de transición

- Solo el desarrollador asignado puede mover de To Do a In Progress.
- Un ítem en Blocked debe tener un comentario describiendo el impedimento.
- La transición a In Review requiere que exista un PR abierto vinculado al ítem.
- La transición a Done requiere aprobación del PR y verificación de los criterios de aceptación BDD.
- Un ítem Rejected vuelve a In Progress y mantiene los comentarios del reviewer.

---

## 9. Convención de Commits y Vinculación con Jira

### 9.1 Formato de commits

```
tipo(GEO-USxx): descripción en presente, imperativo, lowercase
```

**Tipos válidos:** `feat` / `fix` / `test` / `docs` / `chore` / `refactor` / `perf` / `style`

**Ejemplos con historias reales del proyecto:**

```
feat(GEO-US04): agrega endpoint POST /api/fotos/upload con extracción EXIF
fix(GEO-US11): corrige escritura en SQLite cuando el directorio no existe
test(GEO-US14): agrega tests de integración del SyncService ciclo completo
docs(GEO-E01): actualiza README con instrucciones de setup Android
chore(GEO-US18): configura pipeline CI/CD en GitHub Actions
refactor(GEO-US06): extrae lógica de edición a EditarPuntoService
perf(GEO-US20): optimiza queries con AsNoTracking para listado de puntos
```

### 9.2 Convención de ramas

```
feature/GEO-USxx-descripcion-corta
bugfix/GEO-BUGxx-descripcion-corta
hotfix/GEO-BUGxx-descripcion-corta
```

**Ejemplos con historias reales del proyecto:**

```
feature/GEO-US01-estructura-solucion
feature/GEO-US04-subir-fotos-mapa
feature/GEO-US11-sqlite-offline
feature/GEO-US14-sync-automatica
feature/GEO-US18-pipeline-cicd
bugfix/GEO-BUG01-pantalla-blanca-maui
```

### 9.3 Integración Jira-GitHub

Si se configura la integración entre Jira y GitHub:

- Los commits con el ID del ítem (ej: `GEO-US04`) aparecen automáticamente en la tarjeta Jira.
- Los PRs con `GEO-US04` en el título vinculan la tarjeta.
- Incluir `Closes GEO-US04` en la descripción del PR para cerrar la Story automáticamente al mergear.

---

## 10. Configuración del Tablero Scrum

### 10.1 Columnas del tablero

| Columna | Estados incluidos | Límite WIP sugerido |
|---------|------------------|-------------------|
| To Do | To Do | Sin límite |
| In Progress | In Progress, Blocked | 3 por desarrollador |
| In Review | In Review | Sin límite |
| Done | Done, Rejected | Sin límite |

### 10.2 Swimlanes

Configurar swimlanes por épica para agrupar visualmente las tarjetas del sprint:

| Swimlane | Filtro |
|----------|--------|
| GEO-E01 — Fundaciones | Epic Link = GEO-E01 |
| GEO-E02 — Captura online | Epic Link = GEO-E02 |
| GEO-E03 — MAUI Android | Epic Link = GEO-E03 |
| GEO-E04 — Offline-first | Epic Link = GEO-E04 |
| GEO-E05 — Sincronización | Epic Link = GEO-E05 |
| GEO-E06 — Calidad y deploy | Epic Link = GEO-E06 |

### 10.3 Filtros de vista rápida

| Nombre del filtro | JQL |
|------------------|-----|
| Mis tareas del sprint | `assignee = currentUser() AND sprint in openSprints()` |
| Épica offline-first | `"Epic Link" = GEO-E04` |
| Épica sincronización | `"Epic Link" = GEO-E05` |
| Solo componente Mobile | `component = Mobile` |
| Solo componente Api | `component = Api` |
| Pendientes de review | `status = "In Review" ORDER BY updated ASC` |
| Bugs abiertos | `type = Bug AND status != Done` |
| Stories sin estimar | `type = Story AND "Story Points" is EMPTY` |

---

## 11. Procedimiento de Sprint Planning en Jira

El Sprint Planning se ejecuta el primer día de cada sprint (duración: 2 horas). Pasos:

1. **El PO presenta las Stories candidatas.** Mueve las Stories priorizadas del Product Backlog al Sprint en el board de Jira.

2. **El equipo revisa cada Story contra el DoR.** Se verifica el checklist de Definition of Ready (ver `definition-of-ready_v1.0.md`). Las Stories que no cumplan se devuelven al backlog.

3. **Se estiman Story Points.** Si una Story no está estimada, el equipo realiza Planning Poker y registra el valor en el campo Story Points.

4. **Se descomponen en Tasks.** Para cada Story comprometida, se crean las tareas técnicas (GEO-TXX) como sub-ítems con estimación en horas. Las tareas se extraen del `backlog-tecnico_v1.0.md`.

5. **El SM inicia el sprint.** Configura la fecha de inicio, fecha de fin (14 días) y el Sprint Goal en Jira.

6. **Los desarrolladores se asignan tareas.** Cada miembro toma tareas al empezar a trabajar (pull system), no se pre-asignan todas al inicio del sprint.

---

## 12. Métricas y Reportes en Jira

| Ceremonia | Reporte Jira | Qué observar |
|-----------|-------------|-------------|
| Daily Standup | Board view + Burndown Chart | Progreso diario, ítems bloqueados, distribución de trabajo |
| Sprint Review | Velocity Chart + Sprint Report | Stories completadas vs. comprometidas, puntos entregados |
| Sprint Retrospective | Cycle Time Report + Control Chart | Tiempo promedio por Story, cuellos de botella, variabilidad |
| Backlog Refinement | Backlog view (filtro: Sin estimar) | Stories sin Story Points, sin épica asignada, sin criterios BDD |

### Reportes adicionales recomendados

| Reporte | Frecuencia | Propósito |
|---------|-----------|-----------|
| Cumulative Flow Diagram | Semanal | Detectar cuellos de botella entre estados |
| Release Burndown | Por release | Progreso hacia v1.0-alpha, v1.0-beta, v1.0 |
| Resolution Time | Post-sprint | Tiempo promedio de resolución de bugs |

---

## 13. CSV de Importación Masiva (Formato Jira)

Jira permite importar ítems masivamente desde un archivo CSV mediante **Project Settings → External System Import → CSV**.

### 13.1 Encabezados del CSV

```
Issue Type,Summary,Description,Epic Name,Epic Link,Sprint,Story Points,Priority,Component,Fix Version,Labels,Status
```

### 13.2 Primeras filas de ejemplo con datos reales del backlog

```csv
Issue Type,Summary,Description,Epic Name,Epic Link,Sprint,Story Points,Priority,Component,Fix Version,Labels,Status
Epic,"GEO-E01 — Fundaciones del proyecto","Configuración inicial de la solución .NET 10 con 4 proyectos, BD SQL Server y layout MudBlazor.","Fundaciones del proyecto",,,,High,Api,v1.0-alpha,,To Do
Epic,"GEO-E02 — Captura y visualización online","Flujo completo de captura de fotos con EXIF, mapa Leaflet, CRUD de puntos.","Captura y visualización online",,,,High,"Api, Shared",v1.0-alpha,,To Do
Story,"GEO-US01 — Estructura de la solución con los 4 proyectos compilando","Como desarrollador, quiero la estructura de la solución con los 4 proyectos compilando, para tener la base sobre la que construir.",,"GEO-E01","GEO Sprint 01",5,Highest,Api,v1.0-alpha,Must,To Do
Story,"GEO-US02 — Base de datos SQL Server con EF Core y migrations iniciales","Como desarrollador, quiero la base de datos SQL Server configurada con EF Core y las migrations iniciales, para persistir datos del servidor.",,"GEO-E01","GEO Sprint 01",8,Highest,Api,v1.0-alpha,Must,To Do
Story,"GEO-US03 — MudBlazor integrado en GeoFoto.Shared","Como desarrollador, quiero MudBlazor integrado en GeoFoto.Shared con tema configurado, para tener la librería UI disponible en todos los proyectos.",,"GEO-E01","GEO Sprint 01",3,High,Shared,v1.0-alpha,Must,To Do
Story,"GEO-US04 — Subir fotos desde la web y verlas como markers en el mapa","Como supervisor, quiero subir fotos desde la web y verlas como markers en el mapa, para registrar puntos desde escritorio.",,"GEO-E02","GEO Sprint 02",8,Highest,"Api, Shared",v1.0-alpha,Must,To Do
Story,"GEO-US05 — Click en marker y ver popup con fotos y datos del punto","Como supervisor, quiero hacer click en un marker y ver el popup con fotos y datos del punto, para acceder al detalle sin salir del mapa.",,"GEO-E02","GEO Sprint 02",5,High,"Api, Shared",v1.0-alpha,Should,To Do
Task,"GEO-T01 — Crear solución .sln y proyecto GeoFoto.Api","Crear solución GeoFoto.sln con los 4 proyectos: Api, Web, Shared, Mobile.",,,"GEO Sprint 01",,Highest,Api,,,To Do
Task,"GEO-T02 — Crear proyecto GeoFoto.Web","Configurar GeoFoto.Web como Blazor Web App InteractiveServer.",,,"GEO Sprint 01",,Highest,Web,,,To Do
```

> **Nota:** Completar el CSV con las 20 historias y 88 tareas siguiendo el `product-backlog_v1.0.md` y `backlog-tecnico_v1.0.md` como fuente de datos. Las filas de Task deben vincularse a su Story padre mediante el campo Parent en la pantalla de importación de Jira (no se incluye en CSV estándar; se mapea durante la importación).

---

## 14. Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
