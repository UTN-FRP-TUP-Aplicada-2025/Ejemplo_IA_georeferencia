# Plan de Iteración — Sprint 07

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** plan-iteracion_sprint-07_v1.0.md  
**Versión:** 1.0  
**Estado:** Completado  
**Fecha:** 2026-04-16  
**Autor:** Equipo Técnico

---

## 1. Información del Sprint

| Campo | Valor |
|-------|-------|
| Sprint | 07 |
| Fase | Fase 4 — UX Avanzado Mobile + Web |
| Fecha inicio | 2026-07-13 |
| Fecha fin | 2026-08-23 |
| Duración | 6 semanas (3 sub-sprints de 2 semanas cada uno) |
| Sprint Goal | Los técnicos de campo tienen una experiencia UX completa en Android: GPS FAB, marcador de posición propia, radio visual configurable, carrusel de fotos editable, sincronización offline-first robusta, lista de markers con búsqueda y eliminación de markers. |
| Velocidad planificada | 21 pts/sub-sprint × 3 sub-sprints = 63 pts totales |

### Nota de Capacidad

Sprint 07 contiene ~65 story points de trabajo Must+Should, superando la velocidad histórica de 21 pts/sprint. Se divide en tres sub-sprints de 2 semanas para ejecutar ordenadamente:

| Sub-sprint | Período | Sprint Goal | Pts |
|-----------|---------|-------------|-----|
| Sprint 07A | 2026-07-13 → 2026-07-26 | GPS FAB + posición propia + radio visual + ampliar foto | 21 |
| Sprint 07B | 2026-07-27 → 2026-08-09 | Popup carrusel completo + pantalla sync + quitar foto | 23 |
| Sprint 07C | 2026-08-10 → 2026-08-23 | Offline-first robusto + lista markers + eliminar marker | 21 |

---

## 2. Épicas y User Stories

| Sub-sprint | Épica | US | Título | Puntos | Prioridad |
|-----------|-------|----|--------|--------|-----------|
| 07A | GEO-E07 | GEO-US20b | Centrar mapa en posición GPS (FAB) | 5 | Must |
| 07A | GEO-E07 | GEO-US21 | Marcador de posición propia en el mapa | 5 | Must |
| 07A | GEO-E07 | GEO-US22 | Radio visual configurable por marker | 8 | Must |
| 07A | GEO-E07 | GEO-US26 | Ampliar foto en pantalla completa | 3 | Should |
| 07B | GEO-E07 | GEO-US23 | Popup con carrusel de fotos editable | 13 | Must |
| 07B | GEO-E07 | GEO-US27 | Pantalla de estado de sincronización | 5 | Should |
| 07B | GEO-E07 | GEO-US25 | Quitar foto del carrusel | 5 | Must |
| 07C | GEO-E07 | GEO-US24 | Operación offline-first robusta | 8 | Must |
| 07C | GEO-E07 | GEO-US28 | Lista de markers con búsqueda | 8 | Should |
| 07C | GEO-E07 | GEO-US29 | Eliminar marker desde el mapa | 5 | Must |

**Total Story Points Must:** 44 pts  
**Total Story Points Should:** 21 pts  
**Total Sprint 07:** 65 pts

---

## 3. Objetivo de la Demo

### Demo Sprint 07A

1. **GPS FAB:** El técnico toca el FAB de GPS en el mapa. El mapa se centra en su posición con zoom 15. Si GPS tarda más de 10 segundos, aparece mensaje de error. Si no hay permiso, se muestra MudDialog con opción "Ir a Configuración".
2. **Posición propia:** Un círculo azul pulsante aparece en la posición actual del técnico. Se actualiza cada 5 segundos. Es visualmente distinto de los markers de fotos. Al revocar permiso, el círculo desaparece.
3. **Radio visual:** Al abrir el popup de un marker, un slider (10–500m) controla el radio del círculo gris semitransparente en el mapa. El valor persiste entre sesiones (Preferences + SQLite).
4. **Ampliar foto:** Al tocar una foto en el carrusel, se abre en MudOverlay a pantalla completa con zoom y campo de comentario editable.

### Demo Sprint 07B

1. **Popup carrusel:** Al tocar un marker, se abre MudDialog con título editable, descripción editable, carrusel prev/next con indicador N/M, botón de compartir, botón de eliminar foto individual y botón de añadir foto.
2. **Quitar foto:** Al tocar ✕ en una foto del carrusel, aparece SnackBar de confirmación. Confirma → foto eliminada del carrusel y de SQLite. La operación se encola en SyncQueue.
3. **Pantalla sync:** Menú de navegación → "Sincronización" → vista con: última fecha de sync, resumen (pendientes/errores/total), botón "Sincronizar ahora", tabla de operaciones con estado (pendiente/en progreso/completado/error).

### Demo Sprint 07C

1. **Offline-first robusto:** Activar modo avión → crear punto con foto → ver punto en mapa → badge muestra N pendientes. Restaurar red → sync automático dispara → badge en 0 → punto aparece en servidor. Forzar conflicto LWW → versión más reciente prevalece.
2. **Lista de markers:** Menú → "Lista" → tabla con nombre, fotos, estado y coordenadas. Campo de búsqueda filtra en tiempo real. Paginación de 20 elementos.
3. **Eliminar marker:** Desde popup o lista → botón "Eliminar marker" → confirmación → marker desaparece del mapa → se encola DELETE en SyncQueue → sync → eliminado en servidor.

---

## 4. Descomposición de Tareas

### Sprint 07A

#### GEO-US20b — Centrar mapa en posición GPS (5 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T89 | Agregar FAB GPS en Mapa.razor con ícono my_location | Mobile | 1h | ✅ Done |
| GEO-T90 | Implementar CentrarEnPosicionAsync con timeout 10s y try/catch | Mobile | 2h | ✅ Done |
| GEO-T91 | Implementar pantalla de error ESC-02: div oculto + Reintentar | Mobile | 1h | ✅ Done |
| GEO-T92 | Implementar flujos ESC-03: permiso dialog + snackbar permanente | Mobile | 2h | ✅ Done |

#### GEO-US21 — Marcador de posición propia (5 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T93 | Agregar updateUserPosition(lat, lng) a leaflet-interop.js | JS | 1h | ✅ Done |
| GEO-T94 | Agregar clearUserPosition() a leaflet-interop.js | JS | 0.5h | ✅ Done |
| GEO-T95 | Implementar polling 5s en Mapa.razor con CancellationToken | Mobile | 2h | ✅ Done |
| GEO-T96 | Tests unitarios ESC-03: permiso denegado permanente + snackbar | Testing | 2h | ✅ Done |

#### GEO-US22 — Radio visual configurable (8 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T97 | Agregar showMarkerRadius / hideMarkerRadius / updateMarkerRadius en leaflet-interop.js | JS | 2h | ✅ Done |
| GEO-T98 | Agregar MudSlider (10–500m) en MarkerPopup.razor | Mobile | 1h | ✅ Done |
| GEO-T99 | Implementar UpdatePuntoRadioAsync en ISyncService + SQLite | Mobile | 2h | ✅ Done |
| GEO-T100 | Persistir valor en Preferences + leer al abrir popup | Mobile | 1h | ✅ Done |
| GEO-T101 | Tests unitarios: slider → updateMarkerRadius invocado | Testing | 2h | ✅ Done |

#### GEO-US26 — Ampliar foto en pantalla completa (3 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T102 | Crear componente FotoViewer.razor con MudOverlay fullscreen | Shared | 2h | ✅ Done |
| GEO-T103 | Agregar campo comentario editable en FotoViewer (UpdateFotoComentarioAsync) | Shared | 1h | ✅ Done |

---

### Sprint 07B

#### GEO-US23 — Popup con carrusel de fotos editable (13 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T104 | Crear componente FotoCarousel.razor con prev/next e indicador N/M | Shared | 3h | ✅ Done |
| GEO-T105 | Crear componente MarkerPopup.razor con MudDialog: título, descripción, carrusel, acciones | Shared | 3h | ✅ Done |
| GEO-T106 | Integrar OnMarkerClick en leaflet-interop.js → invocar dotnet.invokeMethod | JS | 2h | ✅ Done |
| GEO-T107 | Implementar añadir foto al carrusel (CaptureOrPickFotoAsync → SQLite → SyncQueue) | Mobile | 2h | ✅ Done |
| GEO-T108 | Tests de integración: abrir popup → carrusel muestra fotos del marker | Testing | 3h | ✅ Done |

#### GEO-US25 — Quitar foto del carrusel (5 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T109 | Botón ✕ por foto en FotoCarousel con SnackBar de confirmación | Shared | 1h | ✅ Done |
| GEO-T110 | DeleteFotoAsync en ISyncService: IsDeleted=true + SyncQueue DELETE | Mobile | 2h | ✅ Done |
| GEO-T111 | Tests unitarios: eliminar foto → DeleteFotoAsync → SyncQueue tiene 1 operación DELETE | Testing | 2h | ✅ Done |

#### GEO-US27 — Pantalla de estado de sincronización (5 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T112 | Crear Sincronizacion.razor con última fecha, resumen y tabla de operaciones | Mobile | 3h | ✅ Done |
| GEO-T113 | Agregar SyncStatusBadge en AppBar con 4 estados (badge N / spinner / ✓ / ✗) | Mobile | 1h | ✅ Done |
| GEO-T114 | Implementar botón "Sincronizar ahora" con disparo manual de SyncService | Mobile | 1h | ✅ Done |

---

### Sprint 07C

#### GEO-US24 — Operación offline-first robusta (8 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T115 | Implementar SyncService.PushAsync: cola FIFO, reintento exponencial, LWW | Mobile | 4h | ✅ Done |
| GEO-T116 | Implementar SyncService.PullAsync: GET /api/sync/delta → upsert local | Mobile | 3h | ✅ Done |
| GEO-T117 | Implementar ConflictResolver.cs: UseRemote / UseLocal según UpdatedAt | Mobile | 2h | ✅ Done |
| GEO-T118 | Crear SyncController.GetDelta en API (GET /api/sync/delta?since=) | Api | 2h | ✅ Done |
| GEO-T119 | Tests unitarios ESC-01: markers superpuestos → insertar como independientes | Testing | 2h | ✅ Done |
| GEO-T120 | Tests LWW: server.UpdatedAt > local.UpdatedAt → server prevalece | Testing | 2h | ✅ Done |

#### GEO-US28 — Lista de markers con búsqueda (8 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T121 | Crear ListaMarkers.razor con MudTable: nombre / fotos / estado / coords | Mobile | 3h | ✅ Done |
| GEO-T122 | Implementar búsqueda en tiempo real con MudTextField | Mobile | 1h | ✅ Done |
| GEO-T123 | Paginación 20 elementos/página en MudTable | Mobile | 1h | ✅ Done |
| GEO-T124 | Al tocar fila → navegar a mapa y hacer flyTo al marker | Mobile | 2h | ✅ Done |
| GEO-T125 | Tests unitarios: filtro "parque" → lista muestra solo markers con "parque" en nombre | Testing | 1h | ✅ Done |

#### GEO-US29 — Eliminar marker desde el mapa (5 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T126 | Botón "Eliminar marker" en MarkerPopup con MudDialog de confirmación | Shared | 1h | ✅ Done |
| GEO-T127 | DeletePuntoAsync en ISyncService: IsDeleted=true + SyncQueue DELETE | Mobile | 2h | ✅ Done |
| GEO-T128 | removeMarker(localId) en leaflet-interop.js | JS | 0.5h | ✅ Done |
| GEO-T129 | Tests unitarios: eliminar marker → marker no existe en SQLite ni en mapa | Testing | 1.5h | ✅ Done |

---

## 5. Criterios de Aceptación del Sprint

### CA-S27 — GPS FAB centra el mapa

```gherkin
Dado que el usuario está en la pantalla Mapa
Cuando el usuario toca el FAB de GPS
Entonces el mapa se centra en la posición actual del usuario con zoom 15
Y si el GPS tarda más de 10 segundos aparece el mensaje de error
Y si no hay permiso se muestra MudDialog con botón "Ir a Configuración"
```

### CA-S28 — Marcador de posición propia

```gherkin
Dado que el usuario ha concedido permiso de ubicación
Cuando la app tiene la posición GPS disponible
Entonces se muestra un círculo azul pulsante en la posición del usuario
Y el marcador se actualiza cada 5 segundos
Y es visualmente distinto de los markers de fotos
```

### CA-S29 — Radio visual configurable

```gherkin
Dado que el usuario abre el popup de un marker
Cuando el usuario mueve el slider de radio (10–500m)
Entonces el círculo semitransparente en el mapa se actualiza en tiempo real
Y el valor persiste al cerrar y reabrir el popup
```

### CA-S30 — Popup con carrusel editable

```gherkin
Dado que el usuario toca un marker en el mapa
Cuando se abre el MarkerPopup
Entonces se muestra el título editable del punto
Y se muestra el carrusel de fotos con indicador N/M y botones prev/next
Y existe botón para añadir foto y botón ✕ por cada foto
```

### CA-S31 — Quitar foto del carrusel

```gherkin
Dado que el carrusel muestra 3 fotos para un marker
Cuando el usuario toca ✕ en la segunda foto
Entonces aparece SnackBar de confirmación
Y al confirmar el carrusel pasa a mostrar 2 fotos
Y la foto queda marcada IsDeleted=true en SQLite
Y se encola operación DELETE en SyncQueue
```

### CA-S32 — Operación offline-first

```gherkin
Dado que el dispositivo está en modo avión
Cuando el usuario crea un punto con foto
Entonces el punto aparece en el mapa con estado "Pendiente"
Y el badge del AppBar muestra el contador de pendientes
Cuando el dispositivo recupera conectividad
Entonces se dispara sincronización automática
Y el badge queda en 0
Y el punto aparece en la web
```

### CA-S33 — Conflicto Last-Write-Wins

```gherkin
Dado que un punto fue editado localmente con UpdatedAt = T1
Y el mismo punto fue editado en el servidor con UpdatedAt = T2 donde T2 > T1
Cuando se ejecuta PullAsync
Entonces la versión del servidor (T2) prevalece en el dispositivo
```

### CA-S34 — Escenario ESC-01 (markers superpuestos)

```gherkin
Dado que existe un punto local en (-34.60, -58.38)
Y el servidor devuelve en el delta un punto a 30m de distancia
Cuando se ejecuta PullAsync
Entonces ambos puntos existen en SQLite con distintos LocalId
Y no se genera ninguna operación de merge en SyncQueue
```

### CA-S35 — Lista de markers con búsqueda

```gherkin
Dado que existen 30 markers registrados
Cuando el usuario navega a "Lista de Markers"
Entonces se muestran los primeros 20 markers en la tabla
Y al escribir en el campo de búsqueda la lista se filtra en tiempo real
```

### CA-S36 — Eliminar marker

```gherkin
Dado que el usuario abre el popup de un marker
Cuando el usuario toca "Eliminar marker" y confirma
Entonces el marker desaparece del mapa inmediatamente
Y queda marcado IsDeleted=true en SQLite
Y se encola operación DELETE en SyncQueue
Y tras sincronizar el punto queda eliminado en el servidor
```

---

## 6. Dependencias y Riesgos

| # | Riesgo / Dependencia | Probabilidad | Impacto | Mitigación |
|---|---------------------|-------------|---------|-----------|
| 1 | Sprint 06 incompleto (SyncService no funcional) | Baja | Alto | GEO-US24 construye sobre el sync existente; fallback: re-implementar desde cero |
| 2 | BlazorWebView JS Interop con latencia alta | Media | Medio | Usar DotNetObjectReference para callbacks, minimizar round-trips |
| 3 | MAUI Essentials Geolocation en emulador sin GPS | Media | Medio | Usar mock de ILocationPermissionService en tests; probar en dispositivo físico |
| 4 | MudDialog / MudOverlay con z-index conflictos con Leaflet | Media | Bajo | Ajustar z-index del mapa a 1, overlay a 9999 |
| 5 | Leaflet.js callback a Blazor sin reconexión WebView | Baja | Alto | Asegurar página nunca hace navigate completo; usar JSInterop callbacks persistentes |
| 6 | Sprint 07 con 65 pts vs velocidad 21 pts/sprint | Alta | Alto | Mitigado con split en 07A/07B/07C — 21+23+21 pts |

---

## 7. Definiciones de Ceremonias

| Ceremonia | Sprint 07A | Sprint 07B | Sprint 07C |
|-----------|-----------|-----------|-----------|
| Sprint Planning | 2026-07-13 | 2026-07-27 | 2026-08-10 |
| Sprint Review | 2026-07-26 | 2026-08-09 | 2026-08-23 |
| Sprint Retrospective | 2026-07-26 | 2026-08-09 | 2026-08-23 |

---

## 8. Métricas planificadas

| Métrica | Objetivo |
|---------|----------|
| Velocidad por sub-sprint | 21 pts (07A), 23 pts (07B), 21 pts (07C) |
| Tareas completadas | 41/41 |
| Cobertura de tests SyncService | ≥ 85% |
| Cobertura global | ≥ 75% |
| Bugs abiertos al cierre | 0 críticos |
| Tests pasando dotnet test | 100% |

---

## 9. Release v1.1.0-sprint07

Al finalizar el Sprint 07C exitosamente, se genera el Release v1.1.0-sprint07 con los siguientes entregables:

| Entregable | Ubicación |
|-----------|-----------|
| APK Android con GPS+Carrusel+Sync | GitHub Actions artifact |
| API con endpoint GET /api/sync/delta | Servidor |
| Tests unitarios Sprint 07 | GeoFoto.Tests/Unit/Mobile/ |
| Documentación actualizada | /docs |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-16 | Equipo Técnico | Versión inicial — Sprint 07 (07A/07B/07C) para GEO-E07 UX Avanzado Mobile |

---

**Fin del documento**
