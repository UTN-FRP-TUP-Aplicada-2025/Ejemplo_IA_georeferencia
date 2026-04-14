# Visión de Producto

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** vision-producto_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-13
**Autor:** Equipo Técnico

---

# 1. Propósito

Este documento describe la visión estratégica de GeoFoto, estableciendo el problema que resuelve, el valor esperado para los equipos de trabajo de campo y la organización, y el posicionamiento del producto como un sistema de registro georeferenciado offline-first.

La visión proporciona un marco común para alinear arquitectura, desarrollo y stakeholders, sirviendo como guía para la toma de decisiones durante el ciclo de vida del producto.

---

# 2. Problema de Negocio

Los equipos de trabajo de campo necesitan registrar evidencia fotográfica georeferenciada en ubicaciones donde la conectividad a internet es inexistente o inestable (zonas rurales, instalaciones industriales, áreas subterráneas, obras de construcción). Actualmente, los registros se realizan de forma manual o con herramientas que requieren conexión permanente, lo que genera:

* Pérdida de datos capturados por falta de sincronización.
* Errores en la asignación de coordenadas geográficas.
* Doble trabajo al tener que re-registrar información en oficina.
* Imposibilidad de operar en campo sin señal de datos.
* Falta de visibilidad centralizada de los puntos registrados.

Esta situación provoca ineficiencia operativa, pérdida de información crítica y retrasos en la toma de decisiones que dependen de datos de campo actualizados.

---

# 3. Propuesta de Valor

GeoFoto propone un sistema de registro georeferenciado con arquitectura offline-first que permite:

* Capturar fotografías con coordenadas GPS embebidas (EXIF) desde dispositivos móviles Android.
* Operar en modo completamente offline sin pérdida de funcionalidad.
* Sincronizar automáticamente los datos al recuperar conexión a internet.
* Visualizar todos los puntos registrados en un mapa interactivo con Leaflet.js.
* Gestionar el ciclo de vida completo de puntos y fotografías desde una interfaz web con MudBlazor.

El valor diferencial radica en:

* **Offline-first real:** SQLite es la fuente de verdad operativa en campo; todas las operaciones se ejecutan localmente primero.
* **Sincronización transparente:** Outbox Pattern con SyncService automático, sin intervención del usuario.
* **Geovisualización inmediata:** Markers en mapa Leaflet accesibles desde web y móvil.
* **Componentes compartidos:** Una sola base de código Blazor (GeoFoto.Shared) para web y móvil.
* **Extracción GPS automática:** MetadataExtractor procesa los metadatos EXIF server-side.

---

# 4. Usuarios Objetivo

## 4.1 Técnicos de Campo (Mobile)

Personal que trabaja in situ capturando evidencia fotográfica y registrando puntos de interés geográfico.

**Necesidad principal:** capturar fotos georeferenciadas sin depender de la conectividad, con la certeza de que los datos se sincronizarán automáticamente.

**Dispositivo:** Smartphone Android con la app GeoFoto.Mobile (MAUI Hybrid).

---

## 4.2 Supervisores (Web)

Responsables de gestionar y consultar los puntos registrados por los técnicos de campo desde un entorno de escritorio.

**Necesidad principal:** visualizar en mapa la totalidad de los puntos registrados, consultar fotos asociadas y gestionar la información centralizada.

**Dispositivo:** Navegador web accediendo a GeoFoto.Web.

---

# 5. Objetivos del Producto

| # | Objetivo | Métrica |
|---|----------|---------|
| 1 | Permitir captura de fotos georeferenciadas sin conexión a internet | 100% de operaciones disponibles offline |
| 2 | Sincronizar datos automáticamente al recuperar red | Tiempo de sync < 60 segundos tras reconexión |
| 3 | Visualizar todos los puntos en mapa interactivo | Carga de mapa con 100+ markers en < 3 segundos |
| 4 | Garantizar cero pérdida de datos capturados en campo | 0% de pérdida en escenarios offline → online |
| 5 | Ofrecer una experiencia fluida tanto en web como en móvil | Tiempo de respuesta de UI < 500ms en operaciones comunes |

---

# 6. Alcance del MVP v1.0

El producto en su versión 1.0 incluye:

* Captura de fotos desde galería y cámara nativa (MAUI MediaPicker).
* Extracción automática de coordenadas GPS desde metadatos EXIF (server-side con MetadataExtractor).
* Almacenamiento local en SQLite con sincronización vía Outbox Pattern.
* Visualización en mapa interactivo con markers Leaflet.js.
* Popup de detalle con fotos, coordenadas y edición de nombre/descripción.
* Listado de puntos en MudTable con filtros.
* Panel de estado de sincronización visible con historial de operaciones.
* Sincronización automática al detectar red y manual a demanda.
* Resolución de conflictos automática por Last-Write-Wins.
* Pipeline CI/CD con GitHub Actions (build, test, APK).

---

# 7. Fuera de Alcance v1.0

* Autenticación y autorización de usuarios.
* Soporte multiusuario con resolución de conflictos entre usuarios.
* Mapas offline con tiles cacheados en el dispositivo.
* Editor avanzado de puntos con campos personalizados.
* Exportación de datos a formatos externos (CSV, KML, GeoJSON).
* Soporte para iOS.
* Integración con servicios de mapas premium (Google Maps, Mapbox).

Estos elementos podrán evaluarse en versiones futuras según feedback y necesidades operativas.

---

# 8. Métricas de Éxito

| Métrica | Valor Objetivo | Método de Medición |
|---------|---------------|-------------------|
| Operaciones offline sin pérdida de datos | 100% | Test E2E en modo avión + reconexión |
| Tiempo de sincronización tras reconexión | < 60 segundos (50 operaciones) | Prueba cronometrada con SyncService |
| Carga de mapa con 100+ markers | < 3 segundos | Medición en navegador y MAUI |
| Fotos procesadas con extracción GPS exitosa | ≥ 95% (fotos con EXIF GPS) | Conteo de extracciones exitosas vs total |
| Cobertura de tests del motor de sync | ≥ 85% | Reporte de cobertura xUnit |

---

# 9. Riesgos Iniciales

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|-------------|---------|-----------|
| WebView de MAUI con rendimiento bajo en Android gama baja | Media | Alto | Pruebas tempranas en dispositivos reales; optimización de componentes MudBlazor |
| Tiles de Leaflet no disponibles sin conexión | Alta | Medio | Documentar como limitación v1.0; planificar cache de tiles para v2.0 |
| Conflictos masivos al reconectar múltiples dispositivos | Baja | Alto | Last-Write-Wins con log de auditoría; diseño actual opera con un usuario por dispositivo |
| Fotos sin metadatos EXIF GPS | Media | Medio | Crear punto en coordenadas (0,0) con advertencia visual MudAlert |
| Tamaño elevado de fotos impacta tiempo de sync | Media | Medio | Subida de fotos como operación independiente; indicador de progreso en UI |

---

# 10. Roadmap de Alto Nivel

## Fase 1 — Núcleo Online (Sprints 01–02)

* Estructura de solución y base de datos.
* Captura de fotos, extracción EXIF, visualización en mapa.
* Gestión completa de puntos vía web.
* App MAUI Android funcional con red.

## Fase 2 — Offline-First (Sprints 03–04)

* SQLite local con ILocalDbService.
* Motor de sincronización con Outbox Pattern.
* SyncService automático y manual.
* Resolución de conflictos Last-Write-Wins.

## Fase 3 — Madurez Productiva (Sprints 05–06)

* Pipeline CI/CD con GitHub Actions.
* Tests de integración del motor de sync.
* Optimización de performance con 100+ puntos.
* APK distribuible vía GitHub Releases.

---

# 11. Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |

---

**Fin del documento**
