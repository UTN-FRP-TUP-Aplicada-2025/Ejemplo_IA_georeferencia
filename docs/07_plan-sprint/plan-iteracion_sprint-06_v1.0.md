# Plan de Iteración — Sprint 06

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** plan-iteracion_sprint-06_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Información del Sprint

| Campo | Valor |
|-------|-------|
| Sprint | 06 |
| Fase | Fase 3 — Madurez y Entrega |
| Fecha inicio | 2026-06-29 |
| Fecha fin | 2026-07-12 |
| Duración | 2 semanas |
| Sprint Goal | Producto listo para entrega: pipeline CI/CD operativo con build y tests automáticos, tests del motor de sync con cobertura ≥ 85%, optimización de performance con 100+ puntos, y generación de APK Debug como artifact. |
| Velocidad planificada | 21 pts |

---

## 2. Épicas y User Stories

| Épica | US | Título | Puntos | Prioridad |
|-------|----|--------|--------|-----------|
| GEO-E06 | GEO-US18 | Pipeline CI/CD | 8 | Must |
| GEO-E06 | GEO-US19 | Tests del motor de sync | 8 | Must |
| GEO-E06 | GEO-US20 | Performance con 100+ puntos | 5 | Should |

**Total Story Points:** 21

---

## 3. Objetivo de la Demo

Al cierre del Sprint 06, se podrá demostrar:

1. **Pipeline CI/CD:** Un push a `main` dispara GitHub Actions: build solución, ejecución de tests, generación de reporte, build de APK Android Debug. Los artifacts (APK + test report) quedan disponibles en el pipeline.
2. **Tests del motor de sync:** Suite de tests xUnit que cubre: push queue FIFO, pull delta, backoff exponencial, Last-Write-Wins. Cobertura ≥ 85% en SyncService. Tests de integración con SQLite in-memory.
3. **Performance:** El mapa carga 150 puntos con marker clustering (Leaflet.markercluster). La lista de puntos muestra paginación a 20 elementos por página. Las queries EF Core están optimizadas con `.AsNoTracking()` y proyección.

---

## 4. Descomposición de Tareas

### GEO-US18 — Pipeline CI/CD (8 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T75 | Crear workflow GitHub Actions (.github/workflows/ci.yml) con build | DevOps | 2h | - | To Do |
| GEO-T76 | Agregar step de ejecución de tests con reporte trx | DevOps | 2h | - | To Do |
| GEO-T77 | Agregar step de build APK Android Debug | DevOps | 2h | - | To Do |
| GEO-T78 | Configurar upload de artifacts (APK + test report) | DevOps | 1h | - | To Do |
| GEO-T79 | Documentar pipeline en docs/09_devops/pipeline-ci-cd_v1.0.md | DevOps | 1h | - | To Do |

### GEO-US19 — Tests del motor de sync (8 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T80 | Crear proyecto GeoFoto.Tests con xUnit y Moq | Testing | 1h | - | To Do |
| GEO-T81 | Tests unitarios de SyncService push queue (FIFO, reintentos) | Testing | 3h | - | To Do |
| GEO-T82 | Tests unitarios de SyncService pull delta (merge, timestamps) | Testing | 2h | - | To Do |
| GEO-T83 | Tests de conflicto Last-Write-Wins (local gana / server gana) | Testing | 2h | - | To Do |
| GEO-T84 | Tests de integración con SQLite in-memory (CRUD + SyncQueue) | Testing | 3h | - | To Do |

### GEO-US20 — Performance con 100+ puntos (5 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T85 | Implementar marker clustering en Leaflet (Leaflet.markercluster) | Shared | 2h | - | To Do |
| GEO-T86 | Paginación en ListaPuntos MudTable (20 elementos/página) | Shared | 1h | - | To Do |
| GEO-T87 | Test de carga con seed de 150 puntos de prueba | Testing | 2h | - | To Do |
| GEO-T88 | Optimización de queries EF Core (.AsNoTracking, proyección) | Api | 1h | - | To Do |

---

## 5. Criterios de Aceptación del Sprint

### CA-S22 — Pipeline CI/CD funcional

```gherkin
Dado que se hace push a la rama main
Cuando GitHub Actions ejecuta el workflow ci.yml
Entonces el build compila sin errores
Y los tests se ejecutan y el reporte queda disponible
Y el APK Android Debug se genera como artifact descargable
```

### CA-S23 — Cobertura de tests del motor de sync

```gherkin
Dado que existe el proyecto GeoFoto.Tests con tests de SyncService
Cuando se ejecuta "dotnet test" con cobertura
Entonces la cobertura de SyncService es ≥ 85%
Y todos los tests pasan exitosamente
```

### CA-S24 — Tests Last-Write-Wins

```gherkin
Dado un punto editado localmente con UpdatedAt = T1
Y el mismo punto editado en el servidor con UpdatedAt = T2 donde T2 > T1
Cuando se ejecuta la sincronización
Entonces la versión del servidor (T2) prevalece en el dispositivo local
```

### CA-S25 — Marker clustering

```gherkin
Dado que existen 150 puntos registrados
Cuando se carga la pantalla Mapa
Entonces los markers cercanos se agrupan en clusters con conteo
Y al hacer zoom se desagrupan progresivamente
```

### CA-S26 — Paginación en lista

```gherkin
Dado que existen 50 puntos registrados
Cuando se navega a Lista de Puntos
Entonces se muestran los primeros 20 puntos
Y hay controles de paginación para navegar entre páginas
```

---

## 6. Dependencias y Riesgos

| # | Riesgo / Dependencia | Probabilidad | Impacto | Mitigación |
|---|---------------------|-------------|---------|-----------|
| 1 | Sprint 05 incompleto (sync no funcional) | Media | Alto | Tests de sync dependen de un SyncService funcional |
| 2 | GitHub Actions runner no soporta build MAUI Android | Media | Alto | Usar runner ubuntu-latest con workload maui-android |
| 3 | Cobertura < 85% difícil de alcanzar | Media | Medio | Priorizar tests de rutas críticas (push, pull, conflictos) |
| 4 | Leaflet.markercluster incompatible con versión actual | Baja | Bajo | Plugin estable y ampliamente utilizado |

---

## 7. Definiciones

| Ceremonia | Fecha tentativa | Duración |
|-----------|----------------|----------|
| Sprint Planning | 2026-06-29 | 2h |
| Daily Standup | Lunes a viernes | 15min |
| Sprint Review | 2026-07-12 | 1.5h |
| Sprint Retrospective | 2026-07-12 | 1h |

---

## 8. Métricas planificadas

| Métrica | Objetivo |
|---------|----------|
| Velocidad | 21 pts |
| Tareas completadas | 14/14 |
| Cobertura de tests | ≥ 85% SyncService, ≥ 70% global |
| Bugs abiertos al cierre | 0 |

---

## 9. Release v1.0

Al finalizar el Sprint 06 exitosamente, se genera el Release v1.0 del producto GeoFoto con los siguientes entregables:

| Entregable | Ubicación |
|-----------|-----------|
| APK Android Debug | GitHub Actions artifact |
| API desplegada | localhost / server de desarrollo |
| Documentación SDD | /docs |
| Test report | GitHub Actions artifact |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
