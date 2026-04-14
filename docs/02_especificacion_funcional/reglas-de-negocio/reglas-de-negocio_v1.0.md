# Reglas de Negocio

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** reglas-de-negocio_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico  

---

## Tabla de Contenidos

1. [RN-01: Escritura local primero (Local-First Write)](#rn-01-escritura-local-primero-local-first-write)
2. [RN-02: SyncService en background sin bloqueo de UI](#rn-02-syncservice-en-background-sin-bloqueo-de-ui)
3. [RN-03: Fotos sin GPS crean punto en coordenadas (0,0)](#rn-03-fotos-sin-gps-crean-punto-en-coordenadas-00)
4. [RN-04: Backoff exponencial en reintentos de sincronización](#rn-04-backoff-exponencial-en-reintentos-de-sincronización)
5. [RN-05: Last-Write-Wins por campo UpdatedAt (UTC)](#rn-05-last-write-wins-por-campo-updatedat-utc)
6. [RN-06: Imágenes en filesystem local de Android](#rn-06-imágenes-en-filesystem-local-de-android)
7. [RN-07: Eliminación local con estado PendingDelete](#rn-07-eliminación-local-con-estado-pendingdelete)
8. [RN-08: Dualidad de fuentes de verdad](#rn-08-dualidad-de-fuentes-de-verdad)
9. [Control de Cambios](#2-control-de-cambios)

---

## RN-01: Escritura local primero (Local-First Write)

### Descripción

Toda operación de escritura —creación, edición o eliminación— sobre las entidades ``Puntos_Local`` y ``Fotos_Local`` se persiste primero en la base de datos SQLite local del dispositivo móvil, sin requerir conectividad de red como precondición. La transacción local es síncrona, inmediata y atómica respecto a SQLite. Cada operación genera además un registro en la tabla ``SyncQueue`` con la información necesaria para su posterior envío al servidor.

### Motivación de Negocio

El contexto operativo de GeoFoto.Mobile contempla el uso en zonas rurales, industriales o remotas donde la conectividad de red es inexistente, intermitente o de baja calidad. Si las operaciones de escritura dependieran de la disponibilidad de red, el técnico de campo no podría registrar datos durante extensos períodos, lo que provocaría pérdida de información crítica y reduciría la productividad. La estrategia local-first garantiza cero pérdida de datos y una experiencia de usuario fluida independientemente del estado de la red.

### Definición Formal

Sea $O$ una operación de escritura $O \in \{Create, Update, Delete\}$ sobre una entidad $E \in \{Punto, Foto\}$ ejecutada desde GeoFoto.Mobile:

1. $O$ se ejecuta contra la base de datos SQLite local mediante ``sqlite-net-pcl`` dentro de una transacción atómica.
2. Al completarse exitosamente $O$, se inserta un registro $R$ en ``SyncQueue`` con:
   - ``OperationType`` = tipo de $O$
   - ``EntityType`` = tipo de $E$
   - ``LocalId`` = identificador local de $E$
   - ``Payload`` = serialización JSON del estado actual de $E$
   - ``Status`` = ``Pending``
   - ``Attempts`` = 0
   - ``CreatedAt`` = ``DateTime.UtcNow``
3. En ningún caso la ejecución de $O$ queda condicionada por la respuesta de la API REST ni por el estado de conectividad del dispositivo.

### Condiciones de Aplicación

- Se aplica a todas las operaciones de escritura originadas en **GeoFoto.Mobile** (.NET MAUI Hybrid para Android).
- Se aplica a los casos de uso: **CU-01** (captura desde galería), **CU-02** (captura desde cámara), **CU-04** (almacenamiento local en SQLite), **CU-07** (edición de punto), **CU-08** (eliminación de punto).
- La regla abarca tanto las operaciones sobre ``Puntos_Local`` como sobre ``Fotos_Local`` y la tabla ``SyncQueue``.
- Se aplica independientemente del estado de sincronización previo del registro (``Synced``, ``PendingCreate``, ``PendingUpdate``).

### Excepciones

- **CU-16 (Carga web de fotografías):** Las operaciones realizadas desde GeoFoto.Web se persisten directamente en SQL Server a través de la API REST, dado que el cliente web opera exclusivamente con conectividad activa. No se utiliza SQLite ni ``SyncQueue`` en el contexto de GeoFoto.Web.
- Si la transacción SQLite local falla por motivos de integridad o espacio en disco, la operación se rechaza y se notifica al usuario mediante un ``MudSnackbar`` con severidad ``Error``. No se registra la operación en ``SyncQueue``.

### Criterios de Validación BDD (Dado/Cuando/Entonces)

**Escenario 1: Creación de punto sin conectividad**

- **Dado** que el dispositivo móvil no dispone de conexión a Internet y el técnico de campo se encuentra en la pantalla principal de GeoFoto.Mobile,
- **Cuando** el técnico captura una fotografía con coordenadas EXIF válidas (-34.6037, -58.3816),
- **Entonces** el sistema crea un registro en ``Puntos_Local`` con ``SyncStatus = PendingCreate``, crea un registro en ``Fotos_Local`` asociado al punto, inserta una entrada en ``SyncQueue`` con ``OperationType = Create`` y ``Status = Pending``, y muestra un ``MudSnackbar`` confirmando "Foto agregada al punto (-34.6037, -58.3816)".

**Escenario 2: Edición de punto sin conectividad**

- **Dado** que existe un punto "Poste #12" con ``SyncStatus = Synced`` en ``Puntos_Local`` y el dispositivo no tiene conexión a Internet,
- **Cuando** el técnico modifica el nombre del punto a "Poste de alumbrado #12" y guarda los cambios,
- **Entonces** el sistema actualiza el registro en ``Puntos_Local`` con el nuevo nombre, establece ``SyncStatus = PendingUpdate``, asigna ``UpdatedAt = DateTime.UtcNow``, e inserta un registro en ``SyncQueue`` con ``OperationType = Update``.

**Escenario 3: Escritura local con conectividad disponible**

- **Dado** que el dispositivo móvil tiene conexión a Internet activa,
- **Cuando** el técnico crea un nuevo punto mediante captura de fotografía,
- **Entonces** el sistema persiste los datos en SQLite local de forma inmediata (comportamiento idéntico al escenario sin conectividad) y registra la operación en ``SyncQueue``. La sincronización con el servidor se delega al ``SyncService`` en background.

### Impacto en Casos de Uso

| Caso de Uso | Impacto |
|-------------|---------|
| CU-01 — Captura desde galería | La foto y el punto se persisten en ``Fotos_Local`` y ``Puntos_Local`` antes de cualquier intento de comunicación con el servidor. |
| CU-02 — Captura desde cámara | Comportamiento idéntico a CU-01; la imagen capturada por ``MediaPicker`` se almacena localmente de forma inmediata. |
| CU-04 — Almacenamiento local SQLite | Es el caso de uso principal regido por esta regla; define el flujo completo de persistencia local. |
| CU-07 — Edición de punto | La actualización del nombre y la descripción se realiza contra ``Puntos_Local``; se registra ``PendingUpdate`` en ``SyncQueue``. |
| CU-08 — Eliminación de punto | El registro se marca con ``PendingDelete`` en lugar de eliminarse físicamente (ver RN-07); se registra ``Delete`` en ``SyncQueue``. |

---

## RN-02: SyncService en background sin bloqueo de UI

### Descripción

El ``SyncService`` opera como un servicio en segundo plano (background service) dentro de GeoFoto.Mobile, ejecutándose de forma completamente asíncrona respecto al hilo de la interfaz de usuario. Ninguna operación de sincronización —push al servidor, pull delta, resolución de conflictos ni reintentos— bloquea la interacción del técnico de campo con la aplicación. El servicio se coordina internamente mediante un ``SemaphoreSlim(1,1)`` para garantizar que no se produzcan ejecuciones concurrentes del ciclo de sincronización.

### Motivación de Negocio

El técnico de campo necesita continuar capturando fotografías y registrando puntos georeferenciados mientras se procesan sincronizaciones en background. Si el proceso de sincronización bloqueara la interfaz de usuario, el técnico perdería eficiencia operativa al tener que esperar la finalización de cada ciclo de envío/recepción de datos, especialmente en escenarios de conectividad lenta o con grandes volúmenes de fotografías pendientes. La experiencia de usuario fluida es un requisito crítico para la adopción del sistema en el campo.

### Definición Formal

Sea $S$ el ``SyncService`` y $UI$ el hilo de renderizado de la interfaz (Blazor/MAUI):

1. $S$ se ejecuta en un hilo de ejecución independiente mediante ``Task.Run()`` o un ``BackgroundService`` registrado en el contenedor de inyección de dependencias.
2. $S$ adquiere un ``SemaphoreSlim(1,1)`` al iniciar cada ciclo de sincronización e invoca ``SemaphoreSlim.Release()`` al finalizar, impidiendo ejecuciones concurrentes.
3. En ningún instante $t$ la ejecución de $S$ invoca ``await`` en el hilo de $UI$ ni bloquea el dispatcher principal de la aplicación.
4. La comunicación entre $S$ y $UI$ se realiza mediante eventos observables (``IObservable<SyncProgress>``) o callbacks registrados que se despachan al hilo de UI mediante ``InvokeAsync()``.

### Condiciones de Aplicación

- Se aplica exclusivamente al componente ``SyncService`` de **GeoFoto.Mobile**.
- Se aplica a los casos de uso: **CU-11** (sincronización automática), **CU-12** (push de operaciones pendientes), **CU-13** (pull delta desde servidor).
- Se aplica durante todo el ciclo de vida de la aplicación mientras el servicio esté activo.
- Se aplica tanto a la sincronización automática activada por cambio de conectividad como a la sincronización activada por temporizador periódico.

### Excepciones

- **CU-10 (Sincronización manual):** Cuando el usuario activa la sincronización manual mediante un botón de acción, se muestra un componente ``MudProgressLinear`` con modo indeterminado que indica el progreso de la operación. Sin embargo, la interfaz de usuario permanece completamente responsiva (el técnico puede navegar, consultar puntos y visualizar el mapa). El indicador de progreso proporciona retroalimentación visual sin bloquear la interacción.

### Criterios de Validación BDD (Dado/Cuando/Entonces)

**Escenario 1: Captura de fotos durante sincronización activa**

- **Dado** que el ``SyncService`` se encuentra procesando una cola de 15 operaciones pendientes en background y el técnico se encuentra en la pantalla de captura,
- **Cuando** el técnico captura una nueva fotografía con la cámara nativa,
- **Entonces** la captura se procesa de forma inmediata (< 500ms de latencia percibida), la foto se almacena en ``Fotos_Local`` y el punto se crea en ``Puntos_Local``, sin que la operación de sincronización en curso afecte la responsividad de la interfaz.

**Escenario 2: Navegación del mapa durante sincronización**

- **Dado** que el ``SyncService`` está ejecutando un pull delta que descarga 50 registros actualizados del servidor,
- **Cuando** el técnico interactúa con el mapa Leaflet (zoom, pan, click en markers),
- **Entonces** el mapa responde de forma fluida a todas las interacciones del usuario y, al finalizar el pull delta, los nuevos markers se incorporan al mapa sin requerir recarga manual.

**Escenario 3: Sincronización manual con indicador de progreso**

- **Dado** que el técnico se encuentra en la pantalla principal con 5 operaciones pendientes en ``SyncQueue``,
- **Cuando** el técnico pulsa el botón "Sincronizar" (``MudIconButton`` con ícono ``Icons.Material.Filled.Sync``),
- **Entonces** se muestra un ``MudProgressLinear`` en la parte superior de la pantalla, la interfaz permanece responsiva, y al finalizar la sincronización el indicador desaparece y se muestra un ``MudSnackbar`` con el resumen "5 operaciones sincronizadas correctamente".

### Impacto en Casos de Uso

| Caso de Uso | Impacto |
|-------------|---------|
| CU-10 — Sincronización manual | Se ejecuta en background con indicador visual ``MudProgressLinear``; la interfaz no se bloquea durante el proceso. |
| CU-11 — Sincronización automática | El ``SyncService`` detecta conectividad y procesa la cola automáticamente sin intervención ni bloqueo del usuario. |
| CU-12 — Push de operaciones pendientes | Cada operación de la ``SyncQueue`` se envía al servidor en background; los fallos se gestionan con reintentos (ver RN-04). |
| CU-13 — Pull delta desde servidor | La descarga incremental de datos del servidor se procesa en background y actualiza ``Puntos_Local`` y ``Fotos_Local`` sin afectar la UI. |

---

## RN-03: Fotos sin GPS crean punto en coordenadas (0,0)

### Descripción

Cuando se procesa una fotografía que no contiene metadatos EXIF de geolocalización (las etiquetas ``GpsLatitude``, ``GpsLongitude``, ``GpsLatitudeRef`` o ``GpsLongitudeRef`` están ausentes o son ilegibles), el sistema crea el punto georeferenciado asociado en las coordenadas $(0.0, 0.0)$ — correspondientes a la intersección del meridiano de Greenwich con el ecuador en el Golfo de Guinea. Se presenta al usuario una advertencia visual mediante el componente ``MudAlert`` con severidad ``Warning`` indicando la ausencia de datos GPS y la asignación de coordenadas predeterminadas.

### Motivación de Negocio

El principio rector de GeoFoto es que ninguna fotografía capturada por el técnico de campo se pierda bajo ninguna circunstancia. Si el sistema rechazara imágenes sin coordenadas GPS, el técnico perdería registros fotográficos potencialmente valiosos que fueron tomados con dispositivos sin sensor GPS activo, con cámaras externas, con capturas de pantalla, o en situaciones donde el módulo GPS no pudo obtener una posición fija. La asignación de coordenadas $(0, 0)$ como valor centinela permite que el registro se cree con normalidad y que el supervisor pueda identificar rápidamente los puntos sin geolocalización real para asignarles coordenadas correctas manualmente.

### Definición Formal

Sea $I$ una imagen procesada por el sistema y $EXIF(I)$ el conjunto de metadatos EXIF extraídos mediante ``ImageMetadataReader.ReadMetadata(stream)`` de MetadataExtractor:

1. Si $GpsDirectory \in EXIF(I)$ y las etiquetas ``GpsLatitude``, ``GpsLongitude``, ``GpsLatitudeRef``, ``GpsLongitudeRef`` son legibles:
   - Se extraen las coordenadas decimales $(lat, lon)$ y se asignan al punto.
2. Si $GpsDirectory \notin EXIF(I)$, o alguna de las etiquetas requeridas es nula, ausente o ilegible:
   - Se asigna $(lat, lon) = (0.0, 0.0)$.
   - Se emite un ``MudAlert`` con ``Severity = Severity.Warning`` y el mensaje: *"La imagen no contiene coordenadas GPS. Se asignó ubicación predeterminada (0, 0)."*
3. En ambos casos, la imagen y el punto se persisten normalmente siguiendo el flujo estándar (RN-01).

### Condiciones de Aplicación

- Se aplica a toda fotografía procesada en **GeoFoto.Mobile** (casos de uso CU-01, CU-02) y en **GeoFoto.Web** (caso de uso CU-16).
- Se aplica a los casos de uso: **CU-03** (extracción automática de coordenadas GPS del EXIF), **CU-16** (carga web de fotografías).
- Se aplica tanto a imágenes JPEG con cabecera EXIF incompleta como a formatos que no soportan EXIF (PNG, BMP, WebP).
- La regla se aplica en la capa de servicio (``ExifService``) tanto en el cliente móvil como en el servidor.

### Excepciones

No existen excepciones a esta regla. Toda fotografía procesada por el sistema genera un punto georeferenciado, independientemente de la disponibilidad de metadatos GPS. No se rechaza ninguna imagen por ausencia de coordenadas.

### Criterios de Validación BDD (Dado/Cuando/Entonces)

**Escenario 1: Captura de foto sin GPS en Mobile**

- **Dado** que el técnico se encuentra en la pantalla de captura de GeoFoto.Mobile,
- **Cuando** selecciona desde la galería una imagen ``captura_pantalla.png`` que no contiene metadatos EXIF de GPS,
- **Entonces** el sistema crea un punto en ``Puntos_Local`` con ``Latitud = 0.0`` y ``Longitud = 0.0``, crea el registro en ``Fotos_Local`` con ``LatitudExif = null`` y ``LongitudExif = null``, muestra un ``MudAlert`` con severidad ``Warning`` indicando "La imagen no contiene coordenadas GPS. Se asignó ubicación predeterminada (0, 0)", y el marker se posiciona en el mapa en las coordenadas (0, 0).

**Escenario 2: Carga de foto sin GPS en Web**

- **Dado** que el supervisor se encuentra en la pantalla de carga de GeoFoto.Web,
- **Cuando** carga una fotografía ``documento_scan.jpg`` que no contiene directorio EXIF GPS,
- **Entonces** el sistema crea un punto en la tabla ``Puntos`` de SQL Server con ``Latitud = 0.0`` y ``Longitud = 0.0``, almacena la foto en ``wwwroot/uploads/{year}/{month}/{guid}.jpg``, y muestra un ``MudAlert`` con severidad ``Warning`` indicando la ausencia de coordenadas GPS.

**Escenario 3: Foto con GPS válido (caso positivo)**

- **Dado** que el técnico captura una fotografía con la cámara nativa del dispositivo y el sensor GPS está activo,
- **Cuando** la imagen ``IMG_20260413.jpg`` contiene coordenadas EXIF (-34.6037, -58.3816),
- **Entonces** el sistema crea el punto con ``Latitud = -34.6037`` y ``Longitud = -58.3816``, no se muestra ningún ``MudAlert`` de advertencia, y el marker se posiciona correctamente en el mapa.

### Impacto en Casos de Uso

| Caso de Uso | Impacto |
|-------------|---------|
| CU-01 — Captura desde galería | Las imágenes de galería que no contengan datos GPS generan puntos en (0, 0) con advertencia al usuario. |
| CU-02 — Captura desde cámara | Escenario menos frecuente, ya que la cámara nativa generalmente embebe GPS; pero aplica si el sensor GPS está desactivado. |
| CU-03 — Extracción de coordenadas EXIF | Es el caso de uso directamente afectado; define el flujo de decisión entre coordenadas reales y coordenadas centinela. |
| CU-16 — Carga web de fotografías | Las imágenes cargadas desde el navegador web siguen la misma regla de asignación de coordenadas (0, 0). |

---

## RN-04: Backoff exponencial en reintentos de sincronización

### Descripción

Cuando una operación de la ``SyncQueue`` falla al ser enviada al servidor (error HTTP, timeout, fallo de red), el ``SyncService`` aplica una estrategia de backoff exponencial para los reintentos. Se permite un máximo de 3 intentos por operación. Tras 3 intentos fallidos consecutivos, la operación se marca con ``Status = Failed`` y no se reintenta automáticamente hasta el próximo ciclo de conectividad o una sincronización manual.

### Motivación de Negocio

En escenarios de conectividad inestable (redes móviles con señal débil, cortes intermitentes, servidores temporalmente no disponibles), los reintentos inmediatos y agresivos generan carga innecesaria sobre la red y el servidor, consumen batería del dispositivo y no incrementan la probabilidad de éxito. El backoff exponencial permite manejar fallos transitorios de red de forma eficiente: los intervalos crecientes dan tiempo a que las condiciones de red se estabilicen, mientras que el límite de 3 intentos evita ciclos infinitos de reintento para operaciones que presentan errores persistentes.

### Definición Formal

Sea $O_i$ el intento número $i$ de enviar una operación al servidor, con $i \in \{1, 2, 3\}$:

| Intento ($i$) | Tiempo de espera ($w_i$) | Tiempo acumulado |
|:---:|:---:|:---:|
| 1 | 5 segundos | 5 segundos |
| 2 | 30 segundos | 35 segundos |
| 3 | 5 minutos (300 segundos) | 5 minutos 35 segundos |

Formalmente:

1. Si $O_i$ falla con $i < 3$: se incrementa ``SyncQueue.Attempts`` a $i + 1$, se registra ``LastAttemptAt = DateTime.UtcNow``, se almacena el mensaje de error en ``ErrorMessage``, y se programa el siguiente intento $O_{i+1}$ tras $w_{i+1}$.
2. Si $O_3$ falla: se establece ``Status = Failed``, ``Attempts = 3``, ``LastAttemptAt = DateTime.UtcNow``, y ``ErrorMessage`` con el detalle del último error. La operación no se reintenta automáticamente.
3. Si $O_i$ tiene éxito en cualquier intento: se elimina el registro de ``SyncQueue`` y se actualiza el ``SyncStatus`` de la entidad local a ``Synced``.

### Condiciones de Aplicación

- Se aplica a todas las operaciones de la ``SyncQueue`` procesadas por el ``SyncService`` en **GeoFoto.Mobile**.
- Se aplica al caso de uso: **CU-12** (push de operaciones pendientes al servidor).
- Se aplica a todos los tipos de operación: ``Create``, ``Update`` y ``Delete``.
- Se aplica a los errores transitorios de red: timeout HTTP, errores 5xx del servidor, ``HttpRequestException``, ``TaskCanceledException``.
- Los errores de validación del servidor (respuestas 4xx) se consideran errores permanentes y la operación se marca como ``Failed`` de forma inmediata sin reintentos adicionales.

### Excepciones

- Los errores HTTP con código de estado 4xx (``BadRequest``, ``NotFound``, ``Conflict``, ``UnprocessableEntity``) no activan la estrategia de reintento, ya que se consideran errores de validación o lógica que no se resolverán con reintentos. La operación se marca como ``Failed`` inmediatamente.
- Si el dispositivo pierde la conectividad durante la secuencia de reintentos, el temporizador de espera se cancela y la operación permanece en la cola con su estado actual (``Pending``, ``Attempts = n``) para ser procesada cuando se restablezca la conexión.

### Criterios de Validación BDD (Dado/Cuando/Entonces)

**Escenario 1: Primer intento fallido con reintento exitoso**

- **Dado** que existe una operación de tipo ``Create`` en ``SyncQueue`` con ``Status = Pending`` y ``Attempts = 0``,
- **Cuando** el ``SyncService`` intenta enviar la operación al servidor y recibe un error HTTP 503 (Service Unavailable),
- **Entonces** el sistema establece ``Attempts = 1``, ``LastAttemptAt`` al instante actual UTC, ``ErrorMessage`` con el detalle del error, espera 5 segundos y ejecuta el segundo intento.

**Escenario 2: Tres intentos fallidos consecutivos**

- **Dado** que existe una operación en ``SyncQueue`` con ``Attempts = 2`` (ya falló dos veces),
- **Cuando** el ``SyncService`` ejecuta el tercer intento y recibe un error de timeout,
- **Entonces** el sistema establece ``Status = Failed``, ``Attempts = 3``, registra el mensaje de error, y no programa reintento adicional. La operación permanece en la cola para gestión manual.

**Escenario 3: Error de validación sin reintentos**

- **Dado** que existe una operación ``Create`` pendiente cuyo payload contiene datos inválidos,
- **Cuando** el servidor responde con HTTP 400 (BadRequest),
- **Entonces** el sistema marca la operación como ``Failed`` inmediatamente sin ejecutar reintentos adicionales, independientemente del valor actual de ``Attempts``.

### Impacto en Casos de Uso

| Caso de Uso | Impacto |
|-------------|---------|
| CU-12 — Push de operaciones pendientes | Es el caso de uso directamente regido por esta regla; define el comportamiento ante fallos de envío al servidor. |
| CU-10 — Sincronización manual | Cuando el usuario dispara sincronización manual, las operaciones con ``Status = Failed`` pueden ser reintentadas reiniciando el contador de intentos. |
| CU-11 — Sincronización automática | El ciclo automático respeta los tiempos de espera del backoff exponencial y no fuerza reintentos inmediatos. |

---

## RN-05: Last-Write-Wins por campo UpdatedAt (UTC)

### Descripción

Cuando se detecta un conflicto entre la versión local (SQLite) y la versión remota (SQL Server) de un mismo registro durante el proceso de sincronización, la resolución se determina comparando el campo ``UpdatedAt`` de ambas versiones. Prevalece el registro cuyo ``UpdatedAt`` sea más reciente. Todos los conflictos detectados y sus resoluciones se registran en un log de auditoría para trazabilidad.

### Motivación de Negocio

GeoFoto opera bajo el supuesto de un único técnico por dispositivo móvil, lo que hace improbable la aparición de conflictos de escritura simultánea. En este contexto, la estrategia Last-Write-Wins (LWW) proporciona una resolución de conflictos simple, determinista y predecible que no requiere intervención del usuario ni lógica de merge compleja. La utilización de timestamps UTC elimina ambigüedades por diferencias de zona horaria entre el dispositivo y el servidor.

### Definición Formal

Sea $L$ la versión local de un registro con $L.UpdatedAt$ y $R$ la versión remota del mismo registro con $R.UpdatedAt$:

1. Si $L.UpdatedAt > R.UpdatedAt$: prevalece $L$ (la versión local sobrescribe la versión del servidor).
2. Si $R.UpdatedAt > L.UpdatedAt$: prevalece $R$ (la versión del servidor sobrescribe la versión local).
3. Si $|L.UpdatedAt - R.UpdatedAt| \leq 1\ segundo$: prevalece $R$ (la versión del servidor toma precedencia como criterio de desempate).
4. En todos los casos, se registra una entrada de auditoría con:
   - Identificador de la entidad (``LocalId``, ``RemoteId``)
   - ``UpdatedAt`` de ambas versiones
   - Versión ganadora (``Local`` o ``Remote``)
   - Timestamp del momento de resolución

### Condiciones de Aplicación

- Se aplica durante el proceso de pull delta en **GeoFoto.Mobile**, cuando el ``SyncService`` recibe registros actualizados del servidor que también tienen modificaciones locales pendientes.
- Se aplica al caso de uso: **CU-14** (resolución de conflictos durante sincronización).
- Se aplica a las entidades ``Puntos_Local`` y ``Fotos_Local``.
- Se aplica por registro individual, no por lote completo de sincronización.
- Ambos campos ``UpdatedAt`` se almacenan y comparan en formato UTC (``DateTime.UtcNow``).

### Excepciones

- **Timestamps idénticos (diferencia ≤ 1 segundo):** Si la diferencia absoluta entre $L.UpdatedAt$ y $R.UpdatedAt$ es menor o igual a 1 segundo, se considera empate y la versión del servidor ($R$) toma precedencia. Esta regla de desempate se fundamenta en que el servidor representa el estado centralizado y canónico del sistema.
- **Registros con ``SyncStatus = PendingCreate``:** Los registros que aún no han sido sincronizados por primera vez (no poseen ``RemoteId``) no entran en la lógica de conflicto; se procesan como creaciones nuevas en el servidor.

### Criterios de Validación BDD (Dado/Cuando/Entonces)

**Escenario 1: Versión local más reciente prevalece**

- **Dado** que el punto local "Poste #12" tiene ``UpdatedAt = 2026-04-13T10:30:00Z`` y la versión del servidor del mismo punto tiene ``UpdatedAt = 2026-04-13T10:00:00Z``,
- **Cuando** el ``SyncService`` ejecuta el pull delta y detecta el conflicto,
- **Entonces** prevalece la versión local, se envía la versión local al servidor vía push, se registra el conflicto en el log de auditoría con resolución "Local", y el registro queda con ``SyncStatus = Synced``.

**Escenario 2: Versión del servidor más reciente prevalece**

- **Dado** que el punto local "Poste #12" tiene ``UpdatedAt = 2026-04-13T09:00:00Z`` y la versión del servidor tiene ``UpdatedAt = 2026-04-13T11:00:00Z``,
- **Cuando** el ``SyncService`` detecta el conflicto durante el pull delta,
- **Entonces** prevalece la versión del servidor, se actualizan ``Puntos_Local`` con los datos remotos, se registra el conflicto en el log de auditoría con resolución "Remote", y el registro queda con ``SyncStatus = Synced``.

**Escenario 3: Timestamps iguales, servidor prevalece**

- **Dado** que el punto local tiene ``UpdatedAt = 2026-04-13T10:30:00Z`` y la versión del servidor tiene ``UpdatedAt = 2026-04-13T10:30:00Z`` (diferencia < 1 segundo),
- **Cuando** el ``SyncService`` detecta el conflicto,
- **Entonces** prevalece la versión del servidor como criterio de desempate, se actualizan los datos locales con los valores remotos, y se registra el conflicto con resolución "Remote (desempate)".

### Impacto en Casos de Uso

| Caso de Uso | Impacto |
|-------------|---------|
| CU-14 — Resolución de conflictos | Es el caso de uso directamente regido por esta regla; define la estrategia completa de resolución. |
| CU-13 — Pull delta | Durante el pull delta se comparan los ``UpdatedAt`` de los registros descargados con los locales para identificar conflictos. |
| CU-07 — Edición de punto | Cada edición actualiza ``UpdatedAt`` a ``DateTime.UtcNow``, lo que posiciona cronológicamente la versión local para la resolución futura de conflictos. |

---

## RN-06: Imágenes en filesystem local de Android

### Descripción

Los archivos de imagen capturados o seleccionados en GeoFoto.Mobile se almacenan en el filesystem privado de la aplicación Android, en la ruta ``Application.Current.FileSystem.AppDataDirectory/photos/{guid}.{ext}``, donde ``{guid}`` es un identificador único generado mediante ``Guid.NewGuid()`` y ``{ext}`` es la extensión original del archivo (``jpg``, ``png``). En el servidor, las imágenes se almacenan en la ruta ``wwwroot/uploads/{year}/{month}/{guid}.{ext}``, donde ``{year}`` y ``{month}`` corresponden al año y mes de la fecha de carga.

### Motivación de Negocio

La separación del almacenamiento de archivos binarios (imágenes) respecto de la base de datos relacional (SQLite / SQL Server) es una decisión de diseño que optimiza el rendimiento y la mantenibilidad del sistema. Las bases de datos relacionales no están optimizadas para almacenar blobs de gran tamaño; insertar imágenes de varios megabytes directamente en las tablas degradaría el rendimiento de las consultas, incrementaría el tamaño de la base de datos y complicaría los procesos de backup y restauración. Al utilizar el filesystem nativo, las operaciones de lectura/escritura de imágenes son más rápidas y el consumo de memoria es más eficiente.

### Definición Formal

**En GeoFoto.Mobile (Android):**

- Ruta base: ``FileSystem.AppDataDirectory`` (directorio privado de la aplicación, no accesible por otras aplicaciones).
- Ruta completa de cada imagen: ``{AppDataDirectory}/photos/{Guid.NewGuid()}.{extensión_original}``
- Ejemplo: ``/data/data/com.geofoto.mobile/files/photos/a1b2c3d4-e5f6-7890-abcd-ef1234567890.jpg``

**En GeoFoto.API (Servidor):**

- Ruta base: ``wwwroot/uploads/``
- Ruta completa de cada imagen: ``wwwroot/uploads/{yyyy}/{MM}/{Guid.NewGuid()}.{extensión_original}``
- Ejemplo: ``wwwroot/uploads/2026/04/f9e8d7c6-b5a4-3210-fedc-ba9876543210.jpg``

**Restricciones:**

- Los nombres de archivo se generan mediante ``Guid.NewGuid()`` para garantizar unicidad y evitar colisiones.
- Se conserva la extensión original del archivo para mantener la compatibilidad con los MIME types.
- El directorio ``AppDataDirectory`` se elimina automáticamente al desinstalar GeoFoto.Mobile del dispositivo.

### Condiciones de Aplicación

- Se aplica a todas las fotografías capturadas o seleccionadas en **GeoFoto.Mobile**.
- Se aplica a los casos de uso: **CU-01** (captura desde galería), **CU-02** (captura desde cámara), **CU-04** (almacenamiento local en SQLite — componente de filesystem).
- Se aplica tanto en la persistencia local como en la transferencia al servidor durante la sincronización (las imágenes se envían como ``multipart/form-data``).

### Excepciones

- **GeoFoto.Web (CU-16):** Las imágenes cargadas desde el navegador se almacenan directamente en el servidor en ``wwwroot/uploads/{year}/{month}/{guid}.{ext}``, sin pasar por el filesystem de Android.
- Si el directorio ``photos/`` no existe al momento de almacenar una imagen, el sistema lo crea automáticamente mediante ``Directory.CreateDirectory()``.
- Si el filesystem del dispositivo no tiene espacio suficiente, la operación falla con una excepción ``IOException`` y se notifica al usuario mediante un ``MudSnackbar`` con severidad ``Error``.

### Criterios de Validación BDD (Dado/Cuando/Entonces)

**Escenario 1: Almacenamiento de foto capturada en Mobile**

- **Dado** que el técnico se encuentra en GeoFoto.Mobile y el directorio ``photos/`` existe en ``AppDataDirectory``,
- **Cuando** el técnico captura una foto ``IMG_20260413_092315.jpg`` con la cámara nativa,
- **Entonces** el sistema copia la imagen al directorio ``{AppDataDirectory}/photos/`` con un nombre de archivo basado en GUID (por ejemplo, ``a1b2c3d4-e5f6-7890-abcd-ef1234567890.jpg``), registra la ruta completa en ``Fotos_Local.RutaLocal``, y el archivo original permanece en la galería del dispositivo.

**Escenario 2: Almacenamiento en servidor durante sincronización**

- **Dado** que existe una foto pendiente de sincronización en ``Fotos_Local`` con ``SyncStatus = PendingCreate``,
- **Cuando** el ``SyncService`` envía la foto al servidor como ``multipart/form-data``,
- **Entonces** el servidor almacena la imagen en ``wwwroot/uploads/2026/04/{guid}.jpg``, registra la ruta en ``Fotos.RutaFisica``, y responde con HTTP 201 incluyendo el ``Id`` remoto asignado.

**Escenario 3: Directorio inexistente se crea automáticamente**

- **Dado** que es la primera fotografía que se captura y el directorio ``photos/`` no existe en ``AppDataDirectory``,
- **Cuando** el sistema intenta almacenar la imagen,
- **Entonces** se crea automáticamente el directorio ``photos/`` mediante ``Directory.CreateDirectory()`` y la imagen se almacena correctamente en la nueva ubicación.

### Impacto en Casos de Uso

| Caso de Uso | Impacto |
|-------------|---------|
| CU-01 — Captura desde galería | La imagen seleccionada se copia al directorio ``photos/`` del filesystem local con nombre GUID. |
| CU-02 — Captura desde cámara | La imagen capturada por ``MediaPicker`` se almacena en el filesystem local siguiendo la convención de nombrado. |
| CU-04 — Almacenamiento local SQLite | El registro en ``Fotos_Local`` contiene la referencia a la ruta física en el filesystem; la imagen no se almacena dentro de SQLite. |
| CU-12 — Push de operaciones pendientes | Durante la sincronización, el archivo de imagen se lee desde el filesystem local y se transmite al servidor como ``multipart/form-data``. |

---

## RN-07: Eliminación local con estado PendingDelete

### Descripción

Cuando un punto georeferenciado o una fotografía se elimina desde GeoFoto.Mobile, el registro no se borra físicamente de la base de datos SQLite local. En su lugar, se establece el campo ``SyncStatus = PendingDelete`` y se registra una operación ``Delete`` en la tabla ``SyncQueue``. El registro se oculta de la interfaz de usuario (no aparece en listados, mapas ni búsquedas) pero permanece en la base de datos local hasta que el servidor confirme exitosamente la eliminación durante el proceso de sincronización.

### Motivación de Negocio

La eliminación física inmediata de un registro local sin confirmación del servidor crearía inconsistencias entre las fuentes de datos: la base de datos SQLite ya no contendría el registro, pero SQL Server seguiría manteniéndolo activo. Al implementar una eliminación lógica con estado ``PendingDelete``, se garantiza que la eliminación se propague correctamente al servidor (incluyendo el cascade delete de fotos asociadas) y que solo se proceda con la eliminación física local tras recibir la confirmación HTTP 200/204 de la API. Este enfoque previene la pérdida de datos por fallos de red durante la operación de eliminación.

### Definición Formal

Sea $D$ una operación de eliminación sobre una entidad $E$ (``Punto_Local`` o ``Foto_Local``) con identificador $localId$:

1. Se establece $E.SyncStatus = PendingDelete$.
2. Se actualiza $E.UpdatedAt = DateTime.UtcNow$.
3. Se inserta un registro en ``SyncQueue``:
   - ``OperationType = Delete``
   - ``EntityType`` = tipo de $E$
   - ``LocalId`` = $localId$
   - ``Status = Pending``
4. La entidad $E$ se excluye de todas las consultas a la interfaz de usuario mediante el filtro ``WHERE SyncStatus != 'PendingDelete'``.
5. Al recibir confirmación exitosa del servidor (HTTP 200 o 204):
   - Se elimina físicamente el registro $E$ de SQLite.
   - Si $E$ es una foto, se elimina el archivo de imagen del filesystem local.
   - Si $E$ es un punto, se eliminan en cascada todas las fotos locales asociadas con sus archivos correspondientes.
   - Se elimina la entrada correspondiente de ``SyncQueue``.

### Condiciones de Aplicación

- Se aplica a todas las operaciones de eliminación ejecutadas desde **GeoFoto.Mobile**.
- Se aplica a los casos de uso: **CU-08** (eliminación de punto), **CU-12** (push de operaciones pendientes, específicamente operaciones ``Delete``).
- Se aplica tanto a puntos como a fotos individuales.
- Se aplica independientemente del estado de conectividad al momento de la eliminación.

### Excepciones

- **GeoFoto.Web:** Las eliminaciones desde el cliente web se ejecutan directamente contra la API REST y SQL Server. No se utiliza el mecanismo de ``PendingDelete`` en el contexto web, ya que el cliente siempre opera con conectividad.
- Si el servidor responde con HTTP 404 (Not Found) al intentar eliminar un registro, se interpreta como que el registro ya fue eliminado previamente (por otro medio o por una sincronización anterior). En este caso, se procede con la eliminación física local como si la confirmación hubiera sido exitosa.

### Criterios de Validación BDD (Dado/Cuando/Entonces)

**Escenario 1: Eliminación local sin conectividad**

- **Dado** que existe un punto "Poste #47" con ``SyncStatus = Synced`` y ``RemoteId = 15`` en ``Puntos_Local``, y el dispositivo no tiene conexión a Internet,
- **Cuando** el técnico elimina el punto desde la interfaz,
- **Entonces** el campo ``SyncStatus`` del punto se actualiza a ``PendingDelete``, se inserta una operación ``Delete`` en ``SyncQueue``, el punto desaparece de la lista de puntos y del mapa, pero el registro permanece en SQLite, y se muestra un ``MudSnackbar`` confirmando "Punto eliminado. Se sincronizará con el servidor cuando haya conexión."

**Escenario 2: Confirmación del servidor y eliminación física**

- **Dado** que existe un punto con ``SyncStatus = PendingDelete`` y una operación ``Delete`` en ``SyncQueue``, y el dispositivo recupera la conectividad,
- **Cuando** el ``SyncService`` envía la operación DELETE al servidor y recibe HTTP 204 (No Content),
- **Entonces** el registro se elimina físicamente de ``Puntos_Local``, las fotos asociadas se eliminan de ``Fotos_Local`` y sus archivos se borran del filesystem local, y se elimina la entrada de ``SyncQueue``.

**Escenario 3: Eliminación de punto ya eliminado en servidor**

- **Dado** que existe una operación ``Delete`` pendiente en ``SyncQueue`` para un punto cuyo ``RemoteId = 15``,
- **Cuando** el ``SyncService`` envía DELETE al servidor y recibe HTTP 404 (Not Found),
- **Entonces** el sistema interpreta que el recurso ya fue eliminado previamente, procede con la eliminación física local, y elimina la entrada de ``SyncQueue`` sin marcarla como ``Failed``.

### Impacto en Casos de Uso

| Caso de Uso | Impacto |
|-------------|---------|
| CU-08 — Eliminación de punto | Define el comportamiento completo de eliminación en el cliente móvil: eliminación lógica, ocultamiento de UI, registro en ``SyncQueue``. |
| CU-12 — Push de operaciones pendientes | Las operaciones ``Delete`` de la cola se procesan enviando HTTP DELETE al servidor; la confirmación dispara la eliminación física local. |
| CU-09 — Listado de puntos | Los puntos con ``SyncStatus = PendingDelete`` se excluyen de la consulta mediante filtro en la cláusula WHERE. |
| CU-06 — Visualización en mapa | Los markers correspondientes a puntos con ``PendingDelete`` no se renderizan en el mapa Leaflet. |

---

## RN-08: Dualidad de fuentes de verdad

### Descripción

El sistema GeoFoto opera con dos fuentes de verdad complementarias: **SQLite** como fuente de verdad operativa para las operaciones de campo en GeoFoto.Mobile, y **SQL Server** como fuente de verdad global centralizada para la consolidación y consulta de datos desde GeoFoto.Web. Durante la operación normal del cliente móvil, SQLite es la fuente autoritativa: la interfaz de usuario lee y escribe exclusivamente contra la base de datos local. El ``SyncService`` se encarga de mantener la coherencia eventual (eventual consistency) entre ambas fuentes, convergiendo a un estado consistente al finalizar cada ciclo exitoso de sincronización.

### Motivación de Negocio

La arquitectura dual de fuentes de verdad responde a la necesidad fundamental de habilitar la autonomía offline del técnico de campo sin sacrificar la integridad centralizada de los datos del supervisor. Si SQL Server fuera la única fuente de verdad, GeoFoto.Mobile no podría operar sin conectividad. Si SQLite fuera la única fuente, no existiría una vista consolidada de todos los dispositivos ni sería posible la supervisión web. La dualidad permite que cada componente del sistema opere con la fuente de datos más adecuada a su contexto operativo, mientras que el proceso de sincronización garantiza la convergencia periódica de ambas bases de datos.

### Definición Formal

Sean $DB_{local}$ (SQLite) y $DB_{server}$ (SQL Server) las dos bases de datos del sistema:

1. **Fuente de verdad operativa ($DB_{local}$):**
   - GeoFoto.Mobile lee exclusivamente de $DB_{local}$.
   - GeoFoto.Mobile escribe exclusivamente en $DB_{local}$ (RN-01).
   - $DB_{local}$ contiene las tablas ``Puntos_Local``, ``Fotos_Local`` y ``SyncQueue``.
   - $DB_{local}$ puede contener datos que aún no existen en $DB_{server}$ (registros con ``SyncStatus = PendingCreate``).

2. **Fuente de verdad global ($DB_{server}$):**
   - GeoFoto.Web lee y escribe exclusivamente contra $DB_{server}$ a través de la API REST.
   - $DB_{server}$ contiene las tablas ``Puntos``, ``Fotos`` y es accesible mediante Entity Framework Core.
   - $DB_{server}$ consolida los datos de todos los dispositivos móviles que han sincronizado.

3. **Coherencia eventual:**
   - Tras un ciclo exitoso de sincronización (push + pull delta), se cumple que para todo registro $r$: $r \in DB_{local} \iff r \in DB_{server}$ y los campos de datos de $r$ son idénticos en ambas bases (excepto los identificadores locales vs. remotos).
   - El campo ``RemoteId`` en $DB_{local}$ establece la correspondencia entre el ``LocalId`` de SQLite y el ``Id`` de SQL Server.

### Condiciones de Aplicación

- Se aplica a **todos los casos de uso** del sistema, ya que define la arquitectura fundamental de acceso a datos.
- Se aplica a ambos clientes: GeoFoto.Mobile (opera contra SQLite) y GeoFoto.Web (opera contra SQL Server vía API REST).
- Se aplica a todas las entidades del modelo de datos: ``Puntos``, ``Fotos`` y sus réplicas locales.
- Se aplica durante todo el ciclo de vida de la aplicación, tanto en modo online como offline.

### Excepciones

- **Primera ejecución del dispositivo:** En la primera instalación de GeoFoto.Mobile, $DB_{local}$ está vacía. El ``SyncService`` ejecuta un pull completo (no delta) para poblar la base de datos local con todos los registros existentes en el servidor.
- **Restauración de datos:** En caso de corrupción o pérdida de $DB_{local}$ (por ejemplo, reinstalación de la aplicación), se ejecuta una sincronización completa desde $DB_{server}$. SQL Server actúa como backup implícito de la información del dispositivo.
- **Operaciones exclusivamente web:** Las acciones realizadas desde GeoFoto.Web (edición, eliminación, carga de fotos) se persisten directamente en SQL Server. Estos cambios se propagarán a los dispositivos móviles durante el próximo pull delta.

### Criterios de Validación BDD (Dado/Cuando/Entonces)

**Escenario 1: Operación offline con coherencia posterior**

- **Dado** que el técnico de campo crea 3 puntos georeferenciados mientras el dispositivo se encuentra sin conexión a Internet,
- **Cuando** el dispositivo recupera la conectividad y el ``SyncService`` completa exitosamente el ciclo de sincronización (push de los 3 puntos + pull delta),
- **Entonces** los 3 puntos existen tanto en ``Puntos_Local`` (SQLite) como en ``Puntos`` (SQL Server), con datos idénticos en sus campos compartidos, cada registro local tiene asignado un ``RemoteId`` correspondiente al ``Id`` del servidor, y ``SyncStatus = Synced`` en todos los registros locales.

**Escenario 2: Lectura exclusiva desde fuente local**

- **Dado** que el técnico accede al mapa de GeoFoto.Mobile y existen 10 puntos en ``Puntos_Local`` con distintos ``SyncStatus`` (``Synced``, ``PendingCreate``, ``PendingUpdate``),
- **Cuando** la aplicación renderiza el mapa y los listados de puntos,
- **Entonces** se muestran los 10 puntos consultando exclusivamente SQLite local (excluyendo los ``PendingDelete``), sin realizar ninguna consulta a la API REST ni a SQL Server.

**Escenario 3: Supervisor ve datos consolidados en Web**

- **Dado** que dos técnicos de campo han sincronizado sus dispositivos durante la jornada, el primero con 15 puntos y el segundo con 8 puntos,
- **Cuando** el supervisor accede al mapa de GeoFoto.Web,
- **Entonces** el mapa muestra los 23 puntos consolidados, consultados directamente desde SQL Server a través de la API REST, representando el estado global y centralizado del sistema.

**Escenario 4: Primera instalación con pull completo**

- **Dado** que el técnico instala GeoFoto.Mobile por primera vez y existen 50 puntos en SQL Server,
- **Cuando** la aplicación se conecta al servidor por primera vez,
- **Entonces** el ``SyncService`` ejecuta un pull completo (no delta) que descarga los 50 puntos y sus fotos asociadas a ``Puntos_Local`` y ``Fotos_Local``, estableciendo ``SyncStatus = Synced`` y asignando ``RemoteId`` a cada registro.

### Impacto en Casos de Uso

| Caso de Uso | Impacto |
|-------------|---------|
| CU-01 a CU-08 — Operaciones CRUD Mobile | Todas las operaciones leen y escriben contra SQLite ($DB_{local}$), nunca directamente contra SQL Server. |
| CU-10 a CU-14 — Sincronización | El proceso de sincronización es el mecanismo que mantiene la coherencia eventual entre $DB_{local}$ y $DB_{server}$. |
| CU-16 — Carga web | Las operaciones web leen y escriben contra SQL Server ($DB_{server}$) a través de la API REST. |
| CU-06 — Mapa Mobile | El mapa del cliente móvil renderiza puntos exclusivamente desde SQLite, reflejando el estado operativo local. |
| CU-06 — Mapa Web | El mapa del cliente web renderiza puntos desde SQL Server, reflejando el estado global consolidado. |

---

# 2. Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |

---

**Fin del documento**
