# Guía de UX Writing y Microcopy

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** ux-writing-microcopy_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-14  
**Autor:** Equipo Técnico

---

## 1. Propósito

Este documento establece los lineamientos de escritura para todos los textos visibles en la interfaz de GeoFoto, tanto en la aplicación móvil (MAUI Android) como en la versión web (Blazor). Define el tono, la voz, las convenciones de microcopy y el catálogo completo de textos por pantalla y componente.

El objetivo es garantizar consistencia, claridad y empatía en toda la comunicación textual de la aplicación, considerando que el usuario principal es un técnico de campo que opera bajo presión, con conectividad limitada y en condiciones ambientales adversas.

---

## 2. Perfil del Usuario

El técnico de campo:

- Trabaja en exteriores, frecuentemente con una sola mano libre.
- Opera en zonas con conectividad nula o intermitente.
- Necesita feedback inmediato y certero sobre el estado de sus acciones.
- No tiene formación técnica en informática; espera una interfaz intuitiva.
- Prioriza velocidad y confiabilidad por encima de estética.

---

## 3. Principios de Escritura

### 3.1 Tono y Voz

La voz de GeoFoto es **profesional, directa y tranquilizadora**. El tono varía según el contexto:

| Contexto | Tono | Ejemplo |
|----------|------|---------|
| Confirmación de acción exitosa | Breve, positivo | "Foto guardada" |
| Advertencia recuperable | Informativo, sin alarma | "La imagen no contiene ubicación GPS" |
| Error que bloquea al usuario | Claro, orientado a la solución | "No se pudo acceder a la cámara. Revisá los permisos en Configuración." |
| Estado del sistema | Neutro, factual | "3 operaciones pendientes de sincronizar" |
| Modo offline | Tranquilizador | "Sin conexión — los datos se guardan en el dispositivo" |

### 3.2 Reglas Generales de Escritura

1. **Tuteo con "vos":** La aplicación usa el voseo rioplatense informal. "Arrastrá", "Tocá", "Revisá". No usar "usted" ni "tú".
2. **Oraciones cortas:** Máximo 12 palabras por mensaje de feedback. Los snackbars deben leerse en menos de 3 segundos.
3. **Voz activa:** "Se guardó la foto" → "Foto guardada". "Se detectó un error" → "Error al guardar".
4. **Sin jerga técnica:** No usar "EXIF", "SQLite", "SyncQueue", "API", "HTTP" en textos visibles al usuario. Traducir a lenguaje funcional.
5. **Sin signos de exclamación** en errores ni advertencias. Reservar para celebraciones puntuales ("¡Sincronización completa!").
6. **Verbos en infinitivo** para labels de botones: "Guardar", "Cancelar", "Sincronizar", "Eliminar". No usar gerundios en botones.
7. **Sin puntos finales** en labels de botones, títulos, chips ni badges. Sí usar puntos en párrafos explicativos y tooltips.

### 3.3 Formato de Números y Fechas

| Dato | Formato | Ejemplo |
|------|---------|---------|
| Fecha corta | dd/MM/yyyy | 13/04/2026 |
| Fecha con hora | dd/MM/yyyy HH:mm | 13/04/2026 10:15 |
| Fecha relativa (< 24h) | "hace X min", "hace X h" | "hace 5 min" |
| Coordenadas | decimal con 4 decimales | -34.6037, -58.3816 |
| Contadores en badges | Número solo | 3 |
| Contadores en texto | Número + unidad | "3 fotos", "5 operaciones" |
| Tamaño de archivo | KB o MB, 1 decimal | "2.3 MB" |

---

## 4. Catálogo de Microcopy por Contexto

### 4.1 Permisos del Sistema

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Solicitud inicial de ubicación | MudDialog | **Título:** "GeoFoto necesita tu ubicación" · **Cuerpo:** "Para centrar el mapa y registrar coordenadas de las fotos, necesitamos acceder a tu ubicación." · **Botón primario:** "Permitir" · **Botón secundario:** "Ahora no" |
| Solicitud inicial de cámara | MudDialog | **Título:** "GeoFoto necesita acceder a la cámara" · **Cuerpo:** "Para capturar fotos georeferenciadas en campo." · **Botón primario:** "Permitir" · **Botón secundario:** "Ahora no" |
| Permiso de ubicación denegado (primera vez) | MudSnackbar Warning | "Sin ubicación GPS — el mapa se muestra en posición predeterminada" |
| Permiso de ubicación denegado permanentemente | MudDialog | **Título:** "Ubicación deshabilitada" · **Cuerpo:** "GeoFoto necesita acceso a tu ubicación para funcionar correctamente. Podés habilitarla desde la configuración del dispositivo." · **Botón primario:** "Ir a Configuración" · **Botón secundario:** "Continuar sin ubicación" |
| Permiso de cámara denegado permanentemente | MudDialog | **Título:** "Cámara deshabilitada" · **Cuerpo:** "Para capturar fotos, habilitá el permiso de cámara en la configuración del dispositivo." · **Botón primario:** "Ir a Configuración" · **Botón secundario:** "Cancelar" |

### 4.2 Mapa y Navegación

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Título de la app | MudAppBar | "GeoFoto" |
| Navegación: Mapa | MudNavMenu | "Mapa" |
| Navegación: Subir fotos | MudNavMenu | "Subir" |
| Navegación: Listado | MudNavMenu | "Lista" |
| Navegación: Sincronización | MudNavMenu | "Sync" |
| Tooltip del FAB de cámara | MudFab tooltip | "Tomar foto" |
| Mapa cargando | MudProgressLinear | (sin texto, solo barra de progreso) |
| Mapa sin markers | Overlay centrado | "No hay puntos registrados. Tocá 📷 para capturar tu primera foto." |
| Contador de puntos | MudChip | "{n} puntos" |

### 4.3 Captura de Fotos y Flujo de Marker

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Foto capturada exitosamente (primera foto de un marker) | MudSnackbar Success | "Foto guardada — completá los datos del punto" |
| Foto capturada exitosamente (fotos adicionales al marker) | MudSnackbar Success | "Foto agregada al punto" |
| Diálogo de descripción (primera foto) | MudDialog | **Título:** "Nuevo punto de registro" · **Campo nombre (placeholder):** "Nombre del punto" · **Campo descripción (placeholder):** "Descripción (opcional)" · **Botón primario:** "Guardar" · **Botón secundario:** "Omitir" |
| Error al capturar foto | MudSnackbar Error | "No se pudo guardar la foto" |
| Foto sin GPS | MudSnackbar Warning | "La foto no tiene ubicación — se usó la posición del dispositivo" |
| Cámara no disponible | MudAlert Error | "No se pudo acceder a la cámara. Revisá los permisos en Configuración." |

### 4.4 Configuración del Radio de Asociación

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Botón de acceso | MudIconButton tooltip | "Radio de agrupación" |
| Diálogo de configuración | MudDialog | **Título:** "Radio de agrupación de fotos" · **Texto explicativo:** "Las fotos capturadas dentro de este radio se asocian automáticamente al marker más cercano. Si no hay un marker dentro del radio, se crea uno nuevo." · **Label del slider/input:** "Radio en metros" · **Valor por defecto:** "50 m" · **Rango:** "10 m — 500 m" · **Botón primario:** "Guardar" · **Botón secundario:** "Cancelar" |
| Confirmación de cambio | MudSnackbar Info | "Radio de agrupación actualizado a {n} m" |

### 4.5 Detalle de Punto (Drawer / Bottom Sheet)

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Título del panel | MudText h6 | "{nombre del punto}" |
| Coordenadas | MudText caption | "Lat: {lat} · Lng: {lng}" |
| Fecha de creación | MudText caption | "Creado: {fecha}" |
| Campo nombre (placeholder) | MudTextField | "Nombre del punto" |
| Campo descripción (placeholder) | MudTextField | "Descripción (opcional)" |
| Botón guardar | MudButton | "Guardar" |
| Botón cancelar | MudButton | "Cancelar" |
| Guardar exitoso | MudSnackbar Success | "Punto actualizado" |
| Error al guardar | MudSnackbar Error | "Error al guardar el punto" |
| Botón agregar fotos | MudButton | "Agregar fotos" |
| Fotos subiendo | MudButton disabled | "Subiendo..." |
| Sin fotos | MudText caption | "Sin fotos" |
| Contador de fotos en carrusel | Overlay | "📷 {n} foto(s)" |
| Indicador de foto actual | MudText caption | "Foto {i} de {n}" |

### 4.6 Visor de Fotos (Fullscreen)

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Título del diálogo | MudDialog title | "Fotos de {nombre del punto}" |
| Indicador de posición | MudText | "{i} de {n}" |
| Botón cerrar | MudIconButton tooltip | "Cerrar" |
| Botón anterior | MudIconButton tooltip | "Foto anterior" |
| Botón siguiente | MudIconButton tooltip | "Foto siguiente" |

### 4.7 Listado de Markers

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Título de pantalla | MudText h5 | "Puntos registrados" |
| Campo de búsqueda (placeholder) | MudTextField | "Buscar por nombre..." |
| Columna: miniatura | MudTable header | "Foto" |
| Columna: nombre | MudTable header | "Nombre" |
| Columna: coordenadas | MudTable header | "Ubicación" |
| Columna: fotos | MudTable header | "Fotos" |
| Columna: estado sync | MudTable header | "Estado" |
| Columna: acciones | MudTable header | "Acciones" |
| Chip sincronizado | MudChip Success | "Sincronizado" |
| Chip pendiente | MudChip Warning | "Pendiente" |
| Chip fallido | MudChip Error | "Fallido" |
| Tooltip editar | MudIconButton tooltip | "Editar punto" |
| Tooltip eliminar | MudIconButton tooltip | "Eliminar punto" |
| Sin resultados de búsqueda | MudAlert Info | "No se encontraron puntos con ese nombre" |
| Listado vacío | MudAlert Info | "No hay puntos registrados. Comenzá capturando fotos desde el mapa." |

### 4.8 Eliminación de Punto

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Diálogo de confirmación | MudDialog | **Título:** "Eliminar punto" · **Cuerpo:** "¿Estás seguro de eliminar «{nombre}»? Se eliminarán también las {n} foto(s) asociadas. Esta acción no se puede deshacer." · **Botón primario (destructivo):** "Eliminar" · **Botón secundario:** "Cancelar" |
| Eliminación exitosa | MudSnackbar Success | "Punto eliminado" |
| Error al eliminar | MudSnackbar Error | "Error al eliminar el punto" |

### 4.9 Subir Fotos (Pantalla Web)

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Título de pantalla | MudText h5 | "Subir fotografías georeferenciadas" |
| Zona de upload (placeholder) | MudFileUpload | "Arrastrá tus fotos o tocá para elegir" |
| Formatos aceptados | MudText caption | "Formatos: JPG, PNG, WebP" |
| Estado procesando | MudChip Info | "Procesando..." |
| Estado ok | MudChip Success | "OK" |
| Estado sin GPS | MudChip Warning | "Sin GPS" |
| Estado error | MudChip Error | "Error" |
| Resultado: punto creado | MudAlert Success | "Punto creado" |
| Resultado: sin GPS | MudAlert Warning | "Guardado en (0, 0)" |
| Resultado: error formato | MudAlert Error | "Formato no soportado" |
| Botón ver mapa | MudButton | "Ver en el mapa" |

### 4.10 Conectividad y Sincronización

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Conectado | MudChip Success | "Conectado" |
| Sin conexión | MudChip Error | "Sin conexión" |
| Sincronizando | MudChip Warning | "Sincronizando" |
| Badge de pendientes | MudBadge | "{n}" (solo número) |
| Tooltip badge: sin red + pendientes | Tooltip | "Sin conexión — {n} operación(es) esperando sync" |
| Tooltip badge: con red + pendientes | Tooltip | "{n} operación(es) pendientes de sincronizar" |
| Tooltip badge: sincronizando | Tooltip | "Sincronizando con el servidor..." |
| Tooltip badge: sin red, todo ok | Tooltip | "Sin conexión — todo sincronizado" |
| Tooltip badge: todo ok | Tooltip | "Todo sincronizado" |
| Snackbar: inicio sync | MudSnackbar Info (3s) | "Sincronización iniciada..." |
| Snackbar: sync exitosa | MudSnackbar Success (5s) | "Sincronización completa: {n} operaciones procesadas" |
| Snackbar: conflicto | MudSnackbar Warning (5s) | "Conflicto detectado y resuelto automáticamente" |
| Snackbar: sync fallida | MudSnackbar Error (10s) | "Error en la sincronización. Revisá el estado en Sync." |
| Snackbar: sin conexión | MudSnackbar Info (5s) | "Sin conexión — los datos se guardan en el dispositivo" |
| Snackbar: conexión restaurada | MudSnackbar Success (3s) | "Conexión restaurada — sincronizando..." |
| Sin operaciones pendientes | MudSnackbar Info | "No hay operaciones pendientes" |
| Pantalla Sync: título | MudText h5 | "Estado de sincronización" |
| Tarjeta: pendientes | MudCard | "Pendientes" |
| Tarjeta: sincronizados | MudCard | "Sincronizados" |
| Tarjeta: conflictos | MudCard | "Conflictos" |
| Tarjeta: fallidos | MudCard | "Fallidos" |
| Botón sync manual | MudButton | "Sincronizar ahora" |
| Botón sync manual disabled | MudButton tooltip | "Necesitás conexión para sincronizar" |
| Tabla historial: vacía | MudAlert Info | "No hay operaciones registradas" |
| Columna: operación | MudTable header | "#" |
| Columna: tipo | MudTable header | "Tipo" |
| Columna: entidad | MudTable header | "Entidad" |
| Columna: estado | MudTable header | "Estado" |
| Columna: intentos | MudTable header | "Intentos" |
| Columna: último intento | MudTable header | "Último intento" |
| Columna: error | MudTable header | "Error" |
| Botón reintentar (solo Failed) | MudButton | "Reintentar" |

### 4.11 Mensajes de Error Genéricos

| Situación | Componente | Texto |
|-----------|-----------|-------|
| Error de red genérico | MudSnackbar Error | "Error de conexión. Reintentá en unos segundos." |
| Error inesperado | MudSnackbar Error | "Ocurrió un error inesperado" |
| Espacio insuficiente | MudAlert Error | "Espacio insuficiente en el dispositivo" |
| Archivo demasiado grande | MudSnackbar Warning | "El archivo supera el tamaño máximo de 10 MB" |
| Formato no soportado | MudSnackbar Warning | "Formato no soportado. Usá JPG, PNG o WebP." |

---

## 5. Convenciones de Componentes MudBlazor

### 5.1 Severidad y Duración de MudSnackbar

| Severidad | Color | Duración | Uso |
|-----------|-------|----------|-----|
| Success | Verde | 3 segundos | Confirmaciones de acciones completadas |
| Info | Azul | 5 segundos | Información de estado, transiciones |
| Warning | Naranja | 5 segundos | Advertencias recuperables, datos parciales |
| Error | Rojo | 10 segundos | Errores que requieren atención del usuario |

### 5.2 MudDialog: Estructura Estándar

Todos los diálogos siguen esta estructura:

- **Título:** Verbo + sustantivo o pregunta directa. Máximo 5 palabras.
- **Cuerpo:** Contexto necesario para tomar la decisión. Máximo 2 oraciones.
- **Botón primario (derecha):** Acción afirmativa. Color según severidad.
- **Botón secundario (izquierda):** Siempre "Cancelar" o alternativa neutra.

Para acciones destructivas, el botón primario usa `Color.Error` y texto específico ("Eliminar", no "Aceptar").

### 5.3 Placeholders de MudTextField

Los placeholders usan sustantivos descriptivos sin artículos: "Nombre del punto", "Descripción (opcional)", "Buscar por nombre...". El sufijo "(opcional)" se agrega a campos no requeridos.

### 5.4 Tooltips

Los tooltips usan infinitivo sin artículo: "Tomar foto", "Editar punto", "Cerrar", "Foto anterior". Máximo 3 palabras.

---

## 6. Textos Específicos de la Aplicación Móvil (MAUI)

| Situación | Texto |
|-----------|-------|
| Primer inicio: bienvenida | No se muestra bienvenida. La app abre directamente en el mapa. |
| GPS obtenido, mapa centrado | (Sin feedback textual — el mapa se centra silenciosamente) |
| GPS no disponible, posición por defecto | MudSnackbar Warning: "No se pudo obtener la ubicación — mostrando posición predeterminada" |
| Posición por defecto (sin GPS) | Centro de Argentina: -34.6037, -58.3816 (Buenos Aires) |
| Modo offline activo | Chip rojo permanente "Sin conexión" en MudAppBar + badge de pendientes |

---

## 7. Glosario de Términos de Interfaz

Para mantener consistencia, se utilizan los siguientes términos en toda la aplicación:

| Término interno / técnico | Término visible al usuario |
|---------------------------|---------------------------|
| Punto / Marker | "Punto" o "punto de registro" |
| Foto / Imagen | "Foto" |
| SyncQueue / Cola de sincronización | "Operaciones pendientes" |
| SyncStatus | "Estado" |
| PendingCreate / PendingUpdate | "Pendiente" |
| Synced | "Sincronizado" |
| Failed | "Fallido" |
| Conflict | "Conflicto" |
| Radio de asociación | "Radio de agrupación" |
| fitBounds | (no visible — acción automática del mapa) |
| EXIF / GPS | "Ubicación" o "coordenadas" |
| AppDataDirectory | (no visible — almacenamiento interno) |
| Offline-first | (no visible — comportamiento transparente) |

---

## 8. Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-14 | Equipo Técnico | Creación inicial con catálogo completo de microcopy, principios de escritura y convenciones de componentes. |

---

**Fin del documento**
