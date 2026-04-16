# Especificación API REST

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** api-rest-spec_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## Índice

1. [Introducción](#1-introducción)
2. [Convenciones Generales](#2-convenciones-generales)
3. [Base URL](#3-base-url)
4. [Endpoints de Puntos](#4-endpoints-de-puntos)
   - 4.1 [GET /api/puntos](#41-get-apipuntos)
   - 4.2 [GET /api/puntos/{id}](#42-get-apipuntosid)
   - 4.3 [POST /api/puntos](#43-post-apipuntos)
   - 4.4 [PUT /api/puntos/{id}](#44-put-apipuntosid)
   - 4.5 [DELETE /api/puntos/{id}](#45-delete-apipuntosid)
5. [Endpoints de Fotos](#5-endpoints-de-fotos)
   - 5.1 [POST /api/fotos/upload](#51-post-apifotosupload)
   - 5.2 [GET /api/fotos/{puntoId}](#52-get-apifotospuntoid)
   - 5.3 [GET /api/fotos/imagen/{id}](#53-get-apifotosimagenid)
   - 5.4 [DELETE /api/fotos/{id}](#54-delete-apifotosid)
   - 5.5 [PUT /api/fotos/{id}](#55-put-apifotosid)
   - 5.6 [GET /api/puntos/{id}/fotos/download](#56-get-apipuntosidfotos-download)
6. [Endpoints de Sincronización](#6-endpoints-de-sincronización)
   - 6.1 [GET /api/sync/delta](#61-get-apisyncdelta)
   - 6.2 [POST /api/sync/batch](#62-post-apisyncbatch)
7. [Modelo de Error](#7-modelo-de-error)
8. [Autenticación](#8-autenticación)
9. [Configuración CORS](#9-configuración-cors)
10. [Rate Limiting](#10-rate-limiting)
11. [Control de Cambios](#11-control-de-cambios)

---

## 1. Introducción

El presente documento describe la especificación completa de la API REST del sistema **GeoFoto**. Se detallan todos los endpoints disponibles, sus parámetros, cuerpos de solicitud y respuesta, códigos HTTP y notas de comportamiento. La API sigue los principios REST y se implementa sobre ASP.NET Core, utilizando JSON como formato de intercambio de datos.

La API se organiza en tres grupos funcionales:

| Grupo              | Prefijo          | Descripción                                      |
|---------------------|------------------|--------------------------------------------------|
| **Puntos**          | `/api/puntos`    | Gestión CRUD de puntos georeferenciados.          |
| **Fotos**           | `/api/fotos`     | Carga, consulta y eliminación de fotografías.     |
| **Sincronización**  | `/api/sync`      | Operaciones de sincronización offline-first.      |

---

## 2. Convenciones Generales

| Aspecto                  | Convención                                                        |
|--------------------------|-------------------------------------------------------------------|
| **Formato de datos**     | `application/json` (excepto upload de imágenes).                  |
| **Codificación**         | UTF-8.                                                            |
| **Fechas**               | ISO 8601 con zona horaria UTC (`2026-04-13T10:30:00Z`).          |
| **Identificadores**      | Enteros autoincremento (`int`).                                   |
| **Coordenadas**          | Latitud y longitud en formato decimal (WGS 84).                  |
| **Nombres de campos**    | camelCase en JSON.                                                |
| **Versionado de API**    | Implícito en la ruta (`/api/`). Futuro: prefijo `/api/v2/`.      |
| **Errores**              | Formato ProblemDetails (RFC 7807).                                |
| **Paginación**           | No implementada en v1.0. Futura consideración para listados.     |

---

## 3. Base URL

| Entorno       | URL Base                          |
|---------------|-----------------------------------|
| Desarrollo    | `https://localhost:5001`          |
| Producción    | Según configuración de despliegue |

Todas las rutas de los endpoints se expresan de forma relativa a la base URL.

---

## 4. Endpoints de Puntos

### 4.1 GET /api/puntos

**Descripción:** Se obtiene el listado completo de puntos georeferenciados registrados en el sistema, incluyendo las coordenadas y la cantidad de fotos asociadas a cada punto.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `GET`                                            |
| **Ruta**       | `/api/puntos`                                    |
| **Produce**    | `application/json`                               |

#### Parámetros

No se requieren parámetros.

#### Ejemplo de Respuesta — `200 OK`

```json
[
  {
    "id": 1,
    "latitud": -34.603722,
    "longitud": -58.381592,
    "nombre": "Obelisco",
    "descripcion": "Monumento histórico en el centro de Buenos Aires.",
    "fechaCreacion": "2026-04-10T08:15:00Z",
    "updatedAt": "2026-04-12T14:30:00Z",
    "cantidadFotos": 3
  },
  {
    "id": 2,
    "latitud": -32.946819,
    "longitud": -60.639317,
    "nombre": "Monumento a la Bandera",
    "descripcion": "Punto de interés turístico en Rosario.",
    "fechaCreacion": "2026-04-11T09:00:00Z",
    "updatedAt": "2026-04-11T09:00:00Z",
    "cantidadFotos": 0
  }
]
```

#### Códigos HTTP

| Código | Descripción                                      |
|--------|--------------------------------------------------|
| `200`  | Listado obtenido correctamente.                  |
| `500`  | Error interno del servidor.                      |

#### Notas de Comportamiento

- Si no existen puntos registrados, se retorna un arreglo vacío `[]` con código `200`.
- El campo `cantidadFotos` se calcula dinámicamente a partir de la relación con la entidad `Foto`.
- El listado se ordena por `fechaCreacion` de forma descendente (más recientes primero).

---

### 4.2 GET /api/puntos/{id}

**Descripción:** Se obtiene el detalle de un punto georeferenciado específico, incluyendo el arreglo completo de fotos asociadas.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `GET`                                            |
| **Ruta**       | `/api/puntos/{id}`                               |
| **Produce**    | `application/json`                               |

#### Parámetros

| Nombre | Ubicación | Tipo  | Requerido | Descripción                          |
|--------|-----------|-------|-----------|--------------------------------------|
| `id`   | Path      | `int` | Sí        | Identificador único del punto.       |

#### Ejemplo de Respuesta — `200 OK`

```json
{
  "id": 1,
  "latitud": -34.603722,
  "longitud": -58.381592,
  "nombre": "Obelisco",
  "descripcion": "Monumento histórico en el centro de Buenos Aires.",
  "fechaCreacion": "2026-04-10T08:15:00Z",
  "updatedAt": "2026-04-12T14:30:00Z",
  "fotos": [
    {
      "id": 10,
      "puntoId": 1,
      "nombreArchivo": "IMG_20260410_081500.jpg",
      "fechaTomada": "2026-04-10T08:15:00Z",
      "tamanoBytes": 2457600,
      "latitudExif": -34.603720,
      "longitudExif": -58.381590
    },
    {
      "id": 11,
      "puntoId": 1,
      "nombreArchivo": "IMG_20260410_081530.jpg",
      "fechaTomada": "2026-04-10T08:15:30Z",
      "tamanoBytes": 3145728,
      "latitudExif": -34.603725,
      "longitudExif": -58.381595
    }
  ]
}
```

#### Códigos HTTP

| Código | Descripción                                      |
|--------|--------------------------------------------------|
| `200`  | Punto encontrado y retornado correctamente.      |
| `404`  | No se encontró un punto con el `id` especificado.|
| `500`  | Error interno del servidor.                      |

#### Notas de Comportamiento

- Si el punto no posee fotos asociadas, el campo `fotos` se retorna como un arreglo vacío `[]`.
- Se realiza una única consulta con carga anticipada (eager loading) de la relación `Fotos` para optimizar el rendimiento.

---

### 4.3 POST /api/puntos

**Descripción:** Se crea un nuevo punto georeferenciado. Este endpoint es utilizado principalmente por el proceso de sincronización desde el cliente móvil.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `POST`                                           |
| **Ruta**       | `/api/puntos`                                    |
| **Consume**    | `application/json`                               |
| **Produce**    | `application/json`                               |

#### Parámetros

No se requieren parámetros de ruta ni de query.

#### Cuerpo de la Solicitud

| Campo         | Tipo     | Requerido | Descripción                                         |
|---------------|----------|-----------|-----------------------------------------------------|
| `latitud`     | `double` | Sí        | Latitud del punto en formato decimal (WGS 84).      |
| `longitud`    | `double` | Sí        | Longitud del punto en formato decimal (WGS 84).     |
| `nombre`      | `string` | No        | Nombre descriptivo del punto (máx. 200 caracteres). |
| `descripcion` | `string` | No        | Descripción detallada del punto (máx. 500 caracteres). |

#### Ejemplo de Solicitud

```json
{
  "latitud": -31.420083,
  "longitud": -64.188776,
  "nombre": "Catedral de Córdoba",
  "descripcion": "Catedral ubicada en el centro histórico de la ciudad."
}
```

#### Ejemplo de Solicitud Mínima

```json
{
  "latitud": -31.420083,
  "longitud": -64.188776
}
```

#### Ejemplo de Respuesta — `201 Created`

```json
{
  "id": 3,
  "latitud": -31.420083,
  "longitud": -64.188776,
  "nombre": "Catedral de Córdoba",
  "descripcion": "Catedral ubicada en el centro histórico de la ciudad.",
  "fechaCreacion": "2026-04-13T10:00:00Z",
  "updatedAt": "2026-04-13T10:00:00Z",
  "cantidadFotos": 0
}
```

#### Códigos HTTP

| Código | Descripción                                                     |
|--------|-----------------------------------------------------------------|
| `201`  | Punto creado correctamente. Se incluye header `Location`.       |
| `400`  | Datos inválidos (latitud/longitud fuera de rango, campos vacíos).|
| `500`  | Error interno del servidor.                                     |

#### Notas de Comportamiento

- El campo `fechaCreacion` se asigna automáticamente con la fecha y hora UTC del servidor al momento de la creación.
- El campo `updatedAt` se inicializa con el mismo valor que `fechaCreacion`.
- Si no se proporcionan `nombre` o `descripcion`, se almacenan como `null`.
- Se valida que `latitud` esté en el rango `[-90, 90]` y `longitud` en el rango `[-180, 180]`.
- La respuesta incluye el header `Location` con la URI del recurso creado: `/api/puntos/{id}`.

---

### 4.4 PUT /api/puntos/{id}

**Descripción:** Se actualizan el nombre y la descripción de un punto georeferenciado existente. Las coordenadas no son modificables una vez creado el punto.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `PUT`                                            |
| **Ruta**       | `/api/puntos/{id}`                               |
| **Consume**    | `application/json`                               |
| **Produce**    | `application/json`                               |

#### Parámetros

| Nombre | Ubicación | Tipo  | Requerido | Descripción                          |
|--------|-----------|-------|-----------|--------------------------------------|
| `id`   | Path      | `int` | Sí        | Identificador único del punto.       |

#### Cuerpo de la Solicitud

| Campo          | Tipo      | Requerido | Descripción                                                     |
|----------------|-----------|-----------|------------------------------------------------------------------|
| `nombre`       | `string`  | Sí        | Nuevo nombre del punto (máx. 200 caracteres).                    |
| `descripcion`  | `string`  | No        | Nueva descripción del punto (máx. 1000 caracteres).              |
| `radioMetros`  | `double`  | No        | Radio de agrupación del marker en metros (10–500). Default: 50.  |
| `updatedAt`    | `string`  | No        | Marca temporal ISO 8601 del cliente para resolución LWW. Si se omite, el servidor usa su propio reloj. |

#### Ejemplo de Solicitud

```json
{
  "nombre": "Catedral de Córdoba (Centro)",
  "descripcion": "Catedral Nuestra Señora de la Asunción, centro histórico.",
  "radioMetros": 75.0,
  "updatedAt": "2026-04-13T11:44:00Z"
}
```

#### Ejemplo de Respuesta — `200 OK`

```json
{
  "id": 3,
  "latitud": -31.420083,
  "longitud": -64.188776,
  "nombre": "Catedral de Córdoba (Centro)",
  "descripcion": "Catedral Nuestra Señora de la Asunción, centro histórico.",
  "radioMetros": 75.0,
  "isDeleted": false,
  "fechaCreacion": "2026-04-13T10:00:00Z",
  "updatedAt": "2026-04-13T11:45:00Z",
  "cantidadFotos": 0
}
```

#### Códigos HTTP

| Código | Descripción                                                     |
|--------|-----------------------------------------------------------------|
| `200`  | Punto actualizado correctamente.                                |
| `400`  | Datos inválidos (campos requeridos ausentes o exceden longitud). |
| `404`  | No se encontró un punto con el `id` especificado.               |
| `500`  | Error interno del servidor.                                     |

#### Notas de Comportamiento

- El campo `updatedAt` se actualiza automáticamente con la fecha y hora UTC del servidor.
- Las coordenadas (`latitud`, `longitud`) y `fechaCreacion` no se modifican.
- Si se envían campos adicionales en el cuerpo, estos son ignorados.

---

### 4.5 DELETE /api/puntos/{id}

**Descripción:** Se elimina un punto georeferenciado junto con todas sus fotos asociadas. La eliminación se realiza en cascada.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `DELETE`                                         |
| **Ruta**       | `/api/puntos/{id}`                               |

#### Parámetros

| Nombre | Ubicación | Tipo  | Requerido | Descripción                          |
|--------|-----------|-------|-----------|--------------------------------------|
| `id`   | Path      | `int` | Sí        | Identificador único del punto.       |

#### Ejemplo de Respuesta — `204 No Content`

No se retorna cuerpo en la respuesta.

#### Códigos HTTP

| Código | Descripción                                                     |
|--------|-----------------------------------------------------------------|
| `204`  | Punto y fotos asociadas eliminados correctamente.               |
| `404`  | No se encontró un punto con el `id` especificado.               |
| `500`  | Error interno del servidor.                                     |

#### Notas de Comportamiento

- La eliminación opera en cascada: se eliminan primero todas las fotos asociadas al punto (tanto los registros en base de datos como los archivos físicos del almacenamiento) y luego el punto.
- La operación se ejecuta dentro de una transacción. Si falla la eliminación de algún archivo físico, se revierte toda la operación.
- Esta acción es irreversible.

---

## 5. Endpoints de Fotos

### 5.1 POST /api/fotos/upload

**Descripción:** Se sube una fotografía al servidor mediante `multipart/form-data`. El servidor extrae automáticamente los metadatos EXIF de la imagen utilizando la librería **MetadataExtractor** para obtener coordenadas GPS y fecha de toma.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `POST`                                           |
| **Ruta**       | `/api/fotos/upload`                              |
| **Consume**    | `multipart/form-data`                            |
| **Produce**    | `application/json`                               |

#### Parámetros

| Nombre    | Ubicación | Tipo     | Requerido | Descripción                                              |
|-----------|-----------|----------|-----------|----------------------------------------------------------|
| `file`    | Form      | `file`   | Sí        | Archivo de imagen (JPEG, PNG). Tamaño máximo: 10 MB.     |
| `puntoId` | Form      | `int`    | No        | Identificador del punto al cual asociar la foto.          |

#### Ejemplo de Solicitud (multipart/form-data)

```
POST /api/fotos/upload HTTP/1.1
Content-Type: multipart/form-data; boundary=----FormBoundary

------FormBoundary
Content-Disposition: form-data; name="file"; filename="IMG_20260413_103000.jpg"
Content-Type: image/jpeg

<contenido binario del archivo>
------FormBoundary
Content-Disposition: form-data; name="puntoId"

1
------FormBoundary--
```

#### Ejemplo de Respuesta — `201 Created` (con GPS detectado)

```json
{
  "fotoId": 15,
  "puntoId": 1,
  "latitudExif": -34.603722,
  "longitudExif": -58.381592,
  "fechaTomada": "2026-04-13T10:30:00Z",
  "gpsDetectado": true
}
```

#### Ejemplo de Respuesta — `201 Created` (sin GPS, punto creado automáticamente)

```json
{
  "fotoId": 16,
  "puntoId": 4,
  "latitudExif": 0.0,
  "longitudExif": 0.0,
  "fechaTomada": "2026-04-13T10:35:00Z",
  "gpsDetectado": false
}
```

#### Códigos HTTP

| Código | Descripción                                                        |
|--------|--------------------------------------------------------------------|
| `201`  | Foto subida y procesada correctamente.                             |
| `400`  | Archivo no proporcionado, formato no soportado o tamaño excedido.  |
| `404`  | El `puntoId` especificado no existe.                               |
| `500`  | Error interno del servidor (fallo al guardar archivo o leer EXIF). |

#### Notas de Comportamiento

- **Extracción EXIF:** Se utiliza la librería `MetadataExtractor` para leer los metadatos EXIF del archivo. Se extraen las coordenadas GPS (latitud, longitud) y la fecha de toma (`DateTimeOriginal`).
- **Asociación automática a punto:**
  - Si se proporciona `puntoId`: la foto se asocia al punto indicado.
  - Si **no** se proporciona `puntoId` y el EXIF **contiene coordenadas GPS**: se crea un nuevo `Punto` automáticamente con las coordenadas extraídas y se asocia la foto.
  - Si **no** se proporciona `puntoId` y el EXIF **no contiene GPS**: se crea un nuevo `Punto` con coordenadas `(0.0, 0.0)` y el campo `gpsDetectado` se retorna como `false` a modo de advertencia.
- **Formatos aceptados:** JPEG (`image/jpeg`) y PNG (`image/png`).
- **Tamaño máximo:** 10 MB por archivo.
- **Almacenamiento:** El archivo físico se almacena en el directorio configurado del servidor con un nombre generado para evitar colisiones.
- Si la fecha EXIF no se puede extraer, se utiliza la fecha y hora actual del servidor.

---

### 5.2 GET /api/fotos/{puntoId}

**Descripción:** Se obtiene el listado de todas las fotos asociadas a un punto georeferenciado específico.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `GET`                                            |
| **Ruta**       | `/api/fotos/{puntoId}`                           |
| **Produce**    | `application/json`                               |

#### Parámetros

| Nombre    | Ubicación | Tipo  | Requerido | Descripción                                |
|-----------|-----------|-------|-----------|--------------------------------------------|
| `puntoId` | Path      | `int` | Sí        | Identificador del punto georeferenciado.   |

#### Ejemplo de Respuesta — `200 OK`

```json
[
  {
    "id": 10,
    "puntoId": 1,
    "nombreArchivo": "IMG_20260410_081500.jpg",
    "fechaTomada": "2026-04-10T08:15:00Z",
    "tamanoBytes": 2457600,
    "latitudExif": -34.603720,
    "longitudExif": -58.381590
  },
  {
    "id": 11,
    "puntoId": 1,
    "nombreArchivo": "IMG_20260410_081530.jpg",
    "fechaTomada": "2026-04-10T08:15:30Z",
    "tamanoBytes": 3145728,
    "latitudExif": -34.603725,
    "longitudExif": -58.381595
  },
  {
    "id": 12,
    "puntoId": 1,
    "nombreArchivo": "IMG_20260411_120000.jpg",
    "fechaTomada": "2026-04-11T12:00:00Z",
    "tamanoBytes": 1843200,
    "latitudExif": -34.603718,
    "longitudExif": -58.381588
  }
]
```

#### Códigos HTTP

| Código | Descripción                                                     |
|--------|-----------------------------------------------------------------|
| `200`  | Listado de fotos obtenido correctamente.                        |
| `404`  | No se encontró un punto con el `puntoId` especificado.          |
| `500`  | Error interno del servidor.                                     |

#### Notas de Comportamiento

- Si el punto existe pero no posee fotos asociadas, se retorna un arreglo vacío `[]` con código `200`.
- El listado se ordena por `fechaTomada` de forma ascendente (más antiguas primero).

---

### 5.3 GET /api/fotos/imagen/{id}

**Descripción:** Se sirve el archivo físico de la imagen correspondiente a una foto registrada en el sistema. Se retorna directamente el stream binario del archivo.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `GET`                                            |
| **Ruta**       | `/api/fotos/imagen/{id}`                         |
| **Produce**    | `image/jpeg` o `image/png`                       |

#### Parámetros

| Nombre | Ubicación | Tipo  | Requerido | Descripción                                    |
|--------|-----------|-------|-----------|-------------------------------------------------|
| `id`   | Path      | `int` | Sí        | Identificador único de la foto.                 |

#### Ejemplo de Respuesta — `200 OK`

Se retorna el contenido binario del archivo de imagen con el `Content-Type` correspondiente:

```
HTTP/1.1 200 OK
Content-Type: image/jpeg
Content-Length: 2457600
Content-Disposition: inline; filename="IMG_20260410_081500.jpg"

<contenido binario de la imagen>
```

#### Códigos HTTP

| Código | Descripción                                                         |
|--------|---------------------------------------------------------------------|
| `200`  | Archivo de imagen retornado correctamente.                          |
| `404`  | No se encontró la foto o el archivo físico no existe en el servidor.|
| `500`  | Error interno del servidor (fallo al leer archivo).                 |

#### Notas de Comportamiento

- El `Content-Type` se determina a partir de la extensión del archivo almacenado (`.jpg` → `image/jpeg`, `.png` → `image/png`).
- Se incluye el header `Content-Disposition: inline` para permitir la visualización directa en el navegador.
- Si el registro existe en la base de datos pero el archivo físico no se encuentra en el almacenamiento, se retorna `404` con un mensaje descriptivo en formato ProblemDetails.

---

### 5.4 DELETE /api/fotos/{id}

**Descripción:** Se elimina una foto individual, tanto su registro en la base de datos como el archivo físico almacenado en el servidor.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `DELETE`                                         |
| **Ruta**       | `/api/fotos/{id}`                                |

#### Parámetros

| Nombre | Ubicación | Tipo  | Requerido | Descripción                          |
|--------|-----------|-------|-----------|--------------------------------------|
| `id`   | Path      | `int` | Sí        | Identificador único de la foto.      |

#### Ejemplo de Respuesta — `204 No Content`

No se retorna cuerpo en la respuesta.

#### Códigos HTTP

| Código | Descripción                                                     |
|--------|-----------------------------------------------------------------|
| `204`  | Foto eliminada correctamente (registro y archivo físico).       |
| `404`  | No se encontró una foto con el `id` especificado.               |
| `500`  | Error interno del servidor.                                     |

#### Notas de Comportamiento

- Se elimina el registro de la base de datos y el archivo físico del almacenamiento de forma atómica.
- Si el archivo físico no existe pero el registro sí, se elimina el registro y se registra una advertencia en el log del servidor.
- El campo `cantidadFotos` del punto asociado se actualiza automáticamente al recalcularse en las consultas posteriores.

---

### 5.5 PUT /api/fotos/{id}

**Descripción:** Actualiza el comentario individual de una foto. Usado por el carrusel y el visor fullscreen para persistir la descripción por foto en el servidor.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `PUT`                                            |
| **Ruta**       | `/api/fotos/{id}`                                |
| **Consume**    | `application/json`                               |
| **Produce**    | `application/json`                               |

#### Parámetros

| Nombre | Ubicación | Tipo  | Requerido | Descripción                          |
|--------|-----------|-------|-----------|--------------------------------------|
| `id`   | Path      | `int` | Sí        | Identificador único de la foto.      |

#### Cuerpo de la Solicitud

| Campo        | Tipo     | Requerido | Descripción                                                      |
|--------------|----------|-----------|------------------------------------------------------------------|
| `comentario` | `string` | No        | Descripción individual de la foto (máx. 500 caracteres).         |
| `updatedAt`  | `string` | No        | Marca temporal ISO 8601 del cliente para resolución LWW.         |

#### Ejemplo de Solicitud

```json
{
  "comentario": "Vista desde el norte, base claramente dañada.",
  "updatedAt": "2026-04-13T11:30:00Z"
}
```

#### Ejemplo de Respuesta — `200 OK`

```json
{
  "id": 15,
  "puntoId": 1,
  "comentario": "Vista desde el norte, base claramente dañada.",
  "isDeleted": false,
  "updatedAt": "2026-04-13T11:31:00Z"
}
```

#### Códigos HTTP

| Código | Descripción                                                     |
|--------|-----------------------------------------------------------------|
| `200`  | Foto actualizada correctamente.                                 |
| `404`  | No se encontró una foto con el `id` especificado.               |
| `500`  | Error interno del servidor.                                     |

#### Notas de Comportamiento

- Solo se puede actualizar el campo `comentario`. Los metadatos de la imagen (ruta, tamaño, EXIF) son inmutables.
- Si `updatedAt` del cliente es anterior al `updatedAt` del servidor, se aplica LWW y se retorna el estado actual sin modificar (HTTP 200 con cuerpo del servidor).

---

### 5.6 GET /api/puntos/{id}/fotos/download

**Descripción:** Descarga todas las fotos asociadas a un punto georeferenciado comprimidas en un archivo ZIP. Disponible exclusivamente en el cliente web (GeoFoto.Web). Requiere que el punto tenga al menos una foto.

| Atributo       | Valor                                              |
|----------------|----------------------------------------------------|
| **Método**     | `GET`                                              |
| **Ruta**       | `/api/puntos/{id}/fotos/download`                  |
| **Produce**    | `application/zip`                                  |

#### Parámetros

| Nombre | Ubicación | Tipo  | Requerido | Descripción                          |
|--------|-----------|-------|-----------|--------------------------------------|
| `id`   | Path      | `int` | Sí        | Identificador único del punto.       |

#### Ejemplo de Solicitud

```
GET /api/puntos/3/fotos/download
```

#### Ejemplo de Respuesta — `200 OK`

- Content-Type: `application/zip`  
- Content-Disposition: `attachment; filename="Catedral_de_Cordoba_Centro.zip"`  
- Body: archivo ZIP con las fotos nombradas como `{nombrePuntoSanitizado}_{n}.jpg`

#### Códigos HTTP

| Código | Descripción                                                     |
|--------|-----------------------------------------------------------------|
| `200`  | ZIP generado y retornado correctamente.                         |
| `404`  | No se encontró el punto o el punto no tiene fotos activas.      |
| `500`  | Error interno del servidor al generar el ZIP.                   |

#### Notas de Comportamiento

- Las fotos con `IsDeleted = true` no se incluyen en el ZIP.
- El nombre de cada archivo dentro del ZIP sigue el patrón `{nombrePuntoSanitizado}_{n}.{ext}`, donde `n` es el índice de la foto (1-based) y `{ext}` es la extensión original.
- El nombre del punto se sanitiza para uso en nombre de archivo (se reemplazan espacios por `_` y se eliminan caracteres no permitidos en nombres de archivo).
- El ZIP se genera en memoria con `System.IO.Compression.ZipArchive` y se envía como stream para no sobrecargar la RAM del servidor.

---

## 6. Endpoints de Sincronización

### 6.1 GET /api/sync/delta

**Descripción:** Se obtienen todos los cambios (puntos y fotos) ocurridos desde una marca de tiempo determinada. Este endpoint es utilizado por los clientes móviles para descargar las novedades del servidor durante el proceso de sincronización.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `GET`                                            |
| **Ruta**       | `/api/sync/delta`                                |
| **Produce**    | `application/json`                               |

#### Parámetros

| Nombre  | Ubicación | Tipo     | Requerido | Descripción                                                   |
|---------|-----------|----------|-----------|---------------------------------------------------------------|
| `since` | Query     | `string` | Sí        | Marca temporal ISO 8601 desde la cual se consultan los cambios. |

#### Ejemplo de Solicitud

```
GET /api/sync/delta?since=2026-04-12T00:00:00Z
```

#### Ejemplo de Respuesta — `200 OK`

```json
{
  "puntos": [
    {
      "id": 1,
      "latitud": -34.603722,
      "longitud": -58.381592,
      "nombre": "Obelisco",
      "descripcion": "Monumento histórico en el centro de Buenos Aires.",
      "radioMetros": 50.0,
      "isDeleted": false,
      "fechaCreacion": "2026-04-10T08:15:00Z",
      "updatedAt": "2026-04-12T14:30:00Z"
    },
    {
      "id": 3,
      "latitud": -31.420083,
      "longitud": -64.188776,
      "nombre": "Catedral de Córdoba (Centro)",
      "descripcion": "Catedral Nuestra Señora de la Asunción, centro histórico.",
      "radioMetros": 75.0,
      "isDeleted": false,
      "fechaCreacion": "2026-04-13T10:00:00Z",
      "updatedAt": "2026-04-13T11:45:00Z"
    }
  ],
  "fotos": [
    {
      "id": 15,
      "puntoId": 1,
      "nombreArchivo": "IMG_20260413_103000.jpg",
      "fechaTomada": "2026-04-13T10:30:00Z",
      "tamanoBytes": 2457600,
      "latitudExif": -34.603722,
      "longitudExif": -58.381592,
      "comentario": "Vista frontal del marker.",
      "isDeleted": false,
      "updatedAt": "2026-04-13T10:30:15Z"
    }
  ],
  "ultimaSync": "2026-04-13T10:30:15Z"
}
```

#### Códigos HTTP

| Código | Descripción                                                     |
|--------|-----------------------------------------------------------------|
| `200`  | Cambios obtenidos correctamente.                                |
| `400`  | Parámetro `since` ausente o con formato de fecha inválido.      |
| `500`  | Error interno del servidor.                                     |

#### Notas de Comportamiento

- Se retornan todos los registros de `Punto` y `Foto` cuyo campo `UpdatedAt` sea **posterior** al valor del parámetro `since`.
- Si no existen cambios desde la fecha indicada, se retornan arreglos vacíos con la marca temporal actual:
  ```json
  {
    "puntos": [],
    "fotos": [],
    "ultimaSync": "2026-04-13T10:30:15Z"
  }
  ```
- El campo `ultimaSync` representa la fecha y hora UTC del servidor al momento de procesar la consulta. El cliente debe almacenar este valor para utilizarlo en la próxima solicitud delta.
- Se incluyen registros creados, modificados y eliminados (con `isDeleted = true`). Los clientes móviles deben eliminar localmente los registros con `isDeleted = true` al procesar el delta.
- El parámetro `since` debe estar en formato ISO 8601. Ejemplo válido: `2026-04-12T00:00:00Z`.
- Los campos `radioMetros`, `isDeleted` y `comentario` se incluyen en la respuesta delta desde v1.1.

---

### 6.2 POST /api/sync/batch

**Descripción:** Se procesan múltiples operaciones de sincronización en un único lote (batch). Cada operación se procesa de forma independiente dentro de una transacción, permitiendo que operaciones individuales puedan fallar sin afectar al resto.

| Atributo       | Valor                                            |
|----------------|--------------------------------------------------|
| **Método**     | `POST`                                           |
| **Ruta**       | `/api/sync/batch`                                |
| **Consume**    | `application/json`                               |
| **Produce**    | `application/json`                               |

#### Parámetros

No se requieren parámetros de ruta ni de query.

#### Cuerpo de la Solicitud

Se envía un arreglo de operaciones. Cada operación contiene:

| Campo             | Tipo     | Requerido | Descripción                                                              |
|-------------------|----------|-----------|--------------------------------------------------------------------------|
| `operationType`   | `string` | Sí        | Tipo de operación: `"create"`, `"update"` o `"delete"`.                  |
| `entityType`      | `string` | Sí        | Tipo de entidad: `"punto"` o `"foto"`.                                   |
| `localId`         | `string` | Sí        | Identificador local asignado por el cliente para correlación.            |
| `payload`         | `object` | No        | Datos de la entidad (requerido para `create` y `update`).                |
| `updatedAt`       | `string` | Sí        | Marca temporal ISO 8601 de la última modificación en el cliente.         |

#### Ejemplo de Solicitud

```json
[
  {
    "operationType": "create",
    "entityType": "punto",
    "localId": "local-punto-001",
    "payload": {
      "latitud": -26.836944,
      "longitud": -65.203333,
      "nombre": "Plaza Independencia",
      "descripcion": "Plaza principal de San Miguel de Tucumán."
    },
    "updatedAt": "2026-04-13T09:00:00Z"
  },
  {
    "operationType": "update",
    "entityType": "punto",
    "localId": "remote-3",
    "payload": {
      "id": 3,
      "nombre": "Catedral de Córdoba (Restaurada)",
      "descripcion": "Catedral recientemente restaurada, centro histórico."
    },
    "updatedAt": "2026-04-13T09:15:00Z"
  },
  {
    "operationType": "delete",
    "entityType": "punto",
    "localId": "remote-2",
    "payload": {
      "id": 2
    },
    "updatedAt": "2026-04-13T09:20:00Z"
  }
]
```

#### Ejemplo de Respuesta — `200 OK`

```json
[
  {
    "localId": "local-punto-001",
    "status": "success",
    "remoteId": 5,
    "error": null
  },
  {
    "localId": "remote-3",
    "status": "success",
    "remoteId": 3,
    "error": null
  },
  {
    "localId": "remote-2",
    "status": "error",
    "remoteId": null,
    "error": "El punto con id 2 no fue encontrado."
  }
]
```

#### Códigos HTTP

| Código | Descripción                                                            |
|--------|------------------------------------------------------------------------|
| `200`  | Lote procesado. Cada operación contiene su resultado individual.       |
| `400`  | Formato del cuerpo inválido (no es un arreglo o campos requeridos ausentes). |
| `500`  | Error interno del servidor (fallo general no atribuible a operación individual). |

#### Notas de Comportamiento

- Cada operación se procesa de forma **independiente**: el éxito o fallo de una operación no afecta al procesamiento de las demás.
- El procesamiento se ejecuta dentro de una transacción por operación individual. Si una operación falla, las demás continúan procesándose.
- El campo `localId` es proporcionado por el cliente y se retorna en la respuesta para permitir la correlación entre las operaciones enviadas y los resultados obtenidos.
- Para operaciones de tipo `create`, el campo `remoteId` en la respuesta contiene el identificador asignado por el servidor. El cliente debe actualizar su almacenamiento local con este identificador.
- Para operaciones de tipo `delete`, si la entidad no existe en el servidor, se reporta como `error` pero no se considera un fallo crítico del lote.
- Los valores válidos de `operationType` son: `create`, `update`, `delete`.
- Los valores válidos de `entityType` son: `punto`, `foto`.

---

## 7. Modelo de Error

Todos los endpoints retornan errores en el formato **ProblemDetails** definido en [RFC 7807](https://tools.ietf.org/html/rfc7807), implementado nativamente por ASP.NET Core.

### Estructura del Modelo

| Campo      | Tipo     | Descripción                                                      |
|------------|----------|------------------------------------------------------------------|
| `type`     | `string` | URI de referencia que identifica el tipo de problema.            |
| `title`    | `string` | Resumen breve y legible del tipo de problema.                    |
| `status`   | `int`    | Código de estado HTTP.                                           |
| `detail`   | `string` | Explicación detallada y específica de la ocurrencia del error.   |
| `traceId`  | `string` | Identificador de traza para diagnóstico y correlación de logs.   |

### Ejemplo — Recurso No Encontrado (`404`)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Not Found",
  "status": 404,
  "detail": "El punto con id 99 no fue encontrado.",
  "traceId": "00-abc123def456-789ghi012-01"
}
```

### Ejemplo — Solicitud Inválida (`400`)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "El campo 'latitud' es requerido y debe estar en el rango [-90, 90].",
  "traceId": "00-def456abc789-012jkl345-01"
}
```

### Ejemplo — Error Interno (`500`)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Se produjo un error inesperado al procesar la solicitud.",
  "traceId": "00-ghi789jkl012-345mno678-01"
}
```

### Ejemplo — Archivo Demasiado Grande (`400`)

```json
{
  "type": "https://tools.ietf.org/html/rfc7807",
  "title": "Bad Request",
  "status": 400,
  "detail": "El archivo excede el tamaño máximo permitido de 10 MB.",
  "traceId": "00-jkl012mno345-678pqr901-01"
}
```

---

## 8. Autenticación

En la versión 1.0, la API **no implementa autenticación**. El sistema se concibe para uso interno en redes controladas, sin exposición directa a Internet.

### Plan Futuro

En versiones posteriores se prevé la implementación de autenticación mediante **JWT Bearer Tokens**:

| Aspecto               | Especificación Futura                                  |
|------------------------|--------------------------------------------------------|
| **Esquema**           | Bearer Token (JWT).                                    |
| **Header**            | `Authorization: Bearer <token>`                        |
| **Emisor**            | Servicio de identidad integrado o externo.             |
| **Expiración**        | Configurable (por defecto: 60 minutos).                |
| **Refresh Token**     | Sí, para renovación sin re-autenticación.              |

---

## 9. Configuración CORS

Se configura CORS (Cross-Origin Resource Sharing) para permitir el acceso desde los orígenes autorizados del sistema GeoFoto.

### Orígenes Permitidos

| Origen                           | Descripción                                |
|----------------------------------|--------------------------------------------|
| `https://localhost:5002`         | GeoFoto.Web en entorno de desarrollo.      |
| `https://geofoto.example.com`   | GeoFoto.Web en entorno de producción.      |
| Aplicación móvil (GeoFoto.Mobile) | Acceso desde la aplicación MAUI.         |

### Configuración Aplicada

| Directiva                | Valor                                               |
|--------------------------|-----------------------------------------------------|
| **Métodos permitidos**   | `GET`, `POST`, `PUT`, `DELETE`, `OPTIONS`           |
| **Headers permitidos**   | `Content-Type`, `Authorization`, `Accept`           |
| **Credenciales**         | Permitidas (`AllowCredentials`).                    |
| **Max Age (preflight)**  | 3600 segundos (1 hora).                             |

---

## 10. Rate Limiting

En la versión 1.0, **no se implementa rate limiting**. El sistema se despliega en entornos controlados con un número reducido de usuarios concurrentes.

### Consideraciones Futuras

Para despliegues en producción con mayor escala, se prevé la utilización del middleware `Microsoft.AspNetCore.RateLimiting` con la siguiente configuración orientativa:

| Política          | Límite                     | Ventana    | Aplicación            |
|-------------------|----------------------------|------------|-----------------------|
| **General**       | 100 solicitudes/minuto     | Deslizante | Todos los endpoints.  |
| **Upload**        | 10 solicitudes/minuto      | Fija       | `POST /api/fotos/upload` |
| **Sync**          | 30 solicitudes/minuto      | Deslizante | `/api/sync/*`         |

---

## 11. Control de Cambios

| Versión | Fecha       | Autor           | Descripción                                        |
|---------|-------------|-----------------|-----------------------------------------------------|
| 1.0     | 2026-04-13  | Equipo Técnico  | Creación inicial del documento con 11 endpoints.    |
| 1.1     | 2026-04-16  | Equipo Técnico  | Sprint 07/08 — Agregados `PUT /api/fotos/{id}` (comentario) y `GET /api/puntos/{id}/fotos/download` (zip). Actualizado `PUT /api/puntos/{id}` con campo `radioMetros`. Respuesta delta incluye `isDeleted`, `radioMetros`, `comentario`. |

---

**Fin del documento**
