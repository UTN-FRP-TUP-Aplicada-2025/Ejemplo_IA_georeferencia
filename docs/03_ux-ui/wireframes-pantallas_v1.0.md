# Wireframes de Pantallas

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** wireframes-pantallas_v1.0.md
**Versión:** 1.0
**Estado:** Borrador
**Fecha:** 2026-04-13
**Autor:** Equipo Técnico

---

## 1. Propósito

El presente documento tiene como objetivo representar la estructura visual de cada pantalla de la aplicación GeoFoto mediante diagramas ASCII, identificando los componentes MudBlazor que corresponden a cada elemento de la interfaz de usuario. Los wireframes constituyen una referencia de baja fidelidad que facilita la comunicación entre diseño y desarrollo, permitiendo validar la disposición de los elementos antes de la implementación definitiva.

Cada wireframe se acompaña de una tabla de componentes que establece la correspondencia directa entre las áreas visuales del diagrama y los componentes concretos de la librería MudBlazor que se utilizarán en la implementación.

---

## 2. Convenciones

- Los recuadros ASCII representan áreas visuales delimitadas de la interfaz.
- Cada wireframe se compone de dos partes:
  - **(a)** Diagrama ASCII que ilustra la disposición espacial de los elementos.
  - **(b)** Tabla de componentes MudBlazor utilizados en la pantalla.
- Los nombres de componentes siguen la nomenclatura oficial de MudBlazor: `MudAppBar`, `MudDrawer`, `MudNavMenu`, `MudCard`, `MudTable`, `MudButton`, `MudTextField`, `MudChip`, `MudAlert`, `MudBadge`, `MudDialog`, `MudFab`, `MudGrid`, `MudProgressLinear`, `MudFileUpload`, `MudPaper`, `MudContainer`, `MudIconButton`, `MudText`, entre otros.
- El símbolo `≡` representa el ícono de menú hamburguesa.
- Los corchetes `[ ]` indican botones o elementos interactivos.
- Las líneas `┌ ┐ └ ┘ ─ │ ┬ ┴ ┼ ├ ┤` conforman los bordes de las áreas visuales.

---

## 3. Pantalla: Mapa (`/`)

### 3.a Wireframe

```
┌─ MudAppBar ─────────────────────────────────────────────────┐
│ ≡  GeoFoto       [Mapa] [Subir] [Lista] [Sync]       [●3]  │
│                   MudNavMenu links                 MudBadge  │
└─────────────────────────────────────────────────────────────┘
┌─ MudLayout ────────────────────────┬──── MudDrawer ─────────┐
│                                    │                         │
│   <div id="map">                   │  MudCard (detalle)      │
│   (Leaflet.js)                     │  ┌───────────────────┐  │
│                                    │  │ [Foto] [Foto]     │  │
│   [📍marker]   [📍marker]          │  │ FotoCarousel      │  │
│                                    │  ├───────────────────┤  │
│          [📍cluster: 3]            │  │ Lat: -37.4609     │  │
│                                    │  │ Lng: -61.9328     │  │
│                                    │  ├───────────────────┤  │
│                                    │  │ MudTextField      │  │
│                                    │  │  Nombre           │  │
│                                    │  │ MudTextField      │  │
│                                    │  │  Descripción      │  │
│                                    │  ├───────────────────┤  │
│                                    │  │ [MudButton        │  │
│                                    │  │  Guardar]         │  │
│                                    │  └───────────────────┘  │
└────────────────────────────────────┴─────────────────────────┘
```

### 3.b Componentes MudBlazor

| Componente MudBlazor | Ubicación | Función |
|---|---|---|
| `MudAppBar` | Parte superior | Barra de navegación principal con título de la aplicación. |
| `MudNavMenu` | Dentro de `MudAppBar` | Enlaces de navegación entre las pantallas principales. |
| `MudBadge` | Extremo derecho de `MudAppBar` | Indicador numérico de puntos pendientes de sincronización. |
| `MudLayout` | Área central izquierda | Contenedor principal que alberga el mapa interactivo. |
| `MudDrawer` | Panel lateral derecho | Panel deslizable que muestra el detalle del punto seleccionado. |
| `MudCard` | Dentro de `MudDrawer` | Tarjeta con la información del punto geográfico seleccionado. |
| `FotoCarousel` | Dentro de `MudCard` | Carrusel de fotografías asociadas al punto. |
| `MudTextField` | Dentro de `MudCard` | Campos de edición para nombre y descripción del punto. |
| `MudButton` | Parte inferior de `MudCard` | Botón de acción para guardar los cambios del punto. |
| Leaflet.js (`<div id="map">`) | Área central | Mapa interactivo con marcadores y clusters geográficos. |

---

## 4. Pantalla: Subir Fotos (`/subir`)

### 4.a Wireframe

```
┌─ MudAppBar ─────────────────────────────────────────────────┐
│ ≡  GeoFoto                                   [●3 pendientes]│
└─────────────────────────────────────────────────────────────┘
┌─ MudContainer ──────────────────────────────────────────────┐
│                                                              │
│  MudText h5: "Subir fotografías georeferenciadas"            │
│                                                              │
│  ┌─ MudPaper (upload zone) ───────────────────────────────┐  │
│  │                                                         │  │
│  │  MudFileUpload (drag & drop)                            │  │
│  │                                                         │  │
│  │  "Arrastrá tus fotos o hacé click para elegir"          │  │
│  │                                                         │  │
│  │  Acepta: jpg, png, webp                                 │  │
│  │                                                         │  │
│  └─────────────────────────────────────────────────────────┘  │
│                                                              │
│  MudTable (resultados):                                      │
│  ┌───────────┬──────────┬────────────┬─────────────────────┐  │
│  │ Archivo   │ Estado   │ GPS        │ Resultado            │  │
│  ├───────────┼──────────┼────────────┼─────────────────────┤  │
│  │ img1.jpg  │ MudChip  │ -37.4609   │ MudAlert verde       │  │
│  │           │ "OK"     │ -61.9328   │ "Punto creado"       │  │
│  ├───────────┼──────────┼────────────┼─────────────────────┤  │
│  │ img2.jpg  │ MudChip  │ Sin GPS    │ MudAlert naranja     │  │
│  │           │ "Sin GPS"│            │ "Guardado en (0,0)"  │  │
│  ├───────────┼──────────┼────────────┼─────────────────────┤  │
│  │ img3.jpg  │ MudChip  │ Error      │ MudAlert rojo        │  │
│  │           │ "Error"  │            │ "Formato inválido"   │  │
│  └───────────┴──────────┴────────────┴─────────────────────┘  │
│                                                              │
│  [MudButton "Ver en el mapa"]                                │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### 4.b Componentes MudBlazor

| Componente MudBlazor | Ubicación | Función |
|---|---|---|
| `MudAppBar` | Parte superior | Barra de navegación con indicador de pendientes. |
| `MudBadge` | Extremo derecho de `MudAppBar` | Muestra la cantidad de elementos pendientes de sincronización. |
| `MudContainer` | Área central | Contenedor principal de la página de subida. |
| `MudText` | Encabezado del contenedor | Título descriptivo de la pantalla (Typo `h5`). |
| `MudPaper` | Zona de carga | Superficie elevada que delimita el área de drag & drop. |
| `MudFileUpload` | Dentro de `MudPaper` | Control de carga de archivos con soporte de arrastrar y soltar. |
| `MudTable` | Debajo de la zona de carga | Tabla de resultados del procesamiento de las imágenes subidas. |
| `MudChip` | Columna "Estado" de la tabla | Indicador visual del estado de procesamiento de cada imagen. |
| `MudAlert` | Columna "Resultado" de la tabla | Mensaje de resultado con color según el tipo de outcome. |
| `MudButton` | Parte inferior del contenedor | Botón de navegación al mapa para visualizar los puntos creados. |

---

## 5. Pantalla: Lista de Puntos (`/lista`)

### 5.a Wireframe

```
┌─ MudAppBar ─────────────────────────────────────────────────┐
│ ≡  GeoFoto       [Mapa] [Subir] [Lista] [Sync]       [●3]  │
└─────────────────────────────────────────────────────────────┘
┌─ MudContainer ──────────────────────────────────────────────┐
│                                                              │
│  MudText h5: "Lista de puntos registrados"                   │
│                                                              │
│  MudTextField (búsqueda): "Buscar por nombre..."             │
│                                                              │
│  MudTable:                                                   │
│  ┌──────┬──────────┬──────────┬──────────┬─────┬──────┬────┐ │
│  │Mini  │ Nombre   │ Latitud  │ Longitud │Fotos│Sync  │Acc.│ │
│  ├──────┼──────────┼──────────┼──────────┼─────┼──────┼────┤ │
│  │[img] │ Punto A  │ -37.4609 │ -61.9328 │  3  │MudChip│✏🗑│ │
│  │      │          │          │          │     │verde │    │ │
│  ├──────┼──────────┼──────────┼──────────┼─────┼──────┼────┤ │
│  │[img] │ Punto B  │ -37.3201 │ -59.1325 │  1  │MudChip│✏🗑│ │
│  │      │          │          │          │     │naranj│    │ │
│  ├──────┼──────────┼──────────┼──────────┼─────┼──────┼────┤ │
│  │[img] │ Punto C  │  0.0000  │  0.0000  │  2  │MudChip│✏🗑│ │
│  │      │          │          │          │     │rojo  │    │ │
│  └──────┴──────────┴──────────┴──────────┴─────┴──────┴────┘ │
│                                                              │
│  MudTablePager: [< 1 2 3 ... >]                              │
│                                                              │
│  ┌─ MudDialog (confirmación eliminar) ─────────────────────┐ │
│  │  MudText: "¿Confirma la eliminación del punto 'X'?"     │ │
│  │  [MudButton Cancelar]  [MudButton Eliminar color=Error] │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### 5.b Componentes MudBlazor

| Componente MudBlazor | Ubicación | Función |
|---|---|---|
| `MudAppBar` | Parte superior | Barra de navegación principal. |
| `MudContainer` | Área central | Contenedor principal de la pantalla de listado. |
| `MudText` | Encabezado del contenedor | Título de la pantalla (Typo `h5`). |
| `MudTextField` | Debajo del título | Campo de búsqueda para filtrar puntos por nombre. |
| `MudTable` | Zona central | Tabla con la lista completa de puntos registrados. |
| `MudImage` | Columna "Miniatura" | Imagen en miniatura de la primera foto del punto. |
| `MudChip` | Columna "Sync" | Indicador de estado de sincronización con color semántico: verde (`Synced`), naranja (`Pending`), rojo (`Failed`). |
| `MudIconButton` | Columna "Acciones" | Botones de edición (ícono lápiz) y eliminación (ícono papelera). |
| `MudTablePager` | Debajo de la tabla | Control de paginación de resultados. |
| `MudDialog` | Modal superpuesto | Diálogo de confirmación previo a la eliminación de un punto. |
| `MudButton` | Dentro de `MudDialog` | Botones de acción: "Cancelar" y "Eliminar". |

---

## 6. Pantalla: Estado de Sincronización (`/sync`)

### 6.a Wireframe

```
┌─ MudAppBar ─────────────────────────────────────────────────┐
│ ≡  GeoFoto       [Mapa] [Subir] [Lista] [Sync]       [●3]  │
└─────────────────────────────────────────────────────────────┘
┌─ MudContainer ──────────────────────────────────────────────┐
│                                                              │
│  MudText h5: "Estado de sincronización"                      │
│                                                              │
│  ┌─── MudGrid ───────────────────────────────────────────┐   │
│  │ ┌─ MudCard ──┐ ┌─ MudCard ──┐ ┌─ MudCard ─┐ ┌─ MudCard┐│
│  │ │ Pendientes │ │Sincronizad.│ │Conflictos  │ │ Fallidos ││
│  │ │            │ │            │ │            │ │          ││
│  │ │    [3]     │ │   [47]     │ │    [0]     │ │   [1]    ││
│  │ │  naranja   │ │   verde    │ │   gris     │ │  rojo    ││
│  │ └────────────┘ └────────────┘ └────────────┘ └──────────┘│
│  └───────────────────────────────────────────────────────┘   │
│                                                              │
│  [MudButton "Sincronizar ahora" Color=Primary]               │
│                                                              │
│  MudProgressLinear (visible durante sincronización activa)    │
│  ████████████░░░░░░░░░░░░░░░░░░░░  40%                       │
│                                                              │
│  MudText h6: "Historial de operaciones"                      │
│                                                              │
│  MudTable:                                                   │
│  ┌─────────────────┬────────┬───────────┬────────┬─────────┐ │
│  │ Fecha           │ Tipo   │ Entidad   │ Estado │ Error   │ │
│  ├─────────────────┼────────┼───────────┼────────┼─────────┤ │
│  │ 2026-04-13 10:05│ Push   │ Punto A   │MudChip │   —     │ │
│  │                 │        │           │ verde  │         │ │
│  ├─────────────────┼────────┼───────────┼────────┼─────────┤ │
│  │ 2026-04-13 10:05│ Push   │ Punto B   │MudChip │Timeout  │ │
│  │                 │        │           │ rojo   │         │ │
│  ├─────────────────┼────────┼───────────┼────────┼─────────┤ │
│  │ 2026-04-13 09:30│ Pull   │ Punto C   │MudChip │   —     │ │
│  │                 │        │           │ verde  │         │ │
│  └─────────────────┴────────┴───────────┴────────┴─────────┘ │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### 6.b Componentes MudBlazor

| Componente MudBlazor | Ubicación | Función |
|---|---|---|
| `MudAppBar` | Parte superior | Barra de navegación principal. |
| `MudContainer` | Área central | Contenedor principal de la pantalla de sincronización. |
| `MudText` | Encabezados | Títulos de sección (Typo `h5` y `h6`). |
| `MudGrid` | Zona de resumen | Grilla responsiva para las tarjetas de métricas. |
| `MudCard` | Dentro de `MudGrid` | Tarjetas de resumen con contadores: Pendientes, Sincronizados, Conflictos, Fallidos. |
| `MudButton` | Debajo de las tarjetas | Botón de acción para iniciar la sincronización manual. |
| `MudProgressLinear` | Debajo del botón | Barra de progreso visible durante el proceso de sincronización activo. |
| `MudTable` | Zona inferior | Tabla de historial de operaciones de sincronización. |
| `MudChip` | Columna "Estado" de la tabla | Indicador visual del resultado de cada operación con color semántico. |

---

## 7. Pantalla: MAUI — Pantalla Principal

### 7.a Wireframe

```
┌─ MudAppBar (Color=Secondary) ───────────────────┐
│ ≡  GeoFoto Mobile                    [●3] [⟳]   │
│                              SyncStatusBadge     │
└──────────────────────────────────────────────────┘
┌──────────────────────────────────────────────────┐
│                                                  │
│   <div id="map">                                 │
│   (Leaflet.js — vista móvil portrait)            │
│                                                  │
│                                                  │
│       [📍marker]                                  │
│                                                  │
│                 [📍marker]                        │
│                                                  │
│   [📍cluster: 5]                                  │
│                                                  │
│                                                  │
│                                                  │
│                                                  │
│                                                  │
│                                                  │
│                                                  │
│                                  ┌─────────────┐ │
│                                  │  MudFab     │ │
│                                  │  📷 Cámara   │ │
│                                  └─────────────┘ │
└──────────────────────────────────────────────────┘
┌─ MudDrawer (Bottom Sheet — al tocar marker) ────┐
│                                                  │
│  MudCard (detalle compacto)                      │
│  ┌────────────────────────────────────────────┐  │
│  │ [Foto1] [Foto2]   FotoCarousel             │  │
│  ├────────────────────────────────────────────┤  │
│  │ Lat: -37.4609     Lng: -61.9328            │  │
│  │ MudTextField Nombre                        │  │
│  │ MudTextField Descripción                   │  │
│  │ [MudButton Guardar]  [MudButton Cancelar]  │  │
│  └────────────────────────────────────────────┘  │
│                                                  │
└──────────────────────────────────────────────────┘
```

### 7.b Componentes MudBlazor

| Componente MudBlazor | Ubicación | Función |
|---|---|---|
| `MudAppBar` | Parte superior (Color=Secondary) | Barra de navegación diferenciada visualmente de la versión web mediante color secundario. |
| `SyncStatusBadge` | Extremo derecho de `MudAppBar` | Componente reutilizable que muestra el contador de pendientes de sincronización (siempre visible). |
| `MudIconButton` | Junto al badge en `MudAppBar` | Botón de sincronización manual (ícono de recarga). |
| Leaflet.js (`<div id="map">`) | Área central completa | Mapa interactivo adaptado a disposición vertical (portrait) del dispositivo móvil. |
| `MudFab` | Esquina inferior derecha flotante | Botón de acción flotante con ícono de cámara que invoca `MediaPicker` para captura de fotografía. |
| `MudDrawer` | Panel inferior deslizable | Panel tipo bottom sheet que se despliega al seleccionar un marcador en el mapa. |
| `MudCard` | Dentro de `MudDrawer` | Tarjeta compacta con detalle del punto seleccionado. |
| `FotoCarousel` | Dentro de `MudCard` | Carrusel horizontal de fotografías asociadas al punto. |
| `MudTextField` | Dentro de `MudCard` | Campos de edición para nombre y descripción del punto. |
| `MudButton` | Parte inferior de `MudCard` | Botones de acción "Guardar" y "Cancelar". |

### 7.c Diferencias con la versión Web

| Aspecto | Web (`/`) | MAUI (Pantalla Principal) |
|---|---|---|
| Color de `MudAppBar` | Primary | Secondary (diferenciación visual) |
| `SyncStatusBadge` | Presente | Siempre visible, sin posibilidad de ocultar |
| `MudFab` (cámara) | No presente | Esquina inferior derecha, lanza `MediaPicker` |
| `MudDrawer` | Panel lateral derecho | Bottom sheet (panel inferior deslizable) |
| Orientación del layout | Horizontal (desktop) | Vertical (portrait móvil) |
| Navegación | `MudNavMenu` con links | Menú hamburguesa con `MudDrawer` lateral |

---

## 8. Componentes Reutilizables

Se identifican los siguientes componentes compartidos entre las distintas pantallas de la aplicación, tanto en su versión web como en MAUI:

| Componente | Tipo | Descripción |
|---|---|---|
| `DetallePunto.razor` | `MudCard` compuesto | Tarjeta de detalle del punto geográfico que incluye carrusel de fotos, coordenadas, formulario de edición (nombre y descripción) y botones de acción. Se utiliza en la pantalla Mapa (dentro del `MudDrawer`) y en la pantalla principal MAUI. |
| `MarkerPopup.razor` | Popup Leaflet con MudBlazor | Popup compacto que se muestra al hacer click sobre un marcador en el mapa. Presenta miniatura de la primera foto, nombre del punto y enlace para abrir el detalle completo en el `MudDrawer`. |
| `FotoCarousel.razor` | Carrusel de imágenes | Componente de carrusel horizontal que muestra las fotografías asociadas a un punto geográfico. Soporta navegación con flechas y gestos táctiles en dispositivos móviles. Se utiliza dentro de `DetallePunto.razor`. |
| `SyncStatusBadge.razor` | `MudBadge` compuesto | Indicador visual del estado de sincronización que muestra el conteo de operaciones pendientes. Se posiciona en el `MudAppBar` y permanece siempre visible. El color del badge varía según el estado: verde (sin pendientes), naranja (pendientes activos), rojo (operaciones fallidas). |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---|---|---|---|
| 1.0 | 2026-04-13 | Equipo Técnico | Creación inicial del documento con wireframes de todas las pantallas principales y componentes reutilizables. |

---

**Fin del documento**
