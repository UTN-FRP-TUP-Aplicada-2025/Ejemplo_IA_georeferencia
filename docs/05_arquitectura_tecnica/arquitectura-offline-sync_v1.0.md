# Arquitectura Offline-First y Motor de Sincronización

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** arquitectura-offline-sync_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## Tabla de Contenidos

1. [Propósito y Contexto del Patrón](#1-propósito-y-contexto-del-patrón)
2. [Por qué Outbox Pattern](#2-por-qué-outbox-pattern)
3. [Máquina de Estados de SyncStatus](#3-máquina-de-estados-de-syncstatus)
4. [Flujo Push Completo](#4-flujo-push-completo)
5. [Flujo Pull Completo](#5-flujo-pull-completo)
6. [Algoritmo de Push (Pseudocódigo)](#6-algoritmo-de-push-pseudocódigo)
7. [Algoritmo de Pull (Pseudocódigo)](#7-algoritmo-de-pull-pseudocódigo)
8. [Tabla de Casos de Resolución de Conflictos](#8-tabla-de-casos-de-resolución-de-conflictos)
9. [Estrategia de Backoff Exponencial](#9-estrategia-de-backoff-exponencial)
10. [Descripción Completa de la Tabla SyncQueue](#10-descripción-completa-de-la-tabla-syncqueue)
11. [Integración con Connectivity.ConnectivityChanged](#11-integración-con-connectivityconnectivitychanged)
12. [Consideraciones de Performance](#12-consideraciones-de-performance)
13. [Observabilidad](#13-observabilidad)
14. [Criterios de Aceptación del Motor de Sync](#14-criterios-de-aceptación-del-motor-de-sync)

---

## 1. Propósito y Contexto del Patrón

### 1.1 Necesidad Operativa

GeoFoto se concibe como una aplicación destinada a usuarios que realizan relevamientos fotográficos georreferenciados en campo. Estos entornos operativos presentan características críticas que fundamentan la adopción del patrón offline-first:

- **Conectividad intermitente o inexistente.** Las zonas rurales, áreas de construcción, rutas y parajes frecuentemente carecen de cobertura de red móvil estable. El usuario no puede depender de una conexión permanente para registrar datos.
- **Captura continua sin interrupciones.** El flujo de trabajo de campo exige que el operador capture fotografías, registre coordenadas GPS y añada observaciones de manera fluida, sin que la ausencia de red detenga su labor.
- **Integridad de datos ante desconexiones.** Toda información capturada debe persistir localmente con garantía de durabilidad, independientemente del estado de la red, y sincronizarse cuando la conectividad se restablezca.
- **Trazabilidad completa.** Se requiere un registro auditable de cada operación realizada offline y su posterior sincronización con el servidor.

### 1.2 Contexto de la Industria

El enfoque offline-first constituye un estándar consolidado en aplicaciones de campo y GIS móvil. Las siguientes soluciones comerciales validan la pertinencia del patrón:

| Solución | Fabricante | Estrategia Offline |
|---|---|---|
| **ArcGIS Field Maps** | Esri | Descarga de mapas y capas para uso offline; sincronización bidireccional con ArcGIS Online/Enterprise al recuperar conectividad. |
| **Survey123 for ArcGIS** | Esri | Formularios con almacenamiento local en SQLite; cola de envío (outbox) integrada con reintentos automáticos. |
| **Fulcrum** | Spatial Networks | Base de datos local completa; motor de sincronización con resolución de conflictos last-write-wins y cola de operaciones pendientes. |
| **QField** | OPENGIS.ch | Almacenamiento GeoPackage local; sincronización mediante QFieldCloud con detección de conflictos. |

GeoFoto adopta los principios demostrados por estas plataformas, implementándolos de manera simplificada y adecuada al alcance del proyecto académico, mediante el **Outbox Pattern** sobre SQLite local y una API REST como punto de sincronización.

### 1.3 Principio Fundamental

> **"La aplicación debe funcionar en todo momento como si no existiera conexión a Internet. La sincronización es un proceso secundario, eventual y transparente para el usuario."**

---

## 2. Por qué Outbox Pattern

### 2.1 Alternativas Evaluadas

Se evaluaron cuatro estrategias de sincronización offline, analizando su complejidad, idoneidad y viabilidad para el contexto del proyecto:

#### 2.1.1 CRDTs (Conflict-free Replicated Data Types)

| Aspecto | Valoración |
|---|---|
| **Descripción** | Estructuras de datos que se replican entre nodos y convergen automáticamente sin coordinación central. Cada nodo aplica operaciones localmente y las propaga; la convergencia queda garantizada por propiedades matemáticas. |
| **Ventajas** | Resolución automática de conflictos; no requiere servidor centralizado para coordinación; convergencia eventual garantizada. |
| **Desventajas** | Complejidad algorítmica elevada; requiere modelar cada entidad como un CRDT específico; overhead de metadatos significativo; depuración compleja. |
| **Motivo de descarte** | La complejidad de implementación excede ampliamente las necesidades del proyecto. GeoFoto maneja entidades simples (puntos y fotos) con baja probabilidad de edición concurrente. Los CRDTs aportan garantías innecesarias a un costo de implementación desproporcionado. |

#### 2.1.2 Differential Sync (Sincronización Diferencial)

| Aspecto | Valoración |
|---|---|
| **Descripción** | Algoritmo que mantiene copias shadow en cliente y servidor, calcula diffs entre versiones y aplica patches bidireccionales. Popularizado por Google (Neil Fraser, 2009). |
| **Ventajas** | Eficiente en ancho de banda; manejo granular de cambios a nivel de campo. |
| **Desventajas** | Requiere conexión frecuente para mantener las copias shadow sincronizadas; complejidad en la gestión de estados shadow; difícil de implementar sobre REST convencional. |
| **Motivo de descarte** | La dependencia de una conexión razonablemente frecuente contradice el escenario operativo de GeoFoto, donde la desconexión puede prolongarse horas o días. |

#### 2.1.3 Event Sourcing

| Aspecto | Valoración |
|---|---|
| **Descripción** | Persistencia basada en eventos inmutables que representan cada cambio de estado. El estado actual se reconstruye reproduciendo la secuencia de eventos. |
| **Ventajas** | Auditoría completa; capacidad de reconstruir cualquier estado histórico; natural para sistemas distribuidos. |
| **Desventajas** | Requiere infraestructura de event store; complejidad en proyecciones y snapshots; curva de aprendizaje pronunciada; overhead en almacenamiento local. |
| **Motivo de descarte** | Constituye una sobreingeniería para el volumen y la complejidad de los datos de GeoFoto. El beneficio de auditoría se obtiene de manera más simple con la tabla SyncQueue del Outbox Pattern. |

#### 2.1.4 Outbox Pattern (Seleccionado)

| Aspecto | Valoración |
|---|---|
| **Descripción** | Las operaciones de escritura se registran en una tabla local (outbox/cola de sincronización) junto con la entidad. Un proceso independiente lee la cola y ejecuta las operaciones contra el servidor remoto. |
| **Ventajas** | Simplicidad conceptual y de implementación; operaciones reintentables de manera natural; registro de auditoría integrado; compatible con REST estándar; desacoplamiento entre captura y envío. |
| **Desventajas** | Resolución de conflictos manual (last-write-wins u otra estrategia explícita); no garantiza convergencia automática. |

### 2.2 Decisión Arquitectónica

Se selecciona el **Outbox Pattern** como estrategia de sincronización por las siguientes razones:

1. **Simplicidad.** El patrón se implementa con una tabla SQLite adicional (`SyncQueue`) y un servicio de procesamiento. No requiere librerías ni infraestructura especializada.
2. **Reintentabilidad.** Cada operación en la cola es autocontenida (contiene tipo, entidad, payload) y puede reintentarse de manera independiente sin efectos colaterales.
3. **Trazabilidad.** La tabla `SyncQueue` funciona como log de auditoría, registrando cada operación, su estado, intentos y errores.
4. **Estándar de la industria.** Survey123 y Fulcrum emplean variantes de este mismo patrón, validando su idoneidad para aplicaciones de campo.
5. **Adecuación al alcance.** Para un proyecto académico con dos entidades principales (Punto y Foto), el Outbox Pattern ofrece el balance óptimo entre funcionalidad y complejidad.

---

## 3. Máquina de Estados de SyncStatus

### 3.1 Diagrama de Estados

```
  ┌─────────┐
  │  Local  │──────── sync exitoso ──────────►┌─────────┐
  └────┬────┘                                  │ Synced  │
       │                                       └────┬────┘
       │ modificación local                         │ modificación desde
       │ (crear/editar/eliminar)                    │ otro dispositivo
       ▼                                            ▼
┌──────────────┐                            ┌───────────────┐
│PendingCreate │── sync OK ──────►Synced    │ PendingUpdate │── sync OK ──►Synced
│PendingUpdate │── conflict ─────►Conflict  │               │
│PendingDelete │── sync OK ──────►(borrado) └───────────────┘
└──────┬───────┘
       │
       │ fallo x3
       ▼
┌──────────┐
│  Failed  │── reintento manual ──►PendingXxx
└──────────┘

┌──────────┐
│ Conflict │── resolución ────────►Synced / PendingUpdate
└──────────┘
```

### 3.2 Descripción de Estados

| Estado | Código | Descripción |
|---|---|---|
| **Local** | `Local` | La entidad existe únicamente en el dispositivo. No se ha intentado ninguna sincronización. Es el estado inicial de toda entidad creada offline. |
| **PendingCreate** | `PendingCreate` | Se ha encolado una operación de creación en `SyncQueue`. La entidad aguarda su envío al servidor mediante `POST`. |
| **PendingUpdate** | `PendingUpdate` | Se ha encolado una operación de actualización en `SyncQueue`. La entidad modificada aguarda su envío mediante `PUT`. |
| **PendingDelete** | `PendingDelete` | Se ha encolado una operación de eliminación en `SyncQueue`. La entidad aguarda la confirmación de borrado mediante `DELETE`. |
| **Synced** | `Synced` | La entidad se encuentra sincronizada con el servidor. Los datos locales coinciden con los remotos. El `RemoteId` ha sido asignado. |
| **Conflict** | `Conflict` | Se ha detectado un conflicto durante la sincronización (respuesta HTTP 409). Se requiere aplicar la estrategia de resolución configurada. |
| **Failed** | `Failed` | La operación ha superado el número máximo de reintentos (3 intentos). Se requiere intervención manual del usuario o del administrador. |

### 3.3 Transiciones de Estado

| Transición | Origen | Destino | Evento Disparador |
|---|---|---|---|
| T1 | Local | PendingCreate | El usuario crea una entidad y se encola la operación de sincronización. |
| T2 | PendingCreate | Synced | El servidor responde con HTTP 2xx. Se asigna el `RemoteId` devuelto. |
| T3 | Synced | PendingUpdate | El usuario modifica una entidad previamente sincronizada. Se encola operación de actualización. |
| T4 | PendingUpdate | Synced | El servidor acepta la actualización con HTTP 2xx. |
| T5 | Synced | PendingDelete | El usuario elimina una entidad previamente sincronizada. Se encola operación de eliminación. |
| T6 | PendingDelete | (eliminado) | El servidor confirma la eliminación con HTTP 2xx. Se elimina la entidad local y la entrada de la cola. |
| T7 | PendingCreate / PendingUpdate | Conflict | El servidor responde con HTTP 409 (conflicto de versión). |
| T8 | Conflict | Synced | Se aplica la estrategia de resolución (Last-Write-Wins) y el servidor acepta la versión resultante. |
| T9 | Conflict | PendingUpdate | La resolución genera una nueva versión local que debe sincronizarse. |
| T10 | PendingCreate / PendingUpdate / PendingDelete | Failed | El número de intentos alcanza el máximo configurado (3) sin obtener respuesta exitosa. |
| T11 | Failed | PendingCreate / PendingUpdate / PendingDelete | El usuario solicita reintento manual desde la pantalla de Estado de Sincronización. Se reinicia el contador de intentos. |

---

## 4. Flujo Push Completo

### 4.1 Diagrama de Secuencia

```
┌───────────────────┐    ┌─────────────┐    ┌────────────┐    ┌──────────┐
│ConnectivityService│    │ SyncService  │    │  SyncQueue  │    │  API REST│
└────────┬──────────┘    └──────┬───────┘    └─────┬──────┘    └────┬─────┘
         │                      │                   │                │
    ─────┤ (1) Detecta red      │                   │                │
         │  disponible          │                   │                │
         │─────────────────────►│                   │                │
         │  OnConnectivityChanged                   │                │
         │                      │                   │                │
         │                 (2)  │──────────────────►│                │
         │                      │ GetPending        │                │
         │                      │ (orderBy:CreatedAt)                │
         │                      │◄──────────────────│                │
         │                      │  List<SyncOp>     │                │
         │                      │                   │                │
         │                      │ ─── FOREACH op ───────────────────│
         │                      │                   │                │
         │                 (3)  │ BuildRequest(op)  │                │
         │                      │──────────────────────────────────►│
         │                 (4)  │ POST/PUT/DELETE   │                │
         │                      │   /api/puntos     │                │
         │                      │   /api/fotos      │                │
         │                      │◄──────────────────────────────────│
         │                      │   HTTP Response   │                │
         │                      │                   │                │
         │                 (5)  │ [Si 2xx]          │                │
         │                      │──────────────────►│                │
         │                      │ Status = Done     │                │
         │                      │ UpdateRemoteId    │                │
         │                      │                   │                │
         │                 (6)  │ [Si 409 Conflict] │                │
         │                      │ ApplyLWW(op,resp) │                │
         │                      │──────────────────►│                │
         │                      │ Status = Synced   │                │
         │                      │  o PendingUpdate  │                │
         │                      │                   │                │
         │                 (7)  │ [Si 5xx/timeout]  │                │
         │                      │──────────────────►│                │
         │                      │ Attempts++        │                │
         │                      │ ScheduleRetry     │                │
         │                      │ (backoff)         │                │
         │                      │                   │                │
         │                 (8)  │ [Si Attempts >= 3]│                │
         │                      │──────────────────►│                │
         │                      │ Status = Failed   │                │
         │                      │                   │                │
         │                      │ ─── END FOREACH ──────────────────│
         │                      │                   │                │
```

### 4.2 Descripción Paso a Paso

1. **Detección de conectividad.** `ConnectivityService` detecta un cambio en el estado de la red mediante el evento `Connectivity.ConnectivityChanged` de .NET MAUI. Si el nuevo estado incluye acceso a Internet (`NetworkAccess.Internet`), se invoca al `SyncService`.

2. **Lectura de la cola.** `SyncService` consulta la tabla `SyncQueue` obteniendo todas las operaciones con estado `Pending`, ordenadas ascendentemente por `CreatedAt` para respetar el orden cronológico de captura.

3. **Construcción de la solicitud HTTP.** Para cada operación en la cola, se construye un `HttpRequestMessage` con el método correspondiente (`POST` para Create, `PUT` para Update, `DELETE` para Delete), la URL del endpoint y el payload serializado en JSON.

4. **Envío al servidor.** Se ejecuta la solicitud HTTP contra el endpoint correspondiente de la API REST:
   - `POST /api/puntos` — creación de punto georreferenciado.
   - `PUT /api/puntos/{id}` — actualización de punto existente.
   - `DELETE /api/puntos/{id}` — eliminación de punto.
   - `POST /api/fotos` — envío de fotografía (multipart/form-data).
   - Análogamente para las demás entidades.

5. **Respuesta exitosa (HTTP 2xx).** Se marca la operación como `Done` en la cola. Si se trata de una creación, se actualiza el `RemoteId` en la entidad local con el identificador devuelto por el servidor. Se actualiza el `SyncStatus` de la entidad a `Synced`.

6. **Conflicto (HTTP 409).** Se aplica la estrategia de resolución **Last-Write-Wins**: se compara el `UpdatedAt` local con el del servidor. La versión con timestamp más reciente prevalece. Si la versión local prevalece, se reenvía con el flag de forzado. Si la del servidor prevalece, se actualiza la entidad local.

7. **Error de servidor o timeout (HTTP 5xx / timeout).** Se incrementa el contador `Attempts` de la operación. Se registra el `ErrorMessage` devuelto. Se calcula el próximo reintento aplicando la estrategia de backoff exponencial y se programa mediante `ScheduleRetry`.

8. **Máximo de reintentos superado.** Si `Attempts >= 3`, la operación se marca como `Failed`. El `SyncStatus` de la entidad asociada se actualiza a `Failed`. Se registra el error para su visualización en la pantalla de estado de sincronización.

---

## 5. Flujo Pull Completo

### 5.1 Diagrama de Secuencia

```
┌─────────────┐    ┌────────────┐    ┌──────────┐    ┌───────────┐
│ SyncService  │    │  API REST  │    │ SQLite   │    │Preferences│
└──────┬───────┘    └─────┬──────┘    └────┬─────┘    └─────┬─────┘
       │                  │               │                 │
  (1)  │ Push completado  │               │                 │
       │                  │               │                 │
  (2)  │─────────────────►│               │                 │
       │ GET /api/sync/   │               │                 │
       │ delta?since=     │               │                 │
       │ {lastSync}       │               │                 │
       │◄─────────────────│               │                 │
       │ DeltaResponse    │               │                 │
       │  { puntos[], fotos[], timestamp }│                 │
       │                  │               │                 │
       │ ── FOREACH entity ──             │                 │
       │                  │               │                 │
  (3)  │──────────────────────────────────►│                 │
       │ SELECT * WHERE RemoteId = ?      │                 │
       │◄─────────────────────────────────│                 │
       │                  │               │                 │
  (4)  │ [Si no existe localmente]        │                 │
       │──────────────────────────────────►│                 │
       │ INSERT entity    │               │                 │
       │ SyncStatus=Synced│               │                 │
       │                  │               │                 │
  (5)  │ [Si existe: comparar UpdatedAt]  │                 │
       │ [Server más reciente]            │                 │
       │──────────────────────────────────►│                 │
       │ UPDATE entity    │               │                 │
       │ SyncStatus=Synced│               │                 │
       │                  │               │                 │
       │ ── END FOREACH ──│               │                 │
       │                  │               │                 │
  (6)  │─────────────────────────────────────────────────────►│
       │ Save lastSync = response.timestamp                  │
       │                  │               │                 │
```

### 5.2 Descripción Paso a Paso

1. **Activación del pull.** Una vez que el flujo push ha finalizado (todas las operaciones pendientes han sido procesadas o marcadas como `Failed`), el `SyncService` inicia la fase de pull para obtener cambios remotos.

2. **Solicitud de delta.** Se envía una solicitud `GET /api/sync/delta?since={lastSync}` a la API REST. El parámetro `lastSync` contiene el timestamp UTC de la última sincronización exitosa, almacenado en `Preferences` (almacenamiento de clave-valor de MAUI). En la primera ejecución, `lastSync` es `DateTime.MinValue`, lo que equivale a solicitar todos los datos.

3. **Verificación de existencia local.** Para cada entidad recibida en la respuesta delta, se consulta la base de datos local buscando por `RemoteId`. Se determina si se trata de una entidad nueva o de una actualización de una existente.

4. **Inserción de entidad nueva.** Si la entidad no existe localmente (no se encuentra por `RemoteId`), se inserta en la tabla correspondiente con `SyncStatus = Synced` y se asocia el `RemoteId` recibido del servidor.

5. **Actualización de entidad existente.** Si la entidad ya existe localmente, se comparan los timestamps `UpdatedAt`:
   - Si el `UpdatedAt` del servidor es más reciente que el local, se actualizan los campos de la entidad local con los valores del servidor y se establece `SyncStatus = Synced`.
   - Si el `UpdatedAt` local es más reciente (la entidad fue modificada offline después del pull), se preserva la versión local. Esta situación se resolverá en el próximo ciclo de push.
   - Si la entidad local tiene estado `Pending*`, no se sobrescribe para no perder cambios no sincronizados.

6. **Actualización del timestamp de sincronización.** Al completar exitosamente el procesamiento de todas las entidades del delta, se actualiza el valor de `lastSync` en `Preferences` con el timestamp devuelto por el servidor en la respuesta. Este timestamp se utilizará como parámetro `since` en la próxima solicitud delta.

---

## 6. Algoritmo de Push (Pseudocódigo)

```
async ProcessPushQueue():
    if not ConnectivityService.HasInternet:
        return

    queue = await db.SyncQueue
        .Where(op => op.Status == Pending)
        .OrderBy(op => op.CreatedAt)
        .ToListAsync()

    if queue.IsEmpty:
        return

    foreach op in queue:
        op.Status = InProgress
        await db.UpdateAsync(op)

        try:
            request = BuildHttpRequest(op)
            // POST para Create, PUT para Update, DELETE para Delete
            // URL: /api/{op.EntityType.ToLower()}s/{op.RemoteId}
            // Body: op.Payload (JSON serializado)

            response = await httpClient.SendAsync(request, timeout: 30s)

            if response.IsSuccessStatusCode:          // 2xx
                op.Status = Done
                op.CompletedAt = DateTime.UtcNow

                if op.OperationType == Create:
                    remoteEntity = await response.DeserializeAsync()
                    UpdateLocalEntity(op.EntityType, op.LocalId,
                        remoteId: remoteEntity.Id,
                        syncStatus: Synced)
                else if op.OperationType == Update:
                    UpdateLocalEntity(op.EntityType, op.LocalId,
                        syncStatus: Synced)
                else if op.OperationType == Delete:
                    DeleteLocalEntity(op.EntityType, op.LocalId)

            else if response.StatusCode == 409:       // Conflict
                serverEntity = await response.DeserializeAsync()
                ResolveConflict(op, serverEntity)

            else:                                     // 4xx (no 409), 5xx
                throw new HttpSyncException(response.StatusCode,
                    await response.Content.ReadAsStringAsync())

        catch HttpSyncException ex:
            op.Attempts++
            op.LastAttemptAt = DateTime.UtcNow
            op.ErrorMessage = $"{ex.StatusCode}: {ex.Message}"

            if op.Attempts >= MAX_ATTEMPTS:           // MAX_ATTEMPTS = 3
                op.Status = Failed
                UpdateLocalEntity(op.EntityType, op.LocalId,
                    syncStatus: Failed)
                LogWarning($"Operación {op.Id} marcada como Failed " +
                           $"tras {op.Attempts} intentos")
            else:
                op.Status = Pending
                backoff = CalculateBackoff(op.Attempts)
                op.NextRetryAt = DateTime.UtcNow + backoff
                LogInfo($"Reintento {op.Attempts} programado " +
                        $"en {backoff.TotalSeconds}s")

        catch TaskCanceledException:                  // Timeout
            op.Attempts++
            op.LastAttemptAt = DateTime.UtcNow
            op.ErrorMessage = "Timeout de conexión"
            op.Status = (op.Attempts >= MAX_ATTEMPTS) ? Failed : Pending

        finally:
            await db.UpdateAsync(op)

        await Task.Delay(THROTTLE_DELAY)              // 100ms entre operaciones


function BuildHttpRequest(op: SyncOperation) -> HttpRequestMessage:
    method = op.OperationType switch:
        Create => HttpMethod.Post
        Update => HttpMethod.Put
        Delete => HttpMethod.Delete

    url = op.OperationType switch:
        Create => $"/api/{op.EntityType.ToLower()}s"
        Update => $"/api/{op.EntityType.ToLower()}s/{op.RemoteId}"
        Delete => $"/api/{op.EntityType.ToLower()}s/{op.RemoteId}"

    request = new HttpRequestMessage(method, url)

    if op.OperationType != Delete:
        request.Content = new StringContent(op.Payload,
            Encoding.UTF8, "application/json")

    return request


function ResolveConflict(op: SyncOperation, serverEntity: Entity):
    localEntity = await db.GetByLocalId(op.EntityType, op.LocalId)

    if localEntity.UpdatedAt > serverEntity.UpdatedAt:
        // Local gana (Last-Write-Wins): reenviar con flag de forzado
        op.Status = Pending
        op.Payload = SerializeWithForceFlag(localEntity)
        LogInfo("Conflicto resuelto: versión local prevalece")
    else:
        // Servidor gana: actualizar local con datos del servidor
        UpdateLocalEntity(op.EntityType, op.LocalId,
            data: serverEntity,
            syncStatus: Synced)
        op.Status = Done
        LogInfo("Conflicto resuelto: versión del servidor prevalece")


function CalculateBackoff(attempt: int) -> TimeSpan:
    return attempt switch:
        1 => TimeSpan.FromSeconds(5)
        2 => TimeSpan.FromSeconds(30)
        3 => TimeSpan.FromMinutes(5)
        _ => TimeSpan.FromMinutes(5)
```

---

## 7. Algoritmo de Pull (Pseudocódigo)

```
async ProcessPullDelta():
    if not ConnectivityService.HasInternet:
        return

    lastSync = Preferences.Get<DateTime>("lastSyncTimestamp",
        DateTime.MinValue)

    try:
        response = await httpClient.GetAsync(
            $"/api/sync/delta?since={lastSync:o}",
            timeout: 60s)

        if not response.IsSuccessStatusCode:
            LogWarning($"Pull delta falló: {response.StatusCode}")
            return

        delta = await response.DeserializeAsync<DeltaResponse>()
        // DeltaResponse: { Puntos[], Fotos[], Timestamp }

        await ProcessDeltaEntities("Punto", delta.Puntos)
        await ProcessDeltaEntities("Foto", delta.Fotos)

        // Actualizar timestamp solo si todo el procesamiento fue exitoso
        Preferences.Set("lastSyncTimestamp", delta.Timestamp)
        LogInfo($"Pull delta completado. {delta.Puntos.Count} puntos, " +
                $"{delta.Fotos.Count} fotos procesados")

    catch HttpRequestException ex:
        LogWarning($"Error de red durante pull: {ex.Message}")
    catch TaskCanceledException:
        LogWarning("Timeout durante pull delta")


async ProcessDeltaEntities(entityType: string, entities: List<Entity>):
    foreach remoteEntity in entities:
        localEntity = await db.GetByRemoteId(entityType,
            remoteEntity.Id)

        if localEntity == null:
            // Caso A: Entidad nueva — insertar
            newEntity = MapFromRemote(remoteEntity)
            newEntity.RemoteId = remoteEntity.Id
            newEntity.SyncStatus = Synced
            newEntity.UpdatedAt = remoteEntity.UpdatedAt
            await db.InsertAsync(newEntity)
            LogInfo($"Nueva entidad {entityType} insertada " +
                    $"desde servidor: {remoteEntity.Id}")

        else if localEntity.SyncStatus in [PendingCreate,
                PendingUpdate, PendingDelete]:
            // Caso B: Entidad con cambios locales pendientes — no sobrescribir
            LogInfo($"Entidad {entityType} {localEntity.Id} tiene " +
                    $"cambios pendientes. Se omite actualización del pull.")
            continue

        else if remoteEntity.UpdatedAt > localEntity.UpdatedAt:
            // Caso C: Servidor más reciente — actualizar local
            UpdateFieldsFromRemote(localEntity, remoteEntity)
            localEntity.SyncStatus = Synced
            localEntity.UpdatedAt = remoteEntity.UpdatedAt
            await db.UpdateAsync(localEntity)
            LogInfo($"Entidad {entityType} {localEntity.Id} " +
                    $"actualizada desde servidor")

        else:
            // Caso D: Local igual o más reciente — no se modifica
            LogDebug($"Entidad {entityType} {localEntity.Id} " +
                     $"ya está actualizada")

        if remoteEntity.IsDeleted:
            // Caso E: Entidad eliminada en servidor
            if localEntity != null and
               localEntity.SyncStatus not in [PendingUpdate]:
                await db.DeleteAsync(localEntity)
                LogInfo($"Entidad {entityType} {localEntity.Id} " +
                        $"eliminada por indicación del servidor")


function MapFromRemote(remoteEntity: Entity) -> LocalEntity:
    return new LocalEntity:
        Descripcion = remoteEntity.Descripcion
        Latitud = remoteEntity.Latitud
        Longitud = remoteEntity.Longitud
        FechaCaptura = remoteEntity.FechaCaptura
        RemoteId = remoteEntity.Id
        // Los campos locales (LocalId) se generan automáticamente
```

---

## 8. Tabla de Casos de Resolución de Conflictos

### 8.1 Estrategia General

GeoFoto implementa la estrategia **Last-Write-Wins (LWW)** como mecanismo principal de resolución de conflictos. Esta estrategia compara los timestamps `UpdatedAt` de la versión local y la versión del servidor, prevaleciendo la más reciente.

Se selecciona LWW por su simplicidad de implementación y por la naturaleza de los datos de GeoFoto: las entidades representan observaciones de campo donde la última modificación es generalmente la más relevante.

### 8.2 Matriz de Escenarios

| # | Escenario | Local `UpdatedAt` | Server `UpdatedAt` | Resultado | Acción Detallada |
|---|---|---|---|---|---|
| 1 | **Local más reciente** | 2026-04-13 15:30 UTC | 2026-04-13 14:00 UTC | Prevalece la versión local | Se reenvía la operación con el flag `X-Force-Overwrite: true`. El servidor acepta la versión local y actualiza su registro. La operación se marca como `Done` / `Synced`. |
| 2 | **Servidor más reciente** | 2026-04-13 10:00 UTC | 2026-04-13 12:45 UTC | Prevalece la versión del servidor | Se descartan los cambios locales pendientes. Se actualiza la entidad local con los datos del servidor. La operación en la cola se marca como `Done`. El `SyncStatus` se establece en `Synced`. |
| 3 | **Timestamps idénticos** | 2026-04-13 14:00 UTC | 2026-04-13 14:00 UTC | Prevalece la versión del servidor | Ante igualdad de timestamps, se adopta la convención de que el servidor es la fuente de verdad. Se actualiza la entidad local con los datos del servidor. Se registra un log informativo. |
| 4 | **Eliminado localmente, modificado en servidor** | (PendingDelete) | 2026-04-13 16:00 UTC | Prevalece la eliminación local | La intención explícita de eliminar del usuario prevalece. Se reenvía la operación `DELETE`. Si el servidor devuelve 404 (ya eliminado), se marca como `Done`. |
| 5 | **Modificado localmente, eliminado en servidor** | 2026-04-13 15:00 UTC | (IsDeleted = true) | Se preserva la versión local | Si el usuario modificó la entidad localmente y el servidor la eliminó, se considera que la modificación local expresa la intención del usuario de mantener la entidad. Se envía como `Create` para recrearla en el servidor. |
| 6 | **Creado offline, ya existe en servidor (duplicado)** | (PendingCreate) | Existe con datos similares | Se vincula con la entidad existente | Se asigna el `RemoteId` del servidor a la entidad local. Se aplica LWW sobre los campos que difieran. Se marca como `Synced`. Se evita la duplicación. |
| 7 | **Múltiples modificaciones offline antes de sync** | Varias ediciones acumuladas | Sin cambios | Prevalece la última versión local | Se envía únicamente el estado final de la entidad (no las versiones intermedias). La cola contiene la operación más reciente, las anteriores se consolidan. |
| 8 | **Foto modificada en metadatos, binario en servidor** | Metadatos editados offline | Binario reemplazado en servidor | Prevalece la versión del servidor | Las fotos son datos binarios inmutables. Si el servidor tiene una versión distinta del binario, se descarga la versión del servidor y se actualizan los metadatos locales. |

### 8.3 Limitaciones de la Estrategia

- **Pérdida potencial de datos.** LWW puede descartar cambios válidos si dos usuarios modifican la misma entidad simultáneamente. Este riesgo se considera aceptable dado que GeoFoto está diseñado para uso individual (un dispositivo por usuario) con baja probabilidad de edición concurrente sobre la misma entidad.
- **Dependencia de relojes sincronizados.** LWW asume que los timestamps son comparables. Se mitiga utilizando UTC en todas las marcas temporales y obteniendo el timestamp del servidor para las operaciones remotas.

---

## 9. Estrategia de Backoff Exponencial

### 9.1 Tabla de Reintentos

| Intento | Espera antes del reintento | Tiempo acumulado | Descripción |
|---|---|---|---|
| 1 | 5 segundos | 5 segundos | Primer reintento inmediato con espera mínima. Cubre errores transitorios de red (picos de latencia, DNS temporal). |
| 2 | 30 segundos | 35 segundos | Segundo reintento con espera moderada. Permite la recuperación de problemas de conectividad intermitente o sobrecarga momentánea del servidor. |
| 3 | 5 minutos | 5 minutos 35 segundos | Tercer y último reintento automático. Espera prolongada para permitir la resolución de problemas de infraestructura (reinicio de servidor, restauración de red). |

### 9.2 Comportamiento Post-Fallo

Tras el tercer intento fallido, la operación transiciona al estado `Failed`:

- El campo `Status` se establece en `Failed`.
- El campo `ErrorMessage` contiene el detalle del último error.
- El `SyncStatus` de la entidad asociada se actualiza a `Failed`.
- **No se realizan reintentos automáticos adicionales.**

La recuperación requiere intervención manual:

1. El usuario accede a la pantalla **Estado de Sincronización**.
2. Se visualizan las operaciones fallidas con su detalle de error.
3. El usuario puede seleccionar **"Reintentar"**, lo que reinicia el contador `Attempts` a 0 y establece `Status = Pending`.
4. En el siguiente ciclo de sincronización, la operación se procesa nuevamente.

### 9.3 Fórmula de Backoff

```
backoff(n) = n switch {
    1 => 5s,
    2 => 30s,
    3 => 300s (5 min),
    _ => 300s (cap máximo)
}
```

Se utiliza una tabla fija en lugar de una fórmula exponencial pura (`2^n * base`) para mantener la simplicidad y la previsibilidad del comportamiento. Los valores seleccionados cubren adecuadamente los escenarios típicos de fallos transitorios en redes móviles.

---

## 10. Descripción Completa de la Tabla SyncQueue

### 10.1 Estructura de la Tabla

La tabla `SyncQueue` constituye el componente central del Outbox Pattern. Almacena cada operación de escritura pendiente de sincronización con el servidor.

```sql
CREATE TABLE SyncQueue (
    Id                INTEGER PRIMARY KEY AUTOINCREMENT,
    OperationType     TEXT    NOT NULL,   -- 'Create' | 'Update' | 'Delete'
    EntityType        TEXT    NOT NULL,   -- 'Punto' | 'Foto'
    LocalId           INTEGER NOT NULL,   -- FK a la tabla local de la entidad
    RemoteId          TEXT    NULL,       -- Id del servidor (GUID), null si no sincronizado
    Payload           TEXT    NOT NULL,   -- JSON serializado de la entidad
    Status            TEXT    NOT NULL DEFAULT 'Pending',
                                          -- 'Pending' | 'InProgress' | 'Done' | 'Failed'
    Attempts          INTEGER NOT NULL DEFAULT 0,
    MaxAttempts       INTEGER NOT NULL DEFAULT 3,
    LastAttemptAt     TEXT    NULL,       -- DateTime UTC ISO 8601
    NextRetryAt       TEXT    NULL,       -- DateTime UTC ISO 8601
    ErrorMessage      TEXT    NULL,       -- Detalle del último error
    CreatedAt         TEXT    NOT NULL,   -- DateTime UTC ISO 8601
    CompletedAt       TEXT    NULL        -- DateTime UTC ISO 8601
);

CREATE INDEX IX_SyncQueue_Status ON SyncQueue(Status);
CREATE INDEX IX_SyncQueue_CreatedAt ON SyncQueue(CreatedAt);
CREATE INDEX IX_SyncQueue_EntityType_LocalId ON SyncQueue(EntityType, LocalId);
```

### 10.2 Diccionario de Campos

| Campo | Tipo SQLite | Tipo C# | Nullable | Descripción |
|---|---|---|---|---|
| `Id` | INTEGER PK | `int` | No | Identificador único autoincremental de la operación en la cola. |
| `OperationType` | TEXT | `string` (enum) | No | Tipo de operación a ejecutar contra el servidor. Valores válidos: `Create`, `Update`, `Delete`. Determina el método HTTP utilizado (POST, PUT, DELETE). |
| `EntityType` | TEXT | `string` (enum) | No | Tipo de entidad afectada por la operación. Valores válidos: `Punto`, `Foto`. Determina el endpoint de la API a invocar. |
| `LocalId` | INTEGER | `int` | No | Clave foránea al registro de la entidad en la tabla local correspondiente (`Puntos` o `Fotos`). Permite vincular la operación con la entidad afectada. |
| `RemoteId` | TEXT | `string?` | Sí | Identificador de la entidad en el servidor (GUID). Es `null` para operaciones `Create` hasta que el servidor devuelve el identificador asignado. Para operaciones `Update` y `Delete`, contiene el identificador remoto necesario para construir la URL del endpoint. |
| `Payload` | TEXT | `string` | No | Representación JSON serializada de la entidad en el momento de la encolación. Contiene todos los campos necesarios para que el servidor procese la operación. Para operaciones `Delete`, contiene un JSON mínimo con el identificador. |
| `Status` | TEXT | `string` (enum) | No | Estado actual de la operación en la cola. Valores: `Pending` (pendiente de envío), `InProgress` (en proceso de envío), `Done` (enviada exitosamente), `Failed` (superó el máximo de reintentos). Valor por defecto: `Pending`. |
| `Attempts` | INTEGER | `int` | No | Número de intentos de envío realizados. Se incrementa en cada intento fallido. Cuando alcanza el valor de `MaxAttempts`, la operación transiciona a `Failed`. Valor por defecto: `0`. |
| `MaxAttempts` | INTEGER | `int` | No | Número máximo de intentos permitidos antes de marcar la operación como `Failed`. Valor por defecto: `3`. Configurable para permitir políticas de reintento diferenciadas. |
| `LastAttemptAt` | TEXT | `DateTime?` | Sí | Fecha y hora UTC del último intento de envío, en formato ISO 8601. Es `null` si no se ha realizado ningún intento. Se utiliza para calcular el próximo reintento según la estrategia de backoff. |
| `NextRetryAt` | TEXT | `DateTime?` | Sí | Fecha y hora UTC del próximo reintento programado, en formato ISO 8601. Se calcula como `LastAttemptAt + backoff(Attempts)`. Las operaciones con `NextRetryAt` en el futuro se omiten en el ciclo de procesamiento actual. |
| `ErrorMessage` | TEXT | `string?` | Sí | Mensaje descriptivo del último error ocurrido durante el intento de envío. Incluye el código de estado HTTP y el cuerpo de la respuesta de error. Es `null` si no se han producido errores. |
| `CreatedAt` | TEXT | `DateTime` | No | Fecha y hora UTC en que la operación fue encolada, en formato ISO 8601. Se establece en el momento de la inserción y no se modifica. Determina el orden de procesamiento de la cola (FIFO). |
| `CompletedAt` | TEXT | `DateTime?` | Sí | Fecha y hora UTC en que la operación se completó exitosamente (transicionó a `Done`), en formato ISO 8601. Es `null` mientras la operación se encuentra en estados `Pending`, `InProgress` o `Failed`. |

### 10.3 Modelo C# Correspondiente

```csharp
[Table("SyncQueue")]
public class SyncOperation
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string OperationType { get; set; }   // "Create" | "Update" | "Delete"
    public string EntityType { get; set; }       // "Punto" | "Foto"
    public int LocalId { get; set; }
    public string? RemoteId { get; set; }
    public string Payload { get; set; }
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; } = 0;
    public int MaxAttempts { get; set; } = 3;
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
```

---

## 11. Integración con Connectivity.ConnectivityChanged

### 11.1 Mecanismo de Detección

.NET MAUI proporciona la clase `Microsoft.Maui.Networking.Connectivity` con el evento `ConnectivityChanged`, que se dispara cada vez que el estado de la conexión de red del dispositivo cambia. GeoFoto utiliza este evento como disparador principal del motor de sincronización.

### 11.2 Servicio de Conectividad

```csharp
public class ConnectivityService : IConnectivityService, IDisposable
{
    private readonly ISyncService _syncService;
    private CancellationTokenSource? _debounceCts;
    private const int DEBOUNCE_MS = 2000; // 2 segundos de debounce

    public bool HasInternet =>
        Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public ConnectivityService(ISyncService syncService)
    {
        _syncService = syncService;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private async void OnConnectivityChanged(object? sender,
        ConnectivityChangedEventArgs e)
    {
        // Cancelar debounce anterior si existe
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        try
        {
            // Debounce: esperar estabilización de la conexión
            await Task.Delay(DEBOUNCE_MS, token);

            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                // Red disponible: iniciar sincronización
                await _syncService.SyncAsync(token);
            }
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelado por nuevo evento: ignorar
        }
    }

    public void Dispose()
    {
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
    }
}
```

### 11.3 Estrategia de Debounce

El evento `ConnectivityChanged` puede dispararse múltiples veces en rápida sucesión cuando la red es inestable (por ejemplo, al entrar y salir de una zona con cobertura). Para evitar iniciar múltiples procesos de sincronización concurrentes, se implementa una estrategia de **debounce de 2 segundos**:

1. Al recibir el evento, se cancela cualquier temporizador de debounce anterior.
2. Se inicia un nuevo temporizador de 2 segundos.
3. Si durante esos 2 segundos se recibe otro evento, el temporizador se reinicia.
4. Solo cuando transcurren 2 segundos sin nuevos eventos se procede con la sincronización.

Este mecanismo garantiza que:
- No se inicien sincronizaciones duplicadas.
- Se espera a que la conexión se estabilice antes de intentar sincronizar.
- Los recursos del dispositivo no se desperdician en intentos sobre conexiones transitorias.

### 11.4 Registro del Servicio

```csharp
// En MauiProgram.cs
builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
builder.Services.AddSingleton<ISyncService, SyncService>();
```

El servicio se registra como **Singleton** para garantizar que existe una única instancia escuchando los eventos de conectividad durante todo el ciclo de vida de la aplicación.

---

## 12. Consideraciones de Performance

### 12.1 Tamaño de Lote (Batch Size)

Se establece un tamaño máximo de **50 operaciones por ciclo de sincronización**. Esta limitación persigue los siguientes objetivos:

- **Prevención de bloqueo de UI.** Aunque la sincronización se ejecuta en un hilo secundario, un lote excesivamente grande podría saturar el ancho de banda disponible y degradar la experiencia del usuario si está navegando la aplicación simultáneamente.
- **Recuperabilidad.** En caso de interrupción de la conexión durante un ciclo de sincronización, se limita el número de operaciones que podrían quedar en estado `InProgress` inconsistente.
- **Equidad de recursos.** En redes móviles, los lotes reducidos permiten que otras aplicaciones del dispositivo compartan el ancho de banda disponible.

Si la cola contiene más de 50 operaciones, se procesan las primeras 50 (ordenadas por `CreatedAt`) y las restantes se procesan en el siguiente ciclo de sincronización.

### 12.2 Throttling entre Operaciones

Se introduce una pausa de **100 milisegundos entre operaciones** consecutivas dentro de un lote:

```csharp
await Task.Delay(THROTTLE_DELAY); // 100ms
```

Esta pausa cumple las siguientes funciones:
- **Protección de la API.** Se evita saturar el servidor con ráfagas de solicitudes concurrentes desde múltiples dispositivos sincronizando simultáneamente.
- **Regulación de ancho de banda.** En redes móviles con ancho de banda limitado, el throttling distribuye la carga de manera uniforme.
- **Observabilidad.** La pausa permite que el sistema de logging registre cada operación de manera ordenada.

### 12.3 Carga de Fotografías

Las fotografías se gestionan como operaciones separadas del flujo principal de sincronización debido a su tamaño:

- **Envío independiente.** Las operaciones de tipo `Foto` se procesan después de las operaciones de tipo `Punto`, asegurando que los puntos georreferenciados existan en el servidor antes de asociarles fotografías.
- **Multipart upload.** Las fotos se envían mediante `POST multipart/form-data` al endpoint `/api/fotos`, transmitiendo el archivo binario junto con los metadatos (PuntoId, descripción, coordenadas).
- **Sin compresión adicional.** Las fotografías se almacenan ya comprimidas en formato JPEG. No se aplica compresión adicional durante la sincronización.
- **Timeout extendido.** Las operaciones de carga de fotos utilizan un timeout de 60 segundos (vs. 30 segundos para las operaciones de datos), considerando el mayor volumen de transferencia.

### 12.4 Limpieza de la Cola (Cleanup)

Las entradas completadas de la tabla `SyncQueue` se eliminan periódicamente para evitar el crecimiento indefinido de la base de datos local:

- **Política de retención.** Las operaciones con `Status = Done` y `CompletedAt` anterior a **7 días** se eliminan automáticamente.
- **Momento de ejecución.** La limpieza se ejecuta al inicio de cada ciclo de sincronización, antes del procesamiento de la cola.
- **Preservación de fallos.** Las operaciones con `Status = Failed` **no se eliminan** automáticamente, ya que requieren atención del usuario.

```csharp
async Task CleanupCompletedOperations():
    cutoffDate = DateTime.UtcNow.AddDays(-7)
    await db.SyncQueue
        .Where(op => op.Status == "Done" && op.CompletedAt < cutoffDate)
        .DeleteAsync()
```

---

## 13. Observabilidad

### 13.1 Estrategia de Logging

El motor de sincronización registra eventos en tres niveles de severidad, utilizando `Microsoft.Extensions.Logging.ILogger<SyncService>`:

| Nivel | Eventos Registrados | Ejemplo |
|---|---|---|
| **Information** | Inicio y fin de ciclo de sincronización, operaciones completadas, conflictos resueltos, pull delta completado. | `"Sync push completado: 12 operaciones procesadas, 1 conflicto resuelto"` |
| **Warning** | Reintentos programados, operaciones marcadas como `Failed`, errores de conectividad durante sincronización, conflictos detectados. | `"Operación #45 (Create Punto) falló tras 3 intentos: 503 Service Unavailable"` |
| **Error** | Excepciones no controladas en el motor de sincronización, errores de serialización, inconsistencias en la base de datos local. | `"Error crítico en ProcessPushQueue: NullReferenceException en BuildRequest"` |

### 13.2 Pantalla de Estado de Sincronización

La pantalla **EstadoSync** proporciona al usuario visibilidad completa sobre el estado de la sincronización. Se implementa con componentes MudBlazor:

#### 13.2.1 Panel de Resumen (MudCards)

Se presentan cuatro tarjetas con indicadores numéricos que resumen el estado global de la sincronización:

```
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│   📤 Pendientes │  │   ✅ Sincroniz. │  │   ⚠️ Conflictos │  │   ❌ Fallidas   │
│                 │  │                 │  │                 │  │                 │
│       7         │  │      143        │  │       1         │  │       2         │
│                 │  │                 │  │                 │  │                 │
│  Última sync:   │  │                 │  │  Requiere       │  │  Requiere       │
│  hace 5 min     │  │                 │  │  atención       │  │  reintento      │
└─────────────────┘  └─────────────────┘  └─────────────────┘  └─────────────────┘
```

Cada tarjeta muestra:
- **Pendientes** (`Pending` + `InProgress`): operaciones en cola esperando envío. Color: azul informativo.
- **Sincronizadas** (`Done`): operaciones completadas exitosamente en los últimos 7 días. Color: verde éxito.
- **Conflictos** (`Conflict`): operaciones que requieren resolución. Color: amarillo advertencia.
- **Fallidas** (`Failed`): operaciones que superaron los reintentos máximos. Color: rojo error.

#### 13.2.2 Tabla de Historial de Operaciones (MudTable)

Se presenta una tabla detallada con el historial de operaciones de sincronización:

| Columna | Descripción |
|---|---|
| **#** | Identificador de la operación (`Id`). |
| **Tipo** | Icono y texto indicando la operación (`Create` / `Update` / `Delete`). |
| **Entidad** | Tipo de entidad y nombre o descripción resumida. |
| **Estado** | Chip de color con el estado actual (`Pending`, `Done`, `Failed`, etc.). |
| **Intentos** | Número de intentos realizados vs. máximo (`2/3`). |
| **Último intento** | Fecha y hora del último intento, con formato relativo ("hace 5 min"). |
| **Error** | Mensaje de error truncado (expandible al hacer clic). |
| **Acciones** | Botón "Reintentar" (visible solo para operaciones `Failed`). |

La tabla soporta:
- Paginación (20 registros por página).
- Filtrado por estado.
- Ordenamiento por columna.

#### 13.2.3 Notificaciones en Tiempo Real (MudSnackbar)

Durante el proceso de sincronización, se muestran notificaciones no intrusivas mediante `MudSnackbar`:

| Evento | Tipo Snackbar | Mensaje | Duración |
|---|---|---|---|
| Inicio de sincronización | Info | "Sincronización iniciada..." | 3 segundos |
| Sincronización exitosa | Success | "Sincronización completada: X operaciones procesadas" | 5 segundos |
| Conflicto detectado | Warning | "Se detectó un conflicto. Resuelto automáticamente (LWW)" | 5 segundos |
| Operación fallida | Error | "Operación de sincronización fallida. Verifique el estado de sync" | 10 segundos |
| Sin conexión | Info | "Sin conexión a Internet. Los datos se guardan localmente" | 5 segundos |
| Conexión restaurada | Success | "Conexión restaurada. Iniciando sincronización..." | 3 segundos |

### 13.3 Botón de Sincronización Manual

Además de la sincronización automática por eventos de conectividad, la pantalla incluye un botón **"Sincronizar ahora"** que permite al usuario forzar un ciclo de sincronización manual:

- El botón se habilita solo cuando existe conexión a Internet (`HasInternet == true`) y no hay un ciclo de sincronización en curso.
- Al presionarlo, se ejecuta `SyncService.SyncAsync()` con indicador de progreso visible.
- Si no hay operaciones pendientes, se muestra un snackbar informativo: "No hay operaciones pendientes de sincronización".

---

## 14. Criterios de Aceptación del Motor de Sync

Los siguientes criterios de aceptación se expresan en formato BDD (Behavior-Driven Development) utilizando la estructura **Dado / Cuando / Entonces**.

### CA-SYNC-01: Captura Offline con Encolado Automático

```
Dado que el dispositivo NO tiene conexión a Internet
  Y el usuario se encuentra en la pantalla de captura
Cuando el usuario captura un nuevo punto georreferenciado con fotografía
Entonces el punto se almacena en la tabla local Puntos con SyncStatus = "Local"
  Y se crea una entrada en SyncQueue con OperationType = "Create",
    EntityType = "Punto", Status = "Pending"
  Y se crea una entrada en SyncQueue con OperationType = "Create",
    EntityType = "Foto", Status = "Pending"
  Y el usuario visualiza el punto en el mapa con indicador de "no sincronizado"
  Y la aplicación NO muestra errores de conexión
```

### CA-SYNC-02: Sincronización Automática al Recuperar Conectividad

```
Dado que existen 5 operaciones pendientes en SyncQueue con Status = "Pending"
  Y el dispositivo NO tiene conexión a Internet
Cuando el dispositivo recupera la conexión a Internet
Entonces ConnectivityService detecta el evento ConnectivityChanged
  Y se espera el debounce de 2 segundos
  Y SyncService.SyncAsync() se invoca automáticamente
  Y las 5 operaciones se procesan en orden cronológico (CreatedAt ascendente)
  Y al completar, las operaciones exitosas tienen Status = "Done"
  Y las entidades asociadas tienen SyncStatus = "Synced"
  Y se muestra un MudSnackbar con "Sincronización completada: 5 operaciones procesadas"
```

### CA-SYNC-03: Sincronización Manual desde Pantalla de Estado

```
Dado que el dispositivo tiene conexión a Internet
  Y existen operaciones pendientes en SyncQueue
  Y el usuario se encuentra en la pantalla Estado de Sincronización
Cuando el usuario presiona el botón "Sincronizar ahora"
Entonces se muestra un indicador de progreso durante el procesamiento
  Y SyncService.SyncAsync() se ejecuta procesando todas las operaciones pendientes
  Y las MudCards de resumen se actualizan con los nuevos contadores
  Y la MudTable de historial muestra las operaciones procesadas con su estado resultante
  Y se muestra un MudSnackbar con el resultado de la sincronización
```

### CA-SYNC-04: Resolución de Conflictos por Last-Write-Wins

```
Dado que existe un punto con RemoteId = "abc-123" sincronizado en el dispositivo
  Y el punto fue modificado localmente con UpdatedAt = "2026-04-13T15:30:00Z"
  Y existe una operación PendingUpdate en SyncQueue para dicho punto
Cuando SyncService procesa la operación y el servidor responde con HTTP 409
  Y la versión del servidor tiene UpdatedAt = "2026-04-13T14:00:00Z"
Entonces se aplica Last-Write-Wins comparando ambos timestamps
  Y la versión local prevalece (15:30 > 14:00)
  Y se reenvía la operación con el flag de forzado
  Y la operación se completa exitosamente (Status = "Done")
  Y el SyncStatus de la entidad se establece en "Synced"
  Y se muestra un MudSnackbar: "Conflicto resuelto automáticamente"
```

### CA-SYNC-05: Operación Fallida tras Máximo de Reintentos

```
Dado que existe una operación en SyncQueue con Status = "Pending"
  Y el servidor responde consistentemente con HTTP 503
Cuando SyncService procesa la operación
Entonces en el intento 1: se incrementa Attempts a 1 y se programa reintento en 5 segundos
  Y en el intento 2: se incrementa Attempts a 2 y se programa reintento en 30 segundos
  Y en el intento 3: se incrementa Attempts a 3 y la operación transiciona a Status = "Failed"
  Y el SyncStatus de la entidad asociada se establece en "Failed"
  Y el ErrorMessage contiene "503: Service Unavailable"
  Y la MudCard de "Fallidas" incrementa su contador
  Y NO se realizan reintentos automáticos adicionales
  Y en la MudTable aparece la operación con botón "Reintentar" visible
```

### CA-SYNC-06: Pull Delta tras Push Exitoso

```
Dado que SyncService ha completado el push de todas las operaciones pendientes
  Y el último timestamp de sincronización almacenado es "2026-04-13T10:00:00Z"
Cuando SyncService ejecuta el flujo pull
Entonces se envía GET /api/sync/delta?since=2026-04-13T10:00:00Z
  Y se reciben las entidades modificadas en el servidor desde ese timestamp
  Y las entidades nuevas se insertan localmente con SyncStatus = "Synced"
  Y las entidades existentes con versión del servidor más reciente se actualizan
  Y las entidades con cambios locales pendientes (PendingUpdate) NO se sobrescriben
  Y se actualiza el timestamp de última sincronización con el valor devuelto por el servidor
```

### CA-SYNC-07: Limpieza Automática de Cola

```
Dado que existen entradas en SyncQueue con Status = "Done"
  Y algunas de estas entradas tienen CompletedAt anterior a 7 días
Cuando se inicia un nuevo ciclo de sincronización
Entonces se eliminan las entradas con Status = "Done" y CompletedAt < (UtcNow - 7 días)
  Y las entradas con Status = "Failed" NO se eliminan
  Y las entradas con Status = "Pending" NO se eliminan
  Y las entradas con Status = "Done" y CompletedAt dentro de los últimos 7 días se mantienen
```

### CA-SYNC-08: Debounce de Eventos de Conectividad

```
Dado que el dispositivo se encuentra en una zona con señal inestable
Cuando se reciben 5 eventos ConnectivityChanged en un intervalo de 3 segundos
  Y la secuencia es: Internet → NoInternet → Internet → NoInternet → Internet
Entonces solo se inicia UN ciclo de sincronización
  Y el ciclo se inicia 2 segundos después del último evento (Internet)
  Y NO se inician ciclos intermedios durante el período de inestabilidad
```

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción del Cambio |
|---|---|---|---|
| 1.0 | 2026-04-13 | Equipo Técnico | Creación inicial del documento. Definición completa de la arquitectura offline-first, Outbox Pattern, máquina de estados, flujos push/pull, pseudocódigo de algoritmos, estrategia de resolución de conflictos, backoff exponencial, estructura de SyncQueue, integración con ConnectivityChanged, consideraciones de performance, observabilidad y criterios de aceptación. |

---

**Fin del documento**
