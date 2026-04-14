# Smoke Test de Campo — GeoFoto v1.0

**Versión:** 1.0  
**Última actualización:** Sprint 06

---

## Objetivo

Validar el funcionamiento end-to-end de GeoFoto en un entorno real de campo, cubriendo los flujos críticos de la aplicación móvil y web.

---

## Precondiciones

- API corriendo en el servidor (`http://<host>:5000`)
- App Web accesible (`http://<host>:5001`)
- APK instalada en dispositivo Android con GPS y cámara
- Conexión a Internet disponible (para escenarios online)
- Al menos 5 fotos con datos EXIF GPS en el dispositivo

---

## Escenario 1 — Flujo Web Completo

| Paso | Acción | Resultado Esperado |
|------|--------|--------------------|
| 1.1 | Abrir la app Web en el navegador | Mapa carga con tiles de OpenStreetMap |
| 1.2 | Navegar a **Subir Fotos** | Formulario de upload visible |
| 1.3 | Seleccionar 3 fotos con GPS | Barra de progreso avanza, 3 resultados con ícono ✓ en columna Geo |
| 1.4 | Navegar a **Lista de Puntos** | Tabla muestra al menos 3 puntos con paginación (20/página) |
| 1.5 | Buscar un punto por nombre | Filtro funciona en tiempo real |
| 1.6 | Hacer clic en Editar de un punto | Diálogo de detalle se abre con fotos y mapa |
| 1.7 | Eliminar un punto | Confirmación aparece, tras aceptar el punto desaparece |
| 1.8 | Volver al **Mapa** | Puntos agrupados en clusters cuando hay ≥ 2 cercanos |
| 1.9 | Hacer zoom hasta separar clusters | Markers individuales visibles con popup al hacer clic |

**Criterio de éxito:** Todos los pasos completan sin errores en consola.

---

## Escenario 2 — Flujo Móvil Offline → Online

| Paso | Acción | Resultado Esperado |
|------|--------|--------------------|
| 2.1 | Activar **modo avión** en el dispositivo | Sin conexión |
| 2.2 | Abrir GeoFoto y navegar a **Subir Fotos** | Formulario visible |
| 2.3 | Tomar 2 fotos con la cámara y subirlas | Fotos guardadas localmente en SQLite, snackbar confirma |
| 2.4 | Navegar a **Estado Sync** | Muestra 2 operaciones pendientes, ícono "Sin conexión" |
| 2.5 | Desactivar modo avión | Indicador cambia a "Conectado" |
| 2.6 | Presionar **Sincronizar ahora** | Barra de progreso aparece, operaciones pasan a "Done" |
| 2.7 | Verificar en app Web que los puntos aparecen | Puntos creados desde el móvil visibles en mapa y lista |

**Criterio de éxito:** Sincronización completa sin pérdida de datos.

---

## Escenario 3 — Rendimiento con Volumen

| Paso | Acción | Resultado Esperado |
|------|--------|--------------------|
| 3.1 | Subir 100+ fotos georeferenciadas vía Web | Todas las fotos procesan sin timeout |
| 3.2 | Abrir **Mapa** | Clusters agrupan los 100+ puntos, el mapa no se congela |
| 3.3 | Abrir **Lista de Puntos** | Tabla carga con paginación, navegación fluida entre páginas |
| 3.4 | Interactuar con clusters (zoom in/out) | Clusters se expanden/contraen sin lag perceptible |
| 3.5 | Filtrar por nombre en la lista | Resultados aparecen instantáneamente |

**Criterio de éxito:** Tiempo de carga de mapa < 3 segundos con 100+ puntos. Lista paginada navega sin demora.

---

## Registro de Resultados

| Campo | Valor |
|-------|-------|
| Fecha de ejecución | ___________ |
| Ejecutado por | ___________ |
| Dispositivo Android | ___________ |
| Navegador Web | ___________ |
| Versión APK | ___________ |
| Escenario 1 | ☐ OK / ☐ Fallo |
| Escenario 2 | ☐ OK / ☐ Fallo |
| Escenario 3 | ☐ OK / ☐ Fallo |
| Observaciones | ___________ |
