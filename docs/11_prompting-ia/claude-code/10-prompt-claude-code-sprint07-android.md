# Prompt Claude Code — Sprint 07B/07C: GEO-US28 a GEO-US29 + Completar Android

**Proyecto:** GeoFoto MAUI Hybrid / Blazor  
**Sprint:** 07B (GEO-US28–GEO-US29) + 07C (GEO-US30)  
**Fecha de referencia:** 2026-08-10  
**Puntos:** 13 pts

---

## CONTEXTO DE ENTRADA

La sesión anterior completó Sprint 07A (GEO-US20–GEO-US27):
- GEO-US20: FAB centrar mapa (GPS) — DONE
- GEO-US21: Marcador posición propia (polling 5s) — DONE
- GEO-US22: Radio visual configurable del marker — DONE
- GEO-US23: MarkerPopup + FotoCarousel + FotoViewer — DONE
- GEO-US24: Offline-first + SyncQueue — DONE (base existente)
- GEO-US25: Quitar foto desde carrusel — DONE (en MarkerPopup.razor)
- GEO-US26: Ampliar foto fullscreen — DONE (FotoViewer.razor)
- GEO-US27: Pantalla sincronización — DONE (EstadoSync.razor + última sync)

---

## OBJETIVO DE ESTA SESIÓN

Implementar los stories pendientes del Sprint 07 y cerrar la funcionalidad Android:

### GEO-US28 · 8 pts · Must — Lista de markers con búsqueda
### GEO-US29 · 5 pts · Should — Eliminar marker desde lista o popup
### GEO-US30 · 5 pts · Could — Compartir foto nativa Android

---

## FASE 1 — LEER CONTEXTO BASE

Antes de escribir código, leer:

```
src/GeoFoto.Shared/Pages/ListaPuntos.razor          ← ya existe, verificar estado actual
src/GeoFoto.Shared/Components/MarkerPopup.razor     ← tiene el botón Eliminar marker
src/GeoFoto.Shared/Services/ILocalDbService.cs
src/GeoFoto.Shared/Services/IShareService.cs
src/GeoFoto.Shared/Models/LocalModels.cs
docs/07_plan-sprint/plan-iteracion_sprint-07_v1.0.md
```

Verificar que `ListaPuntos.razor` tiene:
- [ ] `MudTable` o `MudList` con: Titulo, Coordenadas, N Fotos, SyncStatus chip
- [ ] Campo de búsqueda filtrando por Nombre en tiempo real
- [ ] Click en fila → navegar al mapa centrado en ese marker Y abrir MarkerPopup

Si falta alguno de estos puntos, es lo que se implementa en esta sesión.

---

## FASE 2 — IMPLEMENTAR GEO-US28: LISTA DE MARKERS

### Archivo: `src/GeoFoto.Shared/Pages/ListaPuntos.razor`

La página debe obtener puntos de `ILocalDbService.GetPuntosAsync()` y mostrarlos en tabla.

**Funcionalidad requerida:**

```
CA-01: MudTable con columnas: Nombre, Coordenadas, N Fotos, Estado (SyncStatusBadge)
CA-02: MudTextField de búsqueda filtra _puntosFiltrados por Nombre en tiempo real
CA-03: Click en fila:
        → navega a Mapa.razor pasando puntoLocalId en query string o NavigationManager
        → abre MarkerPopup del marker seleccionado
CA-04: Desde la lista se puede acceder al carrusel de fotos (vía MarkerPopup)
```

**Navegación a Mapa:**
- Usar `NavigationManager.NavigateTo("/mapa?marker={localId}")` 
- En `Mapa.razor`, leer el query param y abrir el popup automáticamente en `OnAfterRenderAsync`

**Tests a crear:** `src/GeoFoto.Tests/Unit/Mobile/US28_ListaMarkersTests.cs`

```csharp
[Fact] ListaMarkers_MuestraTodosLosPuntos()
[Fact] ListaMarkers_Busqueda_FiltraPorNombre()
[Fact] ListaMarkers_Busqueda_EsCaseInsensitive()
[Fact] ListaMarkers_PuntoSinNombre_MuestraFallback()
```

---

## FASE 3 — IMPLEMENTAR GEO-US29: ELIMINAR MARKER

El botón de eliminar ya existe en `MarkerPopup.razor` (método `EjecutarEliminarMarkerAsync`).

Verificar que:
1. Al eliminar marker, el leaflet marker se quita del mapa (invocar `leafletInterop.removeMarker(puntoLocalId)` si existe, o reload de markers)
2. Si la lista de markers está abierta en otra pestaña/página, se actualiza al volver

**En `leaflet-interop.js` agregar si no existe:**
```javascript
removeMarker: function(puntoId) {
    var idx = this.markers.findIndex(m => m._puntoId === puntoId);
    if (idx >= 0) {
        this._clusterGroup.removeLayer(this.markers[idx]);
        this.markers.splice(idx, 1);
    }
}
```

**En `addMarkers()`:** al crear cada marker, asignar `marker._puntoId = p.id`.

**Tests a crear:** `src/GeoFoto.Tests/Unit/Mobile/US29_EliminarMarkerTests.cs`

```csharp
[Fact] EliminarMarker_NoSincronizado_DeleteDirecto()
[Fact] EliminarMarker_Sincronizado_SoftDeleteYEncola()
[Fact] EliminarMarker_Cancela_NoModificaNada()
[Fact] EliminarMarker_ConFotos_EliminaFotosTambien()  // soft-delete cascada
```

---

## FASE 4 — IMPLEMENTAR GEO-US30: COMPARTIR FOTO ANDROID

### Archivo: `src/GeoFoto.Mobile/Services/ShareService.cs`

Ya implementado en sesión anterior. Verificar que `GaleriaLocalDialog.razor` llama `IShareService.ShareFileAsync` cuando `_shareService != null`.

Si `ShareService` no está registrado en DI de Mobile, agregar en `MauiProgram.cs`:
```csharp
builder.Services.AddSingleton<IShareService, ShareService>();
```

**Tests:** `src/GeoFoto.Tests/Unit/Mobile/US30_CompartirTests.cs`

```csharp
[Fact] ShareService_ArchivoExiste_InvokaShareApi()
[Fact] ShareService_ArchivoNoExiste_RetornaFalse()
```

---

## FASE 5 — VERIFICACIÓN FINAL

```bash
dotnet build src/GeoFoto.Shared/GeoFoto.Shared.csproj   # 0 errores
dotnet build src/GeoFoto.Tests/GeoFoto.Tests.csproj "-p:BuildProjectReferences=false"
dotnet test src/GeoFoto.Tests/GeoFoto.Tests.csproj --no-build
```

Resultado esperado: **≥ 185 tests pasando, 0 fallidos**.

---

## CRITERIOS DE COMPLETITUD

- [ ] ListaPuntos muestra todos los puntos locales con búsqueda funcional
- [ ] Click en fila navega al mapa y abre el popup del marker
- [ ] Eliminar marker quita el pin del mapa en tiempo real
- [ ] Compartir foto (Android) usa MAUI Share API
- [ ] Todos los tests nuevos pasan
- [ ] `dotnet build GeoFoto.Shared` → 0 errores
