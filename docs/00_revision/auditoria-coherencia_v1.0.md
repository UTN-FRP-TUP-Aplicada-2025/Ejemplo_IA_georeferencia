# Auditoría de Coherencia Documental

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** auditoria-coherencia_v1.0.md
**Versión:** 1.0
**Estado:** Final
**Fecha:** 2025-07-18
**Autor:** Agente de auditoría automatizada

---

## 1. Resumen Ejecutivo

Se realizó una auditoría completa del repositorio `/docs` del proyecto GeoFoto para eliminar residuos del proyecto anterior (Motor DSL de Documentos Fiscales), corregir inconsistencias entre documentos y asegurar que toda la documentación sea coherente con un único proyecto: **GeoFoto**.

### Resultado general

| Métrica | Valor |
|---------|-------|
| Archivos antes de auditoría | ~130 |
| Archivos eliminados (Motor DSL) | 96 |
| Archivos eliminados (redundantes GeoFoto) | 1 |
| Archivos corregidos | 3 |
| Archivos sin cambios (OK) | 31 |
| Archivos creados | 1 (este informe) |
| **Archivos después de auditoría** | **35** |

---

## 2. Documentos Eliminados

### 2.1. Archivos Motor DSL — contenido 100% del proyecto anterior

Todos estos archivos pertenecían al proyecto "Motor DSL de Documentos Fiscales" y no tenían relación con GeoFoto.

| Carpeta | Archivos eliminados | Detalle |
|---------|:-------------------:|---------|
| 00_contexto/ | 2 | alcance-proyecto_v1.0.md, compatibilidad-plataformas_v1.0.md |
| 01_necesidades_negocio/necesidades-de-negocio/ | 6 | NB-01-Desacople a NB-06-Reutilizacion (Motor DSL) |
| 02_especificacion_funcional/ | 1 | definicion-dsl_v1.0.md |
| 02_especificacion_funcional/casos-de-uso/ | 38 | CU-01 a CU-32 (incluye versiones v1.0 y v2.0) |
| 02_especificacion_funcional/modelo-datos/reglas-conceptuales-de-modelo/ | 6 | RC-01 a RC-06 (carpeta completa eliminada) |
| 02_especificacion_funcional/reglas-de-negocio/ | 6 | RN-01 a RN-06 (archivos individuales de Motor DSL) |
| 03_ux-ui/ | 4 | experiencia-de-uso-del-motor, representacion-documento-escpos, representacion-vista-previa-ui, wireframes-documentos |
| 04_prompts_ai/ | 1 | prompt-clasificacion-documento (carpeta completa eliminada) |
| 05_arquitectura_tecnica/ | 7 | contratos-del-motor, decisiones-arquitectura, extensibilidad-motor, flujo-ejecucion-motor, guia-uso-libreria, modelo-logico-de-ejecucion, README |
| 07_plan-sprint/ | 5 | sprint-07, sprint-08 (Motor DSL), template-sprint-retrospectiva, template-sprint-review, velocidad-equipo |
| 08_calidad_y_pruebas/ | 6 | casos-prueba-referenciales, criterios-validacion-motor, estrategia-calidad-motor, guia-testing-extensibilidad, matriz-cobertura-pruebas, plan-pruebas |
| 09_devops/ | 2 | estrategia-versionado, guia-publicacion-nuget |
| 10_developer_guide/ | 8 | conceptos-fundamentales, formato-dsl-templates, formato-perfiles-impresora, guia-integracion-maui, integracion-api-rest, perfiles-impresoras-reales, README, soporte-imagenes-termicas |
| 11_examples/ | 4 | ejemplo-01-simple, ejemplo-02-multa, ejemplo-03-multaapp-nuget, README (carpeta completa eliminada) |
| **Total** | **96** | |

### 2.2. Archivos GeoFoto redundantes

| Archivo | Motivo |
|---------|--------|
| 10_developer_guide/jira-import-guide_v1.0.md | Contenido es subconjunto de `06_backlog-tecnico/jira-metodologia_v1.0.md` (creado en esta sesión) |

---

## 3. Documentos Reescritos

| Archivo | Cambio realizado |
|---------|-----------------|
| docs/README.md | Reescritura completa: eliminada sección duplicada "Primer Viaje del Desarrollador" con referencias a Motor DSL (definicion-dsl, guia-uso-libreria, ejemplo-01-simple), eliminadas referencias a carpetas 04_prompts_ai y 11_examples (ya eliminadas), eliminada sección DevOps con link a estrategia-versionado (eliminado), corregido contacto "Motor DSL" → "GeoFoto", actualizado árbol de estructura |

---

## 4. Documentos Completados

No se detectaron documentos incompletos que requieran nuevo contenido.

---

## 5. Documentos Creados

| Archivo | Propósito |
|---------|-----------|
| docs/06_backlog-tecnico/jira-metodologia_v1.0.md | Guía completa de metodología Jira para GeoFoto: configuración del proyecto, épicas, carga de stories/tasks, sprints, workflow, commits, board, planning, métricas e importación CSV. Creado como parte de FASE 1 de esta sesión. |
| docs/00_revision/auditoria-coherencia_v1.0.md | Este informe de auditoría |

---

## 6. Inconsistencias Corregidas

### 6.1. Sprint assignments en Product Backlog (product-backlog_v1.0.md)

**Problema:** 14 de 20 historias de usuario tenían asignación de sprint incorrecta (off-by-one sistemático desde US04 en adelante). Las historias se asignaban a sprints anteriores a los indicados por los sprint plans (fuente de verdad).

**Ejemplo:** US04 y US05 estaban asignadas a Sprint 01 (29 pts) pero el plan de Sprint 01 solo incluye US01–US03 (16 pts).

**Corrección aplicada:**

| Historia | Sprint anterior (incorrecto) | Sprint correcto (per sprint plans) |
|----------|:----------------------------:|:-----------------------------------:|
| GEO-US04 | Sprint 01 | Sprint 02 |
| GEO-US05 | Sprint 01 | Sprint 02 |
| GEO-US08 | Sprint 02 | Sprint 03 |
| GEO-US09 | Sprint 02 | Sprint 03 |
| GEO-US10 | Sprint 02 | Sprint 03 |
| GEO-US11 | Sprint 03 | Sprint 04 |
| GEO-US12 | Sprint 03 | Sprint 04 |
| GEO-US13 | Sprint 03 | Sprint 04 |
| GEO-US14 | Sprint 04 | Sprint 05 |
| GEO-US15 | Sprint 04 | Sprint 05 |
| GEO-US16 | Sprint 04 | Sprint 05 |
| GEO-US17 | Sprint 04 | Sprint 05 |
| GEO-US18 | Sprint 05 | Sprint 06 |
| GEO-US19 | Sprint 05 | Sprint 06 |

Se corrigieron tanto la tabla resumen (§4) como los campos "Sprint asignado" en las 14 secciones de detalle (§4.1–§4.6).

**Fuente de verdad:** Los 6 planes de sprint (`07_plan-sprint/plan-iteracion_sprint-01 a 06`) definen la asignación canónica.

### 6.2. Sprint de GEO-E06 en jira-metodologia (jira-metodologia_v1.0.md)

**Problema:** La sección §4 de jira-metodologia decía "Sprint(s): Sprint 05, Sprint 06" para GEO-E06, pero según los sprint plans (fuente de verdad), las tres historias de GEO-E06 (US18, US19, US20) están todas en Sprint 06.

**Corrección:** Cambiado a "Sprint(s): Sprint 06".

---

## 7. Estado Final de la Documentación

### Inventario completo (35 archivos)

```text
docs/
├── README.md                                                    [REESCRITO]
├── 00_contexto/
│   ├── acuerdo-equipo_v1.0.md                                   [OK]
│   ├── roadmap-producto_v1.0.md                                 [OK]
│   └── vision-producto_v1.0.md                                  [OK]
├── 00_revision/
│   └── auditoria-coherencia_v1.0.md                             [CREADO]
├── 01_necesidades_negocio/
│   ├── necesidades-negocio_v1.0.md                              [OK]
│   └── necesidades-de-negocio/
│       ├── NB-01-registro-campo-sin-conexion_v1.0.md            [OK]
│       ├── NB-02-sincronizacion-transparente_v1.0.md            [OK]
│       ├── NB-03-geovisualizacion_v1.0.md                      [OK]
│       └── NB-04-gestion-fotografias_v1.0.md                   [OK]
├── 02_especificacion_funcional/
│   ├── especificacion-funcional_v1.0.md                         [OK]
│   ├── casos-de-uso/
│   │   └── casos-de-uso_v1.0.md                                 [OK]
│   ├── modelo-datos/
│   │   └── modelo-conceptual_v1.0.md                            [OK]
│   └── reglas-de-negocio/
│       └── reglas-de-negocio_v1.0.md                            [OK]
├── 03_ux-ui/
│   └── wireframes-pantallas_v1.0.md                             [OK]
├── 05_arquitectura_tecnica/
│   ├── api-rest-spec_v1.0.md                                    [OK]
│   ├── arquitectura-offline-sync_v1.0.md                        [OK]
│   ├── arquitectura-solucion_v1.0.md                            [OK]
│   ├── flujo-ejecucion-sistema_v1.0.md                          [OK]
│   └── modelo-datos-logico_v1.0.md                              [OK]
├── 06_backlog-tecnico/
│   ├── backlog-tecnico_v1.0.md                                  [OK]
│   ├── definition-of-ready_v1.0.md                              [OK]
│   ├── jira-metodologia_v1.0.md                                 [CORREGIDO]
│   └── product-backlog_v1.0.md                                  [CORREGIDO]
├── 07_plan-sprint/
│   ├── plan-iteracion_sprint-01_v1.0.md                         [OK]
│   ├── plan-iteracion_sprint-02_v1.0.md                         [OK]
│   ├── plan-iteracion_sprint-03_v1.0.md                         [OK]
│   ├── plan-iteracion_sprint-04_v1.0.md                         [OK]
│   ├── plan-iteracion_sprint-05_v1.0.md                         [OK]
│   └── plan-iteracion_sprint-06_v1.0.md                         [OK]
├── 08_calidad_y_pruebas/
│   ├── definition-of-done_v1.0.md                               [OK]
│   └── estrategia-testing-motor_v1.0.md                         [OK]
├── 09_devops/
│   ├── entornos-deploy_v1.0.md                                  [OK]
│   └── pipeline-ci-cd_v1.0.md                                   [OK]
└── 10_developer_guide/
    └── guia-setup-proyecto_v1.0.md                              [OK]
```

---

## 8. Deuda Documental Pendiente

| Item | Descripción | Prioridad |
|------|-------------|-----------|
| Renombrar estrategia-testing-motor | El archivo `08_calidad_y_pruebas/estrategia-testing-motor_v1.0.md` tiene "motor" en el nombre por herencia del proyecto original, pero su contenido ya es 100% GeoFoto. Considerar renombrar a `estrategia-testing_v1.0.md`. | Baja |
| Carpeta 04 ausente | La numeración salta de 03_ux-ui a 05_arquitectura_tecnica. La carpeta 04_prompts_ai fue eliminada por ser Motor DSL. No impacta funcionalmente pero rompe secuencia numérica. | Cosmética |

---

## 9. Criterios de Auditoría Aplicados

| # | Criterio | Estado |
|---|----------|--------|
| 1 | Todas las menciones a "Motor DSL", "documento fiscal", "ESC/POS", "impresora térmica" eliminadas | ✅ Cumplido |
| 2 | IDs Jira (GEO-US##, GEO-T##, GEO-E##) coherentes entre product-backlog, backlog-técnico, sprint plans y jira-metodología | ✅ Cumplido |
| 3 | Story points idénticos en product-backlog vs sprint plans | ✅ Cumplido |
| 4 | Sprint assignments en product-backlog coinciden con sprint plans | ✅ Corregido (14 historias) |
| 5 | Nombres de entidades coherentes entre modelo-datos-logico, api-rest-spec y especificacion-funcional | ✅ Cumplido |
| 6 | README.md refleja la estructura real de archivos | ✅ Reescrito |
| 7 | No existen enlaces rotos a archivos eliminados | ✅ Cumplido |
| 8 | Nomenclatura de archivos sigue convención kebab-case_v1.0.md | ✅ Cumplido |
| 9 | No hay contenido duplicado entre documentos | ✅ Cumplido (jira-import-guide eliminado) |
| 10 | Todas las carpetas contienen al menos un archivo | ✅ Cumplido |

---

## 10. Control de Cambios

| Versión | Fecha | Cambio |
|---------|-------|--------|
| 1.0 | 2025-07-18 | Auditoría inicial completa: 97 archivos eliminados, 3 corregidos, 1 reescrito, 2 creados |

---

*Fin del documento.*
