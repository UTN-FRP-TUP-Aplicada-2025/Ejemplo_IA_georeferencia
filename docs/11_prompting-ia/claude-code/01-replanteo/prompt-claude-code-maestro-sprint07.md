# Prompt Maestro para Claude Code — GeoFoto Replanteo Sprint 07
> Pegá este bloque completo en Claude Code (terminal).
> Es un agente autónomo de 5 fases. No interrumpir entre fases.
> Cada fase termina con una confirmación explícita antes de avanzar.

---

```
Sos un arquitecto de software senior especialista en .NET 10, MAUI Blazor Hybrid,
Leaflet.js, SQLite offline-first y Blazor Web. Tu misión es ejecutar 5 fases en
orden estricto sobre el proyecto GeoFoto. No avancés a la siguiente fase sin
confirmar el cierre de la anterior con el texto exacto indicado.

Modo operativo: autónomo. Leés, analizás, modificás archivos, ejecutás comandos,
corregís errores y reportás. No pedís confirmación intermedia salvo al cerrar cada fase.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
FASE 0 — ENTRADA EN CONTEXTO BASE (leer antes de todo lo demás)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Leer en este orden exacto. Para cada doc, confirmar internamente que lo entendiste
antes de pasar al siguiente:

  docs/README.md
  docs/00_contexto/vision-producto_v1.0.md
  docs/00_contexto/roadmap-producto_v1.0.md
  docs/01_necesidades_negocio/
  docs/02_especificacion_funcional/especificacion-funcional_v1.0.md
  docs/02_especificacion_funcional/casos-de-uso/casos-de-uso_v1.0.md
  docs/03_ux-ui/wireframes-pantallas_v1.0.md
  docs/03_ux-ui/ux-writing-microcopy_v1.0.md
  docs/05_arquitectura_tecnica/arquitectura-solucion_v1.0.md
  docs/05_arquitectura_tecnica/arquitectura-offline-sync_v1.0.md
  docs/05_arquitectura_tecnica/flujo-ejecucion-sistema_v1.0.md
  docs/05_arquitectura_tecnica/modelo-datos-logico_v1.0.md
  docs/05_arquitectura_tecnica/api-rest-spec_v1.0.md
  docs/06_backlog-tecnico/product-backlog_v1.0.md
  docs/06_backlog-tecnico/backlog-tecnico_v1.0.md
  docs/08_calidad_y_pruebas/estrategia-testing-motor_v1.0.md
  docs/08_calidad_y_pruebas/definition-of-done_v1.0.md
  docs/09_devops/pipeline-ci-cd_v1.0.md
  docs/10_developer_guide/guia-setup-proyecto_v1.0.md
  docs/11_prompting-ia/  (todos los archivos)
  scripts/               (todos los .bat existentes — leer contenido, no ejecutar)

Después de leer todo, confirmá con exactamente:
"FASE 0 COMPLETA — contexto base asimilado. Arquitectura, modelos, backlog,
scripts y prompts previos comprendidos. Listo para Fase 1."

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
FASE 1 — ACTUALIZACIÓN DE DOCUMENTACIÓN CON NUEVAS HISTORIAS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Las siguientes historias de usuario y escenarios REEMPLAZAN y AMPLÍAN el backlog
existente a partir de GEO-US20. Incorporalas sin borrar GEO-US01 a GEO-US19.

── NUEVAS HISTORIAS — APP ANDROID (GEO-US20 a GEO-US30) ──────────────────────

GEO-US20 · 5 pts · Must
Como técnico de campo, quiero poder centrar el mapa en mi posición actual
tocando un botón, para orientarme rápidamente en terreno.

  CA-01: El FAB de GPS centra el mapa con setView(lat, lng, 15) en menos de 2 s.
  CA-02: Si el permiso GPS está denegado, se muestra dialog de solicitud de permiso
         antes de intentar centrar.
  CA-03: Si el GPS tarda más de 10 s, snackbar warning "No se pudo obtener ubicación".

GEO-US21 · 5 pts · Must
Como técnico de campo, quiero ver mi posición actual como un punto en el mapa
para saber dónde estoy antes de agregar una foto.

  CA-01: Un marcador de posición propia (distinto visual al de fotos) aparece y
         se actualiza en tiempo real según se mueve el dispositivo.
  CA-02: No se confunde con un marker de foto — usa ícono diferente (círculo azul
         pulsante).
  CA-03: Si se pierde GPS, el marcador de posición desaparece con snackbar warning.

GEO-US22 · 8 pts · Must
Como técnico de campo, quiero visualizar y ajustar el radio del marker actual
para decidir si una nueva foto va al marker existente o crea uno nuevo.

  CA-01: Al tocar un marker se muestra un círculo semi-transparente en el mapa
         que representa el radio de agrupación actual (default 50 m).
  CA-02: El usuario puede expandir o reducir el radio desde un slider en el popup
         (rango 10 m – 500 m). El cambio persiste en Preferences.
  CA-03: El radio configurable se aplica globalmente a todos los markers.

GEO-US23 · 13 pts · Must
Como técnico de campo, quiero tocar un marker para abrir un diálogo con el
carrusel de fotos, título y descripción del punto, y poder ampliar cada foto
con su propia descripción.

  CA-01: Click/tap en marker abre MudDialog con: título editable, descripción
         editable del punto y FotoCarousel con todas las fotos.
  CA-02: Desde el carrusel, tap en una foto la amplía en fullscreen (MudOverlay).
  CA-03: En la vista fullscreen se puede agregar/editar una descripción por foto.
  CA-04: Los cambios en título, descripción del punto y comentarios por foto
         persisten en SQLite y se encolan en SyncQueue para sincronización.
  CA-05: Si no hay fotos: mensaje "Este punto no tiene fotos — usá el botón
         de cámara para agregar la primera."

GEO-US24 · 8 pts · Must
Como técnico de campo, quiero trabajar sin conexión a internet, agregar fotos
a markers y que todo se sincronice automáticamente cuando haya red.

  CA-01: En modo offline la app funciona completamente: crear markers, agregar
         fotos, editar títulos y descripciones.
  CA-02: Al recuperar conexión, SyncService.PushAsync() se dispara automáticamente
         y sincroniza toda la cola Pending.
  CA-03: El badge en AppBar muestra: número de items pendientes (offline), spinner
         (sincronizando), check verde (todo sincronizado), ícono rojo (error).

GEO-US25 · 5 pts · Should
Como técnico de campo, quiero quitar fotos desde el carrusel del marker para
corregir capturas incorrectas.

  CA-01: Cada foto en el carrusel tiene un botón ✕ visible.
  CA-02: Al tocarlo aparece dialog de confirmación: "¿Eliminar esta foto?
         Esta acción no se puede deshacer."
  CA-03: Si confirma: la foto se elimina de SQLite, se marca IsDeleted=true y se
         encola PendingDelete. El carrusel se actualiza sin cerrar el popup.

GEO-US26 · 3 pts · Should
Como técnico de campo, quiero ampliar fotos desde el carrusel para verlas
en detalle.

  CA-01: Tap en una foto del carrusel abre visor fullscreen con MudOverlay.
  CA-02: En fullscreen se puede hacer pinch-to-zoom en la imagen.
  CA-03: Cerrar con botón ✕ o tap fuera vuelve al carrusel en la misma posición.

GEO-US27 · 5 pts · Must
Como técnico de campo, quiero ver el estado de sincronización e iniciarlo
manualmente cuando lo necesite.

  CA-01: Pantalla "Sincronización" (AppBar nav) muestra: última sincronización,
         items pendientes, items fallidos con motivo.
  CA-02: Botón "Sincronizar ahora" dispara PushAsync + PullAsync manualmente.
  CA-03: Durante sync: spinner en botón + badge animado en AppBar.
  CA-04: Al terminar: fecha/hora de última sync actualizada.

GEO-US28 · 8 pts · Must
Como técnico de campo, quiero ver la lista de todos los markers, buscarlos
y editar su carrusel desde esa pantalla.

  CA-01: Pantalla "Lista de markers" muestra: nombre, coordenadas, cantidad de
         fotos, estado de sync (chip color).
  CA-02: Campo de búsqueda filtra por nombre en tiempo real.
  CA-03: Tap en un ítem de la lista centra el mapa en ese marker Y abre el popup.
  CA-04: Desde la lista se puede acceder al carrusel de fotos del marker.

GEO-US29 · 5 pts · Should
Como técnico de campo, quiero poder eliminar un marker (con todas sus fotos)
al seleccionarlo.

  CA-01: En el popup del marker, botón "Eliminar marker".
  CA-02: Dialog de confirmación: "¿Eliminar este marker y sus N fotos?
         Esta acción no se puede deshacer."
  CA-03: Si confirma: elimina PuntoLocal + FotoLocal[] de SQLite, quita el marker
         del mapa, encola PendingDelete en SyncQueue.

GEO-US30 · 5 pts · Could
Como técnico de campo, quiero compartir una o más fotos del carrusel a través
de apps del dispositivo (WhatsApp, email, etc.).

  CA-01: En el carrusel, botón "Compartir" por foto individual.
  CA-02: Invoca la Share API nativa de Android con la foto seleccionada.
  CA-03: Si el sistema de compartir no está disponible, snackbar info: "Función
         no disponible en este dispositivo."

── NUEVAS HISTORIAS — WEB FRONTEND (GEO-US31 a GEO-US37) ──────────────────────

GEO-US31 · 5 pts · Must
(Equivalente web de GEO-US20 a GEO-US23 + GEO-US25/26/27/28/29)
Como supervisor, quiero en la web la misma experiencia de mapa que en Android:
centrar en mi posición, ver mi ubicación, gestionar markers, carrusel, editar
títulos y descripciones, quitar fotos, ampliar fotos, ver lista de markers y
eliminar markers. Los permisos de ubicación se solicitan al navegador
(Geolocation API del browser).

  CA-01: El botón de centrar mapa usa navigator.geolocation del browser.
  CA-02: Si el browser deniega geolocation, mensaje: "Permiso de ubicación
         denegado en el navegador. Habilitalo desde la configuración del sitio."
  CA-03: Toda la funcionalidad de marker popup (título, descripción, carrusel,
         ampliar, quitar, eliminar marker) igual que en Android.

GEO-US32 · 5 pts · Must
Como supervisor, quiero descargar localmente todas las fotos de un marker
en un zip.

  CA-01: En el popup del marker (web), botón "Descargar fotos".
  CA-02: Genera un archivo .zip con todas las fotos del carrusel nombradas como
         {nombrePunto}_{n}.jpg y lo descarga al navegador.
  CA-03: Si el punto no tiene fotos, el botón está deshabilitado.

GEO-US33 · 5 pts · Must
Como supervisor, quiero subir fotos al carrusel de un marker existente desde
el navegador, aunque no tengan datos GPS.

  CA-01: En el popup del marker (web), botón "Agregar foto" abre MudFileUpload.
  CA-02: La foto se sube al servidor y se asocia al marker (sin importar si tiene
         o no datos EXIF GPS — ya está vinculada al marker por PuntoId).
  CA-03: El carrusel se actualiza inmediatamente con la foto recién subida.
  CA-04: Si la foto no tiene EXIF GPS, no se muestra error — la foto queda
         vinculada al marker sin datos de coordenadas propias.

── NUEVOS ESCENARIOS (Android) ─────────────────────────────────────────────────

ESC-01: Al sincronizar, el servidor devuelve markers cuyos radios se superponen
        con markers locales existentes. La app NO intenta resolver la superposición:
        los trata como markers completamente independientes y los agrega a SQLite
        con Status="Synced". No hay merge automático de markers por proximidad en sync.

ESC-02: Al iniciar la app, el mapa no puede inicializar (BlazorWebView no cargó,
        Leaflet falló, etc.). Se muestra pantalla de error con mensaje:
        "Mapa no disponible — intentá reiniciar la aplicación."
        con botón "Reintentar" que vuelve a invocar initMap.

ESC-03: Al iniciar la app, el GPS/ubicación no está habilitado o el permiso no
        fue otorgado. La app NO avanza hasta resolver permisos:
        1. Muestra dialog nativo de solicitud de permiso.
        2. Si el usuario deniega: muestra MudDialog con explicación y botón
           "Ir a Configuración" (AppInfo.ShowSettingsUI()).
        3. Si el usuario niega permanentemente: el mapa carga igual pero sin
           centrado GPS, con snackbar permanente "Sin permiso de ubicación —
           el mapa está disponible pero sin tu posición."
        4. En ningún caso la app queda bloqueada sin mapa.

── NUEVOS ESCENARIOS (Web) ──────────────────────────────────────────────────────

ESC-04: Al subir una foto desde el navegador sin EXIF GPS, la foto queda
        vinculada al marker por PuntoId. No se muestra error ni advertencia.
        El marcador mantiene sus coordenadas originales.

── DOCUMENTOS A CREAR O ACTUALIZAR ─────────────────────────────────────────────

ACTUALIZAR (preservar contenido existente, ampliar con lo nuevo):
  docs/06_backlog-tecnico/product-backlog_v1.0.md
    → Agregar GEO-US20 a GEO-US33 con formato existente (BDD, story points,
      épica, sprint asignado, tareas técnicas hijas estimadas)
    → Crear nueva épica GEO-E07 "UX Avanzado Mobile + Web" para US20–33

  docs/06_backlog-tecnico/backlog-tecnico_v1.0.md
    → Agregar tareas técnicas GEO-T60 en adelante para cada US nueva

  docs/02_especificacion_funcional/especificacion-funcional_v1.0.md
    → Agregar F-15 a F-25 para los features nuevos
    → Actualizar F-02 (cámara) con escenario ESC-02 y ESC-03
    → Actualizar F-09 (sync) con ESC-01

  docs/02_especificacion_funcional/casos-de-uso/casos-de-uso_v1.0.md
    → Agregar CU-10 a CU-20 para los casos de uso nuevos
    → Actualizar CU-01 (mapa) con ESC-02 y ESC-03

  docs/00_contexto/roadmap-producto_v1.0.md
    → Agregar Fase 4 "UX Avanzado y Funcionalidades Completas"
    → Sprint 07 con GEO-US20–29 (Android completo)
    → Sprint 08 con GEO-US30–33 (web + compartir + descarga)

  docs/07_plan-sprint/
    → Crear plan-iteracion_sprint-07_v1.0.md
    → Crear plan-iteracion_sprint-08_v1.0.md

CREAR NUEVOS:
  docs/02_especificacion_funcional/escenarios-borde_v1.0.md
    → Documentar ESC-01 a ESC-04 con formato: contexto, precondición,
      flujo esperado, postcondición, tests asociados

  docs/03_ux-ui/wireframes-pantallas_v2.0.md
    → Wireframes en ASCII/Markdown de las pantallas nuevas:
      - Popup de marker con carrusel, título, descripción, botones
      - Visor fullscreen de foto con descripción
      - Pantalla Sincronización con estado e historial
      - Lista de markers con búsqueda y chips de estado
      - Indicador visual de radio de marker en el mapa

REGLAS para actualizar documentación:
  - Mantener formato de encabezado estándar (Proyecto, Documento, Versión, Fecha, Autor)
  - Incrementar versión menor en docs modificados (v1.0 → v1.1)
  - Agregar fila en Control de Cambios al pie de cada doc modificado
  - Usar voseo rioplatense en todos los textos de UI
  - Nunca borrar contenido existente — solo agregar o marcar como [ACTUALIZADO]

Cuando termines de actualizar toda la documentación, confirmá con exactamente:
"FASE 1 COMPLETA — X documentos actualizados, Y documentos nuevos creados.
Historias GEO-US20 a GEO-US33 y escenarios ESC-01 a ESC-04 incorporados."

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
FASE 2 — REVISIÓN DE COHERENCIA ENTRE DOCUMENTOS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Realizar una pasada de coherencia cruzada entre TODOS los documentos de docs/.
Para cada inconsistencia encontrada: registrala, corregila y marcá el cambio.

CHECKLIST DE COHERENCIA (verificar cada punto):

  [ ] Todos los IDs de US en product-backlog coinciden con backlog-tecnico
  [ ] Todos los casos de uso en casos-de-uso.md tienen US correspondiente en backlog
  [ ] Todos los features en especificacion-funcional tienen CU correspondiente
  [ ] Los wireframes cubren todas las pantallas mencionadas en los CU
  [ ] Los modelos de datos (modelo-datos-logico.md) cubren todos los campos
      nuevos (Comentario en FotoLocal, Titulo en PuntoLocal, Radio en Preferences,
      IsDeleted en ambos, UpdatedAt en ambos)
  [ ] La API REST spec (api-rest-spec.md) tiene endpoints para:
      GET /api/sync/delta?since={utc}
      PUT /api/puntos/{id}  (con titulo, descripcion, updatedAt)
      PUT /api/fotos/{id}   (con comentario, updatedAt)
      DELETE /api/fotos/{id}
      DELETE /api/puntos/{id}
      POST /api/fotos/upload (para subida web)
      GET /api/puntos/{id}/fotos/download (zip descarga web)
  [ ] El microcopy (ux-writing-microcopy.md) tiene entradas para todos los textos
      nuevos de las US 20–33 y ESC 01–04
  [ ] El roadmap refleja correctamente Sprint 07 y Sprint 08 nuevos
  [ ] Los planes de sprint 07 y 08 son coherentes con la velocidad histórica
      del equipo (21 pts/sprint) — ajustar alcance si es necesario
  [ ] La estrategia de testing cubre los nuevos features
  [ ] El Definition of Done aplica a las nuevas historias sin contradicciones

Si el modelo de datos en modelo-datos-logico.md no tiene los campos nuevos,
ACTUALIZARLO con las siguientes adiciones:

  PuntoLocal (SQLite Mobile):
    + string? Titulo         → nombre visible del marker (antes era Nombre)
    + string? Descripcion    → descripción del punto
    + double RadioMetros     → radio de agrupación propio (default 50)
    + bool IsDeleted         → soft-delete para sync
    + DateTime UpdatedAt     → para resolución de conflictos

  FotoLocal (SQLite Mobile):
    + string? Comentario     → descripción individual de la foto
    + bool IsDeleted         → soft-delete para sync
    + DateTime UpdatedAt     → para resolución de conflictos

  Punto (SQL Server — EF Core):
    + string? Titulo
    + string? Descripcion
    + double RadioMetros
    + bool IsDeleted
    + DateTime UpdatedAt

  Foto (SQL Server — EF Core):
    + string? Comentario
    + bool IsDeleted
    + DateTime UpdatedAt

Si hay migraciones EF Core que no contemplen estos campos, CREARLAS.
Si hay migrations pendientes de aplicar, documentar el comando en la guía.

Cuando termines, confirmá con exactamente:
"FASE 2 COMPLETA — coherencia verificada. X inconsistencias encontradas y
corregidas. Modelo de datos actualizado. API spec completa."

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
FASE 3 — IMPLEMENTACIÓN DE CÓDIGO + TESTS HASTA EL ÉXITO
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

REGLA FUNDAMENTAL: No declarés éxito en ningún item hasta que el código
compila, el test pasa Y la captura ADB (cuando aplica) lo confirma.
Si algo falla: analizá el error, corregí, reintentá. Máximo 3 intentos por
item antes de documentar el bloqueo y continuar con el siguiente.

── PASO 3.0 — SETUP Y DETECCIÓN DE ENTORNO ────────────────────────────────────

Ejecutar este bloque PowerShell para detectar el entorno real:

```powershell
# Detectar ADB
$adbRutas = @(
    "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
    "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    "C:\Android\platform-tools\adb.exe",
    (Get-Command adb -ErrorAction SilentlyContinue)?.Source
)
$global:ADB = $adbRutas | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
& $global:ADB kill-server; Start-Sleep 2; & $global:ADB start-server; Start-Sleep 3
$global:DEV = ((& $global:ADB devices | Select-String "\sdevice$" | Select-Object -First 1) -replace "\s.*","").Trim()
$global:PKG = "com.companyname.geofoto.mobile"
$global:SS  = "scripts\screenshots\sprint07"
New-Item -ItemType Directory -Force $global:SS | Out-Null
& $global:ADB -s $global:DEV reverse tcp:5000 tcp:5000
Write-Host "ADB=$global:ADB | DEV=$global:DEV | PKG=$global:PKG"
```

── PASO 3.1 — MIGRACIÓN EF CORE (si hay campos nuevos) ─────────────────────────

Si en Fase 2 se detectaron campos nuevos en el modelo EF Core:

```bash
dotnet ef migrations add Sprint07_ExtendedModels --project GeoFoto.Api
dotnet ef database update --project GeoFoto.Api
```

Verificar: `dotnet build GeoFoto.Api` → 0 errores.

── PASO 3.2 — IMPLEMENTAR GEO-US20: CENTRAR MAPA (GPS FAB) ────────────────────

Archivos a modificar/crear:
  GeoFoto.Shared/Pages/Mapa.razor         → lógica del FAB GPS
  GeoFoto.Mobile/Services/DeviceLocationService.cs  → ya existe, verificar
  GeoFoto.Shared/wwwroot/js/leaflet-interop.js      → setView() debe existir

Comportamiento:
  - FAB GPS (azul, abajo derecha) al tocarlo:
    1. Llama IDeviceLocationService.GetCurrentLocationAsync()
    2. Si OK: leafletInterop.setView(lat, lng, 15)
    3. Si timeout (10s): MudSnackbar Warning "No se pudo obtener ubicación"
    4. Si permiso denegado: ver ESC-03 (dialog + botón Configuración)
  - ESC-02: si initMap falla, mostrar div de error con botón Reintentar
  - ESC-03: solicitar permiso GPS antes de centrar; manejar los 3 casos
    (denegado temporal → dialog, denegado permanente → snackbar fijo + mapa sin GPS)

Tests (GeoFoto.Tests/Unit/US20_CentrarMapaTests.cs):
  [Fact] CentrarMapa_PermisoConcedido_InvokaSetView()
  [Fact] CentrarMapa_Timeout_MuestraSnackbarWarning()
  [Fact] CentrarMapa_PermisoDenegado_MuestraDialog()
  [Fact] InitMap_Falla_MuestraPantallaError()

── PASO 3.3 — IMPLEMENTAR GEO-US21: POSICIÓN PROPIA EN MAPA ───────────────────

Archivos a modificar:
  GeoFoto.Shared/wwwroot/js/leaflet-interop.js
    → Agregar: updateUserPosition(lat, lng) — usa L.circleMarker con pulso CSS
    → Agregar: clearUserPosition()
  GeoFoto.Shared/Pages/Mapa.razor
    → Iniciar polling de posición cada 5 s con DeviceLocationService
    → Al perder GPS: clearUserPosition() + snackbar

Tests (GeoFoto.Tests/Unit/US21_PosicionPropiaTests.cs):
  [Fact] PosicionPropia_GPSActivo_MarkerActualizado()
  [Fact] PosicionPropia_GPSPerdido_MarkerEliminado()

── PASO 3.4 — IMPLEMENTAR GEO-US22: RADIO VISUAL DEL MARKER ──────────────────

Archivos a modificar/crear:
  GeoFoto.Shared/wwwroot/js/leaflet-interop.js
    → showMarkerRadius(puntoId, lat, lng, radioMetros) — L.circle semitransparente
    → hideMarkerRadius(puntoId)
    → updateMarkerRadius(puntoId, radioMetros)
  GeoFoto.Shared/Components/MarkerPopup.razor
    → Slider MudSlider min=10 max=500 step=10 para radio
    → Al cambiar: updateMarkerRadius + guardar en Preferences + SQLite
  GeoFoto.Mobile/Services/LocalDbService.cs
    → UpdatePuntoRadioAsync(int puntoId, double radioMetros)

Tests (GeoFoto.Tests/Unit/US22_RadioMarkerTests.cs):
  [Fact] RadioMarker_Cambiado_PersisteSQLite()
  [Fact] RadioMarker_Cambiado_PersistsPreferences()
  [Fact] FotoNueva_DentroRadio_AsociaAlPuntoExistente()
  [Fact] FotoNueva_FueraRadio_CreaNuevoPunto()

── PASO 3.5 — IMPLEMENTAR GEO-US23: POPUP + CARRUSEL + AMPLIAR ───────────────

Archivos a crear:
  GeoFoto.Shared/Components/MarkerPopup.razor
    Parámetros: int PuntoId, bool IsMobile
    Contenido:
      - MudTextField Titulo (editable, guarda on-blur)
      - MudTextField Descripcion (multilínea, editable, guarda on-blur)
      - FotoCarousel (componente hijo)
      - Botón "Agregar foto" (solo Mobile: llama CameraService; Web: MudFileUpload)
      - Botón "Eliminar marker" → GEO-US29
      - Botón "Cerrar"

  GeoFoto.Shared/Components/FotoCarousel.razor
    Parámetros: List<FotoLocal> Fotos, int PuntoId, bool IsMobile
    Contenido:
      - Imagen actual con prev/next arrows
      - MudTextField Comentario bajo la imagen (editable, guarda on-blur)
      - Botón ✕ por foto → confirm dialog → EliminarFoto()
      - Tap en imagen → visor fullscreen (MudOverlay)
      - Si Fotos.Count == 0: mensaje "Este punto no tiene fotos..."

  GeoFoto.Shared/Components/FotoViewer.razor (fullscreen)
    - MudOverlay con imagen a tamaño completo
    - MudTextField Comentario (editable)
    - Botón ✕ para cerrar
    - Parámetro: FotoLocal Foto

Integración en Mapa.razor:
  - OnMarkerClick (JS callback) → abre MudDialog con MarkerPopup
  - Asegurarse que leaflet-interop.js invoca dotnetRef.invokeMethodAsync("OnMarkerClick", puntoId)

Tests (GeoFoto.Tests/Unit/US23_CarruselTests.cs):
  [Fact] MarkerPopup_AbreConFotos_CarruselVisible()
  [Fact] MarkerPopup_SinFotos_MensajeSinFotosVisible()
  [Fact] FotoCarousel_NavegaNext_IndiceActualizado()
  [Fact] FotoCarousel_NavegaPrev_IndiceActualizado()
  [Fact] FotoCarousel_EliminarFoto_EncolarPendingDelete()
  [Fact] LocalDbService_GuardarTituloPunto_PersisteSQLite()
  [Fact] LocalDbService_GuardarDescripcionPunto_PersisteSQLite()
  [Fact] LocalDbService_GuardarComentarioFoto_PersisteSQLite()

── PASO 3.6 — IMPLEMENTAR GEO-US24: OFFLINE-FIRST + SYNC AUTO ────────────────

Ya debe existir SyncService. Verificar y completar:

  SyncService.PushAsync():
    - Procesar cola SyncQueue Pending en orden CreatedAt ASC
    - Create Punto: POST /api/puntos → 201 → actualizar RemoteId, SyncStatus="Synced"
    - Create Foto: POST /api/fotos/upload multipart → actualizar RemoteId
    - Update Punto: PUT /api/puntos/{remoteId} con {titulo, descripcion, radioMetros, updatedAt}
    - Update Foto: PUT /api/fotos/{remoteId} con {comentario, updatedAt}
    - Delete Foto: DELETE /api/fotos/{remoteId} → 204 → borrar FotoLocal
    - Delete Punto: DELETE /api/puntos/{remoteId} → 204 → borrar PuntoLocal
    - Error de red: RetryCount++. Si >= 3 → Status="Failed"

  SyncService.PullAsync():
    - GET /api/sync/delta?since={ultimoSyncUtc}
    - Por cada punto: insertar si nuevo, actualizar si remoto más reciente, eliminar si IsDeleted
    - ESC-01: markers superpuestos del servidor → insertar como nuevos SIN merge

  ConflictResolver.cs:
    - RemotoMásReciente (UpdatedAt): UseRemote
    - LocalMásReciente: UseLocal
    - EliminadoRemoto + LocalEditado: AskUser → MudDialog

  ConnectivityService: detectar cambio de conectividad → disparar PushAsync
  AppBar badge: items Pending (naranja/número), sincronizando (spinner), OK (verde), error (rojo)

  API — SyncController (crear si no existe):
    GET /api/sync/delta?since={utc} → puntos + fotos modificados desde esa fecha
    Incluir IsDeleted=true para notificar eliminaciones

Tests (GeoFoto.Tests/Unit/US24_SyncTests.cs):
  [Fact] PushAsync_CreatePunto_EnviaPostYActualizaRemoteId()
  [Fact] PushAsync_ErrorRed_IncrementaRetryCount()
  [Fact] PushAsync_RetryCount3_MarcaFailed()
  [Fact] PushAsync_ESC01_MarkersSuperposicion_InsertaComoNuevo()
  [Fact] PullAsync_PuntosNuevos_InsertaEnSQLite()
  [Fact] PullAsync_IsDeletedTrue_EliminaLocal()
  [Fact] ConflictResolver_RemotoMasReciente_UseRemote()
  [Fact] ConflictResolver_LocalMasReciente_UseLocal()
  [Fact] ConflictResolver_EliminadoRemotoLocalEditado_AskUser()
  [Fact] SyncBadge_PendingCount_ActualizaCorrectamente()

── PASO 3.7 — IMPLEMENTAR GEO-US25/26: QUITAR Y AMPLIAR FOTOS ────────────────

(Mayormente cubiertos en 3.5. Verificar que estén completos y con tests.)
Tests adicionales en US25_QuitarFotoTests.cs y US26_AmpliarFotoTests.cs.

── PASO 3.8 — IMPLEMENTAR GEO-US27: PANTALLA SINCRONIZACIÓN ──────────────────

Archivo: GeoFoto.Shared/Pages/Sincronizacion.razor
  - Lista de SyncQueue con columnas: Tipo, Operación, Estado, Reintentos, Fecha
  - Chip por estado: Pending (naranja), Synced (verde), Failed (rojo)
  - Botón "Sincronizar ahora" → PushAsync() + PullAsync() → actualizar lista
  - Texto "Última sincronización: {fecha}" o "Nunca" si no hay histórico
  - Durante sync: deshabilitar botón + spinner

Tests (US27_SincronizacionTests.cs):
  [Fact] PantallaSync_MuestraItemsPendientes()
  [Fact] PantallaSync_BotonSincronizar_DispararaPushYPull()
  [Fact] PantallaSync_UltimaSyncFecha_ActualizaPostSync()

── PASO 3.9 — IMPLEMENTAR GEO-US28/29: LISTA DE MARKERS + ELIMINAR ──────────

Archivo: GeoFoto.Shared/Pages/Markers.razor (o ListaPuntos.razor si existe)
  - MudTable con: Titulo, Coordenadas, N Fotos, SyncStatus chip
  - MudTextField para búsqueda en tiempo real (filtra por Titulo)
  - Tap en fila → navegar al mapa centrado en ese marker + abrir popup
  - Desde popup: botón Eliminar marker (GEO-US29 — confirm + delete + quitar del mapa)

Tests (US28_ListaMarkersTests.cs, US29_EliminarMarkerTests.cs):
  [Fact] ListaMarkers_MuestraTodosLosPuntos()
  [Fact] ListaMarkers_Busqueda_FiltraPorTitulo()
  [Fact] EliminarMarker_Confirma_EliminaLocalYEncola()
  [Fact] EliminarMarker_Cancela_NoModificaNada()

── PASO 3.10 — IMPLEMENTAR GEO-US30: COMPARTIR FOTO (ANDROID) ────────────────

Archivo: GeoFoto.Mobile/Services/ShareService.cs
  - Usa Share.RequestAsync() de MAUI Essentials
  - Comparte archivo físico desde RutaLocal
  - Si no disponible: retorna false → Mapa.razor muestra snackbar info

Tests (US30_CompartirTests.cs):
  [Fact] ShareService_ArchivoExiste_InvokaShareApi()
  [Fact] ShareService_ArchivoNoExiste_RetornaFalse()

── PASO 3.11 — IMPLEMENTAR GEO-US31/32/33: WEB ──────────────────────────────

GEO-US31 (Web): reutilizar todos los componentes Shared (MarkerPopup, FotoCarousel,
FotoViewer). Los permisos de geolocation usan JS Interop con navigator.geolocation.
Agregar en leaflet-interop.js para web: manejo del caso de permiso browser denegado.

GEO-US32 (Descarga zip):
  - Endpoint API: GET /api/puntos/{id}/fotos/download → retorna .zip con todas las fotos
  - En MarkerPopup.razor (cuando IsMobile=false): botón "Descargar fotos"
    → hace GET al endpoint y dispara descarga browser vía JS

GEO-US33 (Subir foto desde web):
  - En MarkerPopup.razor (IsMobile=false): MudFileUpload "Agregar foto"
  - POST /api/fotos/upload con puntoId en la URL o form field
  - ESC-04: foto sin EXIF → vincula al puntoId sin coordenadas propias
  - Actualizar carrusel tras subida exitosa

Tests (US31_WebGeoTests.cs, US32_DescargaZipTests.cs, US33_SubirFotoWebTests.cs):
  [Fact] Web_PermisoBrowserDenegado_MuestraMensajeConfig()
  [Fact] DescargaZip_PuntoConFotos_RetornaZip()
  [Fact] DescargaZip_PuntoSinFotos_BotonDeshabilitado()
  [Fact] SubirFotoWeb_SinEXIF_VinculaAlMarker()
  [Fact] SubirFotoWeb_ConEXIF_VinculaAlMarker()

── PASO 3.12 — EJECUTAR SUITE COMPLETA DE TESTS ─────────────────────────────

```bash
dotnet build GeoFoto.sln
# Esperado: 0 errores

dotnet test GeoFoto.Tests/GeoFoto.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --verbosity normal \
  --logger "trx;LogFileName=resultados_sprint07.trx"
# Esperado: todos los tests pasan
# Cobertura SyncService >= 85%
# Cobertura LocalDbService >= 90%
```

Si algún test falla: leer el error, corregir el código (NO el test), reejecutar.
Repetir hasta que todos pasen.

── PASO 3.13 — REORGANIZAR SCRIPTS ──────────────────────────────────────────

MOVER y ACTUALIZAR RUTAS en todos los scripts:

De scripts/ a scripts/launch-all/:
  00_setup.bat
  01_start_api.bat
  02_start_web.bat (o equivalente)
  06b_start_services.bat (o el script que lanza API + Web juntos)
  07b_deploy_and_capture.bat (lanzar APK en celular)
  07_full_rebuild.bat

De scripts/ a scripts/test-all/:
  05_run_tests.bat
  08_validate_screenshots.bat
  Cualquier script con "test" o "validate" o "run" en el nombre

Para CADA script movido:
  1. Actualizá todas las rutas relativas que ahora cambiaron por el nivel extra
     de carpeta (ej: "..\GeoFoto.Api" en vez de "GeoFoto.Api")
  2. Actualizá las referencias cruzadas entre scripts (si un bat llama a otro bat,
     la ruta también cambia)
  3. Verificá que el script sigue funcionando después del movimiento

Crear scripts/launch-all/README.md con descripción de cada script y orden de ejecución.
Crear scripts/test-all/README.md con descripción de cada script de test.

Actualizar docs/10_developer_guide/guia-setup-proyecto_v1.0.md con las nuevas rutas.

Cuando termines toda la Fase 3, confirmá con exactamente:
"FASE 3 COMPLETA — X tests nuevos, todos en verde. Cobertura SyncService Y%.
Scripts reorganizados en launch-all y test-all. Build: 0 errores."

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
FASE 4 — GENERACIÓN DE PROMPTS PARA SPRINTS FUTUROS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Crear los siguientes archivos en docs/11_prompting-ia/:

  10-prompt-claude-code-sprint07-android.md
    Prompt focalizado en GEO-US20 a GEO-US30 (Android).
    Misma estructura que los prompts existentes: lectura obligatoria,
    estado actual confirmado, arquitectura, features en orden con tests
    y verificación ADB por feature. Termina con checklist completo.

  11-prompt-claude-code-sprint08-web.md
    Prompt focalizado en GEO-US31 a GEO-US33 (Web).
    Incluye los escenarios ESC-04. Verificación via browser screenshots.

  12-prompt-claude-code-regression.md
    Prompt para ejecutar la suite de regresión completa: build → tests →
    deploy → capturas ADB → validar capturas → reporte de estado.
    Referencia scripts/test-all/ para la ejecución.

Cada prompt debe:
  - Comenzar con lectura obligatoria de los docs relevantes
  - Listar el estado confirmado del sistema al inicio del sprint
  - Detallar cada feature con comportamiento, archivos a modificar y tests
  - Incluir verificación ADB / browser para cada feature
  - Terminar con checklist y confirmación de cierre de sprint

Cuando termines, confirmá con exactamente:
"FASE 4 COMPLETA — 3 prompts generados en docs/11_prompting-ia/."

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
FASE 5 — SCRIPTS END-TO-END + LANZAR APLICACIÓN
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

── PASO 5.1 — CREAR scripts/launch-all/launch-all.bat ───────────────────────

Script orquestador maestro que hace TODO en secuencia:

```
FASE 1: Verificaciones (ADB, .NET, EF Tools, SQL Server)
FASE 2: Build completo (dotnet build GeoFoto.sln → 0 errores)
FASE 3: Migrations EF Core (dotnet ef database update)
FASE 4: Lanzar API en background (puerto 5000) + wait health check
FASE 5: Lanzar Web en background (puerto 5001) + wait health check
FASE 6: Build APK Android Debug + install en celular conectado
FASE 7: ADB tunnel (reverse tcp:5000 tcp:5000)
FASE 8: Lanzar app en celular (am start)
FASE 9: Esperar 8 s + capturar screenshot de confirmación
FASE 10: Validar que el screenshot muestra el mapa (no pantalla de error)
```

Estructura del script:
  - setlocal enabledelayedexpansion
  - Variables: ADB_PATH, APP_ID, API_PORT=5000, WEB_PORT=5001
  - Detección automática de ADB (misma lógica de scripts existentes)
  - Detección automática del dispositivo conectado
  - Cada fase con echo "[FASE N/10]..." + [OK]/[ERROR] + exit /b 1 si falla
  - Health check API: curl http://localhost:5000/api/puntos → 200
  - Health check Web: curl http://localhost:5001 → 200
  - Timeout de 30 s por health check con retry cada 3 s
  - Al final: resumen de estado de los 3 componentes

── PASO 5.2 — CREAR scripts/test-all/test-e2e.bat ───────────────────────────

Script de test end-to-end que asume API + Web + App ya corriendo:

```
FASE 1: Verificar servicios activos (API, Web, App en celular)
FASE 2: Ejecutar dotnet test → todos los tests deben pasar
FASE 3: Secuencia de capturas ADB del flujo principal:

  e2e_01_inicio_mapa.png           → App abierta, mapa visible con tiles OSM
  e2e_02_posicion_propia.png       → Marcador de posición propia visible
  e2e_03_centrar_mapa.png          → Post-tap FAB GPS, mapa centrado
  e2e_04_marker_existente.png      → Marker de punto existente visible
  e2e_05_popup_abierto.png         → Click en marker, popup abierto
  e2e_06_carrusel_fotos.png        → Carrusel con fotos en el popup
  e2e_07_foto_ampliada.png         → Foto en fullscreen
  e2e_08_lista_markers.png         → Pantalla lista de markers
  e2e_09_pantalla_sync.png         → Pantalla de sincronización
  e2e_10_badge_synced.png          → Badge verde en AppBar

FASE 4: Validar cada captura:
  - Verificar que el archivo existe y no está vacío
  - Verificar que no hay pantalla negra (pixel promedio no es negro)
  - Log de [OK] o [FAIL] por captura

FASE 5: Generar reporte en scripts/screenshots/sprint07/reporte_e2e.txt
  - Fecha/hora
  - Tests unitarios: N pasaron / M fallaron
  - Capturas ADB: N OK / M FAIL
  - Estado general: SUCCESS / FAILURE
```

── PASO 5.3 — EJECUTAR scripts/launch-all/launch-all.bat ────────────────────

Ejecutar el script. Si algo falla:
  - Leer el error completo
  - Corregir el script o el código según corresponda
  - Reejecutar
  - Repetir hasta que los 3 componentes estén corriendo

── PASO 5.4 — EJECUTAR scripts/test-all/test-e2e.bat ────────────────────────

Con los servicios corriendo, ejecutar el test E2E.
Si alguna captura falla, analizar el log, corregir, reejecutar.
No declarar éxito hasta que reporte_e2e.txt muestre SUCCESS.

── PASO 5.5 — GIT PUSH AL CERRAR SPRINT ─────────────────────────────────────

Una vez que test-e2e.bat reporta SUCCESS:

```bash
git add -A
git commit -m "feat(GEO-US20-33): Sprint 07 completo — GPS, radio, carrusel, sync, web

- GEO-US20: centrar mapa en posición GPS
- GEO-US21: marcador de posición propia en mapa
- GEO-US22: radio visual y configurable del marker
- GEO-US23: popup con carrusel, título, descripción por foto
- GEO-US24: offline-first + sync bidireccional + conflictos
- GEO-US25: quitar fotos desde carrusel
- GEO-US26: ampliar fotos fullscreen con descripción
- GEO-US27: pantalla sincronización con estado e historial
- GEO-US28: lista de markers con búsqueda
- GEO-US29: eliminar marker con fotos
- GEO-US30: compartir foto nativa Android
- GEO-US31-33: web — geolocation browser, descarga zip, subir foto
- ESC-01-04: escenarios de borde implementados y testeados
- scripts/launch-all: reorganización de scripts de lanzado
- scripts/test-all: reorganización de scripts de test
- docs: actualización completa Sprint 07 + Sprint 08"

git tag v1.1.0-sprint07
git push origin main --tags
```

Cuando termines la Fase 5 completa, confirmá con exactamente:
"FASE 5 COMPLETA — launch-all.bat operativo (API + Web + Mobile corriendo).
test-e2e.bat: SUCCESS (N/10 capturas OK, todos los tests en verde).
Git push realizado: tag v1.1.0-sprint07."

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
CIERRE GENERAL
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Al completar las 5 fases, reportar:

  SPRINT 07 ENTREGADO
  ═══════════════════
  Historias implementadas : GEO-US20 a GEO-US33 (14 historias)
  Tests unitarios nuevos  : [N]
  Cobertura SyncService   : [X]%
  Cobertura LocalDbService: [Y]%
  Capturas E2E            : [N]/10 OK
  Scripts reorganizados   : launch-all ([N] scripts), test-all ([M] scripts)
  Docs actualizados       : [N] archivos
  Docs nuevos             : [M] archivos
  Git tag                 : v1.1.0-sprint07

  PENDIENTE SPRINT 08: GEO-US30 (compartir) si quedó fuera de alcance.
```
