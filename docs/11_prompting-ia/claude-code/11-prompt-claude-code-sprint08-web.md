# Prompt Claude Code — Sprint 08: GEO-US31 a GEO-US33 + Web Paridad

**Proyecto:** GeoFoto MAUI Hybrid / Blazor  
**Sprint:** 08 (2026-08-24 → 2026-09-06)  
**Fecha de referencia:** 2026-08-24  
**Puntos:** 20 pts

---

## CONTEXTO DE ENTRADA

Sprints 07A, 07B, 07C completados. La app Android tiene:
- GPS FAB + marcador posición propia (US20/US21)
- Radio visual configurable (US22)
- MarkerPopup + FotoCarousel + FotoViewer (US23/US25/US26)
- Offline-first + SyncQueue (US24)
- Pantalla de sync (US27)
- Lista de markers con búsqueda (US28)
- Eliminar marker (US29)
- Compartir foto Android (US30)

**Sprint 08 implementa paridad web + features de descarga y upload desde browser.**

---

## STORIES A IMPLEMENTAR

### GEO-US31 · 5 pts · Must — Web paridad funcional completa

La app web (`GeoFoto.Web`) usa los mismos componentes Shared. Verificar que funcionen en browser:
- MarkerPopup.razor, FotoCarousel.razor, FotoViewer.razor
- Los permisos de geolocalización usan `navigator.geolocation` del browser
- Si el browser deniega, mostrar MudAlert: "Permiso de ubicación denegado en el navegador. Habilitalo desde la configuración del sitio."

**Verificación en `leaflet-interop.js`:**
```javascript
// Ya existe centrarEnUbicacion() — verificar que el callback OnGpsError
// propaga el mensaje al componente Blazor
```

**En Mapa.razor (Web):** `OnGpsError` debe mostrar snackbar permanente con el mensaje correcto.

### GEO-US32 · 5 pts · Must — Descargar fotos como ZIP desde web

**API endpoint (ya especificado en api-rest-spec_v1.0.md):**
```
GET /api/puntos/{id}/fotos/download  →  application/zip
```

**Implementar en `src/GeoFoto.Api/Controllers/PuntosController.cs`:**
```csharp
[HttpGet("{id}/fotos/download")]
public async Task<IActionResult> DescargarFotosZip(int id)
{
    var punto = await _context.Puntos.FindAsync(id);
    if (punto is null) return NotFound();
    
    var fotos = await _context.Fotos
        .Where(f => f.PuntoId == id && !f.IsDeleted)
        .ToListAsync();
    
    if (!fotos.Any()) return NoContent(); // 204 → botón deshabilitado en UI
    
    using var ms = new MemoryStream();
    using (var zip = new System.IO.Compression.ZipArchive(ms, ZipArchiveMode.Create, true))
    {
        var nombre = punto.Nombre ?? $"punto_{id}";
        foreach (var (foto, n) in fotos.Select((f,i) => (f, i+1)))
        {
            var entry = zip.CreateEntry($"{nombre}_{n}{Path.GetExtension(foto.NombreArchivo)}");
            await using var entryStream = entry.Open();
            var ruta = Path.Combine(_uploadsPath, foto.NombreArchivo);
            if (File.Exists(ruta))
                await using (var fs = File.OpenRead(ruta))
                    await fs.CopyToAsync(entryStream);
        }
    }
    ms.Position = 0;
    return File(ms.ToArray(), "application/zip", $"{punto.Nombre ?? "fotos"}.zip");
}
```

**En MarkerPopup.razor (cuando IsMobile=false):**
- Botón "Descargar fotos" → llama `IGeoFotoApiClient.DescargarFotosZipAsync(puntoRemoteId)`
- Disparar descarga en browser vía JS: `window.open(url)` o `downloadFile(bytes, nombre)`

**Agregar en `leaflet-interop.js`:**
```javascript
downloadBlob: function(base64, filename, mimeType) {
    var link = document.createElement('a');
    link.href = 'data:' + mimeType + ';base64,' + base64;
    link.download = filename;
    link.click();
}
```

### GEO-US33 · 5 pts · Must — Subir foto al marker desde browser

Ya implementado en `MarkerPopup.razor` mediante `MudFileUpload` cuando `IsMobile=false`.

Verificar que el flujo completo funciona en web:
1. Usuario selecciona archivo → `OnAgregarFotos` guarda en SQLite local (si WebApp con ILocalDbService) O POST a `/api/fotos/upload`
2. Para `GeoFoto.Web` (InteractiveServer), la subida debe ir directo a la API
3. `ESC-04`: foto sin EXIF → se vincula al marker sin coordenadas propias (Latitud/Longitud = null)

---

## FASE 1 — LEER CONTEXTO

```
src/GeoFoto.Api/Controllers/PuntosController.cs     ← agregar endpoint download
src/GeoFoto.Api/Controllers/FotosController.cs      ← verificar POST /upload
src/GeoFoto.Shared/Services/GeoFotoApiClient.cs     ← agregar método DescargarFotosZipAsync
src/GeoFoto.Shared/Components/MarkerPopup.razor     ← agregar botón download
src/GeoFoto.Web/Pages/Mapa.razor                    ← si existe, verificar permisos web
docs/05_arquitectura_tecnica/api-rest-spec_v1.0.md  ← endpoints 5.5, 5.6
```

---

## FASE 2 — MIGRACIÓN EF CORE (si necesaria)

Verificar que los campos `RadioMetros`, `IsDeleted`, `Comentario` estén en la BD:

```bash
cd src/GeoFoto.Api
dotnet ef migrations add Sprint08_WebFeatures
dotnet ef database update
```

Si ya existe la migración de Sprint07, solo verificar que `database update` esté al día.

---

## FASE 3 — IMPLEMENTAR ENDPOINTS Y CLIENTE

**En `GeoFotoApiClient.cs`** agregar:
```csharp
public async Task<byte[]?> DescargarFotosZipAsync(int puntoRemoteId, CancellationToken ct = default)
{
    var response = await _http.GetAsync($"api/puntos/{puntoRemoteId}/fotos/download", ct);
    if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsByteArrayAsync(ct);
}
```

**En `IGeoFotoApiClient`** agregar la firma correspondiente.

---

## FASE 4 — TESTS

Crear `src/GeoFoto.Tests/Unit/Web/US31_WebGeoTests.cs`:
```csharp
[Fact] Web_PermisoBrowserDenegado_MuestraMensajeConfig()
[Fact] Web_PermisoBrowserOk_MarcadorActualizado()
```

Crear `src/GeoFoto.Tests/Unit/Web/US32_DescargaZipTests.cs`:
```csharp
[Fact] DescargaZip_PuntoConFotos_RetornaBytes()
[Fact] DescargaZip_PuntoSinFotos_RetornaNull()
[Fact] DescargaZip_BotonDeshabilitado_CuandoSinFotos()
```

Crear `src/GeoFoto.Tests/Unit/Web/US33_SubirFotoWebTests.cs`:
```csharp
[Fact] SubirFotoWeb_SinExif_VinculaAlMarker()
[Fact] SubirFotoWeb_ConExif_UsaCoordenadaExif()
```

---

## FASE 5 — VERIFICACIÓN

```bash
dotnet build src/GeoFoto.Api/GeoFoto.Api.csproj       # 0 errores (detener proceso antes)
dotnet build src/GeoFoto.Shared/GeoFoto.Shared.csproj # 0 errores
dotnet test src/GeoFoto.Tests/GeoFoto.Tests.csproj --no-build
```

Resultado esperado: **≥ 200 tests pasando**.

---

## CRITERIOS DE COMPLETITUD

- [ ] Web app muestra mensaje correcto cuando browser deniega geolocalización
- [ ] `GET /api/puntos/{id}/fotos/download` retorna ZIP válido
- [ ] Botón "Descargar fotos" en MarkerPopup (web) descarga el ZIP
- [ ] Botón deshabilitado si el punto no tiene fotos
- [ ] `POST /api/fotos/upload` acepta foto sin EXIF y la vincula al marker
- [ ] Todos los tests nuevos pasan
