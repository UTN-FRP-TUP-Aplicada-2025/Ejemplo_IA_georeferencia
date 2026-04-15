# Prompt para GitHub Copilot — Galería de Fotos con Acciones + Sync Bidireccional (v1)

> Pegá este prompt en Copilot Chat con modo **Edits** (`@workspace`).
> Prerequisito: la app compila (0 errores, 34 tests green), API en puerto 5000,
> Mobile instalada. El fix de OnCapturaFoto (try/catch + Debug logs) YA está aplicado.
> La foto se guarda correctamente en SQLite y filesystem local.

---

## PROMPT

```
Sos un desarrollador .NET 10 senior especialista en Blazor Hybrid, MAUI y sync offline-first.
Tu tarea tiene 4 fases: descubrimiento del código real, galería mobile con acciones,
galería web con acciones, y fix de sincronización bidireccional para fotos.

IMPORTANTE: Este proyecto fue construido en sprints sucesivos con múltiples prompts
de Copilot. El código real puede diferir de la documentación. Por eso la FASE 0 es
obligatoria: leer el código ANTES de modificar.

---

## FASE 0 — DESCUBRIMIENTO DEL CÓDIGO REAL (OBLIGATORIO)

Antes de escribir UNA SOLA línea de código, ejecutá todos estos comandos
y ANOTÁ los resultados. Son tu fuente de verdad.

### 0.1 — Inventariar componentes existentes

```powershell
# Listar TODOS los .razor en Shared
Get-ChildItem -Recurse -Path "src\GeoFoto.Shared" -Filter "*.razor" |
    ForEach-Object { Write-Host "$($_.Directory.Name)\$($_.Name)" }

# Listar TODOS los .razor en Mobile
Get-ChildItem -Recurse -Path "src\GeoFoto.Mobile" -Filter "*.razor" |
    ForEach-Object { Write-Host "$($_.Directory.Name)\$($_.Name)" }
```

### 0.2 — Leer los componentes clave COMPLETOS

```powershell
# Carrusel de fotos actual
Get-ChildItem -Recurse -Filter "FotoCarousel.razor" | ForEach-Object { 
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName 
}

# Visor/galería de fotos (puede llamarse FotoViewer, GaleriaFotos, etc.)
Get-ChildItem -Recurse -Filter "*Viewer*.razor","*Galeria*.razor" | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}

# Detalle del punto
Get-ChildItem -Recurse -Filter "DetallePunto*.razor" | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}

# Mapa principal
Get-ChildItem -Recurse -Filter "Mapa.razor" | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}

# MobileLayout (tiene el FAB de cámara)
Get-ChildItem -Recurse -Filter "MobileLayout.razor" | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}

# Lista de puntos
Get-ChildItem -Recurse -Filter "ListaPuntos.razor","Lista*.razor" | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}
```

### 0.3 — Leer servicios y API

```powershell
# Cliente HTTP
Get-ChildItem -Recurse -Filter "GeoFotoApiClient.cs","IGeoFotoApiClient.cs" | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}

# SyncService
Get-ChildItem -Recurse -Filter "*SyncService*.cs" | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}

# LocalDbService
Get-ChildItem -Recurse -Filter "LocalDbService.cs","ILocalDbService.cs" | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}

# Controladores API de fotos y sync
Get-ChildItem -Recurse -Filter "FotosController.cs","SyncController.cs" | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}

# DTOs
Get-ChildItem -Recurse -Filter "*Dto*.cs","*Result*.cs" | Where-Object {
    $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\"
} | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}
```

### 0.4 — Leer JS interop y leaflet

```powershell
Get-ChildItem -Recurse -Filter "leaflet-interop.js","geofoto-utils.js" | ForEach-Object {
    Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName
}
```

### 0.5 — Verificar endpoints de la API real

```powershell
# Verificar que la API está corriendo
try {
    $puntos = (Invoke-WebRequest "http://localhost:5000/api/puntos" -UseBasicParsing).Content | ConvertFrom-Json
    Write-Host "GET /api/puntos → $($puntos.Count) puntos"
} catch { Write-Host "API no responde — lanzala primero" }

# Verificar swagger para ver endpoints disponibles
try {
    $swagger = (Invoke-WebRequest "http://localhost:5000/swagger/v1/swagger.json" -UseBasicParsing).Content | ConvertFrom-Json
    $swagger.paths.PSObject.Properties | ForEach-Object { Write-Host $_.Name }
} catch { Write-Host "Swagger no disponible" }
```

ANOTÁ TODO LO QUE LEÍSTE. Esto determina:
  - Si FotoViewer/GaleriaFotosDialog ya existe (reutilizar, no recrear)
  - Si DetallePunto ya tiene botón "Agregar foto" (extender, no duplicar)
  - Qué métodos tiene IGeoFotoApiClient (agregar solo los faltantes)
  - Si upload-to-punto endpoint existe en FotosController
  - Qué tiene el SyncService real (PushAsync, PullAsync, etc.)
  - Si DeletedEntities / audit log existe en la API

Confirmá con: "Fase 0 completa. Código analizado. Componentes existentes identificados."

---

## FASE 0B — LECTURA DE DOCUMENTACIÓN

Después de leer el código, leé estos docs como referencia complementaria:
  docs/03_ux-ui/ux-writing-microcopy_v1.0.md     (textos de UI — fuente de verdad)
  docs/05_arquitectura_tecnica/api-rest-spec_v1.0.md
  docs/05_arquitectura_tecnica/arquitectura-offline-sync_v1.0.md
  docs/11_diagnostico/bateria-diagnostico_v1.0.md (estado actual real)

Confirmá: "Documentación leída. Comenzando implementación."

---

## REGLAS DE IMPLEMENTACIÓN

1. NO recrear componentes que ya existen — EXTENDERLOS.
2. NO asumir que un método/servicio/endpoint existe — verificar en Fase 0.
3. Si un componente ya tiene una funcionalidad parcial, COMPLETAR lo que falta.
4. Textos UI según docs/03_ux-ui/ux-writing-microcopy_v1.0.md (voseo rioplatense).
5. Todo try/catch con Logger.LogError + snackbar de error visible.
6. Todo cambio offline → SyncQueue. Todo cambio online → API directa.
7. Después de modificar fotos → StateHasChanged() + recargar datos.
8. Compilar y verificar después de cada bloque — no avanzar si hay errores.

---

## BLOQUE 1 — GALERÍA MOBILE CON ACCIONES

### Objetivo

Cuando el usuario toca un marker en la app Mobile y el punto tiene fotos:
1. Se abre el drawer/bottom sheet con DetallePunto (esto ya debería funcionar).
2. El FotoCarousel muestra las fotos (esto ya debería funcionar).
3. Al tocar una foto → se abre un visor fullscreen con BARRA DE ACCIONES.

### 1.1 — Visor con acciones

Localizar el visor de fotos existente (FotoViewer.razor, GaleriaFotosDialog.razor,
o como se llame en el código real). Si existe, EXTENDERLO. Si no existe, CREARLO.

El visor DEBE tener una barra de acciones inferior con 3 botones:

| Botón | Ícono | Texto | Acción |
|-------|-------|-------|--------|
| Compartir | Icons.Material.Filled.Share | "Compartir" | Solo visible en Mobile. Usa MAUI Share API. |
| Agregar | Icons.Material.Filled.AddAPhoto | "Agregar" | Abre la cámara y asocia la foto al punto actual. |
| Eliminar | Icons.Material.Filled.Delete | "Eliminar" | Pide confirmación y elimina la foto actual. |

### 1.2 — Implementar Compartir (solo MAUI)

Para compartir, necesitás el archivo físico de la foto:

```csharp
private async Task CompartirFoto(FotoDto foto)
{
    try
    {
        // Buscar archivo local primero
        var localPath = Path.Combine(FileSystem.AppDataDirectory, "photos", foto.NombreArchivo);

        if (!File.Exists(localPath))
        {
            // Descargar desde API si no está local
            Snackbar.Add("Descargando foto...", Severity.Info);
            var stream = await Api.DescargarImagenAsync(foto.Id);
            if (stream is null) { Snackbar.Add("No se pudo obtener la foto", Severity.Error); return; }

            Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
            await using var fs = File.Create(localPath);
            await stream.CopyToAsync(fs);
        }

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = $"Foto de {NombrePunto}",
            File = new ShareFile(localPath)
        });
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "[GeoFoto] Error al compartir foto {Id}", foto.Id);
        Snackbar.Add("No se pudo compartir la foto", Severity.Error);
    }
}
```

VERIFICAR que IGeoFotoApiClient tiene un método para descargar la imagen como Stream.
Si no existe, CREARLO:

```csharp
// En IGeoFotoApiClient
Task<Stream?> DescargarImagenAsync(int fotoId, CancellationToken ct = default);

// En GeoFotoApiClient
public async Task<Stream?> DescargarImagenAsync(int fotoId, CancellationToken ct)
{
    var response = await _http.GetAsync($"api/fotos/imagen/{fotoId}", ct);
    if (!response.IsSuccessStatusCode) return null;
    return await response.Content.ReadAsStreamAsync(ct);
}
```

Detectar plataforma para mostrar/ocultar el botón:
```csharp
private bool EsMobile => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();
```

### 1.3 — Implementar Agregar foto desde el visor

Al tocar "Agregar" dentro del visor:
1. Invocar la cámara nativa (reutilizar el servicio ICamaraService que ya existe).
2. Asociar la foto al punto actual — usar la misma lógica que OnCapturaFoto
   pero SIN crear un punto nuevo (el punto ya existe).
3. NO abrir diálogo de descripción (el punto ya tiene nombre).
4. Recargar las fotos del visor para mostrar la nueva.
5. Snackbar: "Foto agregada al punto"

IMPORTANTE: la lógica de guardar la foto localmente (SQLite + filesystem)
ya fue implementada y fixeada en OnCapturaFoto. Reutilizar esa lógica
creando un método compartido si no existe:

```csharp
// Método compartido para asociar foto a un punto existente
private async Task GuardarFotoEnPuntoAsync(Stream fotoStream, int puntoLocalId)
{
    // 1. Guardar archivo en AppDataDirectory/photos/
    var fileName = $"foto_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.jpg";
    var dir = Path.Combine(FileSystem.AppDataDirectory, "photos");
    Directory.CreateDirectory(dir);
    var filePath = Path.Combine(dir, fileName);

    await using (var fs = File.Create(filePath))
        await fotoStream.CopyToAsync(fs);

    // 2. Crear FotoLocal en SQLite
    var fotoLocal = new FotoLocal
    {
        PuntoLocalId = puntoLocalId,
        NombreArchivo = fileName,
        RutaLocal = filePath,
        FechaTomada = DateTime.UtcNow,
        TamanoBytes = new FileInfo(filePath).Length,
        SyncStatus = "PendingCreate"
    };
    await LocalDb.InsertFotoAsync(fotoLocal);

    // 3. Encolar en SyncQueue
    await LocalDb.EnqueueAsync("Create", "Foto", fotoLocal.LocalId,
        System.Text.Json.JsonSerializer.Serialize(fotoLocal));

    Logger.LogInformation("[GeoFoto] FotoLocal creada: {Id} para punto {PId}", fotoLocal.LocalId, puntoLocalId);
}
```

VERIFICAR si un método similar ya existe en el código (puede llamarse
AgregarFotoAlPunto, GuardarFoto, etc.). Si existe, REUTILIZARLO.

### 1.4 — Implementar Eliminar foto

Al tocar "Eliminar":
1. MudDialog de confirmación:
   - Título: "Eliminar foto"
   - Cuerpo: "¿Eliminar esta foto? Esta acción no se puede deshacer."
   - Botón destructivo: "Eliminar" (Color.Error)
   - Botón secundario: "Cancelar"

2. Si confirma:

```csharp
private async Task EliminarFoto(FotoDto foto)
{
    var confirm = await DialogService.ShowMessageBox(
        "Eliminar foto",
        "¿Eliminar esta foto? Esta acción no se puede deshacer.",
        yesText: "Eliminar", cancelText: "Cancelar");
    if (confirm != true) return;

    try
    {
        if (EsMobile)
        {
            // VERIFICAR: ¿la foto tiene LocalId o solo Id (RemoteId)?
            // Si es una foto sincronizada (tiene RemoteId), necesitamos
            // tanto el LocalId como el RemoteId.

            // Marcar PendingDelete en SQLite
            await LocalDb.MarkFotoPendingDeleteAsync(foto.LocalId);
            await LocalDb.EnqueueAsync("Delete", "Foto", foto.LocalId, null);

            // Eliminar archivo local
            var localPath = Path.Combine(FileSystem.AppDataDirectory, "photos", foto.NombreArchivo);
            if (File.Exists(localPath)) File.Delete(localPath);
        }
        else
        {
            // Web: DELETE directo a la API
            await Api.DeleteFotoAsync(foto.Id);
        }

        Snackbar.Add("Foto eliminada", Severity.Success);

        // Actualizar UI del visor
        Fotos.Remove(foto);
        if (_indice >= Fotos.Count) _indice = Math.Max(0, Fotos.Count - 1);
        if (Fotos.Count == 0) MudDialog.Close();

        await OnFotosChanged.InvokeAsync();
        StateHasChanged();
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "[GeoFoto] Error al eliminar foto {Id}", foto.Id);
        Snackbar.Add("Error al eliminar la foto", Severity.Error);
    }
}
```

VERIFICAR: ¿IGeoFotoApiClient tiene DeleteFotoAsync? Si no, CREARLO:
```csharp
Task DeleteFotoAsync(int fotoId, CancellationToken ct = default);
```

VERIFICAR: ¿ILocalDbService tiene MarkFotoPendingDeleteAsync? Si no, CREARLO.

### 1.5 — Verificación Bloque 1

```
[ ] Click en marker con fotos → abre drawer con carrusel
[ ] Click en foto del carrusel → abre visor fullscreen con barra de acciones
[ ] Visor: flechas de navegación funcionan
[ ] Visor: botón "Compartir" visible SOLO en Android, abre sheet nativo del SO
[ ] Visor: botón "Agregar" abre cámara, foto se asocia al punto, carrusel se actualiza
[ ] Visor: botón "Eliminar" pide confirmación, elimina, actualiza carrusel
[ ] Eliminar última foto → cierra visor, carrusel muestra "Sin fotos"
[ ] dotnet build GeoFoto.sln → 0 errores
```

Compilar y verificar antes de pasar al Bloque 2.

---

## BLOQUE 2 — GALERÍA WEB CON ACCIONES

### Objetivo

Misma experiencia que Mobile pero con acciones adaptadas al browser:
- NO hay botón "Compartir" en Web.
- SÍ hay botón "Descargar" (descarga la foto al filesystem del browser).
- SÍ hay botón "Agregar fotos" (MudFileUpload, acepta fotos SIN metadata GPS).
- SÍ hay botón "Eliminar" (DELETE directo a la API).

### 2.1 — Reutilizar el visor de Bloque 1

El mismo componente FotoViewer.razor debe funcionar en ambas plataformas.
Usar `EsMobile` para mostrar/ocultar botones:

```razor
@* Barra de acciones *@
<MudStack Row="true" Justify="Justify.Center" Spacing="3" Class="mt-3">

    @if (EsMobile)
    {
        <MudButton StartIcon="@Icons.Material.Filled.Share"
                   Variant="Variant.Outlined" Size="Size.Small"
                   OnClick="Compartir">Compartir</MudButton>
    }
    else
    {
        <MudButton StartIcon="@Icons.Material.Filled.Download"
                   Variant="Variant.Outlined" Size="Size.Small"
                   OnClick="Descargar">Descargar</MudButton>
    }

    @if (EsMobile)
    {
        <MudButton StartIcon="@Icons.Material.Filled.AddAPhoto"
                   Variant="Variant.Outlined" Size="Size.Small"
                   Color="Color.Primary"
                   OnClick="AgregarDesdeCamara">Agregar</MudButton>
    }
    else
    {
        @* En Web: usar MudFileUpload para subir archivos *@
        <MudFileUpload T="IReadOnlyList<IBrowserFile>"
                       FilesChanged="AgregarDesdeArchivo"
                       Accept=".jpg,.jpeg,.png,.webp" Multiple="true">
            <ActivatorContent>
                <MudButton StartIcon="@Icons.Material.Filled.AddPhotoAlternate"
                           Variant="Variant.Outlined" Size="Size.Small"
                           Color="Color.Primary"
                           HtmlTag="label" for="@context.Id">
                    Agregar fotos
                </MudButton>
            </ActivatorContent>
        </MudFileUpload>
    }

    <MudButton StartIcon="@Icons.Material.Filled.Delete"
               Variant="Variant.Outlined" Size="Size.Small"
               Color="Color.Error"
               OnClick="Eliminar">Eliminar</MudButton>
</MudStack>
```

### 2.2 — Descargar foto (solo Web)

Agregar función JS en geofoto-utils.js:
```javascript
window.geoFotoUtils = window.geoFotoUtils || {};

window.geoFotoUtils.downloadFile = function (url, fileName) {
    // Fetch como blob para evitar problemas CORS con anchor download
    fetch(url)
        .then(resp => resp.blob())
        .then(blob => {
            const blobUrl = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = blobUrl;
            a.download = fileName || 'foto.jpg';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(blobUrl);
        })
        .catch(err => console.error('[GeoFoto] Download error:', err));
};
```

IMPORTANTE: usar fetch + blob en lugar de anchor con href directo,
porque el href a la API puede tener problemas de CORS con download.

```csharp
private async Task Descargar()
{
    try
    {
        var foto = Fotos[_indice];
        var url = Api.GetImagenUrl(foto.Id);
        var nombre = foto.NombreArchivo ?? $"foto_{foto.Id}.jpg";
        await JSRuntime.InvokeVoidAsync("geoFotoUtils.downloadFile", url, nombre);
        Snackbar.Add("Descarga iniciada", Severity.Info);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "[GeoFoto] Error al descargar");
        Snackbar.Add("Error al descargar la foto", Severity.Error);
    }
}
```

### 2.3 — Agregar fotos sin GPS desde Web

Las fotos subidas desde Web pueden NO tener metadata EXIF/GPS.
Se asocian al punto existente usando sus coordenadas.

VERIFICAR si POST /api/fotos/upload-to-punto/{puntoId} existe en FotosController.
Si no existe, CREARLO:

```csharp
// POST /api/fotos/upload-to-punto/{puntoId}
// Acepta un archivo de imagen y lo asocia al punto indicado.
// NO requiere que la foto tenga GPS — usa las coordenadas del punto.
// Actualiza punto.UpdatedAt para que el delta lo detecte.
```

VERIFICAR si IGeoFotoApiClient tiene AgregarFotoAPuntoAsync.
Si no existe, CREARLO con la firma:
```csharp
Task<FotoUploadResultDto> AgregarFotoAPuntoAsync(
    int puntoId, Stream fileStream, string fileName, string contentType,
    CancellationToken ct = default);
```

```csharp
private async Task AgregarDesdeArchivo(IReadOnlyList<IBrowserFile> files)
{
    foreach (var file in files)
    {
        try
        {
            await using var stream = file.OpenReadStream(maxAllowedSize: 10_485_760);
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);

            await Api.AgregarFotoAPuntoAsync(PuntoId, ms, file.Name,
                file.ContentType ?? "image/jpeg");
            Snackbar.Add("Foto agregada al punto", Severity.Success);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[GeoFoto] Error subiendo {N}", file.Name);
            Snackbar.Add($"Error al subir {file.Name}", Severity.Error);
        }
    }

    await RecargarFotos();
}
```

### 2.4 — Verificación Bloque 2

```
[ ] Click en marker en Web → abre drawer con carrusel
[ ] Click en foto → abre visor fullscreen
[ ] Visor Web: NO aparece botón "Compartir"
[ ] Visor Web: botón "Descargar" baja la foto al filesystem del browser
[ ] Visor Web: botón "Agregar fotos" abre file picker, sube imagen sin GPS
[ ] Visor Web: botón "Eliminar" pide confirmación, elimina
[ ] Fotos subidas desde Web visibles tras recargar
[ ] dotnet build GeoFoto.sln → 0 errores
```

Compilar y verificar antes de pasar al Bloque 3.

---

## BLOQUE 3 — SINCRONIZACIÓN BIDIRECCIONAL

### Objetivo

Que las fotos y puntos se sincronicen correctamente en ambas direcciones:
- Mobile → API (push): fotos creadas/eliminadas offline se sincronizan
- API → Mobile (pull delta): fotos creadas/eliminadas desde Web se bajan al dispositivo

### 3.1 — Auditar el SyncService actual

PRIMERO leé el SyncService real (ya lo hiciste en Fase 0). Identificá:

- ¿Tiene PushAsync separado de PullAsync?
- ¿PushAsync procesa operaciones de tipo "Foto"?
- ¿PullAsync descarga fotos nuevas del servidor?
- ¿Hay ordenamiento de operaciones (Puntos antes que Fotos)?

### 3.2 — Fix Push: orden correcto de operaciones

El SyncService debe procesar operaciones en este orden:
1. Create Punto (el punto debe existir en servidor ANTES de subir fotos)
2. Create Foto (requiere que el punto tenga RemoteId)
3. Update Punto
4. Delete Foto (eliminar fotos ANTES de eliminar el punto padre)
5. Delete Punto

Si el SyncService actual NO ordena así, MODIFICAR el método que obtiene
las operaciones pendientes para que devuelva en este orden:

```csharp
var ordenadas = pendientes
    .OrderBy(op => op.OperationType switch
    {
        "Create" when op.EntityType == "Punto" => 0,
        "Create" when op.EntityType == "Foto"  => 1,
        "Update" => 2,
        "Delete" when op.EntityType == "Foto"  => 3,
        "Delete" when op.EntityType == "Punto" => 4,
        _ => 5
    })
    .ThenBy(op => op.CreatedAt)
    .ToList();
```

### 3.3 — Fix Push: sincronizar fotos creadas offline

VERIFICAR si PushAsync sabe manejar operaciones Create de tipo Foto.
Si no, agregar el caso:

```csharp
case ("Create", "Foto"):
    var fotoLocal = await _localDb.GetFotoByLocalIdAsync(op.LocalId);
    if (fotoLocal is null) break;

    // Obtener RemoteId del punto padre
    var punto = await _localDb.GetPuntoByLocalIdAsync(fotoLocal.PuntoLocalId);
    if (punto?.RemoteId is null)
    {
        // Punto no sincronizado todavía — dejar para el próximo ciclo
        Logger.LogWarning("[GeoFoto] Foto {Id} sin punto remoto — skip", op.LocalId);
        continue; // no marcar como failed, no incrementar attempts
    }

    // Subir foto vía API
    var filePath = fotoLocal.RutaLocal;
    if (!File.Exists(filePath))
    {
        await _localDb.MarkFailedAsync(op.Id, "Archivo no encontrado");
        break;
    }

    await using (var stream = File.OpenRead(filePath))
    {
        var result = await _api.AgregarFotoAPuntoAsync(
            punto.RemoteId.Value, stream, fotoLocal.NombreArchivo,
            "image/jpeg", ct);

        fotoLocal.RemoteId = result.FotoId;
        fotoLocal.SyncStatus = "Synced";
        await _localDb.UpdateFotoAsync(fotoLocal);
    }
    break;
```

### 3.4 — Fix Push: sincronizar fotos eliminadas offline

```csharp
case ("Delete", "Foto"):
    var fotoDelete = await _localDb.GetFotoByLocalIdAsync(op.LocalId);
    if (fotoDelete?.RemoteId is not null)
    {
        try { await _api.DeleteFotoAsync(fotoDelete.RemoteId.Value, ct); }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        { /* Ya eliminada en servidor — OK */ }
    }
    // Limpiar local
    await _localDb.DeleteFotoLocalAsync(op.LocalId);
    break;
```

### 3.5 — Fix Pull: detectar fotos nuevas/eliminadas del servidor

VERIFICAR qué devuelve GET /api/sync/delta.

Si el endpoint NO incluye fotos en la respuesta, MODIFICARLO:

```csharp
// En SyncController.cs — GET /api/sync/delta?since={timestamp}
// El DeltaResponse DEBE incluir:
//   - Puntos modificados/creados desde 'since'
//   - Fotos modificadas/creadas desde 'since'
//   - IDs de entidades eliminadas desde 'since' (si hay audit log)
```

Si no existe tabla de auditoría de eliminaciones, CREARLA:

```csharp
// Tabla para trackear hard-deletes
public class DeletedEntity
{
    public int Id { get; set; }
    public string EntityType { get; set; } = "";   // "Punto" | "Foto"
    public int EntityId { get; set; }
    public DateTime DeletedAt { get; set; }
}
```

Agregar al DbContext y crear migration:
```powershell
dotnet ef migrations add AddDeletedEntities --project src\GeoFoto.Api\GeoFoto.Api.csproj
dotnet ef database update --project src\GeoFoto.Api\GeoFoto.Api.csproj
```

Modificar los endpoints DELETE de puntos y fotos para registrar en esta tabla.

En PullAsync del Mobile, procesar las entidades eliminadas:

```csharp
// Borrar localmente lo que fue eliminado en servidor
foreach (var deleted in delta.Eliminados)
{
    if (deleted.EntityType == "Punto")
    {
        var local = await _localDb.GetPuntoByRemoteIdAsync(deleted.EntityId);
        if (local is not null && local.SyncStatus == "Synced")
            await _localDb.DeletePuntoLocalAsync(local.LocalId);
    }
    else if (deleted.EntityType == "Foto")
    {
        var local = await _localDb.GetFotoByRemoteIdAsync(deleted.EntityId);
        if (local is not null && local.SyncStatus == "Synced")
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "photos", local.NombreArchivo);
            if (File.Exists(path)) File.Delete(path);
            await _localDb.DeleteFotoLocalAsync(local.LocalId);
        }
    }
}
```

### 3.6 — Verificación Bloque 3

```
[ ] Crear punto + foto offline → activar red → sync → punto y foto en Web
[ ] Crear punto + foto en Web → sync en Mobile → punto y foto aparecen
[ ] Eliminar foto offline → sync → foto eliminada en servidor
[ ] Eliminar foto en Web → sync delta → foto eliminada en Mobile
[ ] Editar punto en Web → sync delta → punto actualizado en Mobile
[ ] Editar punto offline → sync → punto actualizado en Web
[ ] Badge de pendientes actualiza correctamente después de sync
[ ] No hay duplicados después de sync bidireccional
[ ] dotnet build GeoFoto.sln → 0 errores
```

---

## VERIFICACIÓN FINAL INTEGRADA

Después de los 3 bloques, hacer un ciclo completo:

```
MOBILE:
  [ ] Tomar foto → marker aparece → tocar marker → carrusel con foto
  [ ] Tocar foto → visor con Compartir/Agregar/Eliminar
  [ ] Compartir → abre sheet nativo
  [ ] Agregar → cámara → foto aparece en carrusel
  [ ] Eliminar → confirmación → foto desaparece
  [ ] Modo avión → tomar foto → sacar modo avión → sync automático
  [ ] Foto aparece en Web después del sync

WEB:
  [ ] Click marker → drawer → carrusel
  [ ] Click foto → visor con Descargar/Agregar/Eliminar
  [ ] Descargar → archivo baja al browser
  [ ] Agregar → file picker → foto sin GPS se sube OK
  [ ] Eliminar → confirmación → foto desaparece
  [ ] Foto eliminada en Web → sync → desaparece en Mobile

SYNC:
  [ ] Mobile→Web y Web→Mobile bidireccional sin duplicados
  [ ] Badge de pendientes llega a 0 después de sync exitoso

Si todo pasa: "COMPLETO — Galería con acciones + sync bidireccional operativo."
```
```
