# Especificación Funcional

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** especificacion-funcional_v1.0.md  
**Versión:** 1.1  
**Estado:** Activo  
**Fecha:** 2026-04-16  
**Autor:** Equipo Técnico  

---

## Tabla de Contenidos

1. [Introducción](#1-introducción)  
2. [Alcance](#2-alcance)  
3. [Actores](#3-actores)  
4. [Descripción General del Sistema y Flujo de Alto Nivel](#4-descripción-general-del-sistema-y-flujo-de-alto-nivel)  
5. [Funcionalidades Principales](#5-funcionalidades-principales)  
6. [Reglas de Negocio](#6-reglas-de-negocio)  
7. [Datos Principales del Sistema](#7-datos-principales-del-sistema)  
8. [Criterios de Aceptación BDD](#8-criterios-de-aceptación-bdd)  
9. [Supuestos y Restricciones](#9-supuestos-y-restricciones)  
10. [Control de Cambios](#10-control-de-cambios)  

---

# 1. Introducción

El presente documento describe el comportamiento funcional del sistema **GeoFoto**, una aplicación de registro georeferenciado de fotografías con estrategia offline-first. Se detallan las funcionalidades que el sistema debe ofrecer, las reglas de negocio que rigen su operación, los actores que interactúan con la plataforma y los criterios de aceptación que permiten verificar el correcto cumplimiento de cada requerimiento.

GeoFoto se compone de dos clientes: una aplicación móvil (**GeoFoto.Mobile**) construida con .NET MAUI Hybrid para Android, orientada al trabajo de campo sin conectividad garantizada, y una aplicación web (**GeoFoto.Web**) construida con Blazor Web App (InteractiveServer) y MudBlazor, destinada a la supervisión y gestión centralizada de puntos georeferenciados. Ambos clientes se comunican con una API REST (**GeoFoto.API**) desarrollada en ASP.NET Core Web API sobre .NET 10, que persiste los datos en SQL Server.

El enfoque offline-first implica que toda operación de escritura se realiza primero contra la base de datos local SQLite del dispositivo móvil, y se sincroniza posteriormente con el servidor mediante el patrón Outbox (cola de sincronización). Este documento constituye la referencia funcional principal para el equipo de desarrollo, los testers y los stakeholders del proyecto.

---

# 2. Alcance

## 2.1 Dentro del Alcance

| Área | Descripción |
|------|-------------|
| Captura de fotografías | Selección desde galería del dispositivo y captura directa mediante cámara nativa (Android). En la versión web, carga de archivos desde el sistema de archivos local. |
| Extracción GPS del EXIF | Lectura automática de metadatos EXIF de cada imagen para obtener latitud y longitud, utilizando la librería MetadataExtractor en el servidor y en el cliente móvil. |
| Almacenamiento local SQLite | Persistencia inmediata de puntos, fotos y operaciones pendientes en SQLite (sqlite-net-pcl) en el dispositivo móvil, garantizando operación sin conectividad. |
| Visualización en mapa | Representación de puntos georeferenciados sobre un mapa interactivo Leaflet.js integrado en el layout de MudBlazor, con markers clicables y popups informativos. |
| CRUD de puntos | Creación, lectura, edición y eliminación de puntos georeferenciados y sus fotografías asociadas, con persistencia local-first en el cliente móvil. |
| Sincronización (Outbox Pattern) | Cola de sincronización (``SyncQueue``) que registra cada operación local pendiente y la envía al servidor cuando se detecta conectividad. Soporte para sincronización automática y manual. |
| Resolución de conflictos | Estrategia Last-Write-Wins basada en el campo ``UpdatedAt`` (UTC) para resolver conflictos entre datos locales y remotos durante la sincronización. |
| Pull delta | Descarga incremental de cambios desde el servidor hacia el cliente móvil, basada en la marca temporal de la última sincronización exitosa. |

## 2.2 Fuera del Alcance

| Elemento excluido | Justificación |
|--------------------|---------------|
| Autenticación y autorización | La versión 1.0 opera sin sistema de login; se prevé incorporar identidad en iteraciones futuras. |
| Soporte multi-usuario concurrente | El sistema asume un único técnico por dispositivo móvil; la gestión de múltiples usuarios simultáneos queda diferida. |
| Descarga offline de tiles de mapa | Los mapas Leaflet requieren conexión a Internet para cargar tiles; no se implementa caché local de cartografía. |
| Formatos de exportación avanzados | No se incluyen exportaciones a KML, GeoJSON, Shapefile ni generación de reportes PDF en esta versión. |
| Soporte para iOS | La aplicación móvil se desarrolla exclusivamente para Android mediante .NET MAUI Hybrid; iOS queda fuera del alcance. |

---

# 3. Actores

## 3.1 Técnico de Campo

El técnico de campo es el usuario principal de **GeoFoto.Mobile**, la aplicación .NET MAUI Hybrid para Android. Opera predominantemente en zonas con conectividad limitada o nula. Sus actividades principales consisten en capturar fotografías desde la cámara nativa del dispositivo o seleccionarlas desde la galería, asociarlas a puntos georeferenciados que se crean automáticamente a partir de las coordenadas EXIF, y editar la información descriptiva de cada punto. Todas las operaciones se persisten primero en SQLite local y se sincronizan con el servidor cuando se restablece la conectividad.

## 3.2 Supervisor

El supervisor utiliza **GeoFoto.Web**, la aplicación Blazor Web App con interfaz MudBlazor, desde un navegador de escritorio con conexión a Internet. Sus actividades principales incluyen la visualización del mapa consolidado con todos los puntos georeferenciados, la gestión (edición y eliminación) de puntos y fotografías, y la carga de fotografías desde el sistema de archivos local. El supervisor opera siempre conectado contra la API REST y la base de datos SQL Server.

## 3.3 Sistema de Sincronización

El **SyncService** es un actor no humano que se ejecuta en segundo plano dentro de GeoFoto.Mobile. Su responsabilidad consiste en monitorear el estado de la conectividad de red, procesar la cola de sincronización (``SyncQueue``) enviando las operaciones pendientes al servidor, recibir confirmaciones y actualizar los estados locales, y ejecutar el pull delta para incorporar cambios del servidor en la base de datos SQLite local. El SyncService nunca bloquea la interfaz de usuario y aplica backoff exponencial en caso de fallos de red.

---

# 4. Descripción General del Sistema y Flujo de Alto Nivel

El flujo principal de operación de GeoFoto sigue una secuencia de diez pasos que abarca desde la captura de una fotografía hasta su representación en el mapa:

| Paso | Acción | Descripción |
|------|--------|-------------|
| 1 | Captura de fotografía | El técnico captura una foto con la cámara nativa (MediaPicker) o selecciona una imagen de la galería. |
| 2 | Extracción de metadatos EXIF | El sistema lee los metadatos EXIF de la imagen mediante MetadataExtractor y obtiene latitud, longitud y fecha de toma. |
| 3 | Almacenamiento local en SQLite | Se crea un registro en ``Puntos_Local`` (si no existe un punto para esas coordenadas) y un registro en ``Fotos_Local``. La foto se guarda en el filesystem local de Android (``AppDataDirectory``). |
| 4 | Registro en SyncQueue | Se inserta una entrada en la cola de sincronización con ``OperationType = Create``, ``EntityType = Punto/Foto``, ``Status = Pending``. |
| 5 | Detección de conectividad | El SyncService monitorea el estado de red mediante ``Connectivity.Current`` de .NET MAUI. Cuando detecta conexión activa, inicia el procesamiento de la cola. |
| 6 | Push al servidor (API REST) | El SyncService envía cada operación pendiente a GeoFoto.API mediante HTTP POST/PUT/DELETE. Las fotos se transmiten como multipart/form-data. |
| 7 | Persistencia en SQL Server | La API procesa la operación y persiste los datos en las tablas ``Puntos`` y ``Fotos`` de SQL Server mediante EF Core. Retorna el Id remoto asignado. |
| 8 | Pull delta | El SyncService solicita a la API los registros modificados desde la última sincronización exitosa (parámetro ``sinceUtc``). La API retorna los cambios incrementales. |
| 9 | Actualización de la base local | Se actualizan los registros en ``Puntos_Local`` y ``Fotos_Local`` con los datos recibidos del servidor, se asigna el ``RemoteId``, y se marca ``SyncStatus = Synced``. Se eliminan las entradas procesadas de ``SyncQueue``. |
| 10 | Visualización en mapa | El mapa Leaflet se refresca con los puntos actualizados. Cada punto se representa como un marker clicable que despliega un popup con la información del punto y sus fotografías asociadas. |

Este flujo opera de manera idéntica independientemente del estado de la red: los pasos 1 a 4 se ejecutan siempre de forma inmediata y local; los pasos 5 a 9 se ejecutan de forma asíncrona cuando existe conectividad; el paso 10 refleja el estado actual de los datos locales en todo momento.

---

# 5. Funcionalidades Principales

## F-01 — Captura de foto desde galería (Mobile y Web)

**Descripción:** El sistema permite al usuario seleccionar una o más fotografías existentes desde la galería del dispositivo (Mobile) o desde el sistema de archivos local (Web) para asociarlas a un punto georeferenciado.

**Comportamiento esperado:**  
Se presenta un componente ``MudFileUpload`` configurado para aceptar archivos de imagen (JPEG, PNG). Al seleccionar un archivo, el sistema extrae los metadatos EXIF, persiste la imagen en el almacenamiento local y crea o asocia el registro a un punto georeferenciado existente.

**Ejemplo concreto:**  
El técnico de campo abre GeoFoto.Mobile, pulsa el botón "Agregar foto" representado por un ``MudFileUpload`` con ícono de galería. Selecciona una imagen ``IMG_20260413_092315.jpg`` desde la galería de Android. El sistema lee las coordenadas GPS embebidas en el EXIF (-34.6037, -58.3816), crea un punto local con esas coordenadas, almacena la imagen en ``AppDataDirectory/fotos/`` y muestra una notificación ``MudSnackbar`` confirmando "Foto agregada al punto (-34.6037, -58.3816)".

---

## F-02 — Captura desde cámara nativa (solo Mobile) [ACTUALIZADO]

**Descripción:** El sistema permite al técnico de campo tomar una fotografía directamente desde la cámara nativa del dispositivo Android, utilizando la API ``MediaPicker`` de .NET MAUI.

**Comportamiento esperado:**  
Al pulsar el botón de cámara, se invoca ``MediaPicker.Default.CapturePhotoAsync()``, que abre la aplicación de cámara nativa del dispositivo. Una vez capturada la foto, el sistema recibe el ``FileResult``, copia la imagen al directorio de la aplicación, extrae los metadatos EXIF y procede con el flujo estándar de creación de punto.

**Ejemplo concreto:**  
El técnico pulsa el ``MudIconButton`` con ícono ``Icons.Material.Filled.CameraAlt`` en la barra de acciones de GeoFoto.Mobile. Se abre la cámara nativa de Android. El técnico fotografía un poste de alumbrado y confirma la captura. El sistema recibe la imagen, detecta coordenadas EXIF (-34.6118, -58.4173), crea un registro en ``Puntos_Local`` con nombre auto-generado "Punto 2026-04-13 09:45:02", guarda la foto en ``Fotos_Local`` y registra la operación en ``SyncQueue``.

**Escenario ESC-02 (Mapa no inicializa):** Si el BlazorWebView no cargó o Leaflet falló al inicializar, se muestra una pantalla de error con el mensaje "Mapa no disponible — intentá reiniciar la aplicación." y un botón "Reintentar" que vuelve a invocar initMap.

**Escenario ESC-03 (GPS sin permiso):** Al iniciar la app, si el GPS no está habilitado o el permiso no fue otorgado: (1) Se muestra dialog nativo de solicitud de permiso. (2) Si el usuario deniega: MudDialog con explicación y botón "Ir a Configuración" (AppInfo.ShowSettingsUI()). (3) Si el usuario niega permanentemente: el mapa carga igual pero sin centrado GPS, con snackbar permanente "Sin permiso de ubicación — el mapa está disponible pero sin tu posición." En ningún caso la app queda bloqueada sin mapa.

---

## F-03 — Extracción automática de coordenadas GPS del EXIF

**Descripción:** El sistema extrae automáticamente la latitud y longitud de los metadatos EXIF de cada fotografía cargada, utilizando la librería MetadataExtractor tanto en el lado del servidor como en el cliente móvil.

**Comportamiento esperado:**  
Al recibir una imagen, se invoca ``ImageMetadataReader.ReadMetadata(stream)`` de MetadataExtractor para obtener el directorio ``GpsDirectory``. Se extraen las etiquetas ``GpsLatitude``, ``GpsLongitude`` y ``GpsLatitudeRef``, ``GpsLongitudeRef``, y se convierten a formato decimal. Si la imagen no contiene datos GPS, se asignan coordenadas (0, 0) y se emite una advertencia al usuario.

**Ejemplo concreto:**  
El supervisor carga una foto ``relevamiento_zona_norte.jpg`` mediante el ``MudFileUpload`` en GeoFoto.Web. El servidor procesa la imagen con MetadataExtractor y extrae las coordenadas (-34.5501, -58.4495). El sistema crea automáticamente un registro en la tabla ``Puntos`` de SQL Server con esas coordenadas y asocia la foto. Si la imagen no tuviera datos EXIF GPS (por ejemplo, una captura de pantalla), el sistema crearía el punto en (0, 0) y mostraría un ``MudAlert`` con severidad ``Warning`` indicando "La imagen no contiene coordenadas GPS. Se asignó ubicación predeterminada (0, 0)."

---

## F-04 — Almacenamiento local en SQLite

**Descripción:** Toda operación de escritura en GeoFoto.Mobile se persiste primero en la base de datos SQLite local del dispositivo, independientemente del estado de la conectividad de red.

**Comportamiento esperado:**  
Al crear, editar o eliminar un punto o una foto, el sistema ejecuta la operación contra las tablas ``Puntos_Local`` y ``Fotos_Local`` de SQLite mediante sqlite-net-pcl. La operación se completa de forma inmediata y síncrona, proporcionando al usuario una experiencia fluida sin latencia de red. Simultáneamente, se registra la operación en la tabla ``SyncQueue`` para su posterior envío al servidor.

**Ejemplo concreto:**  
El técnico de campo se encuentra en una zona rural sin cobertura de red. Captura tres fotografías consecutivas de distintas instalaciones. Cada foto se almacena inmediatamente en ``AppDataDirectory/fotos/``, se inserta un registro en ``Fotos_Local`` con ``SyncStatus = PendingCreate``, se inserta o actualiza el registro correspondiente en ``Puntos_Local``, y se crean tres entradas en ``SyncQueue`` con ``Status = Pending``. La interfaz responde instantáneamente sin mostrar errores de conexión.

---

## F-05 — Creación automática de punto georeferenciado

**Descripción:** Al capturar o cargar una fotografía con coordenadas GPS válidas, el sistema crea automáticamente un punto georeferenciado asociado a dichas coordenadas.

**Comportamiento esperado:**  
El sistema verifica si ya existe un punto cercano (dentro de un radio configurable) a las coordenadas extraídas del EXIF. Si no existe, crea un nuevo registro en ``Puntos_Local`` (Mobile) o ``Puntos`` (Web) con la latitud, longitud, un nombre auto-generado con formato "Punto YYYY-MM-DD HH:mm:ss", y una descripción vacía. Si ya existe un punto cercano, asocia la nueva foto al punto existente.

**Ejemplo concreto:**  
El técnico captura una foto en las coordenadas (-34.6037, -58.3816). No existe ningún punto previo registrado en esas coordenadas. El sistema crea automáticamente un registro en ``Puntos_Local`` con ``LocalId = 1``, ``Latitud = -34.6037``, ``Longitud = -58.3816``, ``Nombre = "Punto 2026-04-13 10:15:33"``, ``Descripcion = ""``, ``SyncStatus = PendingCreate``. Luego crea el registro en ``Fotos_Local`` con ``PuntoLocalId = 1``. Al visualizar el mapa, aparece un nuevo marker en esa ubicación.

---

## F-06 — Visualización en mapa con markers Leaflet y layout MudBlazor

**Descripción:** El sistema presenta un mapa interactivo Leaflet.js integrado dentro del layout de MudBlazor, donde cada punto georeferenciado se representa como un marker posicionado en sus coordenadas.

**Comportamiento esperado:**  
El componente de mapa se renderiza dentro de un ``MudMainContent`` con un ``MudContainer``, ocupando el área principal de la interfaz. Los tiles se cargan desde OpenStreetMap. Cada registro de la tabla de puntos (local o remota según el cliente) se representa como un marker de Leaflet con coordenadas ``[Latitud, Longitud]``. El mapa se centra automáticamente para mostrar todos los markers visibles mediante ``fitBounds()``. Los markers se actualizan en tiempo real cuando se agregan, editan o eliminan puntos.

**Ejemplo concreto:**  
El supervisor abre GeoFoto.Web y accede a la vista de mapa. El sistema consulta la API para obtener los 25 puntos registrados, renderiza el mapa Leaflet dentro de un ``MudPaper`` con elevación, y dibuja 25 markers azules. El mapa se ajusta automáticamente con ``fitBounds()`` para mostrar todos los puntos, centrado sobre la zona de Gran Buenos Aires. En la esquina superior derecha se muestra un ``MudChip`` con el total de puntos visibles: "25 puntos".

---

## F-07 — Popup al click en marker

**Descripción:** Al hacer click sobre un marker del mapa, el sistema despliega un popup con la información del punto georeferenciado y las miniaturas de sus fotografías asociadas.

**Comportamiento esperado:**  
Cada marker de Leaflet tiene asociado un evento ``click`` que invoca un callback de Blazor mediante JS Interop. El callback abre un ``MudDialog`` (componente ``MarkerPopup``) que muestra el nombre del punto, su descripción, las coordenadas, la fecha de creación y una galería de miniaturas de las fotos asociadas. Desde el popup se puede acceder a las acciones de edición y eliminación del punto.

**Ejemplo concreto:**  
El supervisor hace click sobre un marker ubicado en (-34.6037, -58.3816). Se abre un ``MudDialog`` con título "Punto 2026-04-13 10:15:33" que presenta: coordenadas en formato ``MudText`` con tipografía ``Body2``, descripción "Poste de alumbrado sector norte", tres miniaturas de fotos en un ``MudGrid`` de 3 columnas, y dos botones: ``MudButton`` "Editar" con variante ``Outlined`` y ``MudButton`` "Eliminar" con variante ``Filled`` y color ``Error``. Al cerrar el diálogo, el popup de Leaflet se cierra.

---

## F-08 — Edición de nombre y descripción del punto

**Descripción:** El sistema permite al usuario modificar el nombre y la descripción de un punto georeferenciado existente. La edición se realiza con estrategia local-first en el cliente móvil.

**Comportamiento esperado:**  
Al acceder a la edición de un punto, se presenta un formulario con dos campos ``MudTextField``: uno para el nombre (obligatorio, máximo 100 caracteres) y otro para la descripción (opcional, máximo 500 caracteres). Al confirmar la edición, el sistema actualiza el registro local, modifica el campo ``UpdatedAt`` a la fecha/hora UTC actual, y en el caso de Mobile, registra una operación ``Update`` en ``SyncQueue``.

**Ejemplo concreto:**  
El técnico de campo abre el detalle del "Punto 2026-04-13 10:15:33" y pulsa "Editar". Se presenta un ``MudDialog`` con un ``MudTextField`` para nombre (valor actual: "Punto 2026-04-13 10:15:33") y un ``MudTextField`` con ``Lines="3"`` para descripción (vacío). El técnico cambia el nombre a "Poste de alumbrado #47" y escribe la descripción "Poste con base dañada, requiere reemplazo". Al pulsar el ``MudButton`` "Guardar", el sistema actualiza ``Puntos_Local``, establece ``SyncStatus = PendingUpdate``, inserta una entrada en ``SyncQueue`` con ``OperationType = Update``, y muestra un ``MudSnackbar`` confirmando "Punto actualizado correctamente".

---

## F-09 — Listado de puntos con filtros [ACTUALIZADO]

**Descripción:** El sistema presenta un listado tabular de todos los puntos georeferenciados con capacidades de búsqueda, filtrado y ordenamiento.

**Comportamiento esperado:**  
Se utiliza un componente ``MudTable<Punto>`` con paginación del lado del servidor, barra de búsqueda integrada (``MudTextField`` con debounce de 300 ms), y columnas ordenables para nombre, fecha de creación, cantidad de fotos y estado de sincronización. Cada fila incluye botones de acción para ver en mapa, editar y eliminar.

**Ejemplo concreto:**  
El supervisor accede a la sección "Listado de Puntos" de GeoFoto.Web. Se renderiza un ``MudTable`` con columnas: Nombre, Coordenadas, Fecha de Creación, Nº Fotos y Acciones. La tabla muestra 10 registros por página con un ``MudTablePager``. El supervisor escribe "poste" en el ``MudTextField`` de búsqueda; la tabla se filtra mostrando 3 resultados coincidentes. Al hacer click en el encabezado "Fecha de Creación", los resultados se ordenan de más reciente a más antiguo. Cada fila presenta tres ``MudIconButton``: mapa (``Icons.Material.Filled.Map``), editar (``Icons.Material.Filled.Edit``) y eliminar (``Icons.Material.Filled.Delete``).

**Escenario ESC-01 (Markers superpuestos en sync):** Al sincronizar, el servidor puede devolver markers cuyos radios se superponen con markers locales existentes. La app NO intenta resolver la superposición: los trata como markers completamente independientes y los agrega a SQLite con Status="Synced". No hay merge automático de markers por proximidad en sync.

---

## F-10 — Eliminación de punto con todas sus fotos

**Descripción:** El sistema permite eliminar un punto georeferenciado junto con todas sus fotografías asociadas, previa confirmación del usuario mediante un diálogo de confirmación.

**Comportamiento esperado:**  
Al solicitar la eliminación de un punto, se presenta un ``MudDialog`` de confirmación con el texto "¿Está seguro de que desea eliminar el punto '{Nombre}' y sus {N} fotos asociadas? Esta acción no se puede deshacer." con botones "Cancelar" y "Eliminar". Al confirmar, en Mobile se marcan los registros de ``Puntos_Local`` y ``Fotos_Local`` con ``SyncStatus = PendingDelete`` y se registra la operación en ``SyncQueue``. En Web, se envía la solicitud directamente a la API, que elimina los registros de SQL Server y los archivos físicos.

**Ejemplo concreto:**  
El técnico desea eliminar el punto "Poste de alumbrado #47" que tiene 3 fotos asociadas. Al pulsar el ``MudIconButton`` de eliminación, se abre un ``MudDialog`` con severidad ``Warning`` que indica "¿Está seguro de que desea eliminar el punto 'Poste de alumbrado #47' y sus 3 fotos asociadas? Esta acción no se puede deshacer." El técnico pulsa el ``MudButton`` "Eliminar" con color ``Error``. El sistema marca el punto y sus 3 fotos con ``SyncStatus = PendingDelete``, oculta el marker del mapa, y registra una operación ``Delete`` en ``SyncQueue``. El punto desaparece del listado y del mapa localmente. Cuando se restablezca la conectividad, el SyncService enviará la solicitud DELETE a la API.

---

## F-11 — Indicador de estado de conexión

**Descripción:** El sistema muestra de forma permanente el estado actual de la conectividad de red mediante un indicador visual en la barra de aplicación.

**Comportamiento esperado:**  
En la barra superior de la aplicación (``MudAppBar``) se muestra un componente ``MudChip`` que refleja el estado de conectividad en tiempo real. El estado se obtiene mediante ``Connectivity.Current.NetworkAccess`` en Mobile y mediante el evento ``navigator.onLine`` vía JS Interop en Web. El chip cambia dinámicamente entre tres estados: "Conectado" (color ``Success``), "Sin conexión" (color ``Error``) y "Sincronizando" (color ``Warning``).

**Ejemplo concreto:**  
El técnico de campo se encuentra trabajando con conectividad WiFi activa. En la ``MudAppBar`` se muestra un ``MudChip`` verde con ícono ``Icons.Material.Filled.Wifi`` y texto "Conectado". El técnico ingresa a un sótano y pierde la cobertura de red. El ``MudChip`` cambia automáticamente a color rojo con ícono ``Icons.Material.Filled.WifiOff`` y texto "Sin conexión". Al salir del sótano y recuperar la señal, el chip cambia a amarillo con ícono ``Icons.Material.Filled.Sync`` y texto "Sincronizando" mientras el SyncService procesa la cola, y finalmente vuelve a verde "Conectado" una vez completada la sincronización.

---

## F-12 — Cola de sincronización visible

**Descripción:** El sistema ofrece una vista que permite al usuario consultar el estado de las operaciones pendientes de sincronización registradas en la ``SyncQueue``.

**Comportamiento esperado:**  
La vista ``EstadoSync`` presenta un ``MudTable`` con las operaciones pendientes de sincronización, mostrando las columnas: tipo de operación, tipo de entidad, estado, cantidad de intentos, fecha del último intento y mensaje de error (si existe). La tabla se actualiza automáticamente cada vez que el SyncService procesa una operación.

**Ejemplo concreto:**  
El técnico ha realizado 5 operaciones offline durante la jornada. Accede a la pantalla "Estado de Sincronización" desde el menú lateral (``MudNavMenu``). Se muestra un ``MudTable`` con 5 filas:

| Operación | Entidad | Estado | Intentos | Último Intento | Error |
|-----------|---------|--------|----------|----------------|-------|
| Create | Punto | Pending | 0 | — | — |
| Create | Foto | Pending | 0 | — | — |
| Update | Punto | Failed | 3 | 2026-04-13 14:30 | Timeout |
| Create | Foto | Pending | 0 | — | — |
| Delete | Punto | Pending | 0 | — | — |

La fila con estado ``Failed`` se resalta en color rojo mediante la propiedad ``RowStyleFunc`` de ``MudTable``. El técnico puede ver que la operación de actualización falló tras 3 intentos por timeout.

---

## F-13 — Sincronización automática al detectar red

**Descripción:** El SyncService detecta automáticamente el restablecimiento de la conectividad de red e inicia el procesamiento de la cola de sincronización sin intervención del usuario.

**Comportamiento esperado:**  
El SyncService se suscribe al evento ``Connectivity.ConnectivityChanged`` de .NET MAUI. Cuando el estado de red cambia de ``None`` a ``Internet``, el servicio inicia automáticamente el procesamiento secuencial de las operaciones en ``SyncQueue`` ordenadas por ``CreatedAt`` ascendente. Las operaciones se procesan una a una; si una operación falla, se incrementa el contador ``Attempts``, se registra el error en ``ErrorMessage``, y se aplica backoff exponencial antes del siguiente reintento. Las operaciones exitosas se eliminan de la cola.

**Ejemplo concreto:**  
El técnico trabajó offline durante 2 horas y acumuló 12 operaciones en la ``SyncQueue``. Al conectarse a la red WiFi de la oficina, el ``SyncService`` detecta el cambio de conectividad a través de ``Connectivity.ConnectivityChanged``. Automáticamente comienza a procesar las 12 operaciones: envía la primera operación (Create Punto) a la API, recibe el ``RemoteId = 42``, actualiza ``Puntos_Local`` con ``RemoteId = 42`` y ``SyncStatus = Synced``, elimina la entrada procesada de ``SyncQueue``, y continúa con la siguiente operación. El indicador de conexión cambia a "Sincronizando" y el contador de operaciones pendientes en la vista ``EstadoSync`` decrece de 12 a 0 progresivamente.

---

## F-14 — Sincronización manual desde EstadoSync

**Descripción:** El sistema permite al usuario forzar la sincronización manual de las operaciones pendientes desde la pantalla de estado de sincronización.

**Comportamiento esperado:**  
En la vista ``EstadoSync`` se presenta un ``MudButton`` con texto "Sincronizar ahora" que, al ser pulsado, invoca manualmente el procesamiento de la ``SyncQueue``. El botón se deshabilita si no hay conectividad de red o si ya existe un proceso de sincronización en curso. Durante la sincronización, el botón muestra un ``MudProgressCircular`` como indicador de carga.

**Ejemplo concreto:**  
El técnico observa en la vista ``EstadoSync`` que tiene 3 operaciones pendientes y una en estado ``Failed``. Pulsa el ``MudButton`` "Sincronizar ahora" con ícono ``Icons.Material.Filled.CloudUpload``. El botón muestra un ``MudProgressCircular`` con ``Size.Small`` y el texto cambia a "Sincronizando...". El SyncService procesa las 4 operaciones: las 3 pendientes se envían con éxito y se eliminan de la cola; la operación fallida se reintenta y en este caso logra completarse. Al finalizar, el ``MudTable`` queda vacío y un ``MudSnackbar`` de severidad ``Success`` muestra "Sincronización completada. 4 operaciones procesadas."

---

## F-15 — Resolución automática de conflictos Last-Write-Wins

**Descripción:** Cuando se detecta un conflicto entre datos locales y datos del servidor durante la sincronización, el sistema aplica automáticamente la estrategia Last-Write-Wins (LWW) comparando el campo ``UpdatedAt`` (UTC) de ambas versiones.

**Comportamiento esperado:**  
Durante el push de una operación ``Update``, la API compara el ``UpdatedAt`` del registro recibido con el ``UpdatedAt`` del registro almacenado en SQL Server. Si el registro entrante tiene un ``UpdatedAt`` más reciente, se aplica la actualización. Si el registro del servidor es más reciente, la API retorna un código HTTP 409 (Conflict) junto con la versión actual del servidor. El SyncService recibe el conflicto, sobrescribe el registro local con la versión del servidor, marca el registro como ``Synced``, y registra el evento de conflicto en un log local.

**Ejemplo concreto:**  
El técnico renombró offline el punto "Poste #47" a "Poste reparado #47" a las 10:00 UTC (``UpdatedAt = 2026-04-13T10:00:00Z``). Mientras tanto, el supervisor desde GeoFoto.Web renombró el mismo punto a "Poste verificado #47" a las 10:05 UTC (``UpdatedAt = 2026-04-13T10:05:00Z``). Cuando el técnico se conecta a las 10:30, el SyncService envía la actualización del técnico a la API. La API detecta que la versión del servidor (``UpdatedAt = 10:05``) es más reciente que la versión enviada (``UpdatedAt = 10:00``), retorna HTTP 409 con la versión del servidor. El SyncService actualiza el registro local con el nombre "Poste verificado #47", marca ``SyncStatus = Synced``, y elimina la operación de la ``SyncQueue``. No se notifica al usuario de forma intrusiva; el conflicto queda registrado en un log interno accesible desde la vista de diagnóstico.

---

## F-16 — Centrado de mapa en posición GPS actual (Mobile y Web)

**Descripción:** El sistema permite centrar el mapa en la posición GPS actual del usuario tocando un FAB (Mobile) o botón (Web).

**Comportamiento esperado:**
En Mobile: al tocar el FAB GPS se llama ``IDeviceLocationService.GetCurrentLocationAsync()`` con timeout de 10s. Si OK: ``leafletInterop.setView(lat, lng, 15)``. Si timeout: ``MudSnackbar`` Warning "No se pudo obtener ubicación". Si permiso denegado: se aplica ESC-03. En Web: se usa ``navigator.geolocation.getCurrentPosition()`` del browser.

**Ejemplo concreto:**
El técnico de campo está en obra y toca el FAB azul de GPS en la esquina inferior derecha. El mapa se centra en sus coordenadas actuales (-34.6037, -58.3816) con zoom 15 en menos de 2 segundos.

---

## F-17 — Marcador de posición propia en mapa

**Descripción:** El sistema muestra la posición actual del usuario en el mapa como un marcador visual diferenciado de los markers de fotos.

**Comportamiento esperado:**
Se renderiza un ``L.circleMarker`` con animación CSS de pulso (color azul) que se actualiza cada 5 segundos mediante polling. Al perder el GPS, el marcador desaparece y se muestra ``MudSnackbar`` warning. El marcador de posición propia nunca se confunde con un marker de foto.

**Ejemplo concreto:**
El técnico ve en el mapa un círculo azul pulsante en su posición exacta. Al entrar a un túnel y perder GPS, el círculo desaparece y aparece un snackbar "Sin señal GPS".

---

## F-18 — Radio visual y configurable del marker

**Descripción:** El sistema muestra un círculo semi-transparente alrededor de cada marker representando el radio de agrupación, y permite ajustarlo desde un slider.

**Comportamiento esperado:**
Al tocar un marker, se muestra un ``L.circle`` semi-transparente representando el radio actual (default 50m, rango 10-500m). En el popup hay un ``MudSlider`` que permite ajustar el radio. El cambio se persiste en ``IPreferencesService`` y SQLite y se aplica globalmente a todos los markers.

**Ejemplo concreto:**
El técnico toca un marker y ve un círculo de 50m a su alrededor. Mueve el slider a 100m, el círculo se expande en tiempo real. La próxima foto tomada a 80m del marker se asocia a ese marker (dentro del radio) en lugar de crear uno nuevo.

---

## F-19 — Popup de marker con carrusel, título y descripción editables

**Descripción:** Al tocar un marker se abre un ``MudDialog`` con título y descripción editables, carrusel de fotos y acciones.

**Comportamiento esperado:**
El popup contiene: ``MudTextField`` para Titulo (editable, guarda on-blur), ``MudTextField`` multilínea para Descripcion (editable, guarda on-blur), componente ``FotoCarousel`` con las fotos del punto, botón "Agregar foto" (cámara en Mobile, ``MudFileUpload`` en Web), botón "Eliminar marker" y botón "Cerrar". Todos los cambios se encolan en SyncQueue.

**Ejemplo concreto:**
El técnico toca el marker "Punto 2026-04-13 10:15:33". Se abre el popup. Edita el nombre a "Poste #47", escribe la descripción "Base dañada" y cierra el campo. El nombre se actualiza en SQLite automáticamente. Al volver al mapa, el marker muestra el nuevo nombre en el popup.

---

## F-20 — Visor fullscreen de fotos con descripción individual

**Descripción:** El sistema permite ampliar cualquier foto del carrusel en modo fullscreen con la posibilidad de editar una descripción individual.

**Comportamiento esperado:**
Al tocar una foto en el carrusel se abre un ``MudOverlay`` con la imagen a tamaño completo. Hay un ``MudTextField`` para editar el comentario de esa foto específica. El botón ✕ o tap fuera cierra el visor y vuelve al carrusel en la misma posición. En Mobile soporta pinch-to-zoom.

**Ejemplo concreto:**
El supervisor hace click en la segunda foto del carrusel del punto "Poste #47". Se abre el visor fullscreen mostrando la foto ampliada. Escribe "Vista desde el norte, base claramente dañada" en el campo de descripción y cierra. El comentario queda guardado en SQLite y se encola para sync.

---

## F-21 — Eliminación de foto desde carrusel

**Descripción:** Cada foto del carrusel tiene un botón ✕ que permite eliminarla con confirmación.

**Comportamiento esperado:**
Al tocar el botón ✕ de una foto aparece un ``MudDialog`` de confirmación: "¿Eliminar esta foto? Esta acción no se puede deshacer." Si confirma: la foto se elimina de SQLite con ``IsDeleted=true``, se encola ``PendingDelete`` en SyncQueue. El carrusel se actualiza sin cerrar el popup.

**Ejemplo concreto:**
El técnico ve que la primera foto del carrusel está desenfocada. Toca ✕, confirma en el dialog, y la foto desaparece del carrusel instantáneamente mientras el popup permanece abierto.

---

## F-22 — Pantalla de sincronización con estado e historial

**Descripción:** Pantalla dedicada que muestra el estado de sincronización, el historial de operaciones y permite sincronizar manualmente.

**Comportamiento esperado:**
La pantalla "Sincronización" (accesible desde AppBar) muestra: fecha/hora de última sync (o "Nunca"), cantidad de items pendientes, items fallidos con motivo. Un botón "Sincronizar ahora" dispara ``PushAsync() + PullAsync()`` con spinner durante la operación. La lista de operaciones usa ``MudChip`` por estado: Pending (naranja), Synced (verde), Failed (rojo).

**Ejemplo concreto:**
El técnico ve en la pantalla de sync que tiene 3 pendientes y 1 fallida. Toca "Sincronizar ahora", el botón muestra spinner durante 4 segundos y la lista se actualiza mostrando todas como Synced.

---

## F-23 — Lista de markers con búsqueda y navegación

**Descripción:** Pantalla que lista todos los markers con búsqueda en tiempo real y acceso directo al mapa y popup.

**Comportamiento esperado:**
La pantalla "Lista de markers" muestra un ``MudTable`` con: nombre del marker, coordenadas, cantidad de fotos, estado de sync (``MudChip`` con color). Un ``MudTextField`` filtra por nombre en tiempo real. Al tocar un ítem el mapa se centra en ese marker Y se abre el popup.

**Ejemplo concreto:**
El técnico busca "poste" y aparecen 5 markers que contienen "poste" en el nombre. Toca "Poste #47", la app navega al mapa y abre automáticamente el popup de ese marker.

---

## F-24 — Descarga de fotos de marker como zip (Web)

**Descripción:** En la web, el supervisor puede descargar todas las fotos de un marker como un archivo .zip.

**Comportamiento esperado:**
En el popup del marker (``IsMobile=false``) aparece el botón "Descargar fotos" (deshabilitado si no hay fotos). Al tocarlo, se llama ``GET /api/puntos/{id}/fotos/download`` que retorna un .zip con las fotos nombradas como ``{nombrePunto}_{n}.jpg``. El browser lo descarga automáticamente.

**Ejemplo concreto:**
El supervisor en el navegador ve el popup del marker "Poste #47" con 3 fotos. Toca "Descargar fotos" y el browser descarga automáticamente ``Poste_47.zip`` con los 3 archivos.

---

## F-25 — Subida de fotos al marker desde web sin requerir GPS

**Descripción:** En la web, el supervisor puede agregar fotos a un marker existente desde el browser, aunque las fotos no tengan datos EXIF GPS.

**Comportamiento esperado:**
En el popup del marker (``IsMobile=false``) aparece el botón "Agregar foto" que abre un ``MudFileUpload``. La foto se sube al servidor con el ``PuntoId`` del marker. No se requiere EXIF GPS — la foto queda vinculada al marker por ``PuntoId``. No se muestra error ni advertencia por falta de coordenadas propias (ESC-04). El carrusel se actualiza inmediatamente.

**Ejemplo concreto:**
El supervisor sube una foto de un documento asociado al poste. La foto no tiene EXIF GPS pero queda vinculada al marker "Poste #47" sin ningún error. El carrusel muestra 4 fotos.

---

# 6. Reglas de Negocio

## RN-01 — Escritura local primero

Toda operación de escritura (creación, edición o eliminación) realizada desde GeoFoto.Mobile se persiste primero en la base de datos SQLite local del dispositivo. Ninguna operación de escritura en el cliente móvil requiere conectividad de red como precondición. La escritura local es síncrona, inmediata y atómica respecto a la transacción SQLite.

## RN-02 — SyncService no bloqueante

El SyncService se ejecuta en un hilo de background y nunca bloquea la interfaz de usuario ni la interacción del técnico con la aplicación. Las operaciones de sincronización se procesan de forma asíncrona mediante ``Task.Run`` y se coordinan a través de un ``SemaphoreSlim`` para evitar ejecuciones concurrentes del ciclo de sincronización.

## RN-03 — Fotos sin GPS generan punto en (0, 0) con advertencia

Cuando una fotografía no contiene metadatos EXIF de geolocalización (GPS), el sistema crea el punto georeferenciado en las coordenadas (0, 0) (intersección del meridiano de Greenwich con el ecuador). Se notifica al usuario mediante un componente ``MudAlert`` con severidad ``Warning`` que indica la ausencia de coordenadas GPS y la asignación de ubicación predeterminada. El usuario puede posteriormente editar las coordenadas del punto manualmente.

## RN-04 — Backoff exponencial en reintentos de sincronización

Cuando una operación de la ``SyncQueue`` falla al ser enviada al servidor, el SyncService aplica una estrategia de backoff exponencial para los reintentos. Los intervalos de espera entre intentos siguen la secuencia: 5 segundos, 30 segundos, 5 minutos. Tras 3 intentos fallidos consecutivos, la operación se marca con ``Status = Failed`` y permanece en la cola para reintento manual o automático en el próximo ciclo de conectividad. El campo ``Attempts`` registra la cantidad de intentos realizados y ``LastAttemptAt`` la fecha/hora del último intento.

## RN-05 — Resolución de conflictos Last-Write-Wins por UpdatedAt

La resolución de conflictos entre la versión local y la versión del servidor se determina comparando el campo ``UpdatedAt`` (almacenado en formato UTC) de ambos registros. La versión con el ``UpdatedAt`` más reciente prevalece. Esta regla se aplica por registro individual, no por lote de sincronización. El campo ``UpdatedAt`` se actualiza automáticamente en cada modificación del registro, tanto en el cliente como en el servidor.

## RN-06 — Almacenamiento de imágenes en filesystem local de Android

Las imágenes capturadas o seleccionadas en GeoFoto.Mobile se almacenan en el directorio privado de la aplicación de Android, accesible mediante ``FileSystem.AppDataDirectory``. Las fotografías se organizan en el subdirectorio ``fotos/`` con nombres de archivo únicos generados mediante ``Guid.NewGuid()`` conservando la extensión original del archivo. Este directorio no es accesible por otras aplicaciones y se elimina automáticamente al desinstalar GeoFoto.Mobile.

## RN-07 — Eliminación pendiente hasta confirmación de sincronización

Cuando se elimina un punto o una foto en GeoFoto.Mobile, el registro no se borra físicamente de SQLite. En su lugar, se marca con ``SyncStatus = PendingDelete`` y se registra una operación ``Delete`` en la ``SyncQueue``. El registro se oculta de la interfaz de usuario pero se mantiene en la base de datos hasta que el SyncService confirme exitosamente la eliminación en el servidor. Solo tras recibir la confirmación HTTP 200/204 de la API, se elimina físicamente el registro de SQLite y el archivo de imagen del filesystem local.

## RN-08 — SQLite como fuente de verdad operativa; SQL Server como fuente global

En GeoFoto.Mobile, la base de datos SQLite local constituye la fuente de verdad operativa: la interfaz de usuario lee y escribe exclusivamente contra SQLite. SQL Server, accesible a través de la API REST, constituye la fuente de verdad global del sistema: consolida los datos de todos los dispositivos y es la base de datos consultada por GeoFoto.Web. El proceso de sincronización se encarga de mantener la coherencia eventual entre ambas fuentes.

---

# 7. Datos Principales del Sistema

El modelo de datos de GeoFoto se distribuye entre dos motores de base de datos: SQL Server (servidor) y SQLite (dispositivo móvil). A continuación se describen las entidades principales y sus campos clave.

## 7.1 Punto (SQL Server — tabla ``Puntos``)

Representa un punto georeferenciado en el servidor central. Es la entidad principal del modelo de dominio.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| ``Id`` | ``int`` (PK, autoincremental) | Identificador único del punto en el servidor. |
| ``Latitud`` | ``double`` | Latitud del punto en formato decimal (WGS84). |
| ``Longitud`` | ``double`` | Longitud del punto en formato decimal (WGS84). |
| ``Nombre`` | ``nvarchar(100)`` | Nombre descriptivo del punto, asignado automáticamente o por el usuario. |
| ``Descripcion`` | ``nvarchar(500)`` | Descripción textual opcional del punto. |
| ``FechaCreacion`` | ``datetime2`` | Fecha y hora de creación del registro (UTC). |
| ``UpdatedAt`` | ``datetime2`` | Fecha y hora de la última modificación (UTC). Utilizado para resolución LWW. |

## 7.2 Foto (SQL Server — tabla ``Fotos``)

Representa una fotografía asociada a un punto georeferenciado en el servidor central.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| ``Id`` | ``int`` (PK, autoincremental) | Identificador único de la foto en el servidor. |
| ``PuntoId`` | ``int`` (FK → ``Puntos.Id``) | Identificador del punto al que pertenece la foto. |
| ``NombreArchivo`` | ``nvarchar(255)`` | Nombre original del archivo de imagen. |
| ``RutaFisica`` | ``nvarchar(500)`` | Ruta física del archivo en el servidor o almacenamiento. |
| ``FechaTomada`` | ``datetime2`` | Fecha y hora de captura de la imagen (extraída del EXIF o fecha de carga). |
| ``TamanoBytes`` | ``long`` | Tamaño del archivo en bytes. |
| ``LatitudExif`` | ``double?`` | Latitud extraída de los metadatos EXIF (nullable si no disponible). |
| ``LongitudExif`` | ``double?`` | Longitud extraída de los metadatos EXIF (nullable si no disponible). |
| ``UpdatedAt`` | ``datetime2`` | Fecha y hora de la última modificación (UTC). |

## 7.3 Punto_Local (SQLite — tabla ``Puntos_Local``)

Réplica local de la entidad Punto, con campos adicionales para gestionar la sincronización.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| ``LocalId`` | ``int`` (PK, autoincremental) | Identificador único local del punto en SQLite. |
| ``RemoteId`` | ``int?`` | Identificador del punto en el servidor (null si aún no se sincronizó). |
| ``Latitud`` | ``double`` | Latitud del punto en formato decimal. |
| ``Longitud`` | ``double`` | Longitud del punto en formato decimal. |
| ``Nombre`` | ``string`` | Nombre descriptivo del punto. |
| ``Descripcion`` | ``string`` | Descripción textual del punto. |
| ``FechaCreacion`` | ``datetime`` | Fecha y hora de creación (UTC). |
| ``UpdatedAt`` | ``datetime`` | Fecha y hora de la última modificación (UTC). |
| ``SyncStatus`` | ``enum`` | Estado de sincronización: ``Synced``, ``PendingCreate``, ``PendingUpdate``, ``PendingDelete``. |

## 7.4 Foto_Local (SQLite — tabla ``Fotos_Local``)

Réplica local de la entidad Foto, con campos adicionales para sincronización y ruta al archivo local.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| ``LocalId`` | ``int`` (PK, autoincremental) | Identificador único local de la foto en SQLite. |
| ``RemoteId`` | ``int?`` | Identificador de la foto en el servidor (null si aún no se sincronizó). |
| ``PuntoLocalId`` | ``int`` (FK → ``Puntos_Local.LocalId``) | Identificador del punto local al que pertenece la foto. |
| ``NombreArchivo`` | ``string`` | Nombre original del archivo de imagen. |
| ``RutaLocal`` | ``string`` | Ruta del archivo en el filesystem local de Android (``AppDataDirectory/fotos/``). |
| ``FechaTomada`` | ``datetime`` | Fecha y hora de captura de la imagen. |
| ``TamanoBytes`` | ``long`` | Tamaño del archivo en bytes. |
| ``LatitudExif`` | ``double?`` | Latitud extraída del EXIF (nullable). |
| ``LongitudExif`` | ``double?`` | Longitud extraída del EXIF (nullable). |
| ``UpdatedAt`` | ``datetime`` | Fecha y hora de la última modificación (UTC). |
| ``SyncStatus`` | ``enum`` | Estado de sincronización: ``Synced``, ``PendingCreate``, ``PendingUpdate``, ``PendingDelete``. |

## 7.5 SyncQueue (SQLite — tabla ``SyncQueue``)

Registro de operaciones pendientes de sincronización con el servidor, implementando el patrón Outbox.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| ``Id`` | ``int`` (PK, autoincremental) | Identificador único de la operación en la cola. |
| ``OperationType`` | ``string`` | Tipo de operación: ``Create``, ``Update``, ``Delete``. |
| ``EntityType`` | ``string`` | Tipo de entidad afectada: ``Punto``, ``Foto``. |
| ``LocalId`` | ``int`` | Identificador local de la entidad afectada. |
| ``Payload`` | ``string`` | Representación JSON del estado de la entidad al momento de la operación. |
| ``Status`` | ``string`` | Estado de la operación: ``Pending``, ``InProgress``, ``Failed``. |
| ``Attempts`` | ``int`` | Cantidad de intentos de envío realizados. |
| ``LastAttemptAt`` | ``datetime?`` | Fecha y hora del último intento de sincronización. |
| ``ErrorMessage`` | ``string?`` | Mensaje de error del último intento fallido. |
| ``CreatedAt`` | ``datetime`` | Fecha y hora de creación de la operación en la cola (UTC). |

---

# 8. Criterios de Aceptación BDD

## CA-01 — Carga de foto desde galería

**Dado** que el usuario se encuentra en la pantalla principal de GeoFoto.Mobile y dispone de una imagen con coordenadas GPS en la galería del dispositivo,  
**Cuando** el usuario pulsa el botón "Agregar foto", selecciona una imagen de la galería y confirma la selección,  
**Entonces** el sistema almacena la imagen en el directorio local ``AppDataDirectory/fotos/``, crea un registro en ``Fotos_Local`` con ``SyncStatus = PendingCreate``, asocia la foto a un punto existente o crea un nuevo punto con las coordenadas EXIF, y muestra una notificación de confirmación en un ``MudSnackbar``.

## CA-02 — Extracción automática de coordenadas EXIF

**Dado** que el usuario ha seleccionado o capturado una fotografía que contiene metadatos EXIF con información GPS válida (latitud y longitud),  
**Cuando** el sistema procesa la imagen mediante MetadataExtractor,  
**Entonces** se extraen correctamente los valores de latitud y longitud en formato decimal, se asignan al punto georeferenciado correspondiente, y el marker se posiciona en las coordenadas exactas extraídas del EXIF al visualizar el mapa.

## CA-03 — Almacenamiento offline sin errores

**Dado** que el dispositivo móvil se encuentra sin conectividad de red (modo avión o zona sin cobertura),  
**Cuando** el técnico de campo captura una fotografía y el sistema intenta almacenar los datos,  
**Entonces** el registro se guarda exitosamente en SQLite local sin mensajes de error relacionados con la red, la interfaz de usuario responde de forma inmediata, y se crea una entrada en ``SyncQueue`` con ``Status = Pending`` para su posterior envío al servidor.

## CA-04 — Visualización de puntos en el mapa Leaflet

**Dado** que existen al menos tres puntos georeferenciados registrados en la base de datos (local o remota, según el cliente),  
**Cuando** el usuario accede a la vista de mapa,  
**Entonces** el mapa Leaflet se renderiza correctamente, se muestran tres markers posicionados en las coordenadas de cada punto, y el mapa se ajusta automáticamente mediante ``fitBounds()`` para que todos los markers sean visibles en el viewport.

## CA-05 — Popup informativo al click en marker

**Dado** que el mapa muestra un marker correspondiente a un punto con nombre "Poste de alumbrado #47", descripción "Base dañada" y 2 fotos asociadas,  
**Cuando** el usuario hace click sobre dicho marker,  
**Entonces** se abre un ``MudDialog`` que muestra el nombre "Poste de alumbrado #47", la descripción "Base dañada", las coordenadas del punto, y 2 miniaturas de las fotografías asociadas, junto con botones de acción para editar y eliminar.

## CA-06 — Edición de un punto georeferenciado

**Dado** que el usuario ha abierto el formulario de edición de un punto con nombre "Punto 2026-04-13 10:15:33" y descripción vacía,  
**Cuando** el usuario modifica el nombre a "Poste reparado #47", escribe la descripción "Reparado el 13/04/2026" y pulsa "Guardar",  
**Entonces** el registro se actualiza en la base de datos con el nuevo nombre y descripción, el campo ``UpdatedAt`` se establece a la fecha/hora UTC actual, en Mobile se registra una operación ``Update`` en ``SyncQueue``, y se muestra una confirmación al usuario.

## CA-07 — Eliminación de punto con confirmación

**Dado** que existe un punto "Poste obsoleto" con 2 fotos asociadas,  
**Cuando** el usuario pulsa el botón de eliminar y confirma la acción en el diálogo de confirmación,  
**Entonces** en Mobile, el punto y sus fotos se marcan con ``SyncStatus = PendingDelete`` y se ocultan de la interfaz; en Web, se envía la solicitud DELETE a la API y los registros se eliminan de SQL Server. En ambos casos, el marker desaparece del mapa y el punto se elimina del listado.

## CA-08 — Visibilidad de la cola de sincronización

**Dado** que el técnico ha realizado 4 operaciones offline (2 creaciones, 1 edición, 1 eliminación),  
**Cuando** el técnico accede a la vista "Estado de Sincronización",  
**Entonces** se muestra un ``MudTable`` con 4 filas que reflejan las operaciones pendientes, incluyendo para cada una el tipo de operación, tipo de entidad, estado actual, cantidad de intentos, fecha del último intento y mensaje de error si corresponde.

## CA-09 — Sincronización automática al recuperar conectividad

**Dado** que el dispositivo se encontraba sin conectividad y existen 5 operaciones pendientes en la ``SyncQueue``,  
**Cuando** el dispositivo recupera la conectividad de red (WiFi o datos móviles),  
**Entonces** el SyncService inicia automáticamente el procesamiento de las 5 operaciones, las envía secuencialmente a la API, actualiza los registros locales con los ``RemoteId`` asignados por el servidor, marca los registros como ``Synced``, y elimina las operaciones procesadas de la cola.

## CA-10 — Sincronización manual desde interfaz

**Dado** que el dispositivo tiene conectividad de red y existen operaciones pendientes en la ``SyncQueue``,  
**Cuando** el usuario pulsa el botón "Sincronizar ahora" en la vista ``EstadoSync``,  
**Entonces** el SyncService inicia el procesamiento de la cola, el botón muestra un indicador de progreso durante la operación, las operaciones procesadas desaparecen de la tabla, y al finalizar se muestra un ``MudSnackbar`` indicando la cantidad de operaciones procesadas exitosamente.

## CA-11 — Resolución de conflicto Last-Write-Wins

**Dado** que el técnico editó offline un punto a las 10:00 UTC y el supervisor editó el mismo punto desde Web a las 10:05 UTC,  
**Cuando** el SyncService del técnico envía la actualización al servidor,  
**Entonces** la API detecta que la versión del servidor es más reciente (10:05 > 10:00), retorna HTTP 409, el SyncService actualiza el registro local con la versión del servidor, marca el registro como ``Synced``, y el punto refleja los datos de la versión más reciente del supervisor.

## CA-12 — Pull delta desde el servidor

**Dado** que el supervisor creó 3 nuevos puntos desde GeoFoto.Web mientras el técnico se encontraba offline, y la última sincronización del técnico fue a las 08:00 UTC,  
**Cuando** el técnico recupera conectividad y el SyncService ejecuta el pull delta con parámetro ``sinceUtc = 2026-04-13T08:00:00Z``,  
**Entonces** la API retorna los 3 nuevos puntos con sus fotos asociadas, el SyncService los inserta en ``Puntos_Local`` y ``Fotos_Local`` con ``SyncStatus = Synced`` y los ``RemoteId`` correspondientes, y los 3 nuevos markers aparecen en el mapa del dispositivo móvil.

---

# 9. Supuestos y Restricciones

## 9.1 Supuestos

| Código | Supuesto |
|--------|----------|
| S-01 | Se asume que los dispositivos Android de los técnicos de campo disponen de cámara con capacidad de registrar coordenadas GPS en los metadatos EXIF de las fotografías. |
| S-02 | Se asume que el servicio de ubicación (GPS) del dispositivo Android se encuentra habilitado y que el usuario ha otorgado los permisos de localización y cámara requeridos por la aplicación. |
| S-03 | Se asume que el servidor con la API REST y SQL Server se encuentra disponible y accesible desde Internet cuando el dispositivo móvil dispone de conectividad de red. |
| S-04 | Se asume que las imágenes cargadas son archivos JPEG o PNG con un tamaño máximo razonable para transmisión móvil (se recomienda no superar 10 MB por imagen). |
| S-05 | Se asume que la frecuencia de conflictos de edición concurrente entre técnico y supervisor sobre el mismo punto será baja, dado que cada técnico opera sobre zonas geográficas distintas. |
| S-06 | Se asume que el dispositivo Android dispone de al menos 500 MB de almacenamiento interno libre para el directorio de la aplicación donde se almacenan las fotografías locales. |
| S-07 | Se asume que el equipo de desarrollo tiene acceso a dispositivos Android físicos con versión 10.0 (API 29) o superior para pruebas de campo. |

## 9.2 Restricciones

| Código | Restricción |
|--------|-------------|
| R-01 | La aplicación móvil se desarrolla exclusivamente para Android. No se contempla soporte para iOS en la versión 1.0. |
| R-02 | La visualización del mapa Leaflet requiere conexión a Internet para la carga de tiles de OpenStreetMap. No se implementa caché de tiles para uso offline. |
| R-03 | El sistema no implementa autenticación ni autorización. Cualquier usuario con acceso a la URL de la API puede consultar y modificar datos. |
| R-04 | La resolución de conflictos se limita a la estrategia Last-Write-Wins. No se ofrece resolución manual de conflictos ni visualización de diferencias entre versiones. |
| R-05 | La sincronización se realiza de forma secuencial (una operación a la vez). No se implementa sincronización paralela ni por lotes para la versión 1.0. |
| R-06 | El stack tecnológico está fijado en .NET 10, ASP.NET Core Web API, Blazor Web App (InteractiveServer), MudBlazor, .NET MAUI Hybrid, Leaflet.js, SQLite (sqlite-net-pcl), EF Core con SQL Server y MetadataExtractor. |
| R-07 | Las coordenadas se almacenan en formato decimal WGS84. No se efectúan transformaciones de sistema de referencia de coordenadas (CRS). |
| R-08 | El sistema opera con un único esquema de base de datos sin soporte para multi-tenancy. Todos los datos residen en un único contexto de base de datos SQL Server. |

---

# 10. Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |
| 1.1 | 2026-04-16 | Agregados F-16 a F-25 (UX avanzado: GPS FAB, marcador propio, radio, carrusel, fullscreen, eliminar foto, sync panel, lista markers, zip descarga, upload web). Actualizados F-02 con ESC-02/ESC-03 y F-09 con ESC-01. |

---

**Fin del documento**
