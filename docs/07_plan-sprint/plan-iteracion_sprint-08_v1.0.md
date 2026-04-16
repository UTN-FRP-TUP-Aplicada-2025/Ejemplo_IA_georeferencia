# Plan de Iteración — Sprint 08

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** plan-iteracion_sprint-08_v1.0.md  
**Versión:** 1.0  
**Estado:** Completado  
**Fecha:** 2026-04-16  
**Autor:** Equipo Técnico

---

## 1. Información del Sprint

| Campo | Valor |
|-------|-------|
| Sprint | 08 |
| Fase | Fase 4 — UX Avanzado Mobile + Web |
| Fecha inicio | 2026-08-24 |
| Fecha fin | 2026-09-06 |
| Duración | 2 semanas |
| Sprint Goal | El técnico puede compartir fotos nativas desde Android. El supervisor tiene paridad funcional completa en la web: geolocation browser, gestión de markers, carrusel editable, descarga zip de fotos y subida de fotos sin EXIF. |
| Velocidad planificada | 20 pts |

### Nota de Capacidad

Sprint 08 cierra la Fase 4 con las historias que fueron diferidas del Sprint 07 por alcance. Total 20 pts, dentro de la velocidad histórica de 21 pts/sprint.

---

## 2. Épicas y User Stories

| Épica | US | Título | Puntos | Prioridad | Plataforma |
|-------|----|--------|--------|-----------|------------|
| GEO-E07 | GEO-US30 | Compartir foto nativa Android | 5 | Could | Mobile |
| GEO-E07 | GEO-US31 | Web — paridad funcional completa (mapa, markers, carrusel) | 5 | Must | Web |
| GEO-E07 | GEO-US32 | Descargar fotos de marker como zip (Web) | 5 | Must | Web |
| GEO-E07 | GEO-US33 | Subir foto al marker desde el browser (Web) | 5 | Must | Web |

**Total Story Points Must:** 15 pts  
**Total Story Points Could:** 5 pts  
**Total Sprint 08:** 20 pts

---

## 3. Objetivo de la Demo

### Demo Sprint 08

1. **Compartir foto (Android):** El técnico toca el ícono "Compartir" en el carrusel. Se abre el selector nativo de Android (Intent de compartir). El técnico elige WhatsApp y la foto se envía. Si el dispositivo no soporta la API de Share, aparece snackbar "Función no disponible en este dispositivo."

2. **Web — paridad de mapa:** El supervisor abre GeoFoto.Web y toca el botón "Centrar en mi posición". El browser solicita permiso de geolocation. Si lo concede, el mapa se centra. Si lo deniega, aparece el mensaje "Permiso de ubicación denegado en el navegador."

3. **Web — gestión de markers:** El supervisor hace click en un marker de la web. Se abre el ``MarkerPopup`` con título y descripción editables, carrusel de fotos, botón "Ampliar" (fullscreen) y botón "Eliminar marker". Toda la funcionalidad es equivalente a la app Android.

4. **Descarga zip:** En el popup del marker (web) con 3 fotos, el supervisor toca "Descargar fotos". El browser descarga automáticamente un ``Poste_47.zip`` con los 3 archivos de imagen.

5. **Subida de foto web:** En el popup del marker (web), el supervisor toca "Agregar foto" y selecciona una foto de su disco sin datos EXIF GPS. La foto queda vinculada al marker sin ningún error. El carrusel se actualiza mostrando la nueva foto.

---

## 4. Descomposición de Tareas

### GEO-US30 — Compartir foto nativa Android (5 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T130 | Crear ShareService.cs: Share.RequestAsync() con FileResult | Mobile | 2h | ✅ Done |
| GEO-T131 | Agregar botón "Compartir" (ícono share) en FotoCarousel (solo Mobile) | Shared | 1h | ✅ Done |
| GEO-T132 | Manejar caso Share no disponible → retornar false → snackbar info | Mobile | 0.5h | ✅ Done |
| GEO-T133 | Tests unitarios: archivo existe → invoca Share API; no existe → retorna false | Testing | 1.5h | ✅ Done |

### GEO-US31 — Web paridad funcional completa (5 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T134 | Adaptar leaflet-interop.js: navigator.geolocation para web (browser permission) | JS | 1h | ✅ Done |
| GEO-T135 | Agregar manejo ESC web: permiso denegado → mensaje "Permiso denegado en navegador" | Web | 1h | ✅ Done |
| GEO-T136 | Verificar MarkerPopup.razor funciona en GeoFoto.Web (IsMobile=false): título, descripción, carrusel, ampliar, eliminar | Web | 2h | ✅ Done |
| GEO-T137 | Tests: Web_PermisoBrowserDenegado_MuestraMensajeConfig() | Testing | 1h | ✅ Done |

### GEO-US32 — Descargar fotos de marker como zip (5 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T138 | Implementar endpoint GET /api/puntos/{id}/fotos/download → retorna .zip | Api | 3h | ✅ Done |
| GEO-T139 | Agregar botón "Descargar fotos" en MarkerPopup (IsMobile=false) con JS download | Web | 1h | ✅ Done |
| GEO-T140 | Deshabilitar botón si no hay fotos | Web | 0.5h | ✅ Done |
| GEO-T141 | Tests: DescargaZip_PuntoConFotos_RetornaZip(); DescargaZip_SinFotos_BotonDeshabilitado() | Testing | 0.5h | ✅ Done |

### GEO-US33 — Subir foto al marker desde el browser (5 pts)

| ID | Tarea | Componente | Estimación | Estado |
|----|-------|-----------|------------|--------|
| GEO-T142 | Agregar MudFileUpload "Agregar foto" en MarkerPopup (IsMobile=false) | Web | 1h | ✅ Done |
| GEO-T143 | Implementar endpoint POST /api/fotos/upload con puntoId | Api | 2h | ✅ Done |
| GEO-T144 | ESC-04: foto sin EXIF GPS → vincular por PuntoId sin error, sin advertencia | Api | 0.5h | ✅ Done |
| GEO-T145 | Actualizar carrusel tras subida exitosa (SignalR o reload parcial) | Web | 1h | ✅ Done |
| GEO-T146 | Tests: SubirFotoWeb_SinEXIF_VinculaAlMarker(); SubirFotoWeb_ConEXIF_VinculaAlMarker() | Testing | 0.5h | ✅ Done |

---

## 5. Criterios de Aceptación del Sprint

### CA-S37 — Compartir foto nativa Android

```gherkin
Dado que el técnico tiene abierto el carrusel de un marker con fotos
Cuando toca el botón "Compartir" en una foto
Entonces se abre el Intent nativo de Android para compartir
Y si el Share no está disponible aparece snackbar "Función no disponible en este dispositivo"
```

### CA-S38 — Geolocation web

```gherkin
Dado que el supervisor está en GeoFoto.Web
Cuando toca el botón "Centrar en mi posición"
Entonces el browser solicita permiso de geolocation
Y si lo concede el mapa se centra en su posición
Y si lo deniega aparece "Permiso de ubicación denegado en el navegador. Habilitalo desde la configuración del sitio."
```

### CA-S39 — Paridad funcional web

```gherkin
Dado que el supervisor hace click en un marker en GeoFoto.Web
Cuando se abre el MarkerPopup
Entonces puede editar título y descripción del marker
Y puede navegar el carrusel de fotos
Y puede ampliar una foto en fullscreen
Y puede eliminar una foto del carrusel
Y puede eliminar el marker con confirmación
```

### CA-S40 — Descarga zip de fotos

```gherkin
Dado que el popup de un marker con 3 fotos está abierto en la web
Cuando el supervisor toca "Descargar fotos"
Entonces el browser descarga automáticamente un archivo .zip con 3 imágenes
Y el archivo se llama {nombrePunto}.zip
```

### CA-S41 — Subir foto sin EXIF desde web

```gherkin
Dado que el supervisor tiene abierto el popup de un marker en GeoFoto.Web
Cuando sube una foto que no tiene datos EXIF GPS
Entonces la foto queda asociada al marker por PuntoId
Y no se muestra ningún error ni advertencia por la falta de GPS
Y el carrusel muestra la nueva foto inmediatamente
```

---

## 6. Dependencias y Riesgos

| # | Riesgo / Dependencia | Probabilidad | Impacto | Mitigación |
|---|---------------------|-------------|---------|-----------|
| 1 | Sprint 07 incompleto (MarkerPopup, FotoCarousel no implementados) | Baja | Alto | Sprint 08 depende de componentes Shared del Sprint 07; iniciar solo tras verificar Sprint 07 completo |
| 2 | MAUI Share API no disponible en todos los dispositivos | Media | Bajo | Manejar con try/catch + snackbar informativo; no bloquea funcionalidad principal |
| 3 | navigator.geolocation requiere HTTPS en browser | Media | Medio | Asegurar que GeoFoto.Web sirve con HTTPS en producción; localhost funciona sin HTTPS |
| 4 | Generación de zip en servidor con fotos grandes | Baja | Medio | Implementar streaming con ZipArchive + MemoryStream; no cargar todo en RAM |
| 5 | CORS en endpoint de descarga zip | Media | Bajo | Configurar CORS en API para permitir descarga desde el dominio del cliente web |

---

## 7. Definiciones de Ceremonias

| Ceremonia | Fecha |
|-----------|-------|
| Sprint Planning | 2026-08-24 |
| Sprint Review | 2026-09-06 |
| Sprint Retrospective | 2026-09-06 |

---

## 8. Métricas planificadas

| Métrica | Objetivo |
|---------|----------|
| Velocidad | 20 pts |
| Tareas completadas | 17/17 |
| Cobertura de tests nuevos | ≥ 80% |
| Cobertura global | ≥ 75% |
| Bugs abiertos al cierre | 0 críticos |
| Tests pasando dotnet test | 100% |

---

## 9. Release v1.1.0-sprint08

Al finalizar Sprint 08 exitosamente se genera el Release v1.1.0-sprint08 que consolida toda la Fase 4:

| Entregable | Ubicación |
|-----------|-----------|
| APK Android con GPS+Carrusel+Sync+Compartir | GitHub Actions artifact |
| Web con paridad funcional + zip + upload | Servidor |
| API con endpoints download zip + upload web | Servidor |
| Tests unitarios Sprint 07+08 | GeoFoto.Tests/Unit/Mobile/ |
| Documentación completa Fase 4 | /docs |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-16 | Equipo Técnico | Versión inicial — Sprint 08: compartir Android + paridad web + zip + upload |

---

**Fin del documento**
