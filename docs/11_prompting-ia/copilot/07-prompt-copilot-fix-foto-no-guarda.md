# Prompt para GitHub Copilot — Fix: Foto No Se Guarda ni Crea Marker Después de Captura

> **CONTEXTO DE LA REGRESIÓN:**
> Se corrigieron 4 bugs previos (pantalla en blanco, overlay z-index, doble botón cámara,
> pantalla blanca post-foto). Ahora el flujo avanza correctamente: la app inicia con el
> mapa centrado, el botón de cámara abre la cámara nativa, el usuario saca la foto y
> vuelve a la app. PERO: la foto no se guarda (ni en SQLite, ni en filesystem, ni en API)
> y no se crea el marker en el mapa. El usuario vuelve al mapa vacío como si nada hubiera pasado.

## PROMPT PARA COPILOT

```
Eres un desarrollador .NET MAUI senior y QA engineer.
La app GeoFoto tiene un bug CRÍTICO: después de tomar una foto con la cámara nativa,
la app vuelve al mapa pero NO guarda la foto y NO crea el marker asociado.

ESTADO ACTUAL CONFIRMADO (lo que SÍ funciona):
  ✅ App inicia correctamente (sin pantalla en blanco)
  ✅ Mapa se centra en la ubicación del dispositivo
  ✅ Botón cámara (MudFab) abre la cámara nativa de Android
  ✅ El usuario puede sacar la foto
  ✅ La app vuelve al mapa después de la foto (sin crash ni pantalla blanca)

LO QUE NO FUNCIONA:
  ❌ La foto no se guarda en el filesystem local (AppDataDirectory)
  ❌ No se crea PuntoLocal en SQLite (tabla Puntos_Local)
  ❌ No se crea FotoLocal en SQLite (tabla Fotos_Local)
  ❌ No se encola en SyncQueue
  ❌ No aparece marker nuevo en el mapa
  ❌ No se muestra snackbar de éxito ni de error (silencio total)

---

## ARQUITECTURA RELEVANTE

### Estructura de la solución
```text
GeoFoto.sln
├── GeoFoto.Api/           → ASP.NET Core Web API, EF Core, SQL Server
│   ├── Controllers/       → PuntosController, FotosController, SyncController
│   └── Program.cs         → Swagger + CORS, puerto 5000
├── GeoFoto.Shared/        → Razor Class Library compartida
│   ├── Pages/Mapa.razor   → Componente principal del mapa + botón cámara
│   ├── Services/          → IGeoFotoApiClient, IFotoUploadStrategy
│   └── wwwroot/js/leaflet-interop.js → JS Interop con Leaflet
├── GeoFoto.Web/           → Blazor Web App InteractiveServer
└── GeoFoto.Mobile/        → MAUI Hybrid Android
    ├── Services/           → LocalDbService (SQLite), SyncService, CameraService
    ├── MauiProgram.cs      → DI + HttpClient
    └── wwwroot/index.html  → Solo blazor.webview.js (NO startBlazor)
```

### Modelos SQLite (sqlite-net-pcl)
```csharp
[Table("Puntos_Local")]
public class PuntoLocal
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public int? RemoteId { get; set; }
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string FechaCreacion { get; set; } = DateTime.UtcNow.ToString("o");
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    public string SyncStatus { get; set; } = "Pending";
}

[Table("Fotos_Local")]
public class FotoLocal
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }
    public int? RemoteId { get; set; }
    [Indexed]
    public int PuntoLocalId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaLocal { get; set; } = string.Empty;
    public string? FechaTomada { get; set; }
    public long TamanoBytes { get; set; }
    public double? LatitudExif { get; set; }
    public double? LongitudExif { get; set; }
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");
    public string SyncStatus { get; set; } = "Pending";
}

[Table("SyncQueue")]
public class SyncQueueEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int LocalId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int Attempts { get; set; } = 0;
    public string? LastAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
```

### Flujo esperado al tomar foto (CU-02)
1. Usuario toca MudFab cámara → `OnCapturaFoto()` en Mapa.razor
2. Se invoca `MediaPicker.Default.CapturePhotoAsync()`
3. Cámara nativa se abre, usuario toma la foto, confirma
4. Se recibe `FileResult` con ruta temporal de la imagen
5. Se copia la imagen a `AppDataDirectory/fotos/{puntoId}/{guid}.jpg`
6. Se extraen coordenadas GPS del EXIF con MetadataExtractor
7. Si no hay GPS en EXIF → se usa la ubicación actual del dispositivo (MAUI Geolocation)
8. Se busca si hay un punto cercano (dentro del radio configurable, default 50m)
9. Si NO hay punto cercano → se crea PuntoLocal en SQLite con nombre auto "Punto YYYY-MM-DD HH:mm:ss"
10. Se crea FotoLocal en SQLite con referencia al PuntoLocalId
11. Se encola operación en SyncQueue con Status="Pending"
12. Se agrega marker al mapa vía JS Interop: `leafletInterop.addMarker(lat, lng, id, desc)`
13. Se muestra MudSnackbar de éxito: "Foto guardada — completá los datos del punto"
14. Se recentra el mapa en la posición de la foto

### JS Interop para markers (leaflet-interop.js)
```javascript
window.leafletInterop = {
    _map: null, _markers: [], _dotnetRef: null, _ready: false,

    init: function (elementId, dotnetRef) { /* ya funciona OK */ },

    addMarkers: function (puntosJson) {
        // Espera JSON string o array de {id, latitud, longitud, nombre, ...}
        // Limpia markers anteriores y agrega los nuevos
    },

    addMarker: function (lat, lng, id, desc) {
        // Agrega UN solo marker al mapa sin limpiar los existentes
        // Si esta función NO existe, hay que crearla
    },

    setView: function (lat, lng, zoom) {
        if (this._ready && this._map) this._map.setView([lat, lng], zoom || 15);
    }
};
```

---

## PASO 0 — DETECTAR ADB Y DISPOSITIVO

```powershell
$adbRutas = @(
    "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
    "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    "C:\Android\platform-tools\adb.exe",
    (Get-Command adb -ErrorAction SilentlyContinue)?.Source
)
$global:ADB = $adbRutas | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $global:ADB) {
    $global:ADB = Get-ChildItem "$env:LOCALAPPDATA","C:\Program Files (x86)" `
        -Recurse -Filter "adb.exe" -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
}
Write-Host "ADB: $global:ADB"

& $global:ADB kill-server; Start-Sleep 2; & $global:ADB start-server; Start-Sleep 3

$global:DEV = (& $global:ADB devices | Select-String "\sdevice$" |
    Select-Object -First 1) -replace "\s.*","" | ForEach-Object { $_.Trim() }
Write-Host "Dispositivo: $global:DEV"

$dims = [regex]::Match((& $global:ADB -s $global:DEV shell wm size), "(\d+)x(\d+)")
$global:W = [int]$dims.Groups[1].Value
$global:H = [int]$dims.Groups[2].Value
Write-Host "Pantalla: $($global:W)x$($global:H)"

$global:PKG = "com.companyname.geofoto.mobile"

function TapPct($xPct, $yPct, $label) {
    $x = [int]($global:W * $xPct); $y = [int]($global:H * $yPct)
    & $global:ADB -s $global:DEV shell input tap $x $y
    Write-Host "  TAP $label ($x,$y)"
    Start-Sleep -Milliseconds 800
}
function Take-Screenshot($name) {
    $ts = Get-Date -Format "HHmmss"
    $local = "debug_screenshots\${name}_${ts}.png"
    New-Item -ItemType Directory -Path "debug_screenshots" -Force | Out-Null
    & $global:ADB -s $global:DEV shell screencap -p /sdcard/sc_temp.png
    & $global:ADB -s $global:DEV pull /sdcard/sc_temp.png $local 2>$null
    Write-Host "  Screenshot: $local ($(((Get-Item $local).Length / 1KB).ToString('F0'))KB)"
}
function LimpiarLogcat { & $global:ADB -s $global:DEV logcat -c 2>$null }
function GetErrores($outFile) {
    & $global:ADB -s $global:DEV logcat -d 2>&1 | Out-File $outFile -Encoding utf8
    $errores = (Get-Content $outFile | Select-String "FATAL|AndroidRuntime|System.Exception|NullReference|InvalidOperation|at GeoFoto" -CaseSensitive:$false)
    if ($errores.Count -gt 0) {
        Write-Host "  ERRORES ENCONTRADOS ($($errores.Count)):"
        $errores | Select-Object -First 15 | ForEach-Object { Write-Host "    $_" }
    } else {
        Write-Host "  Sin errores fatales en logcat"
    }
}
function LaunchApp {
    & $global:ADB -s $global:DEV shell am force-stop $global:PKG 2>$null
    Start-Sleep 1
    & $global:ADB -s $global:DEV shell monkey -p $global:PKG -c android.intent.category.LAUNCHER 1 2>$null
    Start-Sleep 6
}
```

---

## PASO 1 — DIAGNÓSTICO: AUDITAR TODO EL FLUJO DE CAPTURA

Antes de tocar código, necesitás entender EXACTAMENTE dónde se rompe la cadena.

### 1A — Buscar el handler del botón cámara

```powershell
# Buscar OnCapturaFoto, OnCameraClick, CapturePhoto, MediaPicker en todo el repo
Get-ChildItem -Recurse -Include *.razor,*.cs,*.js |
    Select-String -Pattern "CapturaFoto|CameraClick|CapturePhoto|MediaPicker|CapturarFoto|OnFab|TomarFoto" |
    ForEach-Object { Write-Host "$($_.Filename):$($_.LineNumber) → $($_.Line.Trim())" }
```

Buscá en los resultados:
1. ¿Qué componente (.razor) tiene el MudFab con el handler?
2. ¿Qué método se ejecuta al tocar el botón?
3. ¿Ese método llama a MediaPicker?
4. ¿Después de MediaPicker, qué hace con el FileResult?

### 1B — Buscar el servicio de guardado local

```powershell
# Buscar LocalDbService, InsertAsync, PuntoLocal, FotoLocal, SavePunto, GuardarPunto
Get-ChildItem -Recurse -Include *.cs |
    Select-String -Pattern "InsertAsync|LocalDbService|SavePunto|GuardarPunto|GuardarFoto|PuntoLocal|FotoLocal" |
    ForEach-Object { Write-Host "$($_.Filename):$($_.LineNumber) → $($_.Line.Trim())" }
```

### 1C — Buscar el JS interop para agregar un solo marker

```powershell
# Buscar addMarker (singular) en leaflet-interop.js
Get-ChildItem -Recurse -Include *.js |
    Select-String -Pattern "addMarker\b" |
    ForEach-Object { Write-Host "$($_.Filename):$($_.LineNumber) → $($_.Line.Trim())" }
```

### 1D — Leer los archivos clave completos

```powershell
# Leer los archivos completos para entender el flujo
Write-Host "=== Mapa.razor ==="
Get-Content "GeoFoto.Shared\Pages\Mapa.razor" -ErrorAction SilentlyContinue

Write-Host "`n=== MobileLayout.razor ==="
Get-Content "GeoFoto.Mobile\Components\MobileLayout.razor" -ErrorAction SilentlyContinue
Get-Content "GeoFoto.Shared\Components\MobileLayout.razor" -ErrorAction SilentlyContinue

Write-Host "`n=== leaflet-interop.js ==="
Get-Content "GeoFoto.Shared\wwwroot\js\leaflet-interop.js" -ErrorAction SilentlyContinue

Write-Host "`n=== LocalDbService.cs ==="
Get-ChildItem -Recurse -Filter "LocalDbService.cs" | ForEach-Object { Get-Content $_.FullName }

Write-Host "`n=== CameraService.cs / ICameraService.cs ==="
Get-ChildItem -Recurse -Filter "*amera*ervice*.cs" | ForEach-Object {
    Write-Host "--- $($_.FullName) ---"
    Get-Content $_.FullName
}
```

ANALIZAR LOS RESULTADOS Y ANOTAR:
  - Archivo y línea exacta donde está el handler del botón de cámara
  - Si existe `try/catch` que silencia errores (esta es la causa más probable)
  - Si el `FileResult` de `MediaPicker` se procesa o se descarta
  - Si existe un método para guardar en SQLite que nunca se llama
  - Si `addMarker` (singular, no `addMarkers` plural) existe en el JS
  - Si el método llama a `InvokeAsync`/`StateHasChanged` después de guardar

---

## PASO 2 — TEST EN VIVO: CONFIRMAR EL PUNTO DE FALLO CON LOGCAT

```powershell
# Asegurar API corriendo
$apiProc = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue |
    Where-Object { $_.CommandLine -like "*GeoFoto.Api*" }
if (-not $apiProc) {
    Write-Host "Iniciando API..."
    Start-Process powershell -ArgumentList "-NoExit -Command cd GeoFoto.Api; dotnet run --urls http://0.0.0.0:5000"
    Start-Sleep 8
}

# Verificar API
try {
    $resp = Invoke-WebRequest "http://localhost:5000/api/puntos" -UseBasicParsing -TimeoutSec 5
    Write-Host "API OK — Puntos actuales: $(($resp.Content | ConvertFrom-Json).Count)"
} catch { Write-Host "WARN: API no responde" }

# Tunnel ADB
& $global:ADB -s $global:DEV reverse tcp:5000 tcp:5000

# Limpiar logcat, lanzar app, tomar foto
LimpiarLogcat
LaunchApp
Take-Screenshot "pre_camara"

# Tocar botón cámara — AJUSTAR COORDENADAS según ubicación real del MudFab
# El MudFab suele estar abajo-derecha. Verificar con screenshot.
# Valores típicos: x=0.85-0.92, y=0.85-0.92
TapPct 0.90 0.90 "MudFab camara"
Start-Sleep 3
Take-Screenshot "camara_abierta"

# Simular tomar foto: tap en el botón shutter de la cámara nativa
# El shutter suele estar centrado abajo: x=0.50, y=0.92
TapPct 0.50 0.92 "shutter"
Start-Sleep 3
Take-Screenshot "foto_tomada"

# Confirmar/aceptar la foto (botón de check/confirm)
# Suele estar abajo-derecha: x=0.75, y=0.92
TapPct 0.75 0.92 "confirmar foto"
Start-Sleep 5
Take-Screenshot "post_foto"

# Recoger logcat COMPLETO
GetErrores "debug_screenshots\logcat_captura_foto.txt"

# Buscar mensajes específicos de GeoFoto en logcat
Write-Host "`n=== LOGCAT GEOFOTO ==="
Get-Content "debug_screenshots\logcat_captura_foto.txt" |
    Select-String "GeoFoto|geofoto|CapturaFoto|MediaPicker|FileResult|SQLite|PuntoLocal|InsertAsync|addMarker|Exception|Error|FATAL" -CaseSensitive:$false |
    Select-Object -First 40 |
    ForEach-Object { Write-Host $_ }

# Verificar si se creó algún archivo de foto en el dispositivo
Write-Host "`n=== ARCHIVOS EN APP ==="
& $global:ADB -s $global:DEV shell "run-as $($global:PKG) ls -la files/fotos/ 2>/dev/null || echo 'directorio fotos no existe'"
& $global:ADB -s $global:DEV shell "run-as $($global:PKG) find files/ -name '*.jpg' -o -name '*.jpeg' -o -name '*.png' 2>/dev/null | head -10"

# Verificar SQLite
Write-Host "`n=== SQLITE ==="
& $global:ADB -s $global:DEV shell "run-as $($global:PKG) ls -la files/*.db3 2>/dev/null || echo 'no hay .db3'"
```

ANALIZAR LOS RESULTADOS:
  - Si logcat muestra excepciones después de MediaPicker → la foto no se procesa
  - Si logcat muestra "FileResult" pero no "InsertAsync" → falla entre recepción y guardado
  - Si no hay archivos .jpg en files/fotos/ → la copia al filesystem falló
  - Si no hay .db3 → SQLite no se inicializó
  - Si hay .db3 pero no hay archivos → el guardado falla silenciosamente

---

## PASO 3 — DIAGNOSTICAR LAS 5 CAUSAS MÁS PROBABLES

Basándote en los resultados del PASO 1 y PASO 2, checkeá estas causas en orden de probabilidad:

### CAUSA 1: Try/catch vacío que silencia el error (MÁS PROBABLE)
```
BUSCAR en Mapa.razor, MobileLayout.razor y cualquier archivo que maneje la foto:
  - catch { } ← sin logging
  - catch (Exception) { } ← sin logging
  - catch (Exception ex) { /* solo logging pero no muestra nada al usuario */ }
FIX: Agregar Snackbar.Add($"Error: {ex.Message}", Severity.Error) en cada catch
```

### CAUSA 2: El handler del MudFab no tiene implementado el flujo completo
```
BUSCAR: El método OnCapturaFoto/OnCameraClick puede tener:
  - Solo la llamada a MediaPicker pero no el guardado posterior
  - Un TODO o comentario indicando implementación pendiente
  - Una llamada a un servicio que no existe o no está registrado en DI
FIX: Implementar el flujo completo (ver PASO 4)
```

### CAUSA 3: MediaPicker devuelve el FileResult pero no se procesa en el UI thread
```
En MAUI Blazor Hybrid, después de MediaPicker el código vuelve en un thread diferente.
Si no se usa InvokeAsync/await o MainThread.InvokeOnMainThreadAsync, el StateHasChanged
y el JS Interop fallan silenciosamente.
FIX: Envolver todo el post-procesamiento en:
  await InvokeAsync(async () => { /* procesar foto, guardar, agregar marker */ });
```

### CAUSA 4: LocalDbService no está registrado en DI o la DB no está inicializada
```
BUSCAR en MauiProgram.cs:
  - ¿Se registra LocalDbService como Singleton o Scoped?
  - ¿Se llama a InitializeAsync/CreateTableAsync al inicio?
  - ¿Las tablas Puntos_Local, Fotos_Local, SyncQueue se crean?
FIX: Asegurar registro en DI y creación de tablas
```

### CAUSA 5: addMarker (singular) no existe en leaflet-interop.js
```
El JS solo tiene addMarkers (plural, borra y recarga todos).
Si el código C# llama a addMarker (singular) que no existe, falla silenciosamente
porque IJSRuntime no lanza excepción cuando la función JS no existe en algunos contextos.
FIX: Agregar función addMarker en leaflet-interop.js O usar addMarkers recargando
     todos los puntos después de guardar el nuevo.
```

---

## PASO 4 — APLICAR EL FIX

Basándote en el diagnóstico, aplicá las correcciones necesarias.
A continuación están los fixes para CADA causa. Aplicá solo los que correspondan.

### FIX A — Try/catch con logging + snackbar

Buscar TODOS los try/catch en el flujo de captura y agregar feedback visual:
```csharp
catch (Exception ex)
{
    System.Diagnostics.Debug.WriteLine($"[GeoFoto] ERROR en captura: {ex}");
    await InvokeAsync(() =>
    {
        Snackbar.Add($"Error al procesar la foto: {ex.Message}", Severity.Error);
        StateHasChanged();
    });
}
```

### FIX B — Implementar flujo completo OnCapturaFoto

Si el método no tiene el flujo completo, implementarlo así:

```csharp
private async Task OnCapturaFoto()
{
    try
    {
        // 1. Verificar permisos
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                Snackbar.Add("Se necesita permiso de cámara", Severity.Warning);
                return;
            }
        }

        // 2. Capturar foto con cámara nativa
        var resultado = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
        {
            Title = "Tomar foto"
        });

        if (resultado is null)
        {
            System.Diagnostics.Debug.WriteLine("[GeoFoto] Usuario canceló la captura");
            return; // usuario canceló
        }

        System.Diagnostics.Debug.WriteLine($"[GeoFoto] Foto capturada: {resultado.FullPath}");

        // 3. Procesar en UI thread (CRÍTICO en MAUI Blazor Hybrid)
        await InvokeAsync(async () =>
        {
            try
            {
                // 4. Copiar foto al directorio de la app
                var fotosDir = Path.Combine(FileSystem.AppDataDirectory, "fotos");
                Directory.CreateDirectory(fotosDir);
                var nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(resultado.FileName)}";
                var rutaDestino = Path.Combine(fotosDir, nombreArchivo);

                using (var sourceStream = await resultado.OpenReadAsync())
                using (var destStream = File.Create(rutaDestino))
                {
                    await sourceStream.CopyToAsync(destStream);
                }

                var fileInfo = new FileInfo(rutaDestino);
                System.Diagnostics.Debug.WriteLine($"[GeoFoto] Foto copiada: {rutaDestino} ({fileInfo.Length} bytes)");

                // 5. Obtener coordenadas (EXIF o GPS del dispositivo)
                double lat = 0, lng = 0;
                bool tieneGps = false;

                try
                {
                    // Intentar extraer GPS del EXIF
                    using var exifStream = File.OpenRead(rutaDestino);
                    var directories = MetadataExtractor.ImageMetadataReader.ReadMetadata(exifStream);
                    var gpsDir = directories.OfType<MetadataExtractor.Formats.Exif.GpsDirectory>().FirstOrDefault();
                    if (gpsDir is not null)
                    {
                        var geoLoc = gpsDir.GetGeoLocation();
                        if (geoLoc is not null)
                        {
                            lat = geoLoc.Latitude;
                            lng = geoLoc.Longitude;
                            tieneGps = true;
                        }
                    }
                }
                catch (Exception exifEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[GeoFoto] EXIF error (no fatal): {exifEx.Message}");
                }

                // Si no tiene EXIF GPS, usar ubicación del dispositivo
                if (!tieneGps)
                {
                    try
                    {
                        var location = await Geolocation.Default.GetLocationAsync(
                            new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
                        if (location is not null)
                        {
                            lat = location.Latitude;
                            lng = location.Longitude;
                            tieneGps = true;
                        }
                    }
                    catch (Exception gpsEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GeoFoto] GPS error: {gpsEx.Message}");
                    }
                }

                if (!tieneGps)
                {
                    Snackbar.Add("La foto no tiene ubicación — se usó posición (0,0)", Severity.Warning);
                }

                // 6. Crear PuntoLocal en SQLite
                var ahora = DateTime.UtcNow;
                var punto = new PuntoLocal
                {
                    Latitud = lat,
                    Longitud = lng,
                    Nombre = $"Punto {ahora:yyyy-MM-dd HH:mm:ss}",
                    Descripcion = "",
                    FechaCreacion = ahora.ToString("o"),
                    UpdatedAt = ahora.ToString("o"),
                    SyncStatus = "Pending"
                };
                await LocalDb.InsertAsync(punto);
                System.Diagnostics.Debug.WriteLine($"[GeoFoto] PuntoLocal creado: LocalId={punto.LocalId}");

                // 7. Crear FotoLocal en SQLite
                var foto = new FotoLocal
                {
                    PuntoLocalId = punto.LocalId,
                    NombreArchivo = nombreArchivo,
                    RutaLocal = rutaDestino,
                    FechaTomada = ahora.ToString("o"),
                    TamanoBytes = fileInfo.Length,
                    LatitudExif = tieneGps ? lat : null,
                    LongitudExif = tieneGps ? lng : null,
                    UpdatedAt = ahora.ToString("o"),
                    SyncStatus = "Pending"
                };
                await LocalDb.InsertAsync(foto);
                System.Diagnostics.Debug.WriteLine($"[GeoFoto] FotoLocal creada: LocalId={foto.LocalId}");

                // 8. Encolar en SyncQueue
                var syncEntry = new SyncQueueEntry
                {
                    OperationType = "Create",
                    EntityType = "Punto",
                    LocalId = punto.LocalId,
                    Payload = System.Text.Json.JsonSerializer.Serialize(punto),
                    Status = "Pending",
                    CreatedAt = ahora.ToString("o")
                };
                await LocalDb.InsertAsync(syncEntry);
                System.Diagnostics.Debug.WriteLine("[GeoFoto] SyncQueue encolado");

                // 9. Agregar marker al mapa
                await JS.InvokeVoidAsync("leafletInterop.addSingleMarker",
                    lat, lng, punto.LocalId, punto.Nombre ?? "");

                // 10. Recentrar mapa
                await JS.InvokeVoidAsync("leafletInterop.setView", lat, lng, 16);

                // 11. Mostrar feedback
                Snackbar.Add("Foto guardada — completá los datos del punto", Severity.Success);
                StateHasChanged();
            }
            catch (Exception innerEx)
            {
                System.Diagnostics.Debug.WriteLine($"[GeoFoto] ERROR procesando foto: {innerEx}");
                Snackbar.Add($"Error al guardar la foto: {innerEx.Message}", Severity.Error);
                StateHasChanged();
            }
        });

        // 12. Invalidar mapa (fix WebView pause/resume)
        await Task.Delay(250);
        await JS.InvokeVoidAsync("leafletInterop.invalidateSize");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[GeoFoto] ERROR FATAL en captura: {ex}");
        try { Snackbar.Add($"Error de cámara: {ex.Message}", Severity.Error); } catch { }
    }
}
```

### FIX C — Agregar addSingleMarker en leaflet-interop.js

```javascript
// Agregar DENTRO del objeto window.leafletInterop:

addSingleMarker: function (lat, lng, id, desc) {
    if (!this._ready || !this._map) {
        console.warn('[GeoFoto] Mapa no listo para addSingleMarker');
        return;
    }
    try {
        var marker = L.marker([lat, lng])
            .addTo(this._map)
            .bindPopup('<b>' + (desc || 'Punto #' + id) + '</b><br>(' +
                lat.toFixed(5) + ', ' + lng.toFixed(5) + ')');
        this._markers.push({ id: id, marker: marker });

        // Si hay DotNetRef, vincular click
        if (this._dotnetRef) {
            marker.on('click', () => {
                this._dotnetRef.invokeMethodAsync('OnMarkerClicked', id);
            });
        }

        console.log('[GeoFoto] Marker agregado: id=' + id + ' en [' + lat + ',' + lng + ']');
    } catch (e) {
        console.error('[GeoFoto] Error addSingleMarker:', e);
    }
},

invalidateSize: function () {
    if (this._map) {
        this._map.invalidateSize();
        console.log('[GeoFoto] invalidateSize ejecutado');
    }
},
```

### FIX D — Asegurar LocalDbService en DI

En `MauiProgram.cs`, verificar que exista:
```csharp
builder.Services.AddSingleton<LocalDbService>();
// Y que LocalDbService tenga un método de inicialización que cree las tablas:
// await db.CreateTableAsync<PuntoLocal>();
// await db.CreateTableAsync<FotoLocal>();
// await db.CreateTableAsync<SyncQueueEntry>();
```

En `LocalDbService.cs`, verificar que el constructor o `InitializeAsync` cree las tablas:
```csharp
public class LocalDbService
{
    private SQLiteAsyncConnection _db;

    public async Task InitializeAsync()
    {
        if (_db is not null) return;
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "geofoto.db3");
        _db = new SQLiteAsyncConnection(dbPath);
        await _db.CreateTableAsync<PuntoLocal>();
        await _db.CreateTableAsync<FotoLocal>();
        await _db.CreateTableAsync<SyncQueueEntry>();
    }

    public async Task<int> InsertAsync<T>(T item) where T : new()
    {
        await InitializeAsync();
        return await _db.InsertAsync(item);
    }

    public async Task<List<T>> GetAllAsync<T>() where T : new()
    {
        await InitializeAsync();
        return await _db.Table<T>().ToListAsync();
    }
}
```

### FIX E — Asegurar inyección en el componente del mapa

En `Mapa.razor`, verificar que estén inyectados:
```razor
@inject IJSRuntime JS
@inject ISnackbar Snackbar
@inject LocalDbService LocalDb
@* O el nombre que tenga el servicio de base de datos local *@
```

Si `LocalDbService` es exclusivo de Mobile y el componente está en Shared,
usar una interfaz `ILocalStorageService` y registrar la implementación concreta
solo en el proyecto Mobile:
```csharp
// En GeoFoto.Shared:
public interface ILocalStorageService
{
    Task<int> InsertPuntoAsync(PuntoLocal punto);
    Task<int> InsertFotoAsync(FotoLocal foto);
    Task<int> InsertSyncEntryAsync(SyncQueueEntry entry);
    Task<List<PuntoLocal>> GetPuntosAsync();
}

// En GeoFoto.Mobile:
public class MobileStorageService : ILocalStorageService { /* usa SQLite */ }

// En GeoFoto.Web:
public class WebStorageService : ILocalStorageService { /* usa API HTTP */ }
```

---

## PASO 5 — COMPILAR, INSTALAR Y VERIFICAR

```powershell
Write-Host "=== COMPILANDO ==="
dotnet build GeoFoto.Shared\GeoFoto.Shared.csproj --configuration Debug --verbosity quiet
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Shared no compila"; exit 1 }
dotnet build GeoFoto.Mobile\GeoFoto.Mobile.csproj --configuration Debug --framework net10.0-android --verbosity minimal
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Mobile no compila"; exit 1 }
Write-Host "Compilación OK"

Write-Host "`n=== INSTALANDO EN DISPOSITIVO ==="
$apk = Get-ChildItem "GeoFoto.Mobile\bin\Debug\net10.0-android" -Filter "*-Signed.apk" -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
& $global:ADB -s $global:DEV uninstall $global:PKG 2>$null
& $global:ADB -s $global:DEV install -r $apk.FullName
& $global:ADB -s $global:DEV reverse tcp:5000 tcp:5000
Write-Host "APK instalada"

Write-Host "`n=== EJECUTANDO TEST ==="
LimpiarLogcat
LaunchApp
Take-Screenshot "test_01_app_inicio"

# Contar puntos en API antes del test
$puntosAntes = 0
try { $puntosAntes = ((Invoke-WebRequest "http://localhost:5000/api/puntos" -UseBasicParsing -TimeoutSec 3).Content | ConvertFrom-Json).Count } catch {}
Write-Host "Puntos en API antes: $puntosAntes"

# Contar puntos en SQLite antes del test
& $global:ADB -s $global:DEV shell "run-as $($global:PKG) cat files/geofoto.db3" > debug_screenshots\db_antes.db3 2>$null

# Tocar MudFab cámara
TapPct 0.90 0.90 "MudFab camara"
Start-Sleep 4
Take-Screenshot "test_02_camara"

# Shutter
TapPct 0.50 0.92 "shutter"
Start-Sleep 3

# Confirmar
TapPct 0.75 0.92 "confirmar"
Start-Sleep 6
Take-Screenshot "test_03_post_foto"

# Esperar procesamiento
Start-Sleep 3
Take-Screenshot "test_04_resultado"

# Recoger logcat
GetErrores "debug_screenshots\logcat_test.txt"

# Buscar trazas de GeoFoto
Write-Host "`n=== TRAZAS GEOFOTO ==="
Get-Content "debug_screenshots\logcat_test.txt" |
    Select-String "\[GeoFoto\]" |
    ForEach-Object { Write-Host $_ }

# Verificar archivos de fotos
Write-Host "`n=== FOTOS EN DISPOSITIVO ==="
& $global:ADB -s $global:DEV shell "run-as $($global:PKG) find files/ -name '*.jpg' -o -name '*.jpeg' -o -name '*.png' 2>/dev/null"

# Verificar SQLite
Write-Host "`n=== SQLITE DESPUES ==="
& $global:ADB -s $global:DEV shell "run-as $($global:PKG) ls -la files/*.db3 2>/dev/null"

# Verificar puntos en API después
$puntosDespues = 0
try { $puntosDespues = ((Invoke-WebRequest "http://localhost:5000/api/puntos" -UseBasicParsing -TimeoutSec 3).Content | ConvertFrom-Json).Count } catch {}
Write-Host "Puntos en API después: $puntosDespues"

# Verificar si el marker aparece en el screenshot
$tamScreenshot = (Get-Item "debug_screenshots\test_04_resultado_*.png" | Sort-Object LastWriteTime -Descending | Select-Object -First 1).Length
Write-Host "Tamaño screenshot resultado: $([int]($tamScreenshot/1KB))KB"

# EVALUACIÓN
$crashes = (Get-Content "debug_screenshots\logcat_test.txt" | Select-String "FATAL|AndroidRuntime").Count
$proc = (& $global:ADB -s $global:DEV shell ps -A 2>$null | Select-String "geofoto" -CaseSensitive:$false).Count
$trazasOk = (Get-Content "debug_screenshots\logcat_test.txt" | Select-String "\[GeoFoto\] PuntoLocal creado").Count
$trazasFoto = (Get-Content "debug_screenshots\logcat_test.txt" | Select-String "\[GeoFoto\] FotoLocal creada").Count
$trazasMarker = (Get-Content "debug_screenshots\logcat_test.txt" | Select-String "\[GeoFoto\] Marker agregado").Count

Write-Host "`n=== RESULTADO ==="
Write-Host "Crashes: $crashes"
Write-Host "Proceso vivo: $proc"
Write-Host "PuntoLocal creado: $trazasOk"
Write-Host "FotoLocal creada: $trazasFoto"
Write-Host "Marker agregado: $trazasMarker"

if ($crashes -eq 0 -and $trazasOk -gt 0 -and $trazasFoto -gt 0 -and $trazasMarker -gt 0) {
    Write-Host "`n✅ ÉXITO — Foto guardada y marker creado"
    Take-Screenshot "EXITO_marker_creado"

    # Commit
    git add -A
    git commit -m "fix(GEO-US10): corrige flujo de captura - foto se guarda en SQLite y marker aparece en mapa

- Implementa flujo completo OnCapturaFoto con MediaPicker + guardado SQLite
- Agrega addSingleMarker en leaflet-interop.js para marker individual
- Envuelve post-procesamiento en InvokeAsync para UI thread safety
- Agrega try/catch con Snackbar feedback en cada etapa
- Asegura creación de tablas SQLite al inicializar LocalDbService
- Encola operación en SyncQueue para sincronización posterior"

    git push origin (git rev-parse --abbrev-ref HEAD)
    Write-Host "Commit y push realizados"
} else {
    Write-Host "`n❌ FALLÓ — Revisar trazas y screenshots"
    Write-Host "Revisar debug_screenshots\logcat_test.txt"
    Write-Host "Si hay errores de DI (no se resuelve LocalDbService) → verificar MauiProgram.cs"
    Write-Host "Si hay errores de JS (addSingleMarker undefined) → verificar leaflet-interop.js"
    Write-Host "Si hay errores de permisos → verificar AndroidManifest.xml"
    Write-Host "REPETIR desde PASO 3 con la nueva información"
}
```

---

## PASO 6 — SI AÚN FALLA: PRUEBA ALTERNATIVA CON GALERÍA

Si el flujo con cámara sigue sin funcionar por limitaciones de adb con MediaPicker,
probar el flujo de galería que usa el mismo código de guardado:

```powershell
# Precargar imagen con GPS en el dispositivo
# Crear imagen de prueba si no existe
if (-not (Test-Path "test_assets\test_gps.jpg")) {
    New-Item -ItemType Directory -Path "test_assets" -Force | Out-Null
    # Descargar imagen con GPS de ejemplo
    Invoke-WebRequest "https://raw.githubusercontent.com/ianare/exif-samples/master/jpg/gps/DSCN0010.jpg" `
        -OutFile "test_assets\test_gps.jpg" -TimeoutSec 30
}

& $global:ADB -s $global:DEV push "test_assets\test_gps.jpg" "/sdcard/DCIM/GeoFotoTest/test_gps.jpg"
& $global:ADB -s $global:DEV shell am broadcast -a android.intent.action.MEDIA_SCANNER_SCAN_FILE -d "file:///sdcard/DCIM/GeoFotoTest/test_gps.jpg"
Start-Sleep 3

LaunchApp
Take-Screenshot "galeria_01_inicio"

# Navegar a la pantalla de Subir
TapPct 0.30 0.04 "Menu Subir"
Start-Sleep 3
Take-Screenshot "galeria_02_subir"

# Tap en el botón de elegir fotos / file upload
TapPct 0.50 0.35 "FileUpload"
Start-Sleep 4

# Buscar la foto en el picker
TapPct 0.50 0.12 "Recientes"
Start-Sleep 2
TapPct 0.17 0.28 "foto test"
Start-Sleep 10
Take-Screenshot "galeria_03_resultado"

GetErrores "debug_screenshots\logcat_galeria.txt"
Write-Host "`n=== TRAZAS GEOFOTO (galería) ==="
Get-Content "debug_screenshots\logcat_galeria.txt" |
    Select-String "\[GeoFoto\]" |
    ForEach-Object { Write-Host $_ }
```

---

## CRITERIO DE ÉXITO FINAL

  [ ] Logcat muestra "[GeoFoto] PuntoLocal creado: LocalId=N"
  [ ] Logcat muestra "[GeoFoto] FotoLocal creada: LocalId=N"
  [ ] Logcat muestra "[GeoFoto] Marker agregado: id=N"
  [ ] Screenshot post-foto muestra un marker nuevo en el mapa
  [ ] Snackbar de éxito visible en el screenshot
  [ ] Sin crashes ni errores FATAL en logcat
  [ ] Proceso GeoFoto sigue corriendo después del test
  [ ] Archivo .jpg existe en AppDataDirectory/fotos/ del dispositivo
  [ ] Commit y push del fix realizados

Comenzá con el PASO 0 ahora. Ejecutá CADA paso secuencialmente.
No saltes al PASO 4 sin haber completado el diagnóstico del PASO 1 y PASO 2.
El diagnóstico te dirá CUÁLES de los 5 fixes necesitás aplicar.
```
