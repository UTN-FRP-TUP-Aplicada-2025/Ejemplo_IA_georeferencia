# Definition of Ready (DoR)

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** definition-of-ready_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Propósito

Establecer los criterios que toda Historia de Usuario (US) debe cumplir **antes** de ser incluida en un Sprint Backlog. Una US que no cumpla todos los criterios permanece en el Product Backlog hasta ser refinada.

---

## 2. Criterios obligatorios

| #  | Criterio | Verificación |
|----|----------|-------------|
| R1 | La US tiene título y descripción en formato estándar ("Como…, quiero…, para…") | Revisión en Jira |
| R2 | La US tiene al menos un criterio de aceptación escrito en formato BDD (Dado/Cuando/Entonces) | Revisión en Jira |
| R3 | La US está estimada en Story Points por el equipo (Planning Poker) | Campo "Story Points" completado en Jira |
| R4 | La US tiene épica asociada (GEO-EXX) | Campo "Epic Link" en Jira |
| R5 | Las dependencias técnicas están identificadas y resueltas o planificadas en el mismo Sprint | Revisión en refinamiento |
| R6 | La US es lo suficientemente pequeña como para completarse en un Sprint (≤ 13 pts) | Estimación ≤ 13 |
| R7 | El equipo entiende la US y ha resuelto todas las dudas en el refinamiento | Acta de refinamiento |
| R8 | Los datos de prueba necesarios están identificados | Descripción de la US |

---

## 3. Criterios específicos de GeoFoto

| #  | Criterio | Aplica a | Verificación |
|----|----------|----------|-------------|
| R9 | La US describe el comportamiento esperado **tanto offline como online** | Épicas GEO-E04 y GEO-E05 | Sección "Comportamiento offline/online" en la descripción |
| R10 | Si la US involucra sincronización, se indica la estrategia de conflictos aplicable | Épica GEO-E05 | Referencia a RN-06 (Last-Write-Wins) |
| R11 | Si la US involucra UI, se referencia el wireframe correspondiente del documento wireframes-pantallas_v1.0.md | Épicas GEO-E02, GEO-E03, GEO-E04, GEO-E05 | Enlace a wireframe en la descripción |
| R12 | Si la US involucra API REST, los endpoints están documentados en api-rest-spec_v1.0.md | Épicas GEO-E02, GEO-E05 | Referencia a endpoint en la descripción |
| R13 | Si la US involucra permisos Android, están listados explícitamente | Épica GEO-E03 | Lista de permisos en la descripción |

---

## 4. Proceso de verificación

```text
┌─────────────────────────────┐
│      Product Backlog        │
│   (US sin refinar)          │
└─────────┬───────────────────┘
          │
          ▼
┌─────────────────────────────┐
│  Sesión de Refinamiento     │
│  - Discusión de la US       │
│  - Estimación (Poker)       │
│  - Verificar DoR            │
└─────────┬───────────────────┘
          │
    ┌─────┴──────┐
    │ ¿Cumple    │
    │ todos los  │──── No ───► Permanece en PB
    │ criterios? │              (se etiqueta "Needs Refinement")
    └─────┬──────┘
          │ Sí
          ▼
┌─────────────────────────────┐
│   Ready para Sprint         │
│   Planning                  │
└─────────────────────────────┘
```

---

## 5. Checklist rápido

Para facilitar la verificación durante el refinamiento, se utiliza el siguiente checklist:

- [ ] Formato "Como…, quiero…, para…"
- [ ] Al menos 1 criterio BDD
- [ ] Estimación en Story Points
- [ ] Épica asignada
- [ ] Dependencias resueltas
- [ ] Tamaño ≤ 13 pts
- [ ] Dudas resueltas
- [ ] Datos de prueba identificados
- [ ] (Si offline) Comportamiento offline/online descrito
- [ ] (Si sync) Estrategia de conflictos referenciada
- [ ] (Si UI) Wireframe referenciado
- [ ] (Si API) Endpoint documentado
- [ ] (Si Android) Permisos listados

---

## 6. Trazabilidad

| Documento relacionado | Referencia |
|----------------------|-----------|
| Product Backlog | product-backlog_v1.0.md |
| Backlog Técnico | backlog-tecnico_v1.0.md |
| Definition of Done | definition-of-done_v1.0.md |
| Wireframes | wireframes-pantallas_v1.0.md |
| API REST | api-rest-spec_v1.0.md |
| Reglas de Negocio | reglas-de-negocio_v1.0.md |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
