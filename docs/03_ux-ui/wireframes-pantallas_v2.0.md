# Wireframes de Pantallas — v2.0

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First
**Documento:** wireframes-pantallas_v2.0.md
**Versión:** 2.0
**Estado:** Activo
**Fecha:** 2026-04-16
**Autor:** Equipo Técnico

---

## 1. Propósito

Este documento describe mediante wireframes en ASCII/Markdown las nuevas pantallas y componentes incorporados en Sprint 07–08 (épica GEO-E07). Para las pantallas existentes, consultar `wireframes-pantallas_v1.0.md`.

---

## 2. Pantallas Nuevas

---

### 2.1 Popup de Marker con Carrusel (MarkerPopup)

**Plataforma:** Android Mobile + Web  
**Componente:** MarkerPopup.razor  
**Stories:** GEO-US23, GEO-US25, GEO-US28, GEO-US29

```
╔══════════════════════════════════════════════════════╗
║  ✕  Detalle del punto                                ║
╠══════════════════════════════════════════════════════╣
║                                                      ║
║  Título:                                             ║
║  ┌──────────────────────────────────────────────┐    ║
║  │  Poste de alumbrado #47                      │    ║
║  └──────────────────────────────────────────────┘    ║
║                                                      ║
║  Descripción:                                        ║
║  ┌──────────────────────────────────────────────┐    ║
║  │  Base dañada, requiere                       │    ║
║  │  reemplazo                                   │    ║
║  └──────────────────────────────────────────────┘    ║
║                                                      ║
║  ─────────── Fotos (3) ───────────                   ║
║                                                      ║
║  ┌─────────────────────────────────────────────┐     ║
║  │   ◄   [     IMAGEN ACTUAL     ]   ►        │     ║
║  │       [   tap para ampliar    ]            │     ║
║  │                                            │     ║
║  │  Descripción de esta foto:                 │     ║
║  │  ┌──────────────────────────────────────┐  │     ║
║  │  │  Vista desde el norte                │  │     ║
║  │  └──────────────────────────────────────┘  │     ║
║  │                                            │     ║
║  │    [📷 Compartir]        [✕ Eliminar]     │     ║
║  │    (Mobile solo)         (confirm dialog)  │     ║
║  └─────────────────────────────────────────────┘     ║
║                                                      ║
║  Foto 1 de 3    ●  ○  ○                              ║
║                                                      ║
║  ─────────────────────────────────────────           ║
║                                                      ║
║  [📷 Agregar foto]    [🗑 Eliminar marker]           ║
║  (Mobile: cámara)     (confirm dialog)               ║
║  (Web: FileUpload)                                   ║
║                                                      ║
║  ─────────── Web only ────────────────              ║
║  [⬇ Descargar fotos]                               ║
║                                                      ║
╚══════════════════════════════════════════════════════╝
```

**Componentes MudBlazor:**
| Elemento | Componente | Notas |
|----------|------------|-------|
| Título editable | `MudTextField` | `@bind-Value`, guarda on-blur |
| Descripción multilínea | `MudTextField Lines="3"` | guarda on-blur |
| Imágenes del carrusel | `img` + CSS flexbox | prev/next con `MudIconButton` |
| Descripción de foto | `MudTextField` | guarda on-blur |
| Botón Compartir | `MudIconButton` | Solo en Mobile, `IsMobile=true` |
| Botón Eliminar foto | `MudIconButton` Color="Error" | abre `MudDialog` confirmación |
| Indicador posición | Dots HTML | posición actual en carrusel |
| Botón Agregar foto | `MudButton` | Mobile: cámara; Web: `MudFileUpload` |
| Botón Eliminar marker | `MudButton` Color="Error" Variant="Text" | abre `MudDialog` |
| Botón Descargar | `MudButton` Variant="Outlined" | solo Web, disabled si sin fotos |
| Cerrar | `MudIconButton` | cierra `MudDialog` |

---

### 2.2 Visor Fullscreen de Foto (FotoViewer)

**Plataforma:** Android Mobile + Web  
**Componente:** FotoViewer.razor  
**Stories:** GEO-US23, GEO-US26

```
╔══════════════════════════════════════════════════════════╗
║                                              [✕ Cerrar] ║
╠══════════════════════════════════════════════════════════╣
║                                                          ║
║                                                          ║
║              ╔══════════════════════╗                    ║
║              ║                      ║                    ║
║              ║                      ║                    ║
║              ║     IMAGEN           ║                    ║
║              ║     FULLSCREEN       ║                    ║
║              ║                      ║                    ║
║              ║   (pinch-to-zoom)    ║                    ║
║              ║                      ║                    ║
║              ╚══════════════════════╝                    ║
║                                                          ║
║  Descripción de esta foto:                               ║
║  ┌────────────────────────────────────────────────┐      ║
║  │  Vista desde el norte, base claramente dañada  │      ║
║  └────────────────────────────────────────────────┘      ║
║                                                          ║
║  Foto 2 de 3 — "Poste de alumbrado #47"                  ║
║                                                          ║
╚══════════════════════════════════════════════════════════╝
```

**Componentes MudBlazor:**
| Elemento | Componente | Notas |
|----------|------------|-------|
| Overlay | `MudOverlay` | cubre toda la pantalla |
| Imagen | `img` | con CSS `object-fit: contain`, `touch-action: pinch-zoom` |
| Comentario foto | `MudTextField` | guarda on-blur en SQLite |
| Pie de página | `MudText` Typography="Body2" | nombre del punto + posición |
| Cerrar | `MudIconButton` | cierra overlay, vuelve al carrusel en mismo índice |

---

### 2.3 Pantalla Sincronización

**Plataforma:** Android Mobile (+ Web)  
**Componente:** Sincronizacion.razor  
**Stories:** GEO-US24, GEO-US27

```
╔══════════════════════════════════════════════════════╗
║  ≡   GeoFoto         [badge: 3]   🔄 sync           ║
╠══════════════════════════════════════════════════════╣
║                                                      ║
║  Sincronización                                      ║
║  ──────────────────────────────────────              ║
║                                                      ║
║  Última sync: 16/04/2026 10:45:02                    ║
║  (o "Nunca sincronizado" si es la primera vez)       ║
║                                                      ║
║  ┌────────────────────────────────────────────┐      ║
║  │  📊 Resumen                                │      ║
║  │  Pendientes: 3    Fallidas: 1              │      ║
║  └────────────────────────────────────────────┘      ║
║                                                      ║
║  [  🔄 Sincronizar ahora  ]   ← disabled si syncing  ║
║                                                      ║
║  ─────── Operaciones pendientes ─────────            ║
║  ┌──────────────────────────────────────────────┐    ║
║  │ Tipo  │ Entidad │ Estado │ Reintentos │ Error │    ║
║  ├──────────────────────────────────────────────┤    ║
║  │ Crear │ Punto   │🟠Pend  │ 0          │       │    ║
║  │ Crear │ Foto    │🟠Pend  │ 0          │       │    ║
║  │ Upd.  │ Punto   │🔴Fail  │ 3          │Timeout│    ║
║  │ Elim. │ Foto    │🟠Pend  │ 0          │       │    ║
║  └──────────────────────────────────────────────┘    ║
║                                                      ║
╚══════════════════════════════════════════════════════╝
```

**Estados del badge en AppBar:**
```
[3] naranja  → items pendientes (número)
[🔄] azul/spin → sincronizando
[✓] verde    → todo sincronizado
[!] rojo     → error en última sync
```

**Componentes MudBlazor:**
| Elemento | Componente | Notas |
|----------|------------|-------|
| Badge AppBar | `MudBadge` + `MudIconButton` | reactivo a cambios en SyncQueue |
| Texto última sync | `MudText` | actualiza al terminar sync |
| Resumen | `MudPaper` | conteo pendientes + fallidos |
| Botón sync | `MudButton` Variant="Filled" | spinner durante sync |
| Tabla operaciones | `MudTable` | chips por estado |
| Chip estado | `MudChip` | Pending=naranja, Synced=verde, Failed=rojo |

---

### 2.4 Lista de Markers con Búsqueda

**Plataforma:** Android Mobile + Web  
**Componente:** ListaPuntos.razor  
**Stories:** GEO-US28

```
╔══════════════════════════════════════════════════════╗
║  ≡   GeoFoto          Lista de markers               ║
╠══════════════════════════════════════════════════════╣
║                                                      ║
║  🔍 ┌──────────────────────────────────────────┐    ║
║     │  Buscar por nombre...                    │    ║
║     └──────────────────────────────────────────┘    ║
║                                                      ║
║  ─────────── 12 markers encontrados ─────────        ║
║                                                      ║
║  ┌────────────────────────────────────────────────┐  ║
║  │ Nombre            │ Fotos │ Estado │ Coords    │  ║
║  ├────────────────────────────────────────────────┤  ║
║  │ Poste #47         │   3   │🟢Sync  │-34.60,...│  ║
║  │ Transformador N.  │   1   │🟠Pend  │-34.61,...│  ║
║  │ Semáforo cruce    │   5   │🟢Sync  │-34.59,...│  ║
║  │ Luminaria B-12    │   2   │🔴Fail  │-34.62,...│  ║
║  │ ...               │       │        │          │  ║
║  └────────────────────────────────────────────────┘  ║
║                                                      ║
║  < 1  2  3 >                                         ║
║                                                      ║
╚══════════════════════════════════════════════════════╝
```

**Al tocar un ítem:**
```
1. El mapa se centra con setView(lat, lng, 15)
2. Se abre automáticamente el MarkerPopup del punto seleccionado
```

**Componentes MudBlazor:**
| Elemento | Componente | Notas |
|----------|------------|-------|
| Buscador | `MudTextField` Adornment="Start" AdornmentIcon="Search" | debounce 300ms |
| Tabla | `MudTable` | RowsPerPage=10, ordenable |
| Chip estado | `MudChip` | Synced=verde, Pending=naranja, Failed=rojo |
| Paginación | `MudTablePager` | |

---

### 2.5 Indicador Visual de Radio de Marker en el Mapa

**Plataforma:** Android Mobile + Web  
**Stories:** GEO-US22

```
MAPA LEAFLET — Vista con radio de marker activo:

     ·  ·  ·  ·  ·  ·  ·  ·  ·  ·  ·
   ·                                  ·
  ·     Zona de agrupación            ·
 ·      del marker (radio 50m)        ·
·                                      ·
·           [ 📍 Marker ]              ·   ← marker activo (popup abierto)
·        (círculo semi-transparente)   ·
·            azul 30% opacity          ·
 ·                                    ·
  ·                                  ·
   ·                                ·
     ·  ·  ·  ·  ·  ·  ·  ·  ·  ·

POPUP DEL MARKER (con slider):
╔════════════════════════════════╗
║  Radio de agrupación           ║
║  ────────────────────────      ║
║  10m ━━━━━━━━●━━━━━━━ 500m    ║
║               50 m             ║
║                                ║
║  [Aplicar a todos los markers] ║
╚════════════════════════════════╝
```

**Implementación Leaflet:**
```javascript
// En leaflet-interop.js
function showMarkerRadius(puntoId, lat, lng, radioMetros) {
  const circle = L.circle([lat, lng], {
    radius: radioMetros,
    fillColor: '#2196F3',
    fillOpacity: 0.2,
    color: '#2196F3',
    weight: 1
  });
  circle.addTo(map);
  markerRadii[puntoId] = circle;
}
```

---

### 2.6 FAB GPS + Indicador de Posición Propia

**Plataforma:** Android Mobile + Web  
**Stories:** GEO-US20b, GEO-US21

```
MAPA LEAFLET — Vista con posición propia:

  ┌────────────────────────────────────────────────┐
  │                                                │
  │         ·  ·  ·                               │
  │       ·       ·                               │
  │      · [●]    ·  ← Marcador de posición propia│
  │       ·  ·  ·     (círculo azul pulsante)     │
  │                                               │
  │    [📍] ← markers de fotos                   │
  │         [📍]                                 │
  │                                               │
  │                                               │
  │                               ┌───────┐      │
  │                               │  GPS  │      │ ← FAB GPS
  │                               │  🎯  │      │   (azul, esquina inf. der.)
  │                               └───────┘      │
  └────────────────────────────────────────────────┘

Leyenda:
  [●] azul pulsante = posición actual del usuario
  [📍] rojo/naranja = markers de fotos registradas
```

**Animación CSS para marcador propio:**
```css
@keyframes pulse {
  0% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.5); opacity: 0.5; }
  100% { transform: scale(1); opacity: 1; }
}
.user-position-marker {
  animation: pulse 2s infinite;
  background-color: #2196F3;
  border-radius: 50%;
}
```

---

### 2.7 Pantalla de Error de Mapa (ESC-02)

**Plataforma:** Android Mobile  
**Stories:** GEO-US20b (ESC-02)

```
╔══════════════════════════════════════════════════════╗
║  ≡   GeoFoto                                         ║
╠══════════════════════════════════════════════════════╣
║                                                      ║
║                                                      ║
║                  🗺️                                  ║
║                                                      ║
║         Mapa no disponible —                         ║
║    intentá reiniciar la aplicación.                  ║
║                                                      ║
║                                                      ║
║           ┌─────────────────┐                        ║
║           │   🔄 Reintentar  │                        ║
║           └─────────────────┘                        ║
║                                                      ║
║                                                      ║
╚══════════════════════════════════════════════════════╝
```

**Componentes MudBlazor:**
| Elemento | Componente | Notas |
|----------|------------|-------|
| Ícono mapa | `MudIcon` Icon="@Icons.Material.Outlined.MapOff" | grande, centrado |
| Mensaje | `MudText` Typography="H6" | mensaje exacto per ESC-02 |
| Botón | `MudButton` Variant="Filled" | invoca reiniciar initMap |

---

## 3. Notas de Diseño

### Voseo rioplatense en todos los textos
- "intentá reiniciar" (no "intente reiniciar")
- "usá el botón" (no "use el botón")
- "habilitalo" (no "habilítelo")
- "Sincronizá ahora" → permitido como variante de "Sincronizar ahora"

### Paleta de colores para estados
| Estado | Color MudBlazor | Hex |
|--------|----------------|-----|
| Pendiente | `Color.Warning` | #FF9800 |
| Sincronizado | `Color.Success` | #4CAF50 |
| Error/Fallido | `Color.Error` | #F44336 |
| Sincronizando | `Color.Info` | #2196F3 |
| Sin GPS | `Color.Default` | #9E9E9E |

### Responsividad
- Todos los componentes están diseñados para funcionar en pantallas de 360dp+ (Android mínimo).
- En web (desktop), los popups tienen ancho máximo de 600px centrado.

---

## 4. Control de Cambios

| Versión | Fecha | Descripción |
|---------|-------|-------------|
| 2.0 | 2026-04-16 | Creación de wireframes para Sprint 07-08: MarkerPopup, FotoViewer fullscreen, Sincronización, Lista markers, Radio visual, FAB GPS, pantalla error ESC-02 |

---

**Fin del documento**
