# Necesidad de Negocio: Sincronización Transparente

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** NB-02-sincronizacion-transparente_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-13
**Autor:** Equipo Técnico

## NB-02 Sincronizar datos de campo con el servidor sin intervención del usuario

La organización necesita que los datos capturados en campo (puntos georeferenciados y fotografías) se sincronicen automáticamente con el servidor central cuando el dispositivo recupere conectividad a internet, sin requerir intervención manual del técnico de campo.

La sincronización debe ser transparente, confiable y resiliente a fallos de red transitorios, garantizando que toda la información llegue al servidor en el orden correcto y sin pérdida.

---

### Problema Específico

Los procesos manuales de sincronización generan:

- Olvidos por parte de los técnicos, dejando datos sin sincronizar durante días.
- Errores al seleccionar qué datos enviar, provocando registros incompletos en el servidor.
- Frustración del usuario al tener que interactuar con procesos técnicos de transferencia de datos.
- Pérdida de datos cuando el técnico olvida sincronizar antes de eliminar información del dispositivo.
- Imposibilidad de garantizar el orden cronológico de las operaciones enviadas al servidor.

### Solución Adoptada

Se implementa el patrón **Outbox Pattern**, estándar de industria utilizado por soluciones como ArcGIS Field Maps, Survey123 y Fulcrum:

- Toda operación de escritura se registra en la tabla `SyncQueue` de SQLite local.
- El `SyncService` procesa la cola en background sin bloquear la interfaz de usuario.
- Al detectar conectividad vía `Connectivity.ConnectivityChanged`, el servicio inicia el push automáticamente.
- En caso de fallo, se reintentan las operaciones con backoff exponencial (5s → 30s → 5min, máximo 3 intentos).
- Los conflictos se resuelven automáticamente por **Last-Write-Wins** basado en el campo `UpdatedAt` (UTC).

---

### Criterios de Éxito

| Criterio | Métrica | Target |
|----------|---------|--------|
| Tiempo de sync tras reconexión | Desde detección de red hasta última operación enviada | < 60 segundos (50 operaciones) |
| Intervención del usuario requerida | Acciones manuales para sincronizar datos | 0 (automático) |
| Operaciones perdidas durante sync | Operaciones de la SyncQueue que no llegan al servidor | 0% |
| Resolución de conflictos | Conflictos resueltos sin interrumpir al usuario | 100% automático |

---

### Stakeholders

| Stakeholder | Rol | Relación con NB-02 |
|-------------|-----|-------------------|
| Técnicos de campo | Productor de datos | Generan las operaciones que deben sincronizarse |
| Supervisores | Consumidor de datos | Necesitan ver los datos actualizados tras la sync |
| Administradores de sistema | Soporte | Monitorean fallos de sync y operaciones con estado Failed |

---

### Trazabilidad a Casos de Uso

| CU | Descripción | Relación con NB-02 |
|----|-------------|-------------------|
| CU-09 | Ver estado de sincronización | Panel con métricas y estado de la cola |
| CU-10 | Ejecutar sincronización manual | Botón para forzar sync a demanda |
| CU-11 | Sincronización automática por reconexión | Trigger automático vía ConnectivityService |
| CU-12 | Enviar operación pendiente al servidor (push) | Procesamiento individual de cada operación |

---

### Dependencias

- Depende de NB-01 (debe existir data local capturada para sincronizar).
- Habilita NB-03 (los datos sincronizados se visualizan en el mapa del servidor).

---

### Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |

---

**Fin del documento**
