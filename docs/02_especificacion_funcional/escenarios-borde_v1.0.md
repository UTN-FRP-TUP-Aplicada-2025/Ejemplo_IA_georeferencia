# Escenarios de Borde

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** escenarios-borde_v1.0.md
**Versión:** 1.0
**Estado:** Activo
**Fecha:** 2026-04-16
**Autor:** Equipo Técnico

---

## 1. Propósito

Este documento especifica los escenarios de borde identificados para GeoFoto Sprint 07–08. Cada escenario describe una situación límite o no-happy-path que el sistema debe manejar de forma determinista y sin degradar la experiencia de usuario. Cada escenario incluye: contexto, precondición, flujo esperado, postcondición y tests asociados.

---

## 2. Escenarios de Borde

### ESC-01 — Markers superpuestos en sincronización (Android)

**Contexto:**
Durante la sincronización pull (GET /api/sync/delta), el servidor devuelve markers cuyos radios geográficos se superponen con markers locales existentes en el dispositivo.

**Precondición:**
- El dispositivo tiene markers locales con SyncStatus="Synced".
- El servidor devuelve en el delta uno o más puntos cuyas coordenadas caen dentro del radio de agrupación de un punto local.

**Flujo esperado:**
1. El SyncService recibe los puntos del servidor en el delta.
2. Por cada punto recibido: el sistema NO evalúa proximidad geográfica respecto a puntos locales.
3. El punto se inserta en SQLite como un nuevo ``Punto_Local`` independiente con SyncStatus="Synced" y el RemoteId asignado por el servidor.
4. No se produce merge, fusión ni resolución de superposición entre el punto nuevo y los existentes.
5. El mapa muestra ambos markers en sus posiciones exactas, visualmente superpuestos o cercanos.

**Postcondición:**
- Los markers superpuestos coexisten como entidades independientes en SQLite y en el mapa.
- El usuario puede interactuar con cada marker por separado.
- No se encola ninguna operación de merge en SyncQueue.

**Decisión de diseño:**
El merge automático por proximidad en sync introduce complejidad innecesaria y riesgo de pérdida de datos. La decisión de fusionar markers es responsabilidad del usuario, no del sistema de sincronización.

**Tests asociados:**
```
[Fact] PullAsync_ESC01_MarkersSuperposicion_InsertaComoNuevo
  // Dado: punto local en (-34.60, -58.38), radio 50m
  // Y: servidor devuelve punto en (-34.60, -58.38) con distancia 30m (dentro del radio)
  // Cuando: PullAsync() procesa el delta
  // Entonces: el punto del servidor se inserta como nuevo Punto_Local independiente
  //   Y ambos puntos existen en SQLite con distintos LocalId
  //   Y no se genera ninguna operación de merge en SyncQueue
```

---

### ESC-02 — Mapa no disponible al iniciar la app (Android)

**Contexto:**
Al iniciar GeoFoto.Mobile, el BlazorWebView no cargó correctamente o la inicialización de Leaflet.js falló (por ejemplo, fallo de JS, recurso no encontrado, BlazorWebView timeout).

**Precondición:**
- El usuario inicia la app.
- La función `initMap()` de leaflet-interop.js lanza una excepción o no responde en el tiempo esperado.

**Flujo esperado:**
1. El componente ``Mapa.razor`` detecta el fallo de inicialización del mapa (excepción en ``IJSRuntime.InvokeVoidAsync("initMap", ...)``).
2. Se oculta el componente del mapa.
3. Se muestra una pantalla de error con el mensaje exacto: "Mapa no disponible — intentá reiniciar la aplicación."
4. Se presenta un botón "Reintentar" que vuelve a invocar ``initMap()``.
5. Si el reintento falla nuevamente, se mantiene la pantalla de error.
6. Si el reintento tiene éxito, se oculta la pantalla de error y se muestra el mapa.

**Postcondición:**
- El usuario siempre ve la pantalla de error con opción de reintentar.
- La app NUNCA queda bloqueada en pantalla en blanco o en estado de carga infinita.

**Tests asociados:**
```
[Fact] InitMap_Falla_MuestraPantallaError
  // Dado: JSRuntime lanza excepción al invocar initMap
  // Cuando: Mapa.razor intenta inicializar
  // Entonces: se muestra div de error con mensaje exacto
  //   Y el botón "Reintentar" está visible

[Fact] InitMap_Reintento_Exitoso_OcultaPantallaError
  // Dado: el primer initMap falló y se muestra la pantalla de error
  // Cuando: el usuario toca "Reintentar" y el segundo intento tiene éxito
  // Entonces: la pantalla de error desaparece y el mapa está visible
```

---

### ESC-03 — GPS sin permiso al iniciar la app (Android)

**Contexto:**
Al iniciar GeoFoto.Mobile, el GPS no está habilitado en el dispositivo o el usuario no ha otorgado el permiso de ubicación a la aplicación.

**Precondición:**
- El usuario inicia la app.
- El permiso ``ACCESS_FINE_LOCATION`` no fue concedido previamente, o el GPS del dispositivo está desactivado.

**Flujo esperado:**

**Caso A — Primera solicitud de permiso:**
1. La app detecta la ausencia de permiso mediante ``ILocationPermissionService.CheckStatusAsync()``.
2. Se muestra el dialog nativo de Android de solicitud de permiso.
3. El usuario concede el permiso → la app continúa normalmente con GPS activo.

**Caso B — Usuario deniega el permiso (primera vez):**
1. El usuario toca "Denegar" en el dialog nativo.
2. La app muestra un ``MudDialog`` con explicación de por qué se necesita el permiso y un botón "Ir a Configuración" que invoca ``AppInfo.ShowSettingsUI()``.
3. El usuario puede ir a Configuración para conceder el permiso manualmente.
4. El mapa carga SIN centrado GPS.
5. Un MudSnackbar permanente muestra: "Sin permiso de ubicación — el mapa está disponible pero sin tu posición."

**Caso C — Permiso denegado permanentemente:**
1. El usuario había denegado el permiso con "No volver a preguntar" en una sesión anterior.
2. La app detecta que el permiso está denegado permanentemente.
3. NO se muestra el dialog nativo (Android bloquea la solicitud).
4. La app muestra directamente el ``MudSnackbar`` permanente: "Sin permiso de ubicación — el mapa está disponible pero sin tu posición."
5. El mapa carga completamente y el usuario puede usarlo sin GPS.

**Postcondición (todos los casos):**
- El mapa SIEMPRE carga y está disponible para el usuario.
- La app NUNCA queda bloqueada esperando permisos GPS.
- El marcador de posición propia (US21) no se muestra si no hay permiso.

**Tests asociados:**
```
[Fact] CentrarMapa_PermisoDenegado_MuestraDialog
  // Dado: permiso de ubicación denegado (primera vez)
  // Cuando: usuario toca FAB GPS
  // Entonces: se muestra MudDialog con botón "Ir a Configuración"

[Fact] InicioApp_PermisoDenegadoPermanentemente_MapaCargaSinGPS
  // Dado: permiso denegado permanentemente
  // Cuando: la app inicia
  // Entonces: el mapa carga correctamente
  //   Y se muestra MudSnackbar permanente con mensaje de sin ubicación
  //   Y el marcador de posición propia NO aparece

[Fact] InicioApp_SinPermiso_MuestraDialogNativo
  // Dado: primera vez que se solicita permiso
  // Cuando: la app inicia
  // Entonces: se muestra el dialog nativo de solicitud de permiso
```

---

### ESC-04 — Foto sin EXIF GPS subida desde el navegador web

**Contexto:**
Un supervisor sube una foto al carrusel de un marker existente desde el navegador web. La foto no tiene metadatos EXIF de geolocalización (por ejemplo, un screenshot, una foto de documento, o una imagen sin metadatos GPS).

**Precondición:**
- El supervisor está en la web con el popup del marker abierto.
- El marker existe y tiene coordenadas válidas.
- La foto a subir NO contiene ``GpsDirectory`` en sus metadatos EXIF.

**Flujo esperado:**
1. El supervisor selecciona la foto mediante el ``MudFileUpload`` del popup.
2. La foto se envía al endpoint ``POST /api/fotos/upload`` con el ``PuntoId`` del marker.
3. El servidor procesa la imagen con MetadataExtractor y detecta la ausencia de datos GPS.
4. El servidor NO rechaza la solicitud ni retorna error.
5. El servidor crea el registro ``Foto`` en SQL Server asociado al ``PuntoId`` proporcionado.
6. Los campos ``LatitudExif`` y ``LongitudExif`` de la foto quedan en null.
7. El marcador mantiene sus coordenadas originales (no se modifican por la foto sin GPS).
8. La respuesta es ``201 Created`` con los datos de la foto.
9. El carrusel del popup se actualiza inmediatamente con la nueva foto.

**Postcondición:**
- La foto queda vinculada al marker por ``PuntoId``.
- NO se muestra ningún error ni advertencia al supervisor por la falta de GPS en la foto.
- El marker mantiene sus coordenadas geográficas originales.
- El carrusel muestra la foto recién subida.

**Diferencia con RN-03:**
La regla RN-03 aplica cuando se sube una foto que debería definir las coordenadas del punto (flujo de creación de nuevo punto). ESC-04 aplica cuando se agrega una foto a un punto ya existente — en este caso las coordenadas del punto no se modifican y la ausencia de EXIF GPS en la foto es completamente aceptable.

**Tests asociados:**
```
[Fact] SubirFotoWeb_SinEXIF_VinculaAlMarkerSinError
  // Dado: el endpoint recibe una foto sin EXIF GPS con PuntoId=5
  // Cuando: se procesa el upload
  // Entonces: la foto se crea en SQL Server con PuntoId=5
  //   Y LatitudExif y LongitudExif son null
  //   Y la respuesta es 201 Created
  //   Y el punto mantiene sus coordenadas originales

[Fact] SubirFotoWeb_SinEXIF_CarruselActualizado
  // Dado: el carrusel muestra 2 fotos para un marker
  // Cuando: el supervisor sube una foto sin GPS
  // Entonces: el carrusel pasa a mostrar 3 fotos
  //   Y no se muestra ningún mensaje de error
```

---

## 3. Matriz de Cobertura

| Escenario | Feature Afectado | Stories Relacionadas | Plataforma |
|-----------|-----------------|---------------------|------------|
| ESC-01 | F-13 (Sync automático) | GEO-US24 | Android (Mobile) |
| ESC-02 | F-06 (Visualización mapa) | GEO-US20b | Android (Mobile) |
| ESC-03 | F-16 (Centrado GPS) | GEO-US20b, GEO-US21 | Android (Mobile) |
| ESC-04 | F-25 (Subida foto web) | GEO-US33 | Web (Browser) |

---

## 4. Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-16 | Creación inicial con ESC-01 a ESC-04 para Sprint 07-08 |

---

**Fin del documento**
