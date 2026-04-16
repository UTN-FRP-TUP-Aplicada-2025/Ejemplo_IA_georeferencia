# Backlog Técnico

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** backlog-tecnico_v1.0.md  
**Versión:** 1.1  
**Estado:** Activo  
**Fecha:** 2026-04-16  
**Autor:** Equipo Técnico  

---

## 1. Propósito

Este documento descompone cada historia de usuario del Product Backlog en tareas técnicas implementables. Cada tarea representa una unidad de trabajo atómica asignable a un desarrollador, con estimación en horas y componente destino claramente identificado.

El objetivo es proporcionar al equipo de desarrollo una guía detallada de implementación que permita:

- Planificar sprints con granularidad adecuada.  
- Rastrear el progreso de cada historia de usuario mediante sus tareas constituyentes.  
- Identificar dependencias técnicas entre componentes.  
- Facilitar la asignación de trabajo según especialización.  

---

## 2. Convenciones

- **ID de tarea:** `GEO-TXX` — numeración global secuencial.  
- **Componente:** `Api` | `Shared` | `Web` | `Mobile` | `DevOps` | `Testing`.  
- **Estimación:** en horas de desarrollo efectivo.  
- **Estado inicial:** `To Do`.  
- Las tareas se organizan por épica y por historia de usuario.  
- Cada historia de usuario indica sus story points entre paréntesis.  

---

# ÉPICA GEO-E01 — Fundaciones del proyecto

## GEO-US01 — Estructura de la solución (5 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T01 | Crear solución GeoFoto.sln con 4 proyectos (Api, Shared, Web, Mobile) | DevOps | 2h | To Do |
| GEO-T02 | Configurar referencias entre proyectos | DevOps | 1h | To Do |
| GEO-T03 | Configurar Program.cs de GeoFoto.Api con Swagger | Api | 2h | To Do |
| GEO-T04 | Configurar Program.cs de GeoFoto.Web con InteractiveServer | Web | 1h | To Do |

---

## GEO-US02 — Base de datos SQL Server con EF Core (8 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T05 | Instalar EF Core y SQL Server provider | Api | 1h | To Do |
| GEO-T06 | Crear GeoFotoDbContext con DbSet<Punto> y DbSet<Foto> | Api | 2h | To Do |
| GEO-T07 | Crear migration inicial con tablas Puntos y Fotos | Api | 1h | To Do |
| GEO-T08 | Configurar connection string por appsettings.json | Api | 1h | To Do |
| GEO-T09 | Verificar migration apply y seed de datos de prueba | Api | 2h | To Do |

---

## GEO-US03 — MudBlazor integrado (3 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T10 | Instalar MudBlazor NuGet en GeoFoto.Shared | Shared | 1h | To Do |
| GEO-T11 | Configurar MudThemeProvider y tema personalizado | Shared | 1h | To Do |
| GEO-T12 | Crear layout base con MudAppBar + MudNavMenu + MudDrawer | Shared | 2h | To Do |

---

# ÉPICA GEO-E02 — Captura y visualización online

## GEO-US04 — Subir fotos y ver markers (8 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T13 | Crear SubirFotos.razor con MudFileUpload | Shared | 3h | To Do |
| GEO-T14 | Crear endpoint POST /api/fotos/upload multipart | Api | 3h | To Do |
| GEO-T15 | Integrar MetadataExtractor para extracción EXIF | Api | 2h | To Do |
| GEO-T16 | Crear PuntosController con CRUD básico | Api | 2h | To Do |
| GEO-T17 | Crear Mapa.razor con integración Leaflet.js vía IJSRuntime | Shared | 4h | To Do |
| GEO-T18 | Crear leaflet-interop.js con funciones initMap y addMarkers | Shared | 2h | To Do |
| GEO-T19 | Conectar SubirFotos con API y mostrar resultados en MudTable | Shared | 2h | To Do |

---

## GEO-US05 — Popup con fotos y datos (5 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T20 | Crear MarkerPopup.razor con MudDialog | Shared | 2h | To Do |
| GEO-T21 | Crear FotoCarousel.razor para imágenes del punto | Shared | 2h | To Do |
| GEO-T22 | Implementar onMarkerClick en leaflet-interop.js | Shared | 2h | To Do |
| GEO-T23 | Crear endpoint GET /api/fotos/{puntoId} | Api | 1h | To Do |

---

## GEO-US06 — Editar punto (5 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T24 | Crear DetallePunto.razor con MudTextField nombre/descripción | Shared | 2h | To Do |
| GEO-T25 | Crear endpoint PUT /api/puntos/{id} | Api | 1h | To Do |
| GEO-T26 | Conectar formulario de edición con API | Shared | 2h | To Do |

---

## GEO-US07 — Lista de puntos (3 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T27 | Crear ListaPuntos.razor con MudTable | Shared | 2h | To Do |
| GEO-T28 | Agregar filtros de búsqueda por nombre | Shared | 1h | To Do |
| GEO-T29 | Agregar MudChip para SyncStatus con colores | Shared | 1h | To Do |

---

## GEO-US08 — Eliminar punto (3 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T30 | Crear MudDialog de confirmación de eliminación | Shared | 1h | To Do |
| GEO-T31 | Crear endpoint DELETE /api/puntos/{id} con cascade | Api | 1h | To Do |
| GEO-T32 | Eliminar archivos físicos del servidor al borrar | Api | 1h | To Do |

---

# ÉPICA GEO-E03 — App móvil MAUI Android

## GEO-US09 — App Android con mapa (8 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T33 | Configurar GeoFoto.Mobile con BlazorWebView | Mobile | 2h | To Do |
| GEO-T34 | Registrar servicios HTTP y DI en MauiProgram.cs | Mobile | 1h | To Do |
| GEO-T35 | Configurar permisos Android (cámara, ubicación, internet) | Mobile | 1h | To Do |
| GEO-T36 | Verificar Leaflet.js funcional en WebView Android | Mobile | 2h | To Do |
| GEO-T37 | Crear MudAppBar diferenciado para mobile | Shared | 1h | To Do |

---

## GEO-US10 — Captura con cámara (8 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T38 | Implementar MudFab con icono cámara para captura | Shared | 1h | To Do |
| GEO-T39 | Implementar MediaPicker.CapturePhotoAsync() | Mobile | 3h | To Do |
| GEO-T40 | Conectar captura con POST /api/fotos/upload | Mobile | 2h | To Do |

---

# ÉPICA GEO-E04 — Motor offline-first (SQLite)

## GEO-US11 — Guardado offline (13 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T41 | Instalar sqlite-net-pcl en GeoFoto.Mobile | Mobile | 1h | To Do |
| GEO-T42 | Diseñar e implementar ILocalDbService | Mobile | 4h | To Do |
| GEO-T43 | Crear migrations SQLite automáticas al iniciar app | Mobile | 2h | To Do |
| GEO-T44 | Crear tabla SyncQueue con todos los campos | Mobile | 2h | To Do |
| GEO-T45 | Modificar SubirFotos para escritura en SQLite primero | Shared/Mobile | 3h | To Do |
| GEO-T46 | Modificar captura de cámara para escritura en SQLite | Mobile | 3h | To Do |
| GEO-T47 | Implementar IConnectivityService | Mobile | 2h | To Do |
| GEO-T48 | Crear SyncStatusBadge.razor con MudBadge | Shared | 2h | To Do |
| GEO-T49 | Tests unitarios de ILocalDbService (SQLite in-memory) | Testing | 3h | To Do |
| GEO-T50 | Verificar funcionamiento en modo avión (Android) | Testing | 2h | To Do |

---

## GEO-US12 — Indicador de pendientes (5 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T51 | Conectar SyncStatusBadge con conteo de SyncQueue | Mobile | 2h | To Do |
| GEO-T52 | Crear EstadoSync.razor con MudCards de métricas | Shared | 3h | To Do |
| GEO-T53 | Actualizar badge en tiempo real al capturar datos | Shared | 2h | To Do |

---

## GEO-US13 — Experiencia transparente offline/online (5 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T54 | Implementar IDataService con abstracción local/remoto | Mobile | 3h | To Do |
| GEO-T55 | Cargar mapa desde datos locales si no hay red | Mobile | 2h | To Do |
| GEO-T56 | Indicador visual de estado de conexión en MudAppBar | Shared | 1h | To Do |

---

# ÉPICA GEO-E05 — Motor de sincronización

## GEO-US14 — Sync automática (13 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T57 | Implementar ISyncService con push queue | Mobile | 4h | To Do |
| GEO-T58 | Implementar procesamiento FIFO de SyncQueue | Mobile | 3h | To Do |
| GEO-T59 | Integrar ConnectivityChanged trigger | Mobile | 2h | To Do |
| GEO-T60 | Implementar backoff exponencial (5s/30s/5min) | Mobile | 2h | To Do |
| GEO-T61 | Implementar pull delta (GET /api/sync/delta) | Mobile | 3h | To Do |
| GEO-T62 | Crear endpoint GET /api/sync/delta en API | Api | 2h | To Do |
| GEO-T63 | Crear endpoint POST /api/sync/batch en API | Api | 3h | To Do |
| GEO-T64 | MudSnackbar de notificación de sync completada | Shared | 1h | To Do |

---

## GEO-US15 — Sync manual (5 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T65 | Agregar MudButton "Sincronizar ahora" en EstadoSync | Shared | 1h | To Do |
| GEO-T66 | Implementar SyncService.SyncNowAsync() | Mobile | 2h | To Do |
| GEO-T67 | Mostrar MudProgressLinear durante sync manual | Shared | 1h | To Do |

---

## GEO-US16 — Historial de sync (5 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T68 | Crear MudTable de historial en EstadoSync | Shared | 2h | To Do |
| GEO-T69 | Almacenar resultado de cada operación en SyncQueue | Mobile | 2h | To Do |
| GEO-T70 | Filtros por estado y fecha en historial | Shared | 1h | To Do |

---

## GEO-US17 — Resolución de conflictos LWW (8 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T71 | Implementar comparación UpdatedAt en SyncService | Mobile | 3h | To Do |
| GEO-T72 | Implementar merge de campos por Last-Write-Wins | Mobile | 3h | To Do |
| GEO-T73 | Registrar conflictos en log de auditoría | Mobile | 1h | To Do |
| GEO-T74 | Mostrar conflictos resueltos en EstadoSync MudTable | Shared | 1h | To Do |

---

# ÉPICA GEO-E06 — Calidad, UX y deploy productivo

## GEO-US18 — Pipeline CI/CD (8 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T75 | Crear workflow GitHub Actions para build | DevOps | 2h | To Do |
| GEO-T76 | Agregar step de tests con reporte | DevOps | 2h | To Do |
| GEO-T77 | Agregar step de build APK Android Debug | DevOps | 2h | To Do |
| GEO-T78 | Configurar artifacts (APK + test report) | DevOps | 1h | To Do |
| GEO-T79 | Documentar pipeline en docs/09_devops | DevOps | 1h | To Do |

---

## GEO-US19 — Tests del motor de sync (8 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T80 | Crear proyecto GeoFoto.Tests con xUnit | Testing | 1h | To Do |
| GEO-T81 | Tests unitarios de SyncService (push queue) | Testing | 3h | To Do |
| GEO-T82 | Tests unitarios de SyncService (pull delta) | Testing | 2h | To Do |
| GEO-T83 | Tests de conflicto Last-Write-Wins | Testing | 2h | To Do |
| GEO-T84 | Tests de integración con SQLite in-memory | Testing | 3h | To Do |

---

## GEO-US20 — Performance con 100+ puntos (5 pts)

| ID Jira | Título | Componente | Estimación | Estado |
|---------|--------|------------|------------|--------|
| GEO-T85 | Implementar marker clustering en Leaflet | Shared | 2h | To Do |
| GEO-T86 | Paginación en ListaPuntos MudTable | Shared | 1h | To Do |
| GEO-T87 | Test de carga con 150 puntos de prueba | Testing | 2h | To Do |
| GEO-T88 | Optimización de queries EF Core para listado | Api | 1h | To Do |

---

# ÉPICA GEO-E07 — UX Avanzado Mobile + Web

## GEO-US20b — Centrar mapa en posición GPS (5 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T89  | Agregar FAB GPS en Mapa.razor con lógica de centrado (setView 15) | Shared | 2h | To Do |
| GEO-T90  | Implementar timeout 10s en GetCurrentLocationAsync | Mobile | 2h | To Do |
| GEO-T91  | ESC-02: mostrar div de error con botón Reintentar si initMap falla | Shared | 2h | To Do |
| GEO-T92  | ESC-03: manejar 3 casos de permiso GPS (dialog/snackbar/mapa sin GPS) | Mobile | 3h | To Do |

---

## GEO-US21 — Posición propia en mapa (5 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T93  | updateUserPosition(lat,lng) en leaflet-interop.js con circleMarker pulsante | Shared | 2h | To Do |
| GEO-T94  | clearUserPosition() en leaflet-interop.js | Shared | 1h | To Do |
| GEO-T95  | Polling de posición cada 5s en Mapa.razor con clearUserPosition al perder GPS | Shared | 2h | To Do |

---

## GEO-US22 — Radio visual del marker (8 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T96  | showMarkerRadius/hideMarkerRadius/updateMarkerRadius en leaflet-interop.js | Shared | 3h | To Do |
| GEO-T97  | MudSlider (10-500m) en MarkerPopup.razor con handler de cambio | Shared | 2h | To Do |
| GEO-T98  | UpdatePuntoRadioAsync(puntoId, radioMetros) en LocalDbService | Mobile | 2h | To Do |
| GEO-T99  | Persistir radio en IPreferencesService y leer al iniciar | Mobile | 1h | To Do |

---

## GEO-US23 — Popup + carrusel + ampliar foto (13 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T100 | Crear/actualizar MarkerPopup.razor (título, descripción, carrusel, botones) | Shared | 3h | To Do |
| GEO-T101 | Crear/actualizar FotoCarousel.razor (prev/next, botón ✕, mensaje sin fotos) | Shared | 3h | To Do |
| GEO-T102 | Crear FotoViewer.razor (fullscreen MudOverlay + comentario editable + ✕) | Shared | 2h | To Do |
| GEO-T103 | Integrar OnMarkerClick JS→Blazor en Mapa.razor para abrir MarkerPopup | Shared | 2h | To Do |
| GEO-T104 | GuardarTituloPuntoAsync en LocalDbService + SyncQueue PendingUpdate | Mobile | 1h | To Do |
| GEO-T105 | GuardarDescripcionPuntoAsync en LocalDbService + SyncQueue | Mobile | 1h | To Do |
| GEO-T106 | GuardarComentarioFotoAsync en LocalDbService + SyncQueue | Mobile | 1h | To Do |

---

## GEO-US24 — Offline-first + sync automático (8 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T107 | Completar SyncService.PushAsync() con Update Punto/Foto y Delete Punto/Foto | Mobile | 4h | To Do |
| GEO-T108 | Implementar SyncService.PullAsync() + ESC-01 (insertar superpuestos sin merge) | Mobile | 3h | To Do |
| GEO-T109 | ConflictResolver.cs (UseRemote/UseLocal + AskUser → MudDialog) | Mobile | 2h | To Do |
| GEO-T110 | GET /api/sync/delta?since={utc} en SyncController con IsDeleted | Api | 2h | To Do |
| GEO-T111 | Actualizar SyncStatusBadge: Pending(nro naranja)/Syncing(spinner)/OK(verde)/Error(rojo) | Shared | 2h | To Do |

---

## GEO-US25 — Quitar foto desde carrusel (5 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T112 | Botón ✕ en FotoCarousel.razor con confirm dialog ("¿Eliminar esta foto?") | Shared | 2h | To Do |
| GEO-T113 | EliminarFotoAsync: IsDeleted=true + SyncQueue PendingDelete | Mobile | 2h | To Do |
| GEO-T114 | Actualizar carrusel reactivamente tras eliminación sin cerrar popup | Shared | 1h | To Do |

---

## GEO-US26 — Ampliar foto fullscreen (3 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T115 | FotoViewer.razor con MudOverlay + pinch-to-zoom CSS/JS | Shared | 2h | To Do |
| GEO-T116 | Mantener índice de carrusel al cerrar FotoViewer | Shared | 1h | To Do |
| GEO-T117 | Integrar FotoViewer en FotoCarousel.razor con tap en imagen | Shared | 1h | To Do |

---

## GEO-US27 — Pantalla sincronización manual (5 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T118 | Crear/actualizar Sincronizacion.razor con MudTable de SyncQueue | Shared | 2h | To Do |
| GEO-T119 | Botón "Sincronizar ahora" con spinner + PushAsync+PullAsync | Shared | 2h | To Do |
| GEO-T120 | Mostrar y actualizar fecha/hora de última sync en pantalla | Shared | 1h | To Do |

---

## GEO-US28 — Lista de markers con búsqueda (8 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T121 | Crear/actualizar ListaPuntos.razor con MudTable y MudChip SyncStatus | Shared | 2h | To Do |
| GEO-T122 | MudTextField de búsqueda con filtrado reactivo en tiempo real | Shared | 1h | To Do |
| GEO-T123 | Tap en fila → navegar mapa (setView) + abrir MarkerPopup | Shared | 2h | To Do |
| GEO-T124 | GetAllPuntosAsync ordenado por nombre en LocalDbService | Mobile | 1h | To Do |

---

## GEO-US29 — Eliminar marker completo (5 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T125 | Botón "Eliminar marker" en MarkerPopup.razor con confirm (N fotos) | Shared | 2h | To Do |
| GEO-T126 | EliminarPuntoConFotosAsync en LocalDbService (soft delete + SyncQueue) | Mobile | 2h | To Do |
| GEO-T127 | removeMarker(puntoId) en leaflet-interop.js | Shared | 1h | To Do |

---

## GEO-US30 — Compartir foto nativa Android (5 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T128 | Botón "Compartir" en FotoCarousel.razor (solo Mobile) | Shared | 1h | To Do |
| GEO-T129 | Implementar MauiShareService con Share.RequestAsync(file) | Mobile | 2h | To Do |
| GEO-T130 | Manejar Share no disponible con MudSnackbar info | Shared | 1h | To Do |

---

## GEO-US31 — Web: paridad funcional con Android (5 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T131 | navigator.geolocation en leaflet-interop.js para web + manejo de denegación | Shared | 2h | To Do |
| GEO-T132 | Mensaje "Permiso de ubicación denegado..." cuando browser deniega | Shared | 1h | To Do |
| GEO-T133 | Verificar componentes Shared (Popup, Carrusel, Viewer) en GeoFoto.Web | Web | 2h | To Do |

---

## GEO-US32 — Descarga zip de fotos del marker (5 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T134 | GET /api/puntos/{id}/fotos/download retorna .zip con fotos nombradas | Api | 3h | To Do |
| GEO-T135 | Botón "Descargar fotos" en MarkerPopup.razor (IsMobile=false, disabled si sin fotos) | Shared | 1h | To Do |
| GEO-T136 | JS Interop para disparar descarga browser desde Blazor | Shared | 1h | To Do |

---

## GEO-US33 — Subir foto desde web al marker (5 pts)

| ID Jira  | Título | Componente | Estimación | Estado |
|----------|--------|------------|------------|--------|
| GEO-T137 | MudFileUpload en MarkerPopup.razor (IsMobile=false) + POST /api/fotos/upload | Shared | 2h | To Do |
| GEO-T138 | Endpoint POST /api/fotos/upload acepta puntoId sin requerir EXIF GPS (ESC-04) | Api | 2h | To Do |
| GEO-T139 | Recargar fotos del punto en carrusel tras subida exitosa | Shared | 1h | To Do |

---

## Resumen por componente

| Componente | Cantidad de tareas | Total horas |
|------------|--------------------|-------------|
| Api | 16 | 27h |
| Shared | 33 | 56h |
| Shared/Mobile | 1 | 3h |
| Mobile | 22 | 51h |
| Web | 1 | 1h |
| DevOps | 6 | 10h |
| Testing | 9 | 18h |
| **Total** | **88** | **166h** |

> **Nota:** Las tareas GEO-T89 a GEO-T139 corresponden a la épica GEO-E07 (Sprint 07-08). Ver sección anterior.

---

## Resumen por épica

| Épica | Historias | Tareas | Horas |
|-------|-----------|--------|-------|
| GEO-E01 — Fundaciones del proyecto | 3 (US01–US03) | 12 (T01–T12) | 18h |
| GEO-E02 — Captura y visualización online | 5 (US04–US08) | 20 (T13–T32) | 37h |
| GEO-E03 — App móvil MAUI Android | 2 (US09–US10) | 8 (T33–T40) | 13h |
| GEO-E04 — Motor offline-first (SQLite) | 3 (US11–US13) | 16 (T41–T56) | 30h |
| GEO-E05 — Motor de sincronización | 4 (US14–US17) | 18 (T57–T74) | 34h |
| GEO-E06 — Calidad, UX y deploy productivo | 3 (US18–US20) | 14 (T75–T88) | 34h |
| **Total** | **20** | **88** | **166h** |

---

## Control de cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Creación inicial del backlog técnico con 88 tareas distribuidas en 6 épicas y 20 historias de usuario. |
| 1.1 | 2026-04-16 | Equipo Técnico | Agregada épica GEO-E07 con 51 tareas (GEO-T89 a GEO-T139) para historias GEO-US20b a GEO-US33 (Sprint 07-08). |

---

**Fin del documento**
