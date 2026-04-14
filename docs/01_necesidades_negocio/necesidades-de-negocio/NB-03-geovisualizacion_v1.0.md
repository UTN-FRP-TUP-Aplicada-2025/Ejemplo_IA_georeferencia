# Necesidad de Negocio: Geovisualización

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** NB-03-geovisualizacion_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-13
**Autor:** Equipo Técnico

## NB-03 Visualizar en mapa todos los puntos registrados con acceso directo a sus fotos

La organización necesita una vista geográfica centralizada que muestre todos los puntos capturados como markers en un mapa interactivo, con acceso inmediato a las fotografías, coordenadas y datos asociados a cada punto. Esta visualización debe estar disponible tanto en la aplicación web como en la app móvil.

---

### Problema Específico

Sin una visualización geográfica integrada:

- Los supervisores no pueden evaluar la distribución espacial de los puntos registrados.
- No existe una forma intuitiva de acceder a las fotos asociadas a una ubicación geográfica específica.
- La consulta de datos se limita a listados tabulares sin contexto geográfico.
- No es posible identificar rápidamente zonas con alta densidad de registros o áreas sin cobertura.
- Los técnicos de campo no pueden verificar en el mapa si ya existe un punto registrado en la ubicación donde se encuentran.

### Impacto si no se resuelve

- Pérdida de contexto geográfico en la gestión de los registros de campo.
- Ineficiencia operativa al no poder ubicar visualmente los puntos de interés.
- Imposibilidad de tomar decisiones basadas en la distribución geográfica de los datos.

---

### Criterios de Éxito

| Criterio | Métrica | Target |
|----------|---------|--------|
| Puntos visibles en mapa | Porcentaje de puntos con coordenadas representados como markers | 100% |
| Tiempo de carga del mapa | Tiempo hasta visualización completa con 100+ markers | < 3 segundos |
| Acceso a fotos desde marker | Click en marker muestra popup con fotos del punto | Disponible |
| Información del popup | Datos visibles al hacer click: nombre, coordenadas, fotos | Completa |

---

### Stakeholders

| Stakeholder | Rol | Relación con NB-03 |
|-------------|-----|-------------------|
| Supervisores | Usuario Web | Consultan el mapa para gestión y supervisión |
| Técnicos de campo | Usuario Mobile | Verifican puntos existentes en el mapa local |

---

### Trazabilidad a Casos de Uso

| CU | Descripción | Relación con NB-03 |
|----|-------------|-------------------|
| CU-05 | Visualizar mapa con markers | Renderizado de markers Leaflet con coordenadas de puntos |
| CU-06 | Ver detalle de punto con popup | MudDialog con fotos, coordenadas y datos del punto |
| CU-07 | Editar nombre y descripción de punto | Formulario accesible desde el popup del mapa |

---

### Dependencias

- Depende de NB-01 y NB-02 (los puntos deben existir y estar sincronizados para visualizarse).
- Complementa NB-04 (la gestión de puntos incluye acciones accesibles desde el mapa).

---

### Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |

---

**Fin del documento**
