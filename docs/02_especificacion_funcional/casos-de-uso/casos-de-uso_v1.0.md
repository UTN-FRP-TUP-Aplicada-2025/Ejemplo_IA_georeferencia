# Casos de Uso

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** casos-de-uso_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-13
**Autor:** Equipo Técnico

---

# 1. Propósito

El presente documento describe de manera exhaustiva los casos de uso del sistema GeoFoto. Cada caso de uso se especifica con su flujo principal, flujos alternativos, precondiciones, postcondiciones, reglas de negocio relacionadas y criterios de aceptación en formato BDD (Dado/Cuando/Entonces). El objetivo es servir como referencia formal para el desarrollo, las pruebas y la validación funcional del producto.

---

# 2. Tabla Resumen de Casos de Uso

| Código | Nombre | Actor Principal | Épica Jira |
|--------|--------|-----------------|------------|
| CU-01 | Capturar foto desde galería | Técnico / Supervisor | EP-01 Captura |
| CU-02 | Capturar foto con cámara nativa (MAUI) | Técnico de campo | EP-01 Captura |
| CU-03 | Extraer coordenadas GPS del EXIF | Sistema | EP-02 Geolocalización |
| CU-04 | Guardar punto y foto en SQLite local | Sistema | EP-03 Persistencia Local |
| CU-05 | Visualizar mapa con markers | Técnico / Supervisor | EP-04 Visualización |
| CU-06 | Ver detalle de punto con popup | Técnico / Supervisor | EP-04 Visualización |
| CU-07 | Editar nombre y descripción de punto | Técnico / Supervisor | EP-05 Gestión de Puntos |
| CU-08 | Eliminar punto con fotos | Supervisor | EP-05 Gestión de Puntos |
| CU-09 | Ver estado de sincronización | Técnico de campo | EP-06 Sincronización |
| CU-10 | Ejecutar sincronización manual | Técnico de campo | EP-06 Sincronización |
| CU-11 | Sincronización automática por reconexión | Sistema | EP-06 Sincronización |
| CU-12 | Enviar operación pendiente al servidor (push) | Sistema | EP-06 Sincronización |
| CU-13 | Recibir cambios del servidor (pull delta) | Sistema | EP-06 Sincronización |
| CU-14 | Resolver conflicto automáticamente | Sistema | EP-07 Resolución de Conflictos |
| CU-15 | Ver historial de operaciones de sync | Técnico de campo | EP-06 Sincronización |
| CU-16 | Subir foto desde web (online directo) | Supervisor | EP-01 Captura |

---

## CU-01: Capturar foto desde galería

### Actor Principal

Técnico / Supervisor.

### Precondiciones

- El usuario se encuentra autenticado en la aplicación.
- El dispositivo dispone de al menos una imagen almacenada en la galería.
- En plataforma móvil (MAUI), se cuenta con permisos de acceso a almacenamiento. En plataforma web, el navegador permite la selección de archivos.

### Flujo Principal

1. El usuario accede a la pantalla de captura de fotos.
2. El usuario presiona el botón "Seleccionar desde galería".
3. El sistema invoca el selector de archivos según la plataforma:
   - **Móvil (MAUI):** se lanza `MediaPicker.PickPhotoAsync()` presentando la galería nativa del dispositivo.
   - **Web (Blazor):** se presenta el componente `MudFileUpload` con filtro de tipos de imagen (`image/*`).
4. El usuario selecciona una imagen y confirma la selección.
5. El sistema valida que el archivo seleccionado sea una imagen en formato soportado (JPEG, PNG, HEIC).
6. El sistema invoca el caso de uso CU-03 para extraer las coordenadas GPS del EXIF.
7. El sistema invoca el caso de uso CU-04 para guardar el punto y la foto en SQLite local.
8. El sistema muestra una notificación de éxito (`MudSnackbar`) indicando que la foto fue capturada correctamente.
9. El mapa se actualiza mostrando un nuevo marker en la posición correspondiente.

### Flujos Alternativos

**FA-01 — El usuario cancela la selección:**
1. El usuario cierra el selector de archivos sin seleccionar ninguna imagen.
2. El sistema no realiza ninguna acción y permanece en la pantalla de captura.

**FA-02 — El archivo seleccionado no es una imagen válida:**
1. El sistema detecta que el archivo no corresponde a un formato de imagen soportado.
2. Se muestra un `MudAlert` de tipo Warning indicando "El archivo seleccionado no es una imagen válida. Formatos aceptados: JPEG, PNG, HEIC."
3. El flujo retorna al paso 2 del flujo principal.

**FA-03 — La imagen no contiene datos GPS en el EXIF:**
1. El caso de uso CU-03 no logra extraer coordenadas GPS.
2. Se aplica la regla de negocio RN-03: el punto se crea en coordenadas (0, 0).
3. Se muestra un `MudAlert` de tipo Warning indicando "La imagen no contiene información de ubicación. Se registró en coordenadas (0, 0)."
4. El flujo continúa en el paso 7 del flujo principal.

### Postcondiciones

- Se ha creado un registro `Punto_Local` asociado a la foto en SQLite local.
- Se ha creado un registro `Foto_Local` con la referencia al archivo de imagen.
- Se ha registrado una operación `PendingInsert` en la tabla `SyncQueue`.
- El mapa muestra el nuevo marker en la posición correspondiente.

### Reglas de Negocio Relacionadas

- **RN-01:** Toda escritura va primero a SQLite local.
- **RN-03:** Fotos sin GPS crean punto en (0, 0) con advertencia MudAlert.
- **RN-06:** Imágenes en filesystem local Android (`AppDataDirectory`).

### Criterios de Aceptación BDD

**Escenario 1: Selección exitosa desde galería móvil**
```
Dado que el usuario se encuentra en la pantalla de captura desde un dispositivo móvil
Cuando selecciona una imagen con datos GPS desde la galería nativa
Entonces el sistema crea un punto con las coordenadas extraídas del EXIF
  Y almacena la foto en el filesystem local
  Y registra la operación como PendingInsert en SyncQueue
  Y muestra un MudSnackbar de éxito
  Y el mapa presenta un nuevo marker en la posición correspondiente
```

**Escenario 2: Selección exitosa desde galería web**
```
Dado que el usuario se encuentra en la pantalla de captura desde el navegador web
Cuando selecciona una imagen mediante el componente MudFileUpload
Entonces el sistema procesa la imagen y crea el punto correspondiente
  Y almacena la información en SQLite local
```

**Escenario 3: Imagen sin datos GPS**
```
Dado que el usuario selecciona una imagen que no contiene metadatos EXIF de GPS
Cuando el sistema intenta extraer las coordenadas
Entonces se crea el punto en coordenadas (0, 0)
  Y se muestra un MudAlert de advertencia informando la ausencia de ubicación
```

---

## CU-02: Capturar foto con cámara nativa (MAUI)

### Actor Principal

Técnico de campo.

### Precondiciones

- El usuario se encuentra autenticado en la aplicación móvil MAUI.
- El dispositivo dispone de una cámara funcional.
- Se han concedido los permisos de acceso a la cámara y al almacenamiento.

### Flujo Principal

1. El usuario accede a la pantalla principal del mapa.
2. El usuario presiona el botón flotante `MudFab` con ícono de cámara.
3. El sistema invoca `MediaPicker.CapturePhotoAsync()` para lanzar la cámara nativa del dispositivo.
4. El usuario toma una fotografía y confirma la captura.
5. El sistema obtiene el `FileResult` con la ruta temporal de la imagen.
6. El sistema copia la imagen al directorio local de la aplicación (`AppDataDirectory`).
7. El sistema invoca el caso de uso CU-03 para extraer las coordenadas GPS del EXIF.
8. El sistema invoca el caso de uso CU-04 para guardar el punto y la foto en SQLite local.
9. El sistema muestra una notificación de éxito (`MudSnackbar`) indicando que la foto fue capturada correctamente.
10. El mapa se actualiza mostrando un nuevo marker en la posición registrada.

### Flujos Alternativos

**FA-01 — El usuario cancela la captura:**
1. El usuario descarta la foto en la interfaz de la cámara nativa.
2. `MediaPicker.CapturePhotoAsync()` retorna `null`.
3. El sistema no realiza ninguna acción y permanece en la pantalla del mapa.

**FA-02 — La cámara no está disponible:**
1. El sistema detecta que el dispositivo no cuenta con una cámara disponible o los permisos fueron denegados.
2. Se muestra un `MudAlert` de tipo Error indicando "No se pudo acceder a la cámara. Verifique los permisos del dispositivo."
3. El flujo se interrumpe.

**FA-03 — Error al copiar la imagen al almacenamiento local:**
1. Se produce un error de E/S al copiar la imagen al `AppDataDirectory`.
2. Se muestra un `MudAlert` de tipo Error indicando "Error al guardar la imagen. Verifique el espacio disponible en el dispositivo."
3. El flujo se interrumpe.

**FA-04 — La foto no contiene datos GPS en el EXIF:**
1. El caso de uso CU-03 no logra extraer coordenadas GPS.
2. Se aplica la regla de negocio RN-03: el punto se crea en coordenadas (0, 0).
3. Se muestra un `MudAlert` de tipo Warning indicando "La foto no contiene información de ubicación."
4. El flujo continúa en el paso 8 del flujo principal.

### Postcondiciones

- Se ha almacenado la imagen capturada en el filesystem local (`AppDataDirectory`).
- Se ha creado un registro `Punto_Local` y un registro `Foto_Local` en SQLite.
- Se ha registrado una operación `PendingInsert` en la tabla `SyncQueue`.
- El mapa refleja el nuevo punto con su marker correspondiente.

### Reglas de Negocio Relacionadas

- **RN-01:** Toda escritura va primero a SQLite local.
- **RN-03:** Fotos sin GPS crean punto en (0, 0) con advertencia MudAlert.
- **RN-06:** Imágenes en filesystem local Android (`AppDataDirectory`).

### Criterios de Aceptación BDD

**Escenario 1: Captura exitosa con cámara nativa**
```
Dado que el usuario se encuentra en la pantalla del mapa en un dispositivo móvil
Cuando presiona el botón MudFab de cámara y toma una fotografía
Entonces el sistema almacena la imagen en AppDataDirectory
  Y extrae las coordenadas GPS del EXIF
  Y crea un Punto_Local y Foto_Local en SQLite
  Y registra una operación PendingInsert en SyncQueue
  Y muestra un MudSnackbar de éxito
  Y el mapa presenta un nuevo marker
```

**Escenario 2: Cancelación de la captura**
```
Dado que el usuario ha lanzado la cámara nativa
Cuando cancela la captura sin tomar una foto
Entonces el sistema no crea ningún registro
  Y permanece en la pantalla del mapa sin cambios
```

**Escenario 3: Cámara no disponible**
```
Dado que el dispositivo no dispone de cámara o los permisos están denegados
Cuando el usuario presiona el botón MudFab de cámara
Entonces se muestra un MudAlert de error informando que no se pudo acceder a la cámara
```

---

## CU-03: Extraer coordenadas GPS del EXIF

### Actor Principal

Sistema.

### Precondiciones

- Se dispone de un archivo de imagen válido (JPEG, PNG o HEIC) almacenado en el filesystem local o proporcionado mediante upload.
- La librería MetadataExtractor se encuentra disponible en el entorno del servidor o del cliente según corresponda.

### Flujo Principal

1. El sistema recibe la ruta o el stream de la imagen a procesar.
2. El sistema invoca `MetadataExtractor` para leer los metadatos EXIF de la imagen.
3. El sistema localiza el directorio `GpsDirectory` dentro de los metadatos extraídos.
4. El sistema obtiene las etiquetas de latitud (`GpsLatitude`, `GpsLatitudeRef`) y longitud (`GpsLongitude`, `GpsLongitudeRef`).
5. El sistema convierte los valores de grados, minutos y segundos (DMS) a formato decimal.
6. El sistema aplica el signo correspondiente según la referencia hemisférica (N/S para latitud, E/W para longitud).
7. El sistema retorna un objeto con las coordenadas `Latitude` y `Longitude` en formato `double`.

### Flujos Alternativos

**FA-01 — La imagen no contiene directorio EXIF:**
1. `MetadataExtractor` no encuentra metadatos EXIF en la imagen.
2. El sistema retorna coordenadas por defecto (0.0, 0.0).
3. Se registra un log de advertencia indicando la ausencia de metadatos EXIF.

**FA-02 — El EXIF existe pero no contiene datos GPS:**
1. El directorio EXIF está presente, pero no contiene `GpsDirectory`.
2. El sistema retorna coordenadas por defecto (0.0, 0.0).
3. Se registra un log de advertencia indicando la ausencia de datos GPS en el EXIF.

**FA-03 — Los datos GPS están corruptos o incompletos:**
1. Se encuentran etiquetas GPS, pero los valores no pueden ser interpretados correctamente.
2. El sistema captura la excepción y retorna coordenadas por defecto (0.0, 0.0).
3. Se registra un log de error con el detalle de la excepción.

**FA-04 — Error al leer el archivo de imagen:**
1. Se produce una excepción de E/S al intentar leer el archivo.
2. El sistema propaga la excepción al caso de uso invocante.
3. Se registra un log de error con el detalle de la excepción.

### Postcondiciones

- Se han extraído las coordenadas GPS de la imagen y se han retornado al caso de uso invocante, o bien se han retornado coordenadas por defecto (0.0, 0.0) en caso de ausencia de datos GPS.

### Reglas de Negocio Relacionadas

- **RN-03:** Fotos sin GPS crean punto en (0, 0) con advertencia MudAlert.

### Criterios de Aceptación BDD

**Escenario 1: Extracción exitosa de coordenadas GPS**
```
Dado que se dispone de una imagen JPEG con metadatos EXIF que incluyen datos GPS
Cuando el sistema procesa la imagen con MetadataExtractor
Entonces se obtienen la latitud y la longitud en formato decimal
  Y los valores son válidos dentro del rango [-90, 90] para latitud y [-180, 180] para longitud
```

**Escenario 2: Imagen sin datos GPS en EXIF**
```
Dado que se dispone de una imagen JPEG con metadatos EXIF sin directorio GPS
Cuando el sistema procesa la imagen con MetadataExtractor
Entonces se retornan las coordenadas por defecto (0.0, 0.0)
  Y se registra un log de advertencia
```

**Escenario 3: Imagen sin metadatos EXIF**
```
Dado que se dispone de una imagen PNG sin metadatos EXIF
Cuando el sistema procesa la imagen con MetadataExtractor
Entonces se retornan las coordenadas por defecto (0.0, 0.0)
  Y se registra un log de advertencia
```

---

## CU-04: Guardar punto y foto en SQLite local

### Actor Principal

Sistema.

### Precondiciones

- Se dispone de una imagen almacenada en el filesystem local con su ruta válida.
- Se dispone de las coordenadas GPS (extraídas o por defecto).
- La base de datos SQLite local se encuentra inicializada y accesible.

### Flujo Principal

1. El sistema genera un identificador único (GUID) para el nuevo punto.
2. El sistema crea un registro `Punto_Local` con los campos: `Id`, `Nombre` (autogenerado con timestamp), `Descripcion` (vacío), `Latitud`, `Longitud`, `FechaCreacion` (UTC), `UpdatedAt` (UTC).
3. El sistema inserta el registro `Punto_Local` en la tabla correspondiente de SQLite.
4. El sistema genera un identificador único (GUID) para la nueva foto.
5. El sistema crea un registro `Foto_Local` con los campos: `Id`, `PuntoId` (referencia al punto creado), `RutaArchivo` (ruta en filesystem local), `FechaCaptura` (UTC), `UpdatedAt` (UTC).
6. El sistema inserta el registro `Foto_Local` en la tabla correspondiente de SQLite.
7. El sistema crea un registro en la tabla `SyncQueue` con los campos: `Id`, `EntityType` ("Punto"), `EntityId` (Id del punto), `OperationType` ("Insert"), `Status` ("Pending"), `CreatedAt` (UTC), `RetryCount` (0).
8. El sistema confirma la transacción local.

### Flujos Alternativos

**FA-01 — Error al insertar en SQLite:**
1. Se produce una excepción al ejecutar la inserción en SQLite (disco lleno, base corrupta, etc.).
2. El sistema revierte la transacción completa (rollback).
3. Se registra un log de error con el detalle de la excepción.
4. Se notifica al caso de uso invocante que la operación falló.

**FA-02 — La ruta del archivo de imagen no existe:**
1. El sistema verifica que la ruta del archivo de imagen es válida antes de insertar.
2. Si la ruta no existe, se aborta la operación.
3. Se registra un log de error indicando que el archivo de origen no fue encontrado.

### Postcondiciones

- Existe un registro `Punto_Local` en SQLite con las coordenadas y metadatos correspondientes.
- Existe un registro `Foto_Local` en SQLite asociado al punto creado.
- Existe un registro en `SyncQueue` con estado `Pending` para la posterior sincronización.

### Reglas de Negocio Relacionadas

- **RN-01:** Toda escritura va primero a SQLite local.
- **RN-06:** Imágenes en filesystem local Android (`AppDataDirectory`).
- **RN-08:** SQLite fuente operativa; SQL Server fuente global.

### Criterios de Aceptación BDD

**Escenario 1: Guardado exitoso de punto y foto**
```
Dado que se dispone de una imagen almacenada localmente y coordenadas GPS válidas
Cuando el sistema ejecuta el guardado en SQLite
Entonces se crea un registro Punto_Local con latitud y longitud
  Y se crea un registro Foto_Local asociado al punto
  Y se crea un registro en SyncQueue con OperationType "Insert" y Status "Pending"
  Y la transacción se confirma exitosamente
```

**Escenario 2: Error en la inserción**
```
Dado que la base de datos SQLite no tiene espacio disponible
Cuando el sistema intenta insertar el punto y la foto
Entonces se revierte la transacción completa
  Y se registra un log de error
  Y se notifica al invocante que la operación falló
```

---

## CU-05: Visualizar mapa con markers

### Actor Principal

Técnico / Supervisor.

### Precondiciones

- El usuario se encuentra autenticado en la aplicación.
- Existen puntos registrados en la base de datos SQLite local (o en SQL Server para la versión web).
- La librería Leaflet.js se encuentra disponible y cargada mediante `IJSRuntime`.

### Flujo Principal

1. El usuario accede a la pantalla principal que contiene el componente de mapa.
2. El sistema inicializa el mapa Leaflet.js invocando la función de inicialización mediante `IJSRuntime.InvokeVoidAsync()`.
3. El sistema establece la vista inicial del mapa centrándola en la última posición conocida del usuario o en un punto predeterminado.
4. El sistema consulta la base de datos local para obtener todos los registros `Punto_Local` con sus coordenadas.
5. Para cada punto, el sistema crea un marker en las coordenadas (`Latitud`, `Longitud`) correspondientes.
6. El sistema agrupa los markers en un clúster (`MarkerClusterGroup`) para las zonas con alta densidad de puntos.
7. Los markers se renderizan sobre el mapa con íconos diferenciados según el estado de sincronización (pendiente, sincronizado, con conflicto).
8. El mapa queda interactivo, permitiendo zoom, desplazamiento y selección de markers.

### Flujos Alternativos

**FA-01 — No existen puntos registrados:**
1. La consulta a la base de datos retorna cero resultados.
2. El mapa se muestra vacío, centrado en la posición predeterminada.
3. Se presenta un mensaje indicando "No se encontraron puntos registrados."

**FA-02 — Error al cargar Leaflet.js:**
1. La invocación de `IJSRuntime` falla al inicializar Leaflet.js.
2. Se muestra un `MudAlert` de tipo Error indicando "No se pudo cargar el componente de mapa."
3. Se registra un log de error con el detalle de la excepción.

**FA-03 — Punto con coordenadas (0, 0):**
1. Se detectan puntos cuyas coordenadas son (0.0, 0.0).
2. El marker correspondiente se muestra con un ícono especial de advertencia.
3. El popup del marker incluye una nota indicando "Ubicación no determinada."

### Postcondiciones

- El mapa se encuentra visible con todos los markers correspondientes a los puntos registrados.
- Los markers están agrupados en clústeres según la densidad geográfica.
- Cada marker es interactivo y responde a eventos de clic.

### Reglas de Negocio Relacionadas

- **RN-03:** Fotos sin GPS crean punto en (0, 0) con advertencia MudAlert.
- **RN-08:** SQLite fuente operativa; SQL Server fuente global.

### Criterios de Aceptación BDD

**Escenario 1: Visualización exitosa del mapa con markers**
```
Dado que existen 10 puntos registrados en la base de datos local
Cuando el usuario accede a la pantalla del mapa
Entonces el mapa de Leaflet.js se renderiza correctamente
  Y se muestran 10 markers en las coordenadas correspondientes
  Y los markers en zonas cercanas se agrupan en clústeres
```

**Escenario 2: Mapa sin puntos**
```
Dado que no existen puntos registrados en la base de datos local
Cuando el usuario accede a la pantalla del mapa
Entonces el mapa se muestra centrado en la posición predeterminada
  Y se presenta un mensaje indicando que no se encontraron puntos
```

**Escenario 3: Punto en coordenadas (0, 0)**
```
Dado que existe un punto registrado con coordenadas (0.0, 0.0)
Cuando el mapa renderiza los markers
Entonces el marker correspondiente muestra un ícono de advertencia
  Y el popup indica "Ubicación no determinada"
```

---

## CU-06: Ver detalle de punto con popup

### Actor Principal

Técnico / Supervisor.

### Precondiciones

- El mapa se encuentra cargado y visible con al menos un marker.
- El usuario puede interactuar con el mapa (zoom, desplazamiento, clic).

### Flujo Principal

1. El usuario hace clic sobre un marker en el mapa.
2. El sistema captura el evento de clic del marker mediante la interoperabilidad JavaScript-Blazor.
3. El sistema obtiene el identificador del punto asociado al marker seleccionado.
4. El sistema consulta SQLite local para obtener los datos completos del `Punto_Local` y las `Foto_Local` asociadas.
5. El sistema abre un componente `MarkerPopup` implementado como `MudDialog`.
6. El diálogo muestra la información del punto: nombre, descripción, coordenadas (latitud y longitud), fecha de creación.
7. El diálogo presenta un componente `FotoCarousel` con las fotos asociadas al punto, permitiendo la navegación entre imágenes.
8. El diálogo incluye botones de acción: "Editar" (invoca CU-07), "Eliminar" (invoca CU-08), "Cerrar".

### Flujos Alternativos

**FA-01 — El punto no tiene fotos asociadas:**
1. La consulta de fotos asociadas retorna cero resultados.
2. El `FotoCarousel` se oculta.
3. Se muestra un texto informativo: "Este punto no tiene fotos asociadas."

**FA-02 — Error al cargar las fotos del filesystem:**
1. Se detecta que uno o más archivos de imagen referenciados no existen en el filesystem local.
2. Se muestra un ícono de imagen rota en el carrusel para las fotos faltantes.
3. Se registra un log de advertencia indicando la inconsistencia.

### Postcondiciones

- El usuario visualiza la información completa del punto seleccionado.
- El `MudDialog` permanece abierto hasta que el usuario lo cierre o ejecute una acción.

### Reglas de Negocio Relacionadas

- **RN-08:** SQLite fuente operativa; SQL Server fuente global.

### Criterios de Aceptación BDD

**Escenario 1: Visualización exitosa del detalle de un punto**
```
Dado que el mapa muestra markers y el usuario hace clic sobre uno de ellos
Cuando el sistema abre el popup de detalle
Entonces se muestra un MudDialog con el nombre, descripción y coordenadas del punto
  Y se presenta un FotoCarousel con las fotos asociadas
  Y se muestran los botones "Editar", "Eliminar" y "Cerrar"
```

**Escenario 2: Punto sin fotos asociadas**
```
Dado que el usuario selecciona un marker cuyo punto no tiene fotos asociadas
Cuando el sistema abre el popup de detalle
Entonces el FotoCarousel se oculta
  Y se muestra el texto "Este punto no tiene fotos asociadas"
```

---

## CU-07: Editar nombre y descripción de punto

### Actor Principal

Técnico / Supervisor.

### Precondiciones

- El popup de detalle del punto (CU-06) se encuentra abierto.
- El usuario tiene permisos de edición sobre el punto (el técnico solo edita los propios; el supervisor edita cualquiera).

### Flujo Principal

1. El usuario presiona el botón "Editar" en el `MudDialog` de detalle del punto.
2. El sistema habilita los campos `MudTextField` de nombre y descripción para edición.
3. El usuario modifica el nombre y/o la descripción del punto.
4. El usuario presiona el botón "Guardar".
5. El sistema valida que el campo nombre no se encuentre vacío.
6. El sistema actualiza el registro `Punto_Local` en SQLite con los nuevos valores y establece `UpdatedAt` con la fecha/hora actual (UTC).
7. El sistema registra una operación `PendingUpdate` en la tabla `SyncQueue` con el `EntityId` del punto.
8. El sistema cierra el modo de edición y muestra los datos actualizados.
9. Se presenta un `MudSnackbar` de éxito indicando "Punto actualizado correctamente."

### Flujos Alternativos

**FA-01 — El usuario cancela la edición:**
1. El usuario presiona el botón "Cancelar" durante la edición.
2. El sistema revierte los campos a sus valores originales.
3. Se cierra el modo de edición sin realizar cambios.

**FA-02 — El campo nombre está vacío:**
1. El sistema detecta que el campo nombre se encuentra vacío al intentar guardar.
2. Se muestra un mensaje de validación en el `MudTextField` indicando "El nombre es obligatorio."
3. El sistema no permite guardar hasta que se complete el campo.

**FA-03 — Error al actualizar SQLite:**
1. Se produce una excepción al ejecutar la actualización en SQLite.
2. Se muestra un `MudAlert` de tipo Error indicando "No se pudo guardar la actualización."
3. Se registra un log de error.

### Postcondiciones

- El registro `Punto_Local` en SQLite refleja los nuevos valores de nombre y descripción.
- El campo `UpdatedAt` del punto se ha actualizado a la fecha/hora actual (UTC).
- Existe un registro en `SyncQueue` con `OperationType` "Update" y `Status` "Pending".

### Reglas de Negocio Relacionadas

- **RN-01:** Toda escritura va primero a SQLite local.
- **RN-05:** Last-Write-Wins por `UpdatedAt` (UTC).

### Criterios de Aceptación BDD

**Escenario 1: Edición exitosa del nombre y descripción**
```
Dado que el usuario se encuentra en el popup de detalle de un punto
Cuando presiona "Editar", modifica el nombre y la descripción, y presiona "Guardar"
Entonces el registro Punto_Local se actualiza en SQLite con los nuevos valores
  Y el campo UpdatedAt se establece a la fecha/hora actual UTC
  Y se registra una operación PendingUpdate en SyncQueue
  Y se muestra un MudSnackbar de éxito
```

**Escenario 2: Cancelación de la edición**
```
Dado que el usuario se encuentra editando un punto
Cuando presiona el botón "Cancelar"
Entonces los campos regresan a sus valores originales
  Y no se modifica ningún registro en SQLite
```

**Escenario 3: Nombre vacío**
```
Dado que el usuario ha borrado el contenido del campo nombre
Cuando presiona el botón "Guardar"
Entonces se muestra un mensaje de validación "El nombre es obligatorio"
  Y no se permite guardar hasta que se complete el campo
```

---

## CU-08: Eliminar punto con fotos

### Actor Principal

Supervisor.

### Precondiciones

- El popup de detalle del punto (CU-06) se encuentra abierto.
- El usuario autenticado tiene rol de Supervisor.
- El punto existe en la base de datos SQLite local.

### Flujo Principal

1. El usuario presiona el botón "Eliminar" en el `MudDialog` de detalle del punto.
2. El sistema abre un `MudDialog` de confirmación con el mensaje: "¿Está seguro de que desea eliminar este punto y todas sus fotos asociadas? Esta acción se sincronizará con el servidor."
3. El usuario presiona el botón "Confirmar".
4. El sistema marca el registro `Punto_Local` como eliminado (soft delete) estableciendo el campo `IsDeleted` a `true`.
5. El sistema marca todos los registros `Foto_Local` asociados como eliminados.
6. El sistema registra una operación `PendingDelete` en la tabla `SyncQueue` con el `EntityId` del punto.
7. El sistema cierra el `MudDialog` de detalle.
8. El sistema remueve el marker correspondiente del mapa.
9. Se presenta un `MudSnackbar` de éxito indicando "Punto eliminado correctamente."

### Flujos Alternativos

**FA-01 — El usuario cancela la eliminación:**
1. El usuario presiona "Cancelar" en el diálogo de confirmación.
2. El sistema cierra el diálogo de confirmación.
3. No se realiza ninguna modificación en la base de datos.

**FA-02 — Error al marcar como eliminado:**
1. Se produce una excepción al actualizar los registros en SQLite.
2. Se muestra un `MudAlert` de tipo Error indicando "No se pudo eliminar el punto."
3. Se registra un log de error.

### Postcondiciones

- El registro `Punto_Local` y sus `Foto_Local` asociadas están marcados como eliminados en SQLite (soft delete).
- Existe un registro en `SyncQueue` con `OperationType` "Delete" y `Status` "Pending".
- El marker del punto ha sido removido del mapa.
- Los archivos de imagen permanecen en el filesystem local hasta que la sincronización confirme la eliminación en el servidor.

### Reglas de Negocio Relacionadas

- **RN-01:** Toda escritura va primero a SQLite local.
- **RN-07:** Eliminación local → `PendingDelete` hasta sync.

### Criterios de Aceptación BDD

**Escenario 1: Eliminación exitosa con confirmación**
```
Dado que el supervisor se encuentra en el popup de detalle de un punto
Cuando presiona "Eliminar" y confirma la acción en el diálogo de confirmación
Entonces el punto y sus fotos se marcan como eliminados en SQLite (soft delete)
  Y se registra una operación PendingDelete en SyncQueue
  Y el marker se remueve del mapa
  Y se muestra un MudSnackbar de éxito
```

**Escenario 2: Cancelación de la eliminación**
```
Dado que el supervisor ha presionado "Eliminar" y se muestra el diálogo de confirmación
Cuando presiona "Cancelar"
Entonces no se modifica ningún registro
  Y el marker permanece visible en el mapa
```

---

## CU-09: Ver estado de sincronización

### Actor Principal

Técnico de campo.

### Precondiciones

- El usuario se encuentra autenticado en la aplicación.
- La tabla `SyncQueue` existe en la base de datos SQLite local.

### Flujo Principal

1. El usuario navega a la página "Estado de Sincronización" (`EstadoSync`).
2. El sistema consulta la tabla `SyncQueue` de SQLite local para calcular métricas agregadas.
3. El sistema presenta cuatro `MudCard` con las métricas de sincronización:
   - **Pendientes:** cantidad de operaciones con `Status` = "Pending".
   - **Sincronizados:** cantidad de operaciones con `Status` = "Done".
   - **Conflictos:** cantidad de operaciones con `Status` = "Conflict".
   - **Fallidos:** cantidad de operaciones con `Status` = "Failed".
4. Debajo de las tarjetas, el sistema muestra un `MudTable` con el historial reciente de operaciones de sincronización (ver CU-15).
5. El sistema presenta un botón "Sincronizar ahora" (`MudButton`) para ejecutar la sincronización manual (ver CU-10).
6. La página se actualiza automáticamente cada 30 segundos mientras permanezca visible.

### Flujos Alternativos

**FA-01 — No existen operaciones en SyncQueue:**
1. La consulta retorna cero registros.
2. Las cuatro tarjetas muestran el valor "0".
3. El `MudTable` muestra un mensaje: "No se han registrado operaciones de sincronización."

**FA-02 — Error al consultar SQLite:**
1. Se produce una excepción al leer la tabla `SyncQueue`.
2. Se muestra un `MudAlert` de tipo Error indicando "No se pudo obtener el estado de sincronización."
3. Se registra un log de error.

### Postcondiciones

- El usuario visualiza el estado actual de la sincronización con métricas en tiempo real.

### Reglas de Negocio Relacionadas

- **RN-02:** SyncService corre en background, nunca bloquea UI.
- **RN-08:** SQLite fuente operativa; SQL Server fuente global.

### Criterios de Aceptación BDD

**Escenario 1: Visualización del estado de sincronización**
```
Dado que existen 5 operaciones Pending, 20 Done, 1 Conflict y 2 Failed en SyncQueue
Cuando el usuario navega a la página EstadoSync
Entonces se muestran cuatro MudCard con los valores 5, 20, 1 y 2 respectivamente
  Y se muestra un MudTable con el historial de operaciones
  Y se presenta el botón "Sincronizar ahora"
```

**Escenario 2: Sin operaciones registradas**
```
Dado que la tabla SyncQueue está vacía
Cuando el usuario navega a la página EstadoSync
Entonces las cuatro tarjetas muestran el valor 0
  Y el MudTable indica que no se han registrado operaciones
```

---

## CU-10: Ejecutar sincronización manual

### Actor Principal

Técnico de campo.

### Precondiciones

- El usuario se encuentra en la página "Estado de Sincronización" (`EstadoSync`).
- Existen operaciones con `Status` = "Pending" en la tabla `SyncQueue`.
- El dispositivo tiene conectividad de red activa.

### Flujo Principal

1. El usuario presiona el botón `MudButton` "Sincronizar ahora".
2. El sistema verifica la conectividad de red mediante `ConnectivityService`.
3. El sistema deshabilita el botón y muestra un `MudProgressLinear` con modo indeterminado.
4. El sistema invoca `SyncService.SyncNowAsync()` en un hilo secundario.
5. El `SyncService` procesa las operaciones pendientes en orden FIFO (ver CU-12 para push, CU-13 para pull).
6. El `MudProgressLinear` se actualiza con el progreso de las operaciones procesadas.
7. Al finalizar, el sistema actualiza las métricas y el historial en la página `EstadoSync`.
8. El botón se habilita nuevamente.
9. Se presenta un `MudSnackbar` indicando "Sincronización completada. X operaciones procesadas."

### Flujos Alternativos

**FA-01 — No hay conectividad de red:**
1. `ConnectivityService` determina que no hay conexión disponible.
2. Se muestra un `MudAlert` de tipo Warning indicando "No hay conexión a internet. Intente nuevamente cuando disponga de conectividad."
3. El botón permanece habilitado.

**FA-02 — No hay operaciones pendientes:**
1. La consulta a `SyncQueue` no retorna operaciones con `Status` "Pending".
2. Se muestra un `MudSnackbar` informativo indicando "No hay operaciones pendientes de sincronización."

**FA-03 — Error parcial en la sincronización:**
1. Algunas operaciones se procesan exitosamente y otras fallan.
2. Se muestra un `MudSnackbar` de advertencia indicando "Sincronización parcial: X exitosas, Y fallidas."
3. Las métricas y el historial se actualizan reflejando el resultado parcial.

**FA-04 — Sincronización ya en progreso:**
1. El `SyncService` ya se encuentra ejecutando una sincronización (manual o automática).
2. Se muestra un `MudSnackbar` informativo indicando "Ya se encuentra en curso una sincronización."
3. El botón permanece deshabilitado.

### Postcondiciones

- Las operaciones pendientes han sido procesadas (push y pull).
- Las métricas de sincronización en `EstadoSync` reflejan el estado actualizado.
- El historial de operaciones incluye las nuevas entradas.

### Reglas de Negocio Relacionadas

- **RN-02:** SyncService corre en background, nunca bloquea UI.
- **RN-04:** Backoff exponencial (5s → 30s → 5min, máx. 3 intentos).

### Criterios de Aceptación BDD

**Escenario 1: Sincronización manual exitosa**
```
Dado que existen 5 operaciones pendientes en SyncQueue y hay conectividad de red
Cuando el usuario presiona "Sincronizar ahora"
Entonces se muestra un MudProgressLinear durante el proceso
  Y el SyncService procesa las 5 operaciones
  Y al finalizar se muestra un MudSnackbar indicando "5 operaciones procesadas"
  Y las métricas se actualizan en la página EstadoSync
```

**Escenario 2: Sin conectividad de red**
```
Dado que no hay conexión a internet disponible
Cuando el usuario presiona "Sincronizar ahora"
Entonces se muestra un MudAlert de advertencia indicando la falta de conectividad
  Y no se inicia ningún proceso de sincronización
```

**Escenario 3: Sincronización parcial**
```
Dado que existen 10 operaciones pendientes y el servidor responde con error para 3 de ellas
Cuando el usuario ejecuta la sincronización manual
Entonces 7 operaciones se procesan exitosamente
  Y 3 operaciones quedan en estado Failed o Pending con RetryCount incrementado
  Y se muestra un MudSnackbar de advertencia con el resultado parcial
```

---

## CU-11: Sincronización automática por reconexión

### Actor Principal

Sistema.

### Precondiciones

- El dispositivo se encontraba sin conectividad de red.
- Existen operaciones con `Status` = "Pending" en la tabla `SyncQueue`.
- El servicio `ConnectivityService` se encuentra registrado y escuchando eventos de conectividad.

### Flujo Principal

1. El evento `Connectivity.ConnectivityChanged` se dispara indicando un cambio en el estado de la red.
2. El `ConnectivityService` evalúa el nuevo estado de conectividad.
3. Si el estado indica que hay conexión de red disponible (WiFi o datos móviles), el servicio verifica que existan operaciones pendientes en `SyncQueue`.
4. El `ConnectivityService` invoca `SyncService.SyncNowAsync()` automáticamente.
5. El `SyncService` procesa las operaciones pendientes en orden FIFO (ver CU-12 para push, CU-13 para pull).
6. Si la página `EstadoSync` se encuentra visible, las métricas y el historial se actualizan en tiempo real.
7. Se registra un log informativo indicando "Sincronización automática iniciada por reconexión."

### Flujos Alternativos

**FA-01 — El cambio de conectividad indica desconexión:**
1. El nuevo estado indica que la red no está disponible.
2. El `ConnectivityService` no inicia ninguna sincronización.
3. Se registra un log informativo indicando "Conectividad perdida."

**FA-02 — No existen operaciones pendientes:**
1. La consulta a `SyncQueue` no retorna operaciones con `Status` "Pending".
2. El servicio no inicia la sincronización.
3. Se registra un log de depuración indicando "Reconexión detectada, sin operaciones pendientes."

**FA-03 — Sincronización ya en progreso:**
1. El `SyncService` ya se encuentra ejecutando una sincronización.
2. El `ConnectivityService` ignora la solicitud.
3. Se registra un log informativo indicando "Sincronización ya en curso, reconexión ignorada."

### Postcondiciones

- Si existían operaciones pendientes y había conectividad, las operaciones han sido procesadas automáticamente.
- El estado de `SyncQueue` refleja los resultados de la sincronización.

### Reglas de Negocio Relacionadas

- **RN-02:** SyncService corre en background, nunca bloquea UI.
- **RN-04:** Backoff exponencial (5s → 30s → 5min, máx. 3 intentos).

### Criterios de Aceptación BDD

**Escenario 1: Sincronización automática al reconectarse**
```
Dado que el dispositivo estaba sin conexión y existen operaciones pendientes en SyncQueue
Cuando se restablece la conectividad de red
Entonces el ConnectivityService detecta el cambio mediante Connectivity.ConnectivityChanged
  Y se invoca SyncService.SyncNowAsync() automáticamente
  Y las operaciones pendientes se procesan sin intervención del usuario
```

**Escenario 2: Desconexión detectada**
```
Dado que el dispositivo tenía conexión activa
Cuando se pierde la conectividad de red
Entonces el ConnectivityService registra la desconexión
  Y no se inicia ningún proceso de sincronización
```

**Escenario 3: Reconexión sin operaciones pendientes**
```
Dado que se restablece la conectividad y no existen operaciones pendientes
Cuando el ConnectivityService evalúa el estado
Entonces no se inicia ningún proceso de sincronización
  Y se registra un log de depuración
```

---

## CU-12: Enviar operación pendiente al servidor (push)

### Actor Principal

Sistema.

### Precondiciones

- El `SyncService` ha sido invocado (manual o automáticamente).
- Existen registros en `SyncQueue` con `Status` = "Pending" y `RetryCount` < 3.
- El dispositivo tiene conectividad de red activa.

### Flujo Principal

1. El `SyncService` lee la siguiente operación pendiente de `SyncQueue` en orden FIFO (por `CreatedAt` ascendente).
2. El sistema identifica el tipo de operación (`Insert`, `Update`, `Delete`) y la entidad asociada.
3. Según el tipo de operación:
   - **Insert:** el sistema envía una solicitud `POST` al endpoint REST correspondiente con los datos de la entidad.
   - **Update:** el sistema envía una solicitud `PUT` al endpoint REST correspondiente con los datos actualizados.
   - **Delete:** el sistema envía una solicitud `DELETE` al endpoint REST correspondiente con el identificador de la entidad.
4. El servidor procesa la solicitud y responde con un código HTTP.
5. Si la respuesta es `2xx` (éxito):
   - El sistema actualiza el registro en `SyncQueue` estableciendo `Status` = "Done" y `CompletedAt` = fecha/hora actual (UTC).
6. El sistema continúa con la siguiente operación pendiente hasta agotar la cola.

### Flujos Alternativos

**FA-01 — El servidor responde con código 4xx (error del cliente):**
1. Se recibe una respuesta HTTP 4xx.
2. El sistema marca la operación con `Status` = "Failed" y registra el mensaje de error.
3. Se registra un log de error con el detalle de la respuesta.
4. El sistema continúa con la siguiente operación pendiente.

**FA-02 — El servidor responde con código 5xx (error del servidor):**
1. Se recibe una respuesta HTTP 5xx.
2. El sistema incrementa `RetryCount` en el registro de `SyncQueue`.
3. Si `RetryCount` < 3, la operación permanece con `Status` = "Pending" y se aplica backoff exponencial para el próximo intento (5s → 30s → 5min).
4. Si `RetryCount` >= 3, se marca la operación con `Status` = "Failed".
5. Se registra un log de advertencia o error según corresponda.

**FA-03 — Timeout en la solicitud HTTP:**
1. La solicitud HTTP excede el tiempo de espera configurado.
2. El sistema trata el timeout como un error 5xx y aplica el mismo flujo de reintentos (FA-02).

**FA-04 — Conflicto detectado (código 409):**
1. El servidor responde con HTTP 409 Conflict.
2. El sistema marca la operación con `Status` = "Conflict".
3. Se invoca el caso de uso CU-14 para resolver el conflicto automáticamente.

### Postcondiciones

- Las operaciones exitosas se encuentran marcadas como "Done" en `SyncQueue`.
- Las operaciones fallidas se encuentran marcadas como "Failed" o pendientes de reintento según corresponda.
- Los conflictos se encuentran marcados como "Conflict" y han sido procesados por CU-14.

### Reglas de Negocio Relacionadas

- **RN-02:** SyncService corre en background, nunca bloquea UI.
- **RN-04:** Backoff exponencial (5s → 30s → 5min, máx. 3 intentos).
- **RN-08:** SQLite fuente operativa; SQL Server fuente global.

### Criterios de Aceptación BDD

**Escenario 1: Push exitoso de operación pendiente**
```
Dado que existe una operación PendingInsert en SyncQueue
Cuando el SyncService envía la solicitud POST al servidor y recibe respuesta 200
Entonces la operación se marca como Done en SyncQueue
  Y se registra la fecha de completado en CompletedAt
```

**Escenario 2: Error del servidor con reintentos**
```
Dado que existe una operación pendiente con RetryCount = 0
Cuando el servidor responde con código 500
Entonces RetryCount se incrementa a 1
  Y la operación permanece como Pending
  Y el próximo intento se programa con un delay de 5 segundos
```

**Escenario 3: Máximo de reintentos alcanzado**
```
Dado que existe una operación pendiente con RetryCount = 2
Cuando el servidor responde con código 500
Entonces RetryCount se incrementa a 3
  Y la operación se marca como Failed
  Y se registra un log de error
```

**Escenario 4: Conflicto detectado**
```
Dado que existe una operación PendingUpdate en SyncQueue
Cuando el servidor responde con código 409 Conflict
Entonces la operación se marca como Conflict
  Y se invoca el mecanismo de resolución de conflictos (CU-14)
```

---

## CU-13: Recibir cambios del servidor (pull delta)

### Actor Principal

Sistema.

### Precondiciones

- El `SyncService` ha sido invocado (manual o automáticamente).
- El dispositivo tiene conectividad de red activa.
- Se dispone del timestamp de la última sincronización exitosa (`lastSync`).

### Flujo Principal

1. El `SyncService` determina el timestamp de la última sincronización exitosa almacenado localmente.
2. El sistema envía una solicitud `GET` al endpoint `/api/sync/delta?since={lastSync}`.
3. El servidor responde con un conjunto de cambios (delta) que incluye entidades creadas, actualizadas y eliminadas desde `lastSync`.
4. Para cada entidad recibida en el delta:
   - **Creación:** se inserta un nuevo registro en SQLite local si no existe.
   - **Actualización:** se compara `UpdatedAt` entre el registro local y el del servidor. Si el registro del servidor es más reciente, se actualiza el registro local.
   - **Eliminación:** se marca el registro local como eliminado (soft delete).
5. El sistema actualiza el timestamp `lastSync` con la fecha/hora del servidor incluida en la respuesta.
6. Se registra un log informativo indicando "Pull delta completado: X creaciones, Y actualizaciones, Z eliminaciones."

### Flujos Alternativos

**FA-01 — Primera sincronización (lastSync es null):**
1. No existe un timestamp de última sincronización.
2. El sistema envía `GET /api/sync/delta?since=0001-01-01T00:00:00Z` para solicitar todos los registros del servidor.
3. El sistema inserta todos los registros recibidos en SQLite local.

**FA-02 — El servidor retorna un delta vacío:**
1. La respuesta del servidor indica que no hubo cambios desde `lastSync`.
2. El sistema actualiza el timestamp `lastSync` sin modificar datos locales.
3. Se registra un log de depuración indicando "Sin cambios en el servidor."

**FA-03 — Error en la solicitud HTTP:**
1. La solicitud `GET` falla por timeout o error de red.
2. El timestamp `lastSync` no se modifica.
3. Se registra un log de error.
4. El pull se reintentará en la próxima sincronización.

**FA-04 — Conflicto detectado durante actualización:**
1. Al comparar `UpdatedAt`, se detecta que tanto el registro local como el del servidor fueron modificados desde la última sincronización.
2. Se invoca el caso de uso CU-14 para resolver el conflicto.

### Postcondiciones

- La base de datos SQLite local contiene los cambios más recientes del servidor.
- El timestamp `lastSync` se ha actualizado.
- Los conflictos detectados han sido resueltos mediante CU-14.

### Reglas de Negocio Relacionadas

- **RN-02:** SyncService corre en background, nunca bloquea UI.
- **RN-05:** Last-Write-Wins por `UpdatedAt` (UTC).
- **RN-08:** SQLite fuente operativa; SQL Server fuente global.

### Criterios de Aceptación BDD

**Escenario 1: Pull delta exitoso con cambios**
```
Dado que el servidor tiene 3 creaciones y 2 actualizaciones desde el último lastSync
Cuando el SyncService ejecuta GET /api/sync/delta?since={lastSync}
Entonces se insertan 3 nuevos registros en SQLite local
  Y se actualizan 2 registros existentes
  Y se actualiza el timestamp lastSync
```

**Escenario 2: Pull delta sin cambios**
```
Dado que el servidor no tiene cambios desde el último lastSync
Cuando el SyncService ejecuta GET /api/sync/delta?since={lastSync}
Entonces no se modifica ningún registro local
  Y se actualiza el timestamp lastSync
```

**Escenario 3: Primera sincronización**
```
Dado que no existe un timestamp lastSync previo
Cuando el SyncService ejecuta el pull delta
Entonces se solicitan todos los registros al servidor
  Y se insertan en SQLite local
  Y se establece el timestamp lastSync inicial
```

---

## CU-14: Resolver conflicto automáticamente

### Actor Principal

Sistema.

### Precondiciones

- Se ha detectado un conflicto entre un registro local y uno del servidor (durante push o pull).
- Ambos registros (local y servidor) disponen de un campo `UpdatedAt` válido en formato UTC.

### Flujo Principal

1. El sistema obtiene el registro local con su `UpdatedAt` desde SQLite.
2. El sistema obtiene el registro del servidor con su `UpdatedAt` desde la respuesta del endpoint.
3. El sistema compara los valores de `UpdatedAt` de ambos registros.
4. Se aplica la estrategia Last-Write-Wins:
   - Si `UpdatedAt` del servidor es más reciente o igual, se sobrescriben los datos locales con los del servidor.
   - Si `UpdatedAt` local es más reciente, se mantienen los datos locales y se reenvían al servidor.
5. El sistema registra el conflicto en una tabla de auditoría `ConflictLog` con los campos: `Id`, `EntityType`, `EntityId`, `LocalUpdatedAt`, `ServerUpdatedAt`, `Winner` ("Local" o "Server"), `ResolvedAt` (UTC).
6. El sistema actualiza el registro en `SyncQueue` estableciendo `Status` = "Done" y `ConflictResolved` = `true`.
7. Se registra un log informativo indicando el conflicto resuelto y el ganador.

### Flujos Alternativos

**FA-01 — Los timestamps son idénticos:**
1. Los valores de `UpdatedAt` local y del servidor son exactamente iguales.
2. El sistema prioriza la versión del servidor como ganadora.
3. Se registra el conflicto con `Winner` = "Server".

**FA-02 — Error al acceder al registro local:**
1. No se encuentra el registro local en SQLite (pudo haber sido eliminado previamente).
2. Se acepta la versión del servidor sin generar conflicto.
3. Se registra un log de advertencia indicando la inconsistencia.

### Postcondiciones

- El conflicto ha sido resuelto aplicando la estrategia Last-Write-Wins.
- Existe un registro en `ConflictLog` documentando la resolución.
- El registro en `SyncQueue` se encuentra actualizado con el resultado de la resolución.
- Los datos locales reflejan la versión ganadora.

### Reglas de Negocio Relacionadas

- **RN-05:** Last-Write-Wins por `UpdatedAt` (UTC).

### Criterios de Aceptación BDD

**Escenario 1: El servidor gana por timestamp más reciente**
```
Dado que un registro local tiene UpdatedAt = "2026-04-13T10:00:00Z"
  Y el registro del servidor tiene UpdatedAt = "2026-04-13T10:05:00Z"
Cuando el sistema resuelve el conflicto
Entonces se sobrescriben los datos locales con los del servidor
  Y se registra en ConflictLog con Winner = "Server"
  Y se actualiza el SyncQueue con Status = "Done"
```

**Escenario 2: El cliente gana por timestamp más reciente**
```
Dado que un registro local tiene UpdatedAt = "2026-04-13T10:10:00Z"
  Y el registro del servidor tiene UpdatedAt = "2026-04-13T10:05:00Z"
Cuando el sistema resuelve el conflicto
Entonces se mantienen los datos locales
  Y se reenvían al servidor
  Y se registra en ConflictLog con Winner = "Local"
```

**Escenario 3: Timestamps idénticos**
```
Dado que un registro local y el del servidor tienen el mismo UpdatedAt
Cuando el sistema resuelve el conflicto
Entonces se priorizan los datos del servidor
  Y se registra en ConflictLog con Winner = "Server"
```

---

## CU-15: Ver historial de operaciones de sync

### Actor Principal

Técnico de campo.

### Precondiciones

- El usuario se encuentra autenticado en la aplicación.
- La página "Estado de Sincronización" (`EstadoSync`) se encuentra visible.
- La tabla `SyncQueue` contiene registros de operaciones.

### Flujo Principal

1. El usuario accede a la sección de historial dentro de la página `EstadoSync`.
2. El sistema consulta la tabla `SyncQueue` de SQLite local ordenada por `CreatedAt` descendente.
3. El sistema presenta un `MudTable` con las siguientes columnas:
   - **Fecha:** fecha y hora de creación de la operación (`CreatedAt`), formateada en zona horaria local.
   - **Tipo:** tipo de operación (`Insert`, `Update`, `Delete`).
   - **Entidad:** tipo de entidad afectada (`Punto`, `Foto`).
   - **Estado:** estado actual de la operación (`Pending`, `Done`, `Failed`, `Conflict`), mostrado con chip de color diferenciado.
   - **Error:** mensaje de error (si existe), mostrado en texto truncado con tooltip al pasar el cursor.
4. El `MudTable` soporta paginación con 20 registros por página.
5. El `MudTable` soporta filtrado por estado mediante un `MudSelect` desplegable.
6. El `MudTable` soporta ordenamiento por columna al hacer clic en los encabezados.

### Flujos Alternativos

**FA-01 — No existen operaciones registradas:**
1. La consulta retorna cero registros.
2. El `MudTable` muestra un mensaje: "No se han registrado operaciones de sincronización."

**FA-02 — Filtro aplicado sin resultados:**
1. El usuario aplica un filtro por estado que no tiene registros asociados.
2. El `MudTable` muestra el mensaje: "No se encontraron operaciones con el estado seleccionado."

### Postcondiciones

- El usuario visualiza el historial completo de operaciones de sincronización con la posibilidad de filtrar, ordenar y paginar.

### Reglas de Negocio Relacionadas

- **RN-02:** SyncService corre en background, nunca bloquea UI.

### Criterios de Aceptación BDD

**Escenario 1: Visualización del historial con registros**
```
Dado que existen 50 operaciones registradas en SyncQueue
Cuando el usuario accede a la sección de historial en EstadoSync
Entonces se muestra un MudTable con los primeros 20 registros ordenados por fecha descendente
  Y cada fila muestra Fecha, Tipo, Entidad, Estado y Error
  Y el Estado se presenta con un chip de color diferenciado
  Y se dispone de controles de paginación
```

**Escenario 2: Filtrado por estado**
```
Dado que existen operaciones en diversos estados
Cuando el usuario selecciona el filtro "Failed" en el MudSelect
Entonces el MudTable muestra únicamente las operaciones con estado Failed
```

**Escenario 3: Historial vacío**
```
Dado que no existen operaciones en SyncQueue
Cuando el usuario accede a la sección de historial
Entonces el MudTable muestra el mensaje "No se han registrado operaciones de sincronización"
```

---

## CU-16: Subir foto desde web (online directo)

### Actor Principal

Supervisor.

### Precondiciones

- El usuario se encuentra autenticado en la aplicación web (Blazor Server o WebAssembly).
- El dispositivo tiene conectividad de red activa.
- El servidor REST se encuentra disponible.

### Flujo Principal

1. El usuario navega a la página "Subir Fotos" (`SubirFotos`).
2. El sistema presenta un componente `MudFileUpload` configurado para aceptar archivos de imagen (`image/*`).
3. El usuario selecciona una o más imágenes desde su dispositivo.
4. El sistema muestra una vista previa (thumbnail) de las imágenes seleccionadas.
5. El usuario presiona el botón "Subir" (`MudButton`).
6. El sistema construye una solicitud `POST` multipart al endpoint `/api/fotos/upload` incluyendo los archivos de imagen.
7. El servidor recibe las imágenes y para cada una:
   - Invoca `MetadataExtractor` para extraer las coordenadas GPS del EXIF.
   - Crea un registro `Punto` en SQL Server con las coordenadas extraídas (o (0, 0) si no hay GPS).
   - Crea un registro `Foto` asociado al punto y almacena el archivo de imagen.
8. El servidor responde con `200 OK` y los datos de los puntos creados.
9. El sistema muestra un `MudSnackbar` de éxito indicando "X foto(s) subida(s) correctamente."
10. Si la página del mapa se encuentra abierta en otra pestaña o sección, se actualizan los markers.

### Flujos Alternativos

**FA-01 — El usuario cancela la selección de archivos:**
1. El usuario cierra el selector de archivos sin seleccionar ninguna imagen.
2. El sistema no realiza ninguna acción.

**FA-02 — El archivo excede el tamaño máximo permitido:**
1. El sistema detecta que una o más imágenes exceden el tamaño máximo configurado (ej. 10 MB).
2. Se muestra un `MudAlert` de tipo Warning indicando "La imagen [nombre] excede el tamaño máximo permitido (10 MB)."
3. La imagen se excluye del upload, las restantes se procesan normalmente.

**FA-03 — Error en la solicitud HTTP:**
1. La solicitud `POST` falla por timeout o error de red.
2. Se muestra un `MudAlert` de tipo Error indicando "Error al subir las fotos. Verifique la conectividad e intente nuevamente."
3. Se registra un log de error.

**FA-04 — Una o más imágenes no contienen datos GPS:**
1. El servidor detecta que una imagen no contiene datos GPS en el EXIF.
2. Se aplica la regla de negocio RN-03: se crea el punto en coordenadas (0, 0).
3. La respuesta del servidor incluye la advertencia correspondiente.
4. El sistema muestra un `MudAlert` de advertencia indicando cuáles imágenes no tenían ubicación.

**FA-05 — El servidor responde con error 5xx:**
1. El servidor retorna un error interno.
2. Se muestra un `MudAlert` de tipo Error indicando "Error del servidor al procesar las fotos."
3. Se sugiere al usuario reintentar la operación.

### Postcondiciones

- Las imágenes han sido almacenadas en el servidor.
- Se han creado registros `Punto` y `Foto` en SQL Server con las coordenadas GPS extraídas.
- Los puntos creados estarán disponibles para otros usuarios en la próxima sincronización delta.

### Reglas de Negocio Relacionadas

- **RN-03:** Fotos sin GPS crean punto en (0, 0) con advertencia MudAlert.
- **RN-08:** SQLite fuente operativa; SQL Server fuente global.

### Criterios de Aceptación BDD

**Escenario 1: Upload directo exitoso desde web**
```
Dado que el supervisor se encuentra en la página SubirFotos con conectividad activa
Cuando selecciona 3 imágenes con datos GPS y presiona "Subir"
Entonces el sistema envía las imágenes al endpoint POST /api/fotos/upload
  Y el servidor crea 3 puntos con sus fotos asociadas en SQL Server
  Y se muestra un MudSnackbar indicando "3 foto(s) subida(s) correctamente"
```

**Escenario 2: Imagen sin datos GPS**
```
Dado que el supervisor sube una imagen que no contiene datos GPS en el EXIF
Cuando el servidor procesa la imagen
Entonces se crea un punto en coordenadas (0, 0)
  Y se muestra un MudAlert de advertencia indicando la ausencia de ubicación
```

**Escenario 3: Archivo excede el tamaño máximo**
```
Dado que el supervisor selecciona una imagen de 15 MB
Cuando el sistema valida los archivos seleccionados
Entonces se muestra un MudAlert indicando que la imagen excede el tamaño máximo
  Y la imagen no se incluye en el upload
```

**Escenario 4: Error de red durante el upload**
```
Dado que el supervisor presiona "Subir" y se pierde la conectividad durante la transferencia
Cuando la solicitud HTTP falla por timeout
Entonces se muestra un MudAlert de error sugiriendo verificar la conectividad
  Y no se crean registros en el servidor
```

---

# 3. Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |

---

**Fin del documento**
