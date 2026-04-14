--
# Plan de Iteración — Sprint 02

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** plan-iteracion_sprint-02_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Información del Sprint

| Campo | Valor |
|-------|-------|
| Sprint | 02 |
| Fase | Fase 1 — Núcleo Online |
| Fecha inicio | 2026-05-04 |
| Fecha fin | 2026-05-17 |
| Duración | 2 semanas |
| Sprint Goal | Flujo completo online: el usuario puede subir fotos con EXIF, ver markers en mapa Leaflet, abrir popup de detalle y explorar lista de puntos. Se demuestra el ciclo completo captura → persistencia → visualización. |
| Velocidad planificada | 21 pts |

---

## 2. Épicas y User Stories

| Épica | US | Título | Puntos | Prioridad |
|-------|----|--------|--------|-----------|
| GEO-E02 | GEO-US04 | Subir fotos y ver markers | 8 | Must |
| GEO-E02 | GEO-US05 | Popup con fotos y datos | 5 | Must |
| GEO-E02 | GEO-US06 | Editar punto | 5 | Should |
| GEO-E02 | GEO-US07 | Lista de puntos | 3 | Should |

**Total Story Points:** 21

---

## 3. Objetivo de la Demo

Al cierre del Sprint 02, se podrá demostrar:

1. **Subida de fotos:** Desde GeoFoto.Web, el usuario selecciona una o varias fotos con coordenadas EXIF; el sistema las sube al servidor, extrae latitud/longitud y crea automáticamente el Punto asociado.
2. **Mapa con markers:** La pantalla Mapa muestra todos los puntos como markers de Leaflet.js. Los markers son clickeables.
3. **Popup de detalle:** Al hacer click en un marker se abre un MudDialog con nombre, descripción, coordenadas y un carrusel de fotos del punto.
4. **Edición de punto:** El usuario edita nombre y descripción de un punto existente desde un formulario y los cambios se persisten vía PUT /api/puntos/{id}.
5. **Lista de puntos:** La pantalla Lista muestra los puntos en un MudTable con columnas Nombre, Coordenadas y cantidad de fotos.

---

## 4. Descomposición de Tareas

### GEO-US04 — Subir fotos y ver markers (8 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T13 | Crear SubirFotos.razor con MudFileUpload (múltiples archivos) | Shared | 3h | - | To Do |
| GEO-T14 | Crear endpoint POST /api/fotos/upload multipart con validación | Api | 3h | - | To Do |
| GEO-T15 | Integrar MetadataExtractor para extracción EXIF (lat, lng, fecha) | Api | 2h | - | To Do |
| GEO-T16 | Crear PuntosController con GET /api/puntos y GET /api/puntos/{id} | Api | 2h | - | To Do |
| GEO-T17 | Crear Mapa.razor con integración Leaflet.js vía IJSRuntime | Shared | 4h | - | To Do |
| GEO-T18 | Crear leaflet-interop.js con funciones initMap, addMarkers, fitBounds | Shared | 2h | - | To Do |
| GEO-T19 | Conectar SubirFotos con API y actualizar mapa tras subida exitosa | Shared | 2h | - | To Do |

### GEO-US05 — Popup con fotos y datos (5 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T20 | Crear MarkerPopup.razor con MudDialog para detalle del punto | Shared | 2h | - | To Do |
| GEO-T21 | Crear FotoCarousel.razor para imágenes del punto | Shared | 2h | - | To Do |
| GEO-T22 | Implementar onMarkerClick en leaflet-interop.js que invoca DotNetObjectReference | Shared | 2h | - | To Do |
| GEO-T23 | Crear endpoint GET /api/fotos/{puntoId} que retorna fotos de un punto | Api | 1h | - | To Do |

### GEO-US06 — Editar punto (5 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T24 | Crear DetallePunto.razor con MudTextField para nombre y descripción | Shared | 2h | - | To Do |
| GEO-T25 | Crear endpoint PUT /api/puntos/{id} con validación | Api | 1h | - | To Do |
| GEO-T26 | Conectar formulario con API y feedback MudSnackbar | Shared | 2h | - | To Do |

### GEO-US07 — Lista de puntos (3 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T27 | Crear ListaPuntos.razor con MudTable sorteable | Shared | 2h | - | To Do |
| GEO-T28 | Agregar filtro de búsqueda por nombre en MudTextField | Shared | 1h | - | To Do |
| GEO-T29 | Agregar MudChip para cantidad de fotos por fila | Shared | 1h | - | To Do |

---

## 5. Criterios de Aceptación del Sprint

### CA-S05 — Subida de fotos con EXIF

```gherkin
Dado que el usuario selecciona una foto JPG con coordenadas EXIF
Cuando hace click en "Subir"
Entonces la foto se almacena en el servidor y se crea un Punto con latitud y longitud extraídas
```

### CA-S06 — Markers en mapa

```gherkin
Dado que existen 3 puntos con coordenadas geográficas
Cuando se carga la pantalla Mapa
Entonces se muestran 3 markers en el mapa Leaflet en las posiciones correctas
```

### CA-S07 — Popup con detalle

```gherkin
Dado que se muestra un marker en el mapa
Cuando el usuario hace click en el marker
Entonces se abre un MudDialog con nombre del punto, coordenadas y carrusel de fotos
```

### CA-S08 — Edición de punto

```gherkin
Dado que el usuario abre el detalle de un punto
Cuando modifica el nombre y hace click en "Guardar"
Entonces el nombre actualizado se persiste en el servidor y se muestra un MudSnackbar de confirmación
```

### CA-S09 — Lista de puntos

```gherkin
Dado que existen 5 puntos registrados
Cuando se navega a la pantalla Lista de Puntos
Entonces se muestra un MudTable con 5 filas mostrando Nombre, Coordenadas y Cantidad de Fotos
```

---

## 6. Dependencias y Riesgos

| # | Riesgo / Dependencia | Probabilidad | Impacto | Mitigación |
|---|---------------------|-------------|---------|-----------|
| 1 | Sprint 01 incompleto (solución no compila) | Baja | Crítico | Sprint 01 incluye solo infraestructura — bajo riesgo de deuda |
| 2 | Fotos sin datos EXIF (sin coordenadas) | Alta | Medio | Validar presencia de EXIF; si no tiene, solicitar coordenadas manuales |
| 3 | Integración Leaflet.js con Blazor via JS Interop inestable | Media | Alto | Prototipar integración en spike técnico, primera tarea del sprint |
| 4 | Archivos de fotos muy grandes (>10 MB) | Media | Medio | Limitar tamaño a 10 MB en el endpoint con validación |

---

## 7. Definiciones

| Ceremonia | Fecha tentativa | Duración |
|-----------|----------------|----------|
| Sprint Planning | 2026-05-04 | 2h |
| Daily Standup | Lunes a viernes | 15min |
| Sprint Review | 2026-05-17 | 1h |
| Sprint Retrospective | 2026-05-17 | 45min |

---

## 8. Métricas planificadas

| Métrica | Objetivo |
|---------|----------|
| Velocidad | 21 pts |
| Tareas completadas | 18/18 |
| Cobertura de tests | N/A (se enfoca en integración UI) |
| Bugs abiertos al cierre | ≤ 2 |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**

