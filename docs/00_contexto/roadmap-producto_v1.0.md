# Roadmap de Producto

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** roadmap-producto_v1.0.md
**Versión:** 1.1
**Estado:** Activo
**Fecha:** 2026-04-16
**Autor:** Equipo Técnico

---

# 1. Propósito

Conectar la visión del producto con el backlog técnico y el plan de sprints. Este roadmap proporciona una vista de alto nivel de las fases de desarrollo, sus objetivos, entregables y la trazabilidad hacia épicas, historias de usuario y releases.

---

# 2. Fases del Producto

## Fase 1 — Núcleo Online (Sprints 01–02)

- **Objetivo:** Un supervisor puede subir fotos georeferenciadas desde la web, verlas en un mapa interactivo, gestionar puntos y un técnico accede al mapa desde su Android.
- **Épicas:**
  - GEO-E01 — Fundaciones del proyecto
  - GEO-E02 — Captura y visualización online
  - GEO-E03 — App móvil MAUI Android
- **Historias:** GEO-US01 a GEO-US10
- **Entregable:** Aplicación web funcional con mapa Leaflet, upload de fotos con extracción EXIF y app MAUI Android conectada al servidor.
- **Release target:** v1.0.0-beta
- **Criterio de entrada:** Repositorio creado, SDK .NET 10 disponible, SQL Server accesible.
- **Criterio de salida:** Todas las US Must Have de la fase cumplen el Definition of Done. Demo ejecutable en navegador y dispositivo Android.
- **Capacidad demostrable:** Subir fotos desde la web → ver markers en mapa → click en marker muestra popup con fotos → editar punto → listar puntos en tabla → eliminar punto → instalar app en Android → ver mapa → capturar foto con cámara.

---

## Fase 2 — Offline-First (Sprints 03–04)

- **Objetivo:** Un técnico de campo puede capturar fotos y crear puntos sin conexión a internet, y los datos se sincronizan automáticamente al recuperar red.
- **Épicas:**
  - GEO-E04 — Motor offline-first (SQLite)
  - GEO-E05 — Motor de sincronización
- **Historias:** GEO-US11 a GEO-US17
- **Entregable:** Motor offline-first completo con SQLite, SyncService automático, resolución de conflictos Last-Write-Wins y panel de estado de sincronización.
- **Release target:** v1.0.0-rc
- **Criterio de entrada:** Fase 1 completada. API REST y app MAUI funcionales con red.
- **Criterio de salida:** Operación offline verificada en modo avión. Sincronización automática funcional. Conflictos resueltos automáticamente.
- **Capacidad demostrable:** Modo avión → capturar fotos → ver puntos locales → badge con pendientes → recuperar red → sync automático → badge en cero → ver puntos en web → forzar conflicto → resolución automática → historial visible.

---

## Fase 3 — Madurez Productiva (Sprints 05–06)

- **Objetivo:** El sistema tiene CI/CD funcionando, tests con cobertura garantizada, performance validada y APK distribuible.
- **Épicas:**
  - GEO-E06 — Calidad, UX y deploy productivo
- **Historias:** GEO-US18 a GEO-US20
- **Entregable:** Pipeline CI/CD en GitHub Actions, tests de integración del motor de sync con cobertura ≥ 80%, rendimiento validado con 100+ puntos, APK en GitHub Releases.
- **Release target:** v1.0.0
- **Criterio de entrada:** Fase 2 completada. Motor de sync estable.
- **Criterio de salida:** Pipeline en verde. Cobertura ≥ 80% en SyncService. Mapa fluido con 100+ markers. APK descargable desde GitHub Releases.
- **Capacidad demostrable:** Push a main → pipeline verde → reporte de cobertura → APK generada → instalar APK → walkthrough completo campo → offline → sync → web.

---

## Fase 4 — UX Avanzado y Funcionalidades Completas (Sprints 07–08)

- **Objetivo:** Los técnicos de campo tienen una experiencia UX completa en Android (GPS, carrusel, radio, sync visual, lista de markers) y los supervisores tienen la misma experiencia en web más capacidades exclusivas (zip, upload desde browser).
- **Épicas:**
  - GEO-E07 — UX Avanzado Mobile + Web
- **Historias:**
  - Sprint 07: GEO-US20b a GEO-US29 (Android completo — GPS, radio, carrusel, sync, lista, eliminar)
  - Sprint 08: GEO-US30 a GEO-US33 (Android compartir + Web geolocation + zip + upload)
- **Entregable:** App Android con GPS FAB, marcador propio, radio visual, carrusel completo con edición, sync badge, lista de markers y eliminación. Web con paridad funcional, descarga zip y subida de fotos.
- **Release target:** v1.1.0-sprint07 / v1.1.0-sprint08
- **Criterio de entrada:** Fase 3 completada. Pipeline CI/CD funcionando. Tests con cobertura ≥ 80%.
- **Criterio de salida:** Todos los Must Have de GEO-US20b–29 aprobados en dispositivo Android físico. GEO-US31–33 validados en browser. Cobertura SyncService ≥ 85%.
- **Nota de capacidad:** Sprint 07 tiene ~57 story points (Must+Should) vs velocidad histórica de 21 pts/sprint. Se recomienda dividir en Sprint 07A (GEO-US20b+21+22+26 = 21 pts), Sprint 07B (GEO-US23+27+25 = 23 pts) y Sprint 07C (GEO-US24+28+29 = 21 pts). Los planes de sprint contemplan esta distribución.

---

## Futuro — Evolución (v2.0)

- **Objetivo:** Autenticación, mapas offline con tiles cacheados, soporte multiusuario, almacenamiento en Azure Blob.
- **Épicas:** Por definir según feedback de usuarios y stakeholders.
- **Release target:** v2.0.0

---

# 3. Sprint Goals por Fase

## Fase 1 — Núcleo Online

| Sprint | Sprint Goal | Épicas |
|--------|------------|--------|
| Sprint 01 | Un supervisor puede subir fotos desde la web y verlas en el mapa interactivo | GEO-E01, GEO-E02 |
| Sprint 02 | Un supervisor gestiona puntos desde la web Y un técnico accede al mapa desde su celular Android | GEO-E02, GEO-E03 |

## Fase 2 — Offline-First

| Sprint | Sprint Goal | Épicas |
|--------|------------|--------|
| Sprint 03 | Un técnico de campo puede capturar fotos y crear puntos SIN conexión a internet | GEO-E04 |
| Sprint 04 | Los datos capturados offline se sincronizan automáticamente y el técnico ve el historial completo | GEO-E05 |

## Fase 3 — Madurez Productiva

| Sprint | Sprint Goal | Épicas |
|--------|------------|--------|
| Sprint 05 | El sistema tiene CI/CD funcionando y el motor de sync tiene cobertura de tests garantizada | GEO-E06 |
| Sprint 06 | El sistema está listo para uso productivo en campo con fluidez y distribución resueltos | GEO-E06 |

## Fase 4 — UX Avanzado y Funcionalidades Completas

| Sprint    | Sprint Goal | Épicas  |
|-----------|------------|---------|
| Sprint 07A | GPS FAB + posición propia + radio visual + ampliar foto (21 pts) | GEO-E07 |
| Sprint 07B | Popup carrusel completo + pantalla sync + quitar foto (23 pts) | GEO-E07 |
| Sprint 07C | Offline-first robusto + lista markers + eliminar marker (21 pts) | GEO-E07 |
| Sprint 08  | Compartir foto Android + Web paridad + descarga zip + upload browser (20 pts) | GEO-E07 |

---

# 4. Matriz de Trazabilidad Fase → Épica → Sprints → Release → Capacidad

| Fase | Épicas | Sprints | Release | Capacidad Entregada |
|------|--------|---------|---------|-------------------|
| Fase 1 — Núcleo Online | GEO-E01, GEO-E02, GEO-E03 | Sprint 01–02 | v1.0.0-beta | Captura web + mapa + gestión de puntos + app MAUI con red |
| Fase 2 — Offline-First | GEO-E04, GEO-E05 | Sprint 03–04 | v1.0.0-rc | Operación offline + sync automático + resolución de conflictos |
| Fase 3 — Madurez | GEO-E06 | Sprint 05–06 | v1.0.0 | CI/CD + tests + performance + distribución APK |
| Fase 4 — UX Avanzado | GEO-E07 | Sprint 07–08 | v1.1.0 | GPS FAB + radio + carrusel + sync badge + lista + web paridad + zip + upload |
| Futuro | Por definir | Por definir | v2.0.0 | Auth + mapas offline + multiusuario + Azure Blob |

---

# 5. Dependencias entre Fases

- **Fase 2 depende de Fase 1:** La API REST, la app MAUI y las páginas Blazor compartidas deben estar funcionales antes de implementar el motor offline y sincronización.
- **Fase 3 depende de Fase 2:** El motor de sync debe estar estable antes de construir el pipeline CI/CD que lo valide y las pruebas de rendimiento que lo estresen.
- **Futuro depende de Fase 3:** Las decisiones de autenticación, multiusuario y almacenamiento cloud se informan por el feedback obtenido durante el uso productivo de v1.0.

---

# 6. Criterios de Transición entre Fases

Para avanzar de una fase a la siguiente:

- [ ] Todas las historias Must Have de la fase cumplen el [Definition of Done](../08_calidad_y_pruebas/definition-of-done_v1.0.md).
- [ ] Pruebas de regresión ejecutadas sin fallos críticos.
- [ ] Release etiquetado según SemVer.
- [ ] Sprint Review aprobado por el Product Owner.
- [ ] Deuda técnica de la fase documentada y priorizada.

---

# 7. Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |
| 1.1 | 2026-04-16 | Agregada Fase 4 "UX Avanzado y Funcionalidades Completas" con Sprint 07A/07B/07C y Sprint 08. Épica GEO-E07. |

---

**Fin del documento**
