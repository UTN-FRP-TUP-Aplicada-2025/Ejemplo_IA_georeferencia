# Necesidad de Negocio: Registro de Campo sin Conexión

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** NB-01-registro-campo-sin-conexion_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-13
**Autor:** Equipo Técnico

## NB-01 Registrar fotos georeferenciadas en campo sin conexión a internet

La organización necesita que los técnicos de campo puedan capturar fotografías georeferenciadas y registrar puntos de interés geográfico en ubicaciones donde no existe cobertura de datos, garantizando que la información capturada se preserve íntegramente y se sincronice con el servidor central cuando la conectividad esté disponible.

Este requerimiento es fundamental para la operación en campo, donde las condiciones de conectividad son impredecibles y la pérdida de datos tiene un costo operativo alto.

---

### Problema Específico

Los sistemas actuales de registro dependen de conectividad permanente a internet, lo que genera:

- Imposibilidad de operar en zonas rurales, instalaciones industriales o áreas subterráneas sin señal de datos móviles.
- Pérdida de fotos y coordenadas capturadas cuando la aplicación no puede comunicarse con el servidor.
- Los técnicos deben memorizar o anotar manualmente las ubicaciones para cargarlas posteriormente, introduciendo errores humanos.
- Doble trabajo al tener que registrar la misma información en campo y luego transcribirla al sistema central.

### Impacto si no se resuelve

- Pérdida irreversible de evidencia fotográfica y datos de ubicación capturados en campo.
- Incremento de errores en la asignación de coordenadas geográficas.
- Reducción de la productividad de los técnicos por doble carga de trabajo.
- Imposibilidad de expandir operaciones a zonas sin cobertura de datos.

---

### Criterios de Éxito

| Criterio | Métrica | Target |
|----------|---------|--------|
| Operaciones disponibles sin conexión | Captura de fotos y creación de puntos funcional en modo avión | 100% |
| Pérdida de datos en modo offline | Datos capturados que se pierden tras apagar/reiniciar la app | 0% |
| Persistencia local | Fotos y puntos almacenados en SQLite antes de cualquier intento de sync | 100% |
| Experiencia de usuario offline vs online | Diferencia funcional percibida por el técnico | Ninguna |

---

### Stakeholders

| Stakeholder | Rol | Relación con NB-01 |
|-------------|-----|-------------------|
| Técnicos de campo | Usuario principal | Capturan datos en zonas sin conectividad |
| Supervisores | Consumidor de datos | Necesitan que los datos lleguen al servidor sin pérdida |
| Administradores de sistema | Soporte técnico | Monitorean la cola de sincronización y resuelven fallos |

---

### Trazabilidad a Casos de Uso

| CU | Descripción | Relación con NB-01 |
|----|-------------|-------------------|
| CU-01 | Capturar foto desde galería | Selección de foto desde almacenamiento local del dispositivo |
| CU-02 | Capturar foto con cámara nativa (MAUI) | Captura directa con MediaPicker en campo |
| CU-03 | Extraer coordenadas GPS del EXIF | Obtención automática de ubicación desde metadatos |
| CU-04 | Guardar punto y foto en SQLite local | Persistencia local como primer paso antes del sync |

---

### Dependencias

- Prerequisito para NB-02 (la sincronización solo es relevante si existe data local capturada offline).
- Habilita NB-03 (los datos sincronizados se visualizan en el mapa).
- Alimenta NB-04 (los puntos creados offline forman parte del ciclo de vida gestionable).

---

### Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 1.0 | 2026-04-13 | Versión inicial |

---

**Fin del documento**
