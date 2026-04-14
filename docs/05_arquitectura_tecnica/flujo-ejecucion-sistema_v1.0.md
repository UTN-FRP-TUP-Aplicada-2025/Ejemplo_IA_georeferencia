# Flujo de Ejecución del Sistema

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** flujo-ejecucion-sistema_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico  

---

## Tabla de Contenidos

1. [Propósito](#1-propósito)  
2. [Flujo A — Captura y Guardado Offline](#2-flujo-a--captura-y-guardado-offline)  
3. [Flujo B — Sincronización Push al Recuperar Red](#3-flujo-b--sincronización-push-al-recuperar-red)  
4. [Flujo C — Visualización en Mapa](#4-flujo-c--visualización-en-mapa)  
5. [Control de Cambios](#5-control-de-cambios)  

---

## 1. Propósito

Este documento describe los tres flujos de ejecución principales del sistema **GeoFoto**, desde la perspectiva del usuario y los componentes técnicos involucrados en cada operación. Los flujos documentados son:

- **Flujo A — Captura y Guardado Offline (Mobile sin red).** Describe el proceso completo desde que el técnico de campo captura una fotografía hasta que el punto georeferenciado y la imagen se persisten localmente en SQLite, con la operación encolada para sincronización futura.

- **Flujo B — Sincronización Push al Recuperar Red.** Describe el proceso automático que se activa cuando el dispositivo móvil detecta conectividad, transfiriendo las operaciones pendientes desde la cola local (`SyncQueue`) hacia la API REST centralizada.

- **Flujo C — Visualización en Mapa (Web o Mobile con red).** Describe la secuencia de carga de la página de mapa, la obtención de puntos desde la API, la inicialización de Leaflet.js y la interacción del usuario con los marcadores georeferenciados.

El objetivo es proporcionar al equipo de desarrollo una referencia precisa del comportamiento esperado de cada flujo, incluyendo los componentes participantes, las interacciones entre ellos y el manejo de errores en cada etapa.

---

## 2. Flujo A — Captura y Guardado Offline

### 2.1 Diagrama de Secuencia

```text
Técnico        App MAUI        SQLite         SyncQueue
  │               │               │               │
  │──tap cámara──►│               │               │
  │               │──MediaPicker──►               │
  │◄──foto────────│               │               │
  │               │──save foto───►│               │
  │               │──save punto──►│               │
  │               │──push op─────►│───────────────►│
  │               │               │               │
  │◄──MudSnackbar─│               │               │
  │  "Punto guardado localmente"  │               │
```

### 2.2 Descripción de Cada Etapa

#### Etapa 1 — Inicio de Captura

El técnico de campo pulsa el botón `MudFab` con icono de cámara ubicado en la esquina inferior derecha de la pantalla `SubirFotos.razor`. Este gesto inicia el flujo de captura.

#### Etapa 2 — Invocación de la Cámara Nativa

La aplicación invoca `MediaPicker.CapturePhotoAsync()` a través de la API de .NET MAUI Essentials. Este método delega al sistema operativo la apertura de la cámara nativa del dispositivo (Android Camera Intent o iOS UIImagePickerController), garantizando la experiencia nativa de captura.

#### Etapa 3 — Almacenamiento de la Fotografía en Disco

Una vez que el usuario confirma la captura, la imagen resultante se copia desde la ubicación temporal del sistema operativo al directorio de datos de la aplicación, siguiendo la convención de nomenclatura:

```text
{AppDataDirectory}/photos/{guid}.jpg
```

Se utiliza un GUID como nombre de archivo para evitar colisiones y facilitar la trazabilidad posterior.

#### Etapa 4 — Extracción de Coordenadas GPS desde EXIF

El servicio `IExifService` procesa la imagen almacenada utilizando la librería `MetadataExtractor` para extraer las coordenadas GPS embebidas en los metadatos EXIF del archivo. Si el dispositivo registró la ubicación al momento de la captura, se obtienen latitud y longitud directamente desde estos metadatos. En caso contrario, se recurre a `Geolocation.GetLocationAsync()` como fuente alternativa de coordenadas.

#### Etapa 5 — Creación del Registro Punto_Local

Se crea una instancia de `Punto_Local` en la base de datos SQLite con los siguientes atributos principales:

| Campo | Valor |
|---|---|
| `Id` | GUID generado localmente |
| `Latitud` | Coordenada obtenida en Etapa 4 |
| `Longitud` | Coordenada obtenida en Etapa 4 |
| `FechaCaptura` | `DateTime.UtcNow` |
| `SyncStatus` | `Local` |
| `RemoteId` | `null` (pendiente de sincronización) |

#### Etapa 6 — Creación del Registro Foto_Local

Se crea una instancia de `Foto_Local` vinculada al `Punto_Local` recién creado:

| Campo | Valor |
|---|---|
| `Id` | GUID generado localmente |
| `PuntoLocalId` | Id del punto creado en Etapa 5 |
| `RutaArchivo` | Ruta relativa al archivo `.jpg` |
| `FechaCaptura` | Timestamp de la imagen |
| `SyncStatus` | `Local` |

#### Etapa 7 — Encolado de Operación de Sincronización para Punto

Se inserta un registro en la tabla `SyncQueue` con los siguientes valores:

| Campo | Valor |
|---|---|
| `OperationType` | `Create` |
| `EntityType` | `Punto` |
| `EntityId` | Id del `Punto_Local` |
| `Payload` | Serialización JSON del punto |
| `Status` | `Pending` |
| `CreatedAt` | `DateTime.UtcNow` |
| `Attempts` | `0` |

#### Etapa 8 — Encolado de Operación de Sincronización para Foto

Se inserta un segundo registro en `SyncQueue` análogo al anterior:

| Campo | Valor |
|---|---|
| `OperationType` | `Create` |
| `EntityType` | `Foto` |
| `EntityId` | Id de la `Foto_Local` |
| `Payload` | Serialización JSON de la foto (incluye ruta binaria) |
| `Status` | `Pending` |
| `CreatedAt` | `DateTime.UtcNow` |
| `Attempts` | `0` |

#### Etapa 9 — Actualización de la Interfaz de Usuario

La UI se actualiza de forma reactiva:

- Se agrega un nuevo marcador en el mapa local correspondiente a las coordenadas del punto capturado.
- El componente `SyncStatusBadge` incrementa su contador de operaciones pendientes en dos unidades (una por el punto y otra por la foto).

#### Etapa 10 — Confirmación Visual

Se muestra un componente `MudSnackbar` con el mensaje **"Punto guardado localmente"** y severidad `Success`, confirmando al técnico que la operación se completó satisfactoriamente sin requerir conectividad.

### 2.3 Manejo de Errores

| Situación | Componente Afectado | Acción del Sistema |
|---|---|---|
| Permiso de cámara denegado por el usuario | `MediaPicker` | Se muestra un `MudAlert` con severidad `Warning` indicando que la captura requiere permiso de acceso a la cámara. No se crea ningún registro. |
| Almacenamiento insuficiente en el dispositivo | Sistema de archivos | Se captura la excepción `IOException` y se muestra un `MudAlert` con severidad `Error` indicando que no hay espacio disponible. Se recomienda al usuario liberar almacenamiento. |
| Falla de escritura en SQLite | `sqlite-net-pcl` | Se ejecuta un reintento automático (máximo 3 intentos). Si la escritura falla de manera persistente, se muestra un `MudSnackbar` con severidad `Error` indicando que el punto no pudo guardarse. Se registra el error en el log local. |
| GPS no disponible y EXIF sin coordenadas | `IExifService` / `Geolocation` | Se permite al usuario ingresar coordenadas manualmente o se almacena el punto con coordenadas `null`, marcándolo como pendiente de geolocalización. |

---

## 3. Flujo B — Sincronización Push al Recuperar Red

### 3.1 Diagrama de Secuencia

```text
ConnectivityService   SyncService    SyncQueue    API REST    SQL Server
      │                    │            │            │            │
      │──NetworkAvailable──►            │            │            │
      │                    │──read──────►            │            │
      │                    │◄──pending ops──         │            │
      │                    │──POST punto────────────►│            │
      │                    │                         │──INSERT───►│
      │                    │◄──201 Created───────────│            │
      │                    │──mark Done──►           │            │
      │                    │──POST foto─────────────►│            │
      │                    │                         │──INSERT───►│
      │                    │◄──201 Created───────────│            │
      │                    │──mark Done──►           │            │
      │                    │──pull delta────────────►│            │
      │                    │                         │──SELECT───►│
      │                    │◄──JSON delta────────────│◄───────────│
      │                    │──merge local►           │            │
```

### 3.2 Descripción de Cada Etapa

#### Etapa 1 — Detección de Conectividad

El evento `Connectivity.ConnectivityChanged` de .NET MAUI Essentials se dispara cuando el sistema operativo detecta un cambio en el estado de la red. El servicio `ConnectivityService` evalúa si el nuevo estado corresponde a `NetworkAccess.Internet` y, en caso afirmativo, notifica al `SyncService`.

#### Etapa 2 — Debounce de Activación

El `SyncService` aplica un debounce de **2 segundos** antes de iniciar el proceso de sincronización. Este retardo previene múltiples activaciones consecutivas provocadas por fluctuaciones rápidas en el estado de la red (por ejemplo, al entrar y salir de cobertura Wi-Fi).

#### Etapa 3 — Lectura de la Cola de Sincronización

Se consulta la tabla `SyncQueue` filtrando por `Status = Pending`, ordenando los registros por `CreatedAt` de forma ascendente. Este ordenamiento garantiza que las operaciones se procesen en el mismo orden cronológico en que fueron creadas, respetando las dependencias implícitas (un punto debe sincronizarse antes que sus fotos asociadas).

#### Etapa 4 — Marcado de Operación en Progreso

Para cada operación extraída de la cola, se actualiza su estado a `InProgress` antes de iniciar la transmisión. Este marcado evita que una segunda instancia del servicio de sincronización (o una reactivación por reconexión rápida) procese la misma operación simultáneamente.

#### Etapa 5 — Construcción de la Solicitud HTTP

Se deserializa el campo `Payload` del registro `SyncQueue` y se construye el `HttpRequestMessage` correspondiente según el `OperationType` y el `EntityType`:

| OperationType | EntityType | Método HTTP | Endpoint |
|---|---|---|---|
| `Create` | `Punto` | `POST` | `/api/puntos` |
| `Create` | `Foto` | `POST` | `/api/fotos` (multipart/form-data) |
| `Update` | `Punto` | `PUT` | `/api/puntos/{remoteId}` |
| `Delete` | `Punto` | `DELETE` | `/api/puntos/{remoteId}` |
| `Delete` | `Foto` | `DELETE` | `/api/fotos/{remoteId}` |

#### Etapa 6 — Envío al Endpoint Correspondiente

La solicitud HTTP se envía a la API REST (`GeoFoto.Api`) utilizando `HttpClient` con un timeout configurado de **30 segundos** para operaciones de datos y **120 segundos** para subida de archivos de imagen.

#### Etapa 7 — Respuesta Exitosa (2xx)

Al recibir una respuesta con código de estado en el rango 2xx:

- Se actualiza el estado de la operación en `SyncQueue` a `Done`.
- Se extrae el `Id` remoto devuelto por la API y se almacena en el campo `RemoteId` de la entidad local correspondiente (`Punto_Local` o `Foto_Local`).
- Se actualiza el campo `SyncStatus` de la entidad local a `Synced`.
- Se registra la fecha de sincronización en `SyncedAt`.

#### Etapa 8 — Conflicto (409 Conflict)

Cuando la API responde con código `409`, se aplica la política de resolución de conflictos **Last-Write-Wins** definida en la regla de negocio **RN-05**:

- Se compara el campo `UpdatedAt` de la versión local contra la versión remota.
- La versión con el timestamp más reciente prevalece.
- Si la versión local es más reciente, se reenvía con el encabezado `X-Force-Overwrite: true`.
- Si la versión remota es más reciente, se descarta la versión local y se actualiza con los datos del servidor.
- Se registra el conflicto resuelto en el log local para auditoría.

#### Etapa 9 — Error de Servidor o Timeout (5xx / Timeout)

Cuando la solicitud falla con un código de estado 5xx o por timeout de red:

- Se incrementa el campo `Attempts` del registro en `SyncQueue`.
- Se actualiza el estado a `Pending` para permitir un reintento posterior.
- Se calcula el próximo intento aplicando la estrategia de backoff exponencial definida en la regla de negocio **RN-04**:

```text
retardo = min(2^intentos × 1000 ms, 300000 ms)
```

- Si `Attempts` alcanza el límite máximo configurado (por defecto **5 intentos**), se marca la operación como `Failed` y se notifica al usuario.

#### Etapa 10 — Pull Delta Post-Push

Una vez completado el ciclo de push, el `SyncService` ejecuta una operación de pull delta para obtener los registros modificados en el servidor desde la última sincronización:

- Se envía `GET /api/sync/pull?desde={lastSyncTimestamp}`.
- Se reciben los puntos y fotos creados o modificados por otros usuarios o desde la aplicación web.
- Se insertan o actualizan en la base de datos SQLite local.
- Se actualiza el timestamp de última sincronización.

#### Etapa 11 — Actualización de la Interfaz de Usuario

Al finalizar el ciclo completo de sincronización:

- Se muestra un `MudSnackbar` con el mensaje **"Sincronización completada"** y severidad `Success`.
- El componente `SyncStatusBadge` reinicia su contador a **0** (si todas las operaciones se procesaron exitosamente).
- Los nuevos puntos obtenidos en el pull delta se agregan al mapa local.

### 3.3 Manejo de Errores por Código HTTP

| Código HTTP | Significado | Acción del Sistema |
|---|---|---|
| `200 OK` | Operación exitosa (actualización) | Marcar operación como `Done`. Actualizar `RemoteId` y `SyncStatus`. |
| `201 Created` | Recurso creado exitosamente | Marcar operación como `Done`. Almacenar `RemoteId` devuelto. |
| `400 Bad Request` | Datos inválidos en el payload | Marcar operación como `Failed`. Registrar detalle del error. No reintentar (error de datos, no transitorio). |
| `401 Unauthorized` | Token de autenticación inválido o expirado | Pausar la sincronización. Solicitar al usuario re-autenticación. |
| `404 Not Found` | Recurso remoto no encontrado (ya eliminado) | Marcar operación como `Done`. Eliminar la entidad local si corresponde. |
| `409 Conflict` | Conflicto de versión con el servidor | Aplicar Last-Write-Wins (RN-05). Registrar conflicto en log. |
| `413 Payload Too Large` | Archivo de imagen excede el límite del servidor | Marcar como `Failed`. Notificar al usuario para reducir la resolución. |
| `429 Too Many Requests` | Rate limiting activo en la API | Respetar encabezado `Retry-After`. Reintentar después del período indicado. |
| `500 Internal Server Error` | Error interno del servidor | Incrementar `Attempts`. Reintentar con backoff exponencial. |
| `502 Bad Gateway` | Error de proxy o gateway | Incrementar `Attempts`. Reintentar con backoff exponencial. |
| `503 Service Unavailable` | Servidor temporalmente no disponible | Incrementar `Attempts`. Reintentar con backoff exponencial. |
| `504 Gateway Timeout` | Timeout en el servidor | Incrementar `Attempts`. Reintentar con backoff exponencial. |
| Timeout de red (`TaskCanceledException`) | Sin respuesta dentro del período configurado | Incrementar `Attempts`. Reintentar con backoff exponencial. Verificar estado de conectividad antes del reintento. |

---

## 4. Flujo C — Visualización en Mapa

### 4.1 Diagrama de Secuencia

```text
Usuario     Blazor Page    IJSRuntime    Leaflet.js    API REST    SQL Server
  │             │              │             │            │            │
  │──navega /──►│              │             │            │            │
  │             │──GET puntos──────────────────────────►│            │
  │             │              │             │            │──SELECT───►│
  │             │◄──JSON array─────────────────────────│◄───────────│
  │             │──initMap()──►│             │            │            │
  │             │              │──L.map()───►│            │            │
  │             │──addMarkers──►             │            │            │
  │             │              │──L.marker──►│            │            │
  │◄──mapa con markers─────────────────────│            │            │
  │──click marker──────────────────────────►│            │            │
  │             │◄──onMarkerClick───────────│            │            │
  │             │──GET fotos/punto──────────────────────►│            │
  │             │              │             │            │──SELECT───►│
  │             │◄──JSON fotos──────────────────────────│◄───────────│
  │◄──MarkerPopup.razor────────│             │            │            │
```

### 4.2 Descripción de Cada Etapa

#### Etapa 1 — Navegación a la Página de Mapa

El usuario accede a la ruta `/mapa` desde la barra de navegación lateral (`MudNavMenu`). El framework Blazor resuelve la ruta y activa el componente `Mapa.razor` definido en `GeoFoto.Shared`.

#### Etapa 2 — Solicitud de Puntos Georeferenciados a la API

Durante el evento `OnInitializedAsync()` del componente `Mapa.razor`, se invoca el servicio HTTP para obtener la lista completa de puntos georeferenciados:

```text
GET /api/puntos → PuntosController.GetAll()
```

La solicitud incluye los encabezados de autenticación correspondientes si el usuario ha iniciado sesión.

#### Etapa 3 — Consulta a la Base de Datos Centralizada

El controlador `PuntosController` delega la consulta al `GeoFotoDbContext` mediante Entity Framework Core, ejecutando una consulta `SELECT` contra la tabla `Puntos` en SQL Server. Se incluyen los campos de latitud, longitud, descripción, fecha de captura y cantidad de fotos asociadas.

#### Etapa 4 — Respuesta JSON al Cliente

La API serializa la colección de puntos en formato JSON con política camelCase (`System.Text.Json`) y la devuelve al cliente con código de estado `200 OK`. Cada elemento del array contiene la información mínima necesaria para representar un marcador en el mapa.

#### Etapa 5 — Inicialización del Mapa Leaflet

Una vez recibidos los datos, el componente `Mapa.razor` invoca la función de interoperabilidad JavaScript a través de `IJSRuntime`:

```text
await JS.InvokeVoidAsync("initMap", elementId, centerLat, centerLng, zoom);
```

Esta función crea una instancia de `L.map()` en el contenedor HTML designado, configurando la vista inicial centrada en el baricentro de los puntos recibidos o en una ubicación por defecto.

#### Etapa 6 — Carga de Tiles del Mapa Base

Leaflet.js solicita los tiles del mapa base al proveedor configurado (OpenStreetMap por defecto) mediante peticiones HTTP a:

```text
https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png
```

Los tiles se cargan de forma progresiva conforme el nivel de zoom y la extensión visible del mapa lo requieren.

#### Etapa 7 — Adición de Marcadores

El componente itera sobre la colección de puntos recibidos y, para cada uno, invoca la función JavaScript de interoperabilidad:

```text
await JS.InvokeVoidAsync("addMarker", punto.Latitud, punto.Longitud, punto.Id, punto.Descripcion);
```

Leaflet crea una instancia de `L.marker()` por cada punto y la agrega a la capa de marcadores del mapa. Se asocia un evento `click` a cada marcador que invoca un callback hacia el componente Blazor.

#### Etapa 8 — Visualización del Mapa con Marcadores

El usuario visualiza el mapa completamente renderizado con todos los marcadores posicionados en sus coordenadas correspondientes. El contorno del mapa se ajusta automáticamente (`fitBounds`) para mostrar todos los puntos en la vista inicial.

#### Etapa 9 — Interacción con un Marcador

Cuando el usuario hace clic sobre un marcador, Leaflet dispara el evento `click` asociado, que invoca el método .NET registrado como callback mediante `DotNetObjectReference`:

```text
onMarkerClick(puntoId) → Blazor [JSInvokable] OnMarkerSelected(int puntoId)
```

El componente `Mapa.razor` recibe el identificador del punto seleccionado.

#### Etapa 10 — Carga de Fotografías del Punto Seleccionado

El componente ejecuta una solicitud HTTP para obtener las fotografías asociadas al punto:

```text
GET /api/puntos/{id} → PuntosController.GetById(id)
```

La respuesta incluye el detalle completo del punto junto con la colección de fotografías asociadas (URLs de las imágenes, fechas de captura, coordenadas individuales). Con estos datos, se renderiza el componente `MarkerPopup.razor` que presenta un carrusel de imágenes (`FotoCarousel`) y los metadatos del punto en un popup anclado al marcador seleccionado.

### 4.3 Manejo de Errores

| Situación | Componente Afectado | Acción del Sistema |
|---|---|---|
| API inalcanzable (sin conexión al servidor) | `HttpClient` | Se captura la excepción `HttpRequestException`. Se muestra un `MudAlert` con severidad `Error` y el mensaje **"Sin conexión al servidor"**. El mapa se renderiza vacío. Si la aplicación es móvil, se cargan los puntos desde SQLite local como alternativa. |
| Sin puntos registrados en la base de datos | `PuntosController` | La API devuelve un array JSON vacío (`[]`). El mapa se inicializa sin marcadores. Se muestra un `MudAlert` con severidad `Info` y el mensaje **"No se encontraron puntos georeferenciados"**. |
| Falla en la carga de tiles de Leaflet | `Leaflet.js` / Proveedor de tiles | El mapa se renderiza con la estructura de controles pero los tiles se muestran en blanco o con el placeholder de error. Se registra el error en la consola del navegador. No se interrumpe la funcionalidad de marcadores ni la interacción con los puntos. |
| Error al obtener fotos de un punto | `HttpClient` | Se muestra el popup del marcador con los datos del punto pero sin carrusel de imágenes. Se indica al usuario mediante texto **"No se pudieron cargar las fotografías"** dentro del popup. |
| Timeout en la carga inicial de puntos | `HttpClient` | Se aplica un timeout de **15 segundos** para la solicitud inicial. Si se excede, se muestra un `MudSnackbar` con severidad `Warning` y el mensaje **"La carga de puntos tardó demasiado. Intente nuevamente"**. Se ofrece un botón de reintento. |
| Error de JavaScript en la inicialización del mapa | `IJSRuntime` | Se captura la excepción `JSException`. Se muestra un `MudAlert` con severidad `Error` indicando que el componente de mapa no pudo inicializarse. Se registra el error para diagnóstico. |

---

## 5. Control de Cambios

| Versión | Fecha | Autor | Descripción del Cambio |
|---|---|---|---|
| 1.0 | 2026-04-13 | Equipo Técnico | Creación inicial del documento. Se documentan los tres flujos principales de ejecución del sistema: captura offline, sincronización push y visualización en mapa. |

---

**Fin del documento**
