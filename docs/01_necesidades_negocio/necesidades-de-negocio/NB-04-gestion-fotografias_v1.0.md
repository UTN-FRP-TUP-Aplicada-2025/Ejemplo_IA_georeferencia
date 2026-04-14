# Necesidad de Negocio: Gestión de Fotografías

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** NB-04-gestion-fotografias_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-13
**Autor:** Equipo Técnico

## NB-04 Gestionar el ciclo de vida completo de fotografías georeferenciadas

La organización necesita herramientas para crear, consultar, editar y eliminar puntos y fotografías georeferenciadas de forma completa, tanto desde la aplicación web como desde el dispositivo móvil. El ciclo de vida debe contemplar la operación offline con propagación de cambios al sincronizar.

---

### Problema Específico

Sin una gestión completa del ciclo de vida:

- No es posible corregir datos erróneos de puntos registrados en campo (nombre, descripción).
- No existe un mecanismo para eliminar registros incorrectos o duplicados.
- El listado de puntos no permite consultas filtradas para encontrar registros específicos.
- Las ediciones realizadas desde la web no se propagan automáticamente a los dispositivos de campo.
- Las eliminaciones realizadas offline no se sincronizan correctamente con el servidor.

### Impacto si no se resuelve

- Acumulación de registros incorrectos o duplicados en el sistema central.
- Inconsistencias entre los datos del servidor y los datos del dispositivo.
- Pérdida de productividad al no poder gestionar datos desde cualquier plataforma.

---

### Criterios de Éxito

| Criterio | Métrica | Target |
|----------|---------|--------|
| Operaciones CRUD completas | Crear, leer, editar, eliminar puntos y fotos | 100% funcional |
| Propagación de ediciones | Cambios en un punto visibles en todas las plataformas tras sync | 100% |
| Listado filtrable | MudTable con búsqueda por nombre, filtro por estado de sync | Disponible |
| Eliminación con confirmación | MudDialog de confirmación antes de eliminar punto con fotos | Implementado |

---

### Stakeholders

| Stakeholder | Rol | Relación con NB-04 |
|-------------|-----|-------------------|
| Supervisores | Gestor de datos | Editan y eliminan puntos desde la web |
| Técnicos de campo | Creador de datos | Crean y consultan puntos desde mobile |

---

### Trazabilidad a Casos de Uso

| CU | Descripción | Relación con NB-04 |
|----|-------------|-------------------|
| CU-07 | Editar nombre y descripción de punto | Actualización local-first con sync posterior |
| CU-08 | Eliminar punto con fotos | Eliminación con estado PendingDelete hasta confirmación del servidor |
| CU-13 | Recibir cambios del servidor (pull delta) | Propagación de cambios del servidor al dispositivo local |
| CU-14 | Resolver conflicto automáticamente | Last-Write-Wins cuando se edita el mismo punto desde dos plataformas |
| CU-16 | Subir foto desde web (online directo) | Creación de punto y foto directamente en el servidor |

---

### Dependencias

- Depende de NB-01 (la gestión opera sobre datos creados en campo).
- Depende de NB-02 (las ediciones y eliminaciones se sincronizan vía SyncService).
- Complementa NB-03 (las acciones de gestión son accesibles desde el mapa).

---

### Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |

---

**Fin del documento**
