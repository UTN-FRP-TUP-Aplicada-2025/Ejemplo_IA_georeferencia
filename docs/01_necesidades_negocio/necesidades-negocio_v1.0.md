# Necesidades de Negocio

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** necesidades-negocio_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-13
**Autor:** Equipo Técnico

---

# 1. Propósito

Este documento identifica y describe las necesidades de negocio que motivan la creación de GeoFoto. Su objetivo es expresar los problemas desde la perspectiva operativa y organizacional, antes de definir soluciones funcionales o de implementación.

Las necesidades aquí expuestas constituyen la base para la visión del producto, la especificación funcional y el diseño de la arquitectura offline-first, asegurando que el desarrollo responda a problemas reales de captura, sincronización y visualización de datos georeferenciados en campo.

---

# 2. Contexto Actual

Los equipos de trabajo de campo necesitan registrar evidencia fotográfica con ubicación geográfica precisa en zonas donde la conectividad a internet es inexistente o intermitente. Actualmente, los procesos de registro se realizan de forma manual o con herramientas que requieren conexión permanente, lo que genera:

* Pérdida de datos capturados cuando no hay red disponible.
* Errores en la asignación manual de coordenadas geográficas.
* Doble trabajo al transcribir información de campo a sistemas centrales.
* Dependencia total de la conectividad para operar.
* Falta de visibilidad centralizada de los puntos registrados en mapa.

La ausencia de un sistema integrado offline-first impide que los técnicos de campo trabajen con autonomía y que los supervisores tengan visibilidad en tiempo real sobre los registros.

---

# 3. Problemas Identificados

* Imposibilidad de operar sin conexión a internet.
* Pérdida de datos fotográficos y de ubicación capturados en campo.
* Proceso de sincronización manual propenso a errores y olvidos.
* Falta de extracción automática de coordenadas GPS desde los metadatos EXIF de las fotos.
* Ausencia de visualización geográfica centralizada de los puntos registrados.
* Gestión fragmentada del ciclo de vida de fotografías y puntos de interés.

**Ejemplo:**
Un técnico captura 15 fotos en una instalación sin cobertura de datos. Al regresar a la oficina, debe transcribir manualmente las coordenadas y subir las fotos una por una a un sistema web, con riesgo de perder datos o asignar ubicaciones incorrectas.

---

# 4. Necesidades de Negocio

## NB-01 Registrar fotos georeferenciadas en campo sin conexión a internet

La organización necesita que los técnicos de campo puedan capturar fotografías georeferenciadas y registrar puntos de interés incluso en zonas sin cobertura de datos, garantizando que la información capturada no se pierda.

**Ejemplo:** Un técnico trabaja en una zona rural sin señal y necesita registrar 10 puntos con fotos. Todas las operaciones se guardan localmente y se sincronizan al volver a zona con cobertura.

**Trazabilidad:** CU-01, CU-02, CU-03, CU-04

---

## NB-02 Sincronizar datos de campo con el servidor sin intervención manual

La organización necesita que los datos capturados en campo se sincronicen automáticamente con el servidor central cuando el dispositivo recupere conectividad, sin requerir acción alguna del usuario.

**Ejemplo:** El técnico termina su jornada de campo, al llegar a un área con Wi-Fi los datos se sincronizan automáticamente en background.

**Trazabilidad:** CU-09, CU-10, CU-11, CU-12

---

## NB-03 Visualizar en mapa todos los puntos registrados con acceso directo a sus fotos

La organización necesita una vista geográfica centralizada que muestre todos los puntos capturados como markers en un mapa interactivo, con acceso inmediato a las fotografías y datos de cada punto.

**Ejemplo:** Un supervisor abre el sistema web, ve un mapa con 50 markers distribuidos geográficamente y hace click en uno para ver las fotos y coordenadas del punto.

**Trazabilidad:** CU-05, CU-06, CU-07

---

## NB-04 Gestionar el ciclo de vida completo de fotografías georeferenciadas

La organización necesita poder crear, consultar, editar y eliminar puntos y fotografías georeferenciadas, tanto desde la web como desde el dispositivo móvil.

**Ejemplo:** Un supervisor detecta un punto con datos incorrectos, edita el nombre y descripción desde la web, y la corrección se propaga al dispositivo del técnico en la próxima sincronización.

**Trazabilidad:** CU-07, CU-08, CU-13, CU-14, CU-16

---

# 5. Resultados Esperados del Negocio

* Eliminación total de la pérdida de datos capturados en campo.
* Reducción del tiempo entre captura y disponibilidad centralizada de la información.
* Eliminación del proceso manual de transcripción de coordenadas.
* Visibilidad geográfica completa y actualizada de todos los puntos registrados.
* Autonomía operativa de los técnicos de campo independientemente de la conectividad.

---

# 6. Indicadores de Éxito del Negocio

| Indicador | Valor Objetivo |
|-----------|---------------|
| Pérdida de datos en campo | 0% |
| Operaciones disponibles offline | 100% |
| Tiempo de sync tras reconexión | < 60 segundos (50 operaciones) |
| Fotos con extracción GPS automática exitosa | ≥ 95% |
| Puntos visibles en mapa tras sync | 100% |

---

# 7. Stakeholders Clave

| Stakeholder | Rol | Interés Principal |
|-------------|-----|------------------|
| Técnicos de campo | Usuario Mobile | Capturar datos sin depender de red |
| Supervisores | Usuario Web | Visualizar y gestionar puntos centralizados |
| Administradores de sistema | Soporte | Monitorear estado de sync, resolver fallos |

---

# 8. Supuestos de Negocio

* Los dispositivos Android de los técnicos cuentan con cámara y GPS.
* Las fotos capturadas con la cámara nativa contienen metadatos EXIF con coordenadas GPS en la mayoría de los casos.
* Existe conectividad periódica (Wi-Fi o datos móviles) que permite la sincronización, aunque no sea permanente.
* El sistema opera con un usuario por dispositivo (sin autenticación en v1.0).
* El volumen esperado es de hasta 100 puntos por proyecto con 1-5 fotos por punto.

---

# 9. Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |

---

**Fin del documento**
