# Estrategia de Testing y Calidad

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** estrategia-testing-motor_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Propósito

Definir la estrategia de testing del proyecto GeoFoto, incluyendo los niveles de prueba, herramientas, cobertura objetivo y criterios de calidad para asegurar la confiabilidad del sistema — especialmente del motor de sincronización offline-first.

---

## 2. Niveles de prueba

### 2.1 Tests unitarios

| Aspecto | Detalle |
|---------|---------|
| Framework | xUnit |
| Mocking | Moq |
| Cobertura objetivo | ≥ 70% global, ≥ 85% SyncService |
| Proyecto | GeoFoto.Tests |
| Convención de nombres | `Clase_Metodo_Escenario` (ej: `SyncService_PushAsync_EnviaRegistrosPendientes`) |

**Componentes con tests unitarios obligatorios:**

| Componente | Proyecto | Prioridad |
|-----------|----------|-----------|
| SyncService | Mobile | Crítica |
| LocalDbService | Mobile | Crítica |
| ConnectivityService | Mobile | Alta |
| PuntosController | Api | Alta |
| FotosController | Api | Alta |
| DataService | Mobile | Media |

### 2.2 Tests de integración

| Aspecto | Detalle |
|---------|---------|
| Base de datos | SQLite in-memory para tests locales |
| API | WebApplicationFactory<Program> para tests de endpoints |
| Ejecución | En pipeline CI/CD |

**Escenarios de integración clave:**

1. **CRUD completo de puntos:** Crear → Leer → Actualizar → Eliminar vía API REST
2. **Upload de fotos con EXIF:** Subir foto → Verificar extracción de coordenadas → Verificar punto creado
3. **SQLite CRUD:** Crear tablas → Insertar → Consultar → Actualizar → Eliminar en SQLite in-memory
4. **SyncQueue procesamiento:** Encolar → Procesar → Verificar estado → Verificar envío a API

### 2.3 Tests de aceptación (BDD)

| Aspecto | Detalle |
|---------|---------|
| Formato | Dado / Cuando / Entonces |
| Documentación | En cada User Story del product-backlog_v1.0.md |
| Verificación | Manual durante Sprint Review |

---

## 3. Cobertura de código

### 3.1 Objetivos por módulo

| Módulo | Cobertura mínima | Justificación |
|--------|-----------------|---------------|
| SyncService (push + pull) | 85% | Componente crítico — errores causan pérdida de datos |
| LocalDbService | 80% | Persistencia local es la base del offline-first |
| ConnectivityService | 70% | Wrapper simple pero crítico para triggers |
| PuntosController | 70% | CRUD estándar con validación |
| FotosController | 70% | Upload + EXIF extraction |
| Componentes Blazor (.razor) | N/A | No se aplica cobertura a componentes de UI |

### 3.2 Herramienta de cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport"
```

---

## 4. Escenarios de prueba del motor de sincronización

El motor de sincronización es el componente más complejo y requiere testing exhaustivo:

### 4.1 Push Queue

| # | Escenario | Resultado esperado |
|---|-----------|--------------------|
| T-SYNC-01 | Encolar 3 registros y procesar | Se envían en orden FIFO |
| T-SYNC-02 | Encolar con conexión disponible | Se envía inmediatamente |
| T-SYNC-03 | Encolar sin conexión | Queda en estado Pendiente |
| T-SYNC-04 | Fallo de red durante envío | Estado cambia a Error, se programa reintento |
| T-SYNC-05 | Backoff exponencial tras fallo | 5s → 30s → 5min, máximo 3 intentos |
| T-SYNC-06 | Máximo de reintentos alcanzado | Estado final = Error, no se reintenta más |

### 4.2 Pull Delta

| # | Escenario | Resultado esperado |
|---|-----------|--------------------|
| T-SYNC-07 | Pull con since=null (primera vez) | Se descargan todos los registros del servidor |
| T-SYNC-08 | Pull con since=timestamp | Solo se descargan registros modificados después del timestamp |
| T-SYNC-09 | Pull con 0 cambios | No se modifica la BD local |
| T-SYNC-10 | Pull con punto nuevo en servidor | Se inserta en SQLite local |
| T-SYNC-11 | Pull con punto eliminado en servidor | Se marca como eliminado en SQLite local |

### 4.3 Conflictos Last-Write-Wins

| # | Escenario | Resultado esperado |
|---|-----------|--------------------|
| T-SYNC-12 | Local UpdatedAt > Server UpdatedAt | Versión local prevalece |
| T-SYNC-13 | Server UpdatedAt > Local UpdatedAt | Versión server prevalece, local se actualiza |
| T-SYNC-14 | UpdatedAt iguales | Versión del servidor prevalece (tiebreaker) |
| T-SYNC-15 | Conflicto en push | Se registra en log de auditoría |

---

## 5. Testing de escenarios offline

| # | Escenario | Pasos | Resultado esperado |
|---|-----------|-------|--------------------|
| T-OFF-01 | Captura en modo avión | Activar modo avión → Capturar foto | Datos guardados en SQLite, badge = 1 |
| T-OFF-02 | Restaurar conexión | Desactivar modo avión | Sync automática, badge = 0 |
| T-OFF-03 | Captura múltiple offline | Capturar 5 fotos offline | 5 registros en SyncQueue |
| T-OFF-04 | App reiniciada offline | Cerrar y abrir app en modo avión | Datos locales persisten |
| T-OFF-05 | Mapa con datos locales | Ver mapa en modo avión | Markers de datos locales visibles |

---

## 6. Herramientas

| Herramienta | Uso |
|------------|-----|
| xUnit | Framework de tests unitarios y de integración |
| Moq | Mocking de dependencias (IConnectivityService, HttpClient, etc.) |
| SQLite in-memory | Base de datos de prueba para tests de integración local |
| WebApplicationFactory | Tests de integración de API REST |
| dotnet-coverage | Reporte de cobertura de código |
| ReportGenerator | Visualización HTML de reportes de cobertura |

---

## 7. Integración con CI/CD

```yaml
# Extracto de .github/workflows/ci.yml
- name: Run Tests
  run: dotnet test --configuration Release --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage"

- name: Upload Test Results
  uses: actions/upload-artifact@v4
  with:
    name: test-results
    path: "**/test-results.trx"

- name: Upload Coverage
  uses: actions/upload-artifact@v4
  with:
    name: coverage-report
    path: "**/coverage.cobertura.xml"
```

---

## 8. Criterios de calidad

| Criterio | Umbral | Acción si no se cumple |
|----------|--------|----------------------|
| Cobertura global | ≥ 70% | Bloquea merge a main |
| Cobertura SyncService | ≥ 85% | Bloquea merge a main |
| Tests fallidos | 0 | Bloquea merge a main |
| Bugs críticos abiertos | 0 | Bloquea release |
| Bugs mayores abiertos | ≤ 2 | Requiere plan de acción |

---

## 9. Trazabilidad

| Documento | Referencia |
|-----------|-----------|
| Product Backlog | product-backlog_v1.0.md (criterios BDD por US) |
| Definition of Done | definition-of-done_v1.0.md |
| Pipeline CI/CD | pipeline-ci-cd_v1.0.md |
| Arquitectura Offline-Sync | arquitectura-offline-sync_v1.0.md |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
