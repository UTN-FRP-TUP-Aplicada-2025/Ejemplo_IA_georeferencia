# Plan de Iteración — Sprint 04

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** plan-iteracion_sprint-04_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Información del Sprint

| Campo | Valor |
|-------|-------|
| Sprint | 04 |
| Fase | Fase 2 — Offline-First |
| Fecha inicio | 2026-06-01 |
| Fecha fin | 2026-06-14 |
| Duración | 2 semanas |
| Sprint Goal | Motor offline-first funcional: los datos se guardan primero en SQLite, el indicador de pendientes muestra el conteo de registros no sincronizados, y la app funciona sin conexión a internet. Se demuestra captura completa en modo avión. |
| Velocidad planificada | 23 pts |

---

## 2. Épicas y User Stories

| Épica | US | Título | Puntos | Prioridad |
|-------|----|--------|--------|-----------|
| GEO-E04 | GEO-US11 | Guardado offline en SQLite | 13 | Must |
| GEO-E04 | GEO-US12 | Indicador de pendientes | 5 | Must |
| GEO-E04 | GEO-US13 | Experiencia transparente offline/online | 5 | Should |

**Total Story Points:** 23

---

## 3. Objetivo de la Demo

Al cierre del Sprint 04, se podrá demostrar:

1. **Captura offline:** Con el dispositivo Android en modo avión, el usuario captura fotos; los datos (punto + foto) se almacenan en SQLite local sin errores. La app no muestra errores de conexión.
2. **Indicador de pendientes:** Un MudBadge en la barra de navegación muestra el número de registros pendientes de sincronización (ej: "3"). La pantalla EstadoSync muestra MudCards con métricas: Total, Pendientes, Enviados, Error.
3. **Experiencia transparente:** Si hay conexión, los datos se envían directamente al servidor. Si no hay conexión, se guardan localmente sin que el usuario note diferencia en el flujo. Un indicador visual en el MudAppBar muestra el estado de conexión (verde = online, naranja = offline).

---

## 4. Descomposición de Tareas

### GEO-US11 — Guardado offline en SQLite (13 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T41 | Instalar sqlite-net-pcl en GeoFoto.Mobile | Mobile | 1h | - | To Do |
| GEO-T42 | Diseñar e implementar ILocalDbService con operaciones CRUD | Mobile | 4h | - | To Do |
| GEO-T43 | Crear migrations SQLite automáticas al iniciar app | Mobile | 2h | - | To Do |
| GEO-T44 | Crear tabla SyncQueue con campos: Id, TipoEntidad, EntidadId, Operacion, Payload, Estado, Intentos, CreatedAt, UpdatedAt | Mobile | 2h | - | To Do |
| GEO-T45 | Modificar SubirFotos para escritura en SQLite primero (offline-first) | Shared/Mobile | 3h | - | To Do |
| GEO-T46 | Modificar captura de cámara para escritura en SQLite con foto local | Mobile | 3h | - | To Do |
| GEO-T47 | Implementar IConnectivityService wrapper de Connectivity.Current | Mobile | 2h | - | To Do |
| GEO-T48 | Crear SyncStatusBadge.razor con MudBadge | Shared | 2h | - | To Do |
| GEO-T49 | Tests unitarios de ILocalDbService (SQLite in-memory) | Testing | 3h | - | To Do |
| GEO-T50 | Verificar funcionamiento en modo avión (Android) | Testing | 2h | - | To Do |

### GEO-US12 — Indicador de pendientes (5 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T51 | Conectar SyncStatusBadge con conteo de SyncQueue pendientes | Mobile | 2h | - | To Do |
| GEO-T52 | Crear EstadoSync.razor con MudCards de métricas (Total, Pendiente, Enviado, Error) | Shared | 3h | - | To Do |
| GEO-T53 | Actualizar badge en tiempo real al capturar datos nuevos | Shared | 2h | - | To Do |

### GEO-US13 — Experiencia transparente offline/online (5 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T54 | Implementar IDataService con abstracción local/remoto | Mobile | 3h | - | To Do |
| GEO-T55 | Cargar mapa desde datos locales si no hay red | Mobile | 2h | - | To Do |
| GEO-T56 | Indicador visual de estado de conexión en MudAppBar (verde/naranja) | Shared | 1h | - | To Do |

---

## 5. Criterios de Aceptación del Sprint

### CA-S13 — Captura offline

```gherkin
Dado que el dispositivo Android está en modo avión
Cuando el usuario captura una foto desde la cámara
Entonces los datos se almacenan en SQLite local
Y no se muestra ningún error de conexión
Y el punto aparece en el mapa (con datos locales)
```

### CA-S14 — Indicador de pendientes

```gherkin
Dado que existen 3 registros en SyncQueue con estado "Pendiente"
Cuando se visualiza el MudAppBar
Entonces el badge muestra el número "3"
```

### CA-S15 — Pantalla Estado Sync

```gherkin
Dado que el usuario navega a la pantalla Estado de Sincronización
Cuando se carga la pantalla
Entonces se muestran MudCards con: Total registros, Pendientes, Enviados y Errores
```

### CA-S16 — Experiencia transparente

```gherkin
Dado que el dispositivo pasa de modo avión a tener conexión
Cuando se restablece la red
Entonces el indicador en MudAppBar cambia de naranja a verde
Y el flujo de la app no requiere intervención del usuario
```

---

## 6. Dependencias y Riesgos

| # | Riesgo / Dependencia | Probabilidad | Impacto | Mitigación |
|---|---------------------|-------------|---------|-----------|
| 1 | Sprint 03 incompleto (app Android no funcional) | Media | Crítico | GEO-US11 depende de que MAUI esté configurado |
| 2 | sqlite-net-pcl incompatible con .NET 10 MAUI | Baja | Alto | Verificar compatibilidad; alternativa: Microsoft.Data.Sqlite |
| 3 | Almacenamiento de fotos locales consume mucho espacio | Media | Medio | Limitar tamaño de foto a 10 MB; limpiar tras sincronizar |
| 4 | ConnectivityChanged no dispara en todos los dispositivos | Media | Alto | Polling fallback cada 30 segundos |

---

## 7. Definiciones

| Ceremonia | Fecha tentativa | Duración |
|-----------|----------------|----------|
| Sprint Planning | 2026-06-01 | 2h |
| Daily Standup | Lunes a viernes | 15min |
| Sprint Review | 2026-06-14 | 1h |
| Sprint Retrospective | 2026-06-14 | 45min |

---

## 8. Métricas planificadas

| Métrica | Objetivo |
|---------|----------|
| Velocidad | 23 pts |
| Tareas completadas | 16/16 |
| Cobertura de tests | 70% en ILocalDbService |
| Bugs abiertos al cierre | ≤ 3 |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
