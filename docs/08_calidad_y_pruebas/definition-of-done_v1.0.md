# Definition of Done (DoD)

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** definition-of-done_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Propósito

Establecer los criterios que toda Historia de Usuario (US) debe cumplir para considerarse **terminada** ("Done"). Una US que no cumpla todos los criterios obligatorios no se cuenta en la velocidad del Sprint.

---

## 2. Criterios obligatorios (toda US)

| #  | Criterio | Verificación |
|----|----------|-------------|
| D1 | El código compila sin errores ni warnings bloqueantes | `dotnet build` exitoso |
| D2 | Los tests unitarios relacionados pasan al 100% | `dotnet test` sin fallos |
| D3 | El código cumple la cobertura mínima del módulo afectado | Reporte de cobertura |
| D4 | El código fue revisado por al menos un par (code review) | Pull Request aprobado en GitHub |
| D5 | Los criterios de aceptación BDD de la US están verificados | Checklist en Jira marcado |
| D6 | No hay bugs críticos ni mayores abiertos asociados a la US | Filtro en Jira |
| D7 | El código sigue las convenciones del equipo (Conventional Commits, naming) | Revisión en PR |
| D8 | El branch fue mergeado a `develop` vía Pull Request | GitHub branch protection |
| D9 | La documentación técnica fue actualizada si aplica | Verificación manual |

---

## 3. Criterios específicos de GeoFoto

| #  | Criterio | Aplica a | Verificación |
|----|----------|----------|-------------|
| D10 | La funcionalidad opera correctamente **tanto online como offline** | Épicas GEO-E04 y GEO-E05 | Test manual en modo avión |
| D11 | Los datos se persisten correctamente en SQLite al operar sin conexión | Épica GEO-E04 | Test con SQLite in-memory + test manual |
| D12 | La sincronización no genera duplicados ni pérdida de datos | Épica GEO-E05 | Tests de SyncService |
| D13 | Los componentes MudBlazor se renderizan correctamente en Web y Android | Épicas GEO-E02, GEO-E03 | Verificación visual en ambas plataformas |
| D14 | Los endpoints API devuelven códigos HTTP correctos y ProblemDetails en errores | Épica GEO-E02 | Tests de integración con WebApplicationFactory |
| D15 | Los permisos Android solicitados son los mínimos necesarios | Épica GEO-E03 | Revisión de AndroidManifest.xml |

---

## 4. Criterios por nivel

### 4.1 Done para una Tarea (GEO-TXX)

- [ ] Código implementado y compilable
- [ ] Tests unitarios escritos (si aplica)
- [ ] Commit con mensaje Conventional Commits

### 4.2 Done para una User Story (GEO-USXX)

- [ ] Todos los criterios D1–D9 cumplidos
- [ ] Criterios D10–D15 cumplidos (si aplican según épica)
- [ ] Todas las tareas hijas completadas
- [ ] Demo preparada para Sprint Review

### 4.3 Done para un Sprint

- [ ] Todas las US comprometidas cumplen DoD
- [ ] Sprint Review realizada con demo
- [ ] Sprint Retrospective realizada
- [ ] Backlog actualizado con items no completados
- [ ] Métricas de velocidad registradas

### 4.4 Done para un Release

- [ ] Todos los Sprints de la fase cumplen DoD de Sprint
- [ ] Pipeline CI/CD ejecuta sin fallos en main
- [ ] APK Android Debug generado como artifact
- [ ] Cobertura global ≥ 70%
- [ ] Cobertura SyncService ≥ 85%
- [ ] 0 bugs críticos abiertos
- [ ] Documentación SDD completa y actualizada

---

## 5. Checklist rápido para Sprint Review

```text
┌─────────────────────────────────────────────┐
│            CHECKLIST DoD — US               │
├─────────────────────────────────────────────┤
│ □ Compila sin errores                       │
│ □ Tests pasan al 100%                       │
│ □ Cobertura cumple umbral del módulo        │
│ □ Code review aprobado                      │
│ □ Criterios BDD verificados                 │
│ □ Sin bugs críticos / mayores               │
│ □ Conventional Commits                      │
│ □ Merge a develop vía PR                    │
│ □ Docs actualizados                         │
│ □ (Si offline) Funciona en modo avión       │
│ □ (Si sync) Sin duplicados ni pérdida       │
│ □ (Si UI) Renderiza en Web + Android        │
│ □ (Si API) HTTP codes + ProblemDetails ok   │
│ □ (Si Android) Permisos mínimos             │
└─────────────────────────────────────────────┘
```

---

## 6. Trazabilidad

| Documento relacionado | Referencia |
|----------------------|-----------|
| Definition of Ready | definition-of-ready_v1.0.md |
| Estrategia de Testing | estrategia-testing-motor_v1.0.md |
| Acuerdo de equipo | acuerdo-equipo_v1.0.md |
| Pipeline CI/CD | pipeline-ci-cd_v1.0.md |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
