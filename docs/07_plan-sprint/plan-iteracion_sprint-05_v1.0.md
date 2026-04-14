# Plan de Iteración — Sprint 05

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** plan-iteracion_sprint-05_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Información del Sprint

| Campo | Valor |
|-------|-------|
| Sprint | 05 |
| Fase | Fase 2 — Offline-First |
| Fecha inicio | 2026-06-15 |
| Fecha fin | 2026-06-28 |
| Duración | 2 semanas |
| Sprint Goal | Motor de sincronización completo: la app sincroniza automáticamente al recuperar conexión, el usuario puede forzar sincronización manual, se visualiza el historial de operaciones y los conflictos se resuelven por Last-Write-Wins. |
| Velocidad planificada | 31 pts |

---

## 2. Épicas y User Stories

| Épica | US | Título | Puntos | Prioridad |
|-------|----|--------|--------|-----------|
| GEO-E05 | GEO-US14 | Sync automática al recuperar conexión | 13 | Must |
| GEO-E05 | GEO-US15 | Sync manual | 5 | Must |
| GEO-E05 | GEO-US16 | Historial de sincronización | 5 | Should |
| GEO-E05 | GEO-US17 | Resolución de conflictos LWW | 8 | Must |

**Total Story Points:** 31

---

## 3. Objetivo de la Demo

Al cierre del Sprint 05, se podrá demostrar:

1. **Sync automática:** Se capturan datos offline, luego se activa la conexión; la app detecta conectividad y envía automáticamente los registros pendientes al servidor. El badge de pendientes se actualiza a 0.
2. **Sync manual:** El usuario presiona "Sincronizar ahora" en la pantalla EstadoSync; se ejecuta la sincronización y se muestra un MudProgressLinear durante el proceso.
3. **Historial:** La pantalla EstadoSync muestra un MudTable con el historial de operaciones de sync: fecha, entidad, operación, resultado, tiempo.
4. **Conflictos LWW:** Si un punto fue editado en el servidor y offline simultáneamente, al sincronizar gana la versión con UpdatedAt más reciente y el conflicto queda registrado.

---

## 4. Descomposición de Tareas

### GEO-US14 — Sync automática al recuperar conexión (13 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T57 | Implementar ISyncService con push queue (procesamiento FIFO) | Mobile | 4h | - | To Do |
| GEO-T58 | Implementar procesamiento FIFO de SyncQueue con reintentos | Mobile | 3h | - | To Do |
| GEO-T59 | Integrar ConnectivityChanged para trigger automático de sync | Mobile | 2h | - | To Do |
| GEO-T60 | Implementar backoff exponencial (5s → 30s → 5min, max 3 intentos) | Mobile | 2h | - | To Do |
| GEO-T61 | Implementar pull delta (GET /api/sync/delta?since=timestamp) | Mobile | 3h | - | To Do |
| GEO-T62 | Crear endpoint GET /api/sync/delta en API con filtro por timestamp | Api | 2h | - | To Do |
| GEO-T63 | Crear endpoint POST /api/sync/batch en API para envío masivo | Api | 3h | - | To Do |
| GEO-T64 | MudSnackbar de notificación al completar sync exitosamente | Shared | 1h | - | To Do |

### GEO-US15 — Sync manual (5 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T65 | Agregar MudButton "Sincronizar ahora" en pantalla EstadoSync | Shared | 1h | - | To Do |
| GEO-T66 | Implementar SyncService.SyncNowAsync() con feedback | Mobile | 2h | - | To Do |
| GEO-T67 | Mostrar MudProgressLinear durante sync manual | Shared | 1h | - | To Do |

### GEO-US16 — Historial de sincronización (5 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T68 | Crear MudTable de historial en pantalla EstadoSync | Shared | 2h | - | To Do |
| GEO-T69 | Almacenar resultado de cada operación (éxito/error/conflicto) en SyncQueue | Mobile | 2h | - | To Do |
| GEO-T70 | Filtros por estado y rango de fecha en historial | Shared | 1h | - | To Do |

### GEO-US17 — Resolución de conflictos LWW (8 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T71 | Implementar comparación UpdatedAt en SyncService (local vs. server) | Mobile | 3h | - | To Do |
| GEO-T72 | Implementar merge de campos por Last-Write-Wins y actualizar local | Mobile | 3h | - | To Do |
| GEO-T73 | Registrar conflictos resueltos en log de auditoría local | Mobile | 1h | - | To Do |
| GEO-T74 | Mostrar conflictos resueltos en EstadoSync con MudChip "Conflicto" | Shared | 1h | - | To Do |

---

## 5. Criterios de Aceptación del Sprint

### CA-S17 — Sync automática

```gherkin
Dado que existen 5 registros pendientes en SyncQueue
Y el dispositivo está en modo avión
Cuando se desactiva el modo avión y se restablece la conexión
Entonces los 5 registros se envían al servidor automáticamente
Y el badge de pendientes muestra "0"
Y se muestra un MudSnackbar "Sincronización completada"
```

### CA-S18 — Backoff exponencial

```gherkin
Dado que un registro falla al sincronizar por error del servidor (500)
Cuando se reintenta
Entonces el primer reintento es a los 5 segundos
Y el segundo reintento a los 30 segundos
Y el tercer intento a los 5 minutos
Y tras 3 fallos consecutivos el registro queda en estado "Error"
```

### CA-S19 — Sync manual

```gherkin
Dado que el usuario está en la pantalla Estado de Sincronización
Cuando presiona "Sincronizar ahora"
Entonces se muestra un MudProgressLinear
Y se procesan todos los registros pendientes
```

### CA-S20 — Historial visible

```gherkin
Dado que se han realizado 10 operaciones de sincronización
Cuando se visualiza el historial en EstadoSync
Entonces se muestra un MudTable con las 10 operaciones, fecha, tipo y resultado
```

### CA-S21 — Conflicto LWW

```gherkin
Dado que un punto fue editado en el servidor (UpdatedAt = 14:00)
Y el mismo punto fue editado offline (UpdatedAt = 14:05)
Cuando se sincroniza
Entonces la versión offline (14:05) prevalece como Last-Write-Wins
Y el conflicto queda registrado en el historial
```

---

## 6. Dependencias y Riesgos

| # | Riesgo / Dependencia | Probabilidad | Impacto | Mitigación |
|---|---------------------|-------------|---------|-----------|
| 1 | Sprint 04 incompleto (SQLite no funcional) | Media | Crítico | Todo el motor de sync depende de SQLite + SyncQueue |
| 2 | Latencia alta en sync batch con muchos registros | Media | Medio | Limitar batch a 50 registros por llamada |
| 3 | Conflictos de reloj entre dispositivo y servidor | Media | Alto | Usar UTC en todos los timestamps; documentar requisito |
| 4 | Pérdida de conexión durante sync en progreso | Media | Alto | Transacciones atómicas por registro; reintento individual |

---

## 7. Definiciones

| Ceremonia | Fecha tentativa | Duración |
|-----------|----------------|----------|
| Sprint Planning | 2026-06-15 | 2h |
| Daily Standup | Lunes a viernes | 15min |
| Sprint Review | 2026-06-28 | 1h |
| Sprint Retrospective | 2026-06-28 | 45min |

---

## 8. Métricas planificadas

| Métrica | Objetivo |
|---------|----------|
| Velocidad | 31 pts |
| Tareas completadas | 18/18 |
| Cobertura de tests | 85% en SyncService |
| Bugs abiertos al cierre | ≤ 3 |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**


