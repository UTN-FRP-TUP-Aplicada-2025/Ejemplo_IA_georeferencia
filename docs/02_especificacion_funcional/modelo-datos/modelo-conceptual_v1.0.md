# Modelo Conceptual de Datos

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** modelo-conceptual_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Propósito

El presente documento describe el modelo conceptual de datos del dominio de GeoFoto. Se identifican las entidades principales, sus atributos clave, las relaciones entre ellas y las reglas conceptuales que rigen su comportamiento. El modelo contempla la naturaleza **offline-first** de la aplicación, por lo que cada entidad posee una representación dual: una canónica en el servidor (SQL Server) y una local en el dispositivo móvil (SQLite), con campos adicionales para gestionar la sincronización.

---

## 2. Entidades del Dominio

### 2.1 Punto

Representa un punto de interés georeferenciado dentro del sistema. Un punto se define por sus coordenadas geográficas (latitud y longitud), un nombre opcional, una descripción opcional y la fecha en que fue creado.

| Campo          | Tipo              | Obligatorio | Descripción                                      |
|----------------|-------------------|:-----------:|--------------------------------------------------|
| Id             | Entero (PK)       | Sí          | Identificador único del punto.                   |
| Latitud        | Decimal (10,7)    | Sí          | Coordenada de latitud en formato WGS-84.         |
| Longitud       | Decimal (10,7)    | Sí          | Coordenada de longitud en formato WGS-84.        |
| Nombre         | Texto (200)       | No          | Nombre descriptivo del punto de interés.         |
| Descripcion    | Texto (1000)      | No          | Descripción detallada del punto.                 |
| FechaCreacion  | Fecha/hora        | Sí          | Fecha y hora de creación del registro.           |
| UpdatedAt      | Fecha/hora (UTC)  | Sí          | Marca temporal de última modificación en UTC.    |

### 2.2 Foto

Representa una fotografía georeferenciada asociada a un Punto. Contiene información del archivo, coordenadas EXIF extraídas de la imagen y la fecha de captura.

| Campo          | Tipo              | Obligatorio | Descripción                                      |
|----------------|-------------------|:-----------:|--------------------------------------------------|
| Id             | Entero (PK)       | Sí          | Identificador único de la foto.                  |
| PuntoId        | Entero (FK)       | Sí          | Referencia al punto al que pertenece la foto.    |
| NombreArchivo  | Texto (260)       | Sí          | Nombre original del archivo de imagen.           |
| RutaFisica     | Texto (500)       | Sí          | Ruta de almacenamiento del archivo en el sistema.|
| FechaTomada    | Fecha/hora        | No          | Fecha y hora de captura según metadatos EXIF.    |
| TamanoBytes    | Entero largo      | Sí          | Tamaño del archivo en bytes.                     |
| LatitudExif    | Decimal (10,7)    | No          | Latitud extraída de los metadatos EXIF.          |
| LongitudExif   | Decimal (10,7)    | No          | Longitud extraída de los metadatos EXIF.         |
| UpdatedAt      | Fecha/hora (UTC)  | Sí          | Marca temporal de última modificación en UTC.    |

### 2.3 ColaSincronizacion (SyncQueue)

Representa una operación de sincronización pendiente en la cola de salida (*outbox*). Cada vez que se realiza una escritura en la base de datos local, se genera una entrada en esta cola para que el proceso de sincronización la envíe al servidor cuando exista conectividad.

| Campo           | Tipo              | Obligatorio | Descripción                                                  |
|-----------------|-------------------|:-----------:|--------------------------------------------------------------|
| Id              | Entero (PK)       | Sí          | Identificador único de la entrada en la cola.                |
| OperationType   | Texto             | Sí          | Tipo de operación: `Create`, `Update` o `Delete`.            |
| EntityType      | Texto             | Sí          | Tipo de entidad afectada: `Punto` o `Foto`.                  |
| LocalId         | Entero            | Sí          | Identificador local del registro afectado.                   |
| Payload         | Texto (JSON)      | Sí          | Contenido serializado del registro a sincronizar.            |
| Status          | Texto             | Sí          | Estado de la entrada: `Pending`, `Processing`, `Done`, `Failed`. |
| Attempts        | Entero            | Sí          | Cantidad de intentos de sincronización realizados.           |
| LastAttemptAt   | Fecha/hora        | No          | Marca temporal del último intento de sincronización.         |
| ErrorMessage    | Texto             | No          | Mensaje de error del último intento fallido.                 |
| CreatedAt       | Fecha/hora (UTC)  | Sí          | Fecha y hora de creación de la entrada en la cola.           |

### 2.4 EstadoSync (Enum)

Define los posibles estados de sincronización de un registro local. Se utiliza en el campo `SyncStatus` de las entidades locales.

| Valor           | Descripción                                                                                  |
|-----------------|----------------------------------------------------------------------------------------------|
| `Local`         | El registro fue creado localmente y nunca se ha sincronizado con el servidor.                |
| `Synced`        | El registro se encuentra sincronizado; la copia local y la remota son idénticas.             |
| `PendingCreate` | El registro fue creado localmente y se encuentra pendiente de envío al servidor.             |
| `PendingUpdate` | El registro fue modificado localmente y los cambios se encuentran pendientes de sincronización. |
| `PendingDelete` | El registro fue marcado para eliminación y la operación se encuentra pendiente de sincronización. |
| `Conflict`      | Se detectó un conflicto entre la versión local y la versión del servidor durante la sincronización. |
| `Failed`        | El último intento de sincronización falló; el registro requiere intervención o reintento.    |

---

## 3. Representación Dual: Servidor vs Local

Cada entidad del dominio posee **dos representaciones** que coexisten durante el ciclo de vida de la aplicación:

- **Servidor (SQL Server):** Contiene las tablas `Puntos` y `Fotos` con los campos canónicos del dominio. Los identificadores son asignados por el servidor y constituyen la fuente de verdad del sistema.
- **Local (SQLite):** Contiene las tablas `Puntos_Local` y `Fotos_Local` con los mismos campos del dominio más campos adicionales para gestionar la sincronización: `LocalId` (identificador local), `RemoteId` (referencia al identificador del servidor, nulo hasta la primera sincronización exitosa) y `SyncStatus` (estado de sincronización).

Esta dualidad permite que la aplicación funcione de manera completamente autónoma sin conectividad, almacenando los datos localmente y reconciliándolos con el servidor cuando se restablece la conexión.

### 3.1 Comparación de campos: Punto (Servidor) vs Punto_Local (SQLite)

| Campo (Servidor)   | Tipo SQL Server       | Campo (Local)    | Tipo SQLite      | Notas                                         |
|---------------------|-----------------------|------------------|------------------|-----------------------------------------------|
| `Id` (PK)           | `int`                 | `RemoteId`       | `INTEGER` null   | Nulo hasta la primera sincronización exitosa. |
| —                   | —                     | `LocalId` (PK)   | `INTEGER`        | Identificador primario en el dispositivo.     |
| `Latitud`           | `decimal(10,7)`       | `Latitud`        | `REAL`           | Coordenada de latitud.                        |
| `Longitud`          | `decimal(10,7)`       | `Longitud`       | `REAL`           | Coordenada de longitud.                       |
| `Nombre`            | `nvarchar(200)` null  | `Nombre`         | `TEXT` null       | Nombre descriptivo.                           |
| `Descripcion`       | `nvarchar(1000)` null | `Descripcion`    | `TEXT` null       | Descripción del punto.                        |
| `FechaCreacion`     | `datetime2`           | `FechaCreacion`  | `TEXT` (ISO 8601) | Fecha de creación del registro.               |
| `UpdatedAt`         | `datetime2`           | `UpdatedAt`      | `TEXT` (ISO 8601) | Última modificación en UTC.                   |
| —                   | —                     | `SyncStatus`     | `TEXT`           | Estado de sincronización (ver §2.4).          |

### 3.2 Comparación de campos: Foto (Servidor) vs Foto_Local (SQLite)

| Campo (Servidor)   | Tipo SQL Server       | Campo (Local)    | Tipo SQLite      | Notas                                         |
|---------------------|-----------------------|------------------|------------------|-----------------------------------------------|
| `Id` (PK)           | `int`                 | `RemoteId`       | `INTEGER` null   | Nulo hasta la primera sincronización exitosa. |
| —                   | —                     | `LocalId` (PK)   | `INTEGER`        | Identificador primario en el dispositivo.     |
| `PuntoId` (FK)      | `int`                 | `PuntoLocalId` (FK) | `INTEGER`     | Referencia al `LocalId` del punto asociado.   |
| `NombreArchivo`     | `nvarchar(260)`       | `NombreArchivo`  | `TEXT`           | Nombre original del archivo.                  |
| `RutaFisica`        | `nvarchar(500)`       | `RutaLocal`      | `TEXT`           | Ruta de almacenamiento (servidor / local).    |
| `FechaTomada`       | `datetime2` null      | `FechaTomada`    | `TEXT` null       | Fecha de captura EXIF.                        |
| `TamanoBytes`       | `bigint`              | `TamanoBytes`    | `INTEGER`        | Tamaño del archivo en bytes.                  |
| `LatitudExif`       | `decimal(10,7)` null  | `LatitudExif`    | `REAL` null       | Latitud EXIF.                                 |
| `LongitudExif`      | `decimal(10,7)` null  | `LongitudExif`   | `REAL` null       | Longitud EXIF.                                |
| `UpdatedAt`         | `datetime2`           | `UpdatedAt`      | `TEXT` (ISO 8601) | Última modificación en UTC.                   |
| —                   | —                     | `SyncStatus`     | `TEXT`           | Estado de sincronización (ver §2.4).          |

---

## 4. Relaciones y Cardinalidad

### 4.1 Diagrama de Relaciones

```
┌─────────────┐              ┌─────────────┐
│   Punto     │ 1 ────── 0..*│    Foto     │
│  (Servidor) │              │  (Servidor) │
└──────┬──────┘              └──────┬──────┘
       │                            │
       │  representación local      │  representación local
       ▼                            ▼
┌─────────────┐              ┌─────────────┐
│ Punto_Local │ 1 ────── 0..*│ Foto_Local  │
│  (SQLite)   │              │  (SQLite)   │
└──────┬──────┘              └─────────────┘
       │
       │ 0..*
       ▼
┌─────────────┐
│  SyncQueue  │
│  (SQLite)   │
└─────────────┘
```

### 4.2 Tabla de Relaciones

| Relación                     | Cardinalidad | Regla                                                                                          |
|------------------------------|:------------:|------------------------------------------------------------------------------------------------|
| Punto → Foto                 | 1 a muchos   | Eliminación en cascada (`CASCADE DELETE`). Al eliminar un punto se eliminan todas sus fotos.   |
| Punto_Local → Foto_Local     | 1 a muchos   | Relación por `PuntoLocalId`. Misma semántica de cascada aplicada a nivel local.                |
| Punto_Local → SyncQueue      | 1 a muchos   | Se genera una entrada en la cola por cada operación pendiente sobre el punto.                  |
| Foto_Local → SyncQueue       | 1 a muchos   | Se genera una entrada en la cola por cada operación pendiente sobre la foto.                   |

---

## 5. Reglas Conceptuales del Modelo

Se enumeran a continuación las reglas conceptuales que rigen el comportamiento del modelo de datos:

1. **Pertenencia obligatoria:** Toda `Foto` pertenece a exactamente un `Punto`. No pueden existir fotos huérfanas en el sistema.

2. **Registro en cola de sincronización (RN-01):** Toda operación de escritura (creación, actualización o eliminación) sobre una entidad local genera automáticamente una entrada en `SyncQueue`.

3. **Identificador remoto nulo inicial:** El campo `RemoteId` de las entidades locales permanece nulo (`null`) hasta que se complete exitosamente la primera sincronización con el servidor.

4. **Identificador local como primario offline:** El campo `LocalId` constituye el identificador primario durante las operaciones en modo desconectado. Todas las relaciones locales se establecen mediante `LocalId`.

5. **Ciclo de vida mediante SyncStatus:** El campo `SyncStatus` refleja el estado del ciclo de vida de cada registro local, desde su creación (`PendingCreate`) hasta su sincronización exitosa (`Synced`), incluyendo estados intermedios de conflicto y error.

6. **Resolución de conflictos por marca temporal (RN-05):** El campo `UpdatedAt` se almacena siempre en UTC y actúa como árbitro para la resolución de conflictos durante la sincronización. En caso de conflicto, prevalece el registro con el valor de `UpdatedAt` más reciente.

7. **Punto por defecto para fotos sin GPS (RN-03):** Las fotografías que no contengan coordenadas GPS en sus metadatos EXIF generan un punto en las coordenadas (0, 0) como ubicación predeterminada.

---

## 6. Trazabilidad a Casos de Uso

La siguiente tabla establece la relación entre las entidades del modelo conceptual y los casos de uso del sistema:

| Entidad         | Casos de Uso                              | Descripción                                                                    |
|-----------------|-------------------------------------------|--------------------------------------------------------------------------------|
| Punto           | CU-04, CU-05, CU-06, CU-07, CU-08, CU-16 | Gestión de puntos de interés: creación, consulta, edición, eliminación y mapa. |
| Foto            | CU-01, CU-02, CU-03, CU-04, CU-16        | Captura, almacenamiento, consulta de fotos y visualización en mapa.            |
| SyncQueue       | CU-09, CU-10, CU-11, CU-12, CU-15        | Cola de sincronización: envío, recepción, resolución de conflictos y estado.   |
| EstadoSync      | CU-09, CU-12, CU-14                       | Gestión del estado de sincronización y notificación de conflictos.             |

---

## 7. Control de Cambios

| Versión | Fecha       | Autor           | Descripción del Cambio           |
|---------|-------------|-----------------|----------------------------------|
| 1.0     | 2026-04-13  | Equipo Técnico  | Creación inicial del documento.  |

---

**Fin del documento**
