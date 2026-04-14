# Prompt para GitHub Copilot — Fix Pantalla en Blanco al Sacar Foto (Estrategia Correcta)

> ACLARACIÓN IMPORTANTE:
> adb NO puede controlar MediaPicker ni retornar una foto a la app desde la cámara nativa.
> La estrategia correcta es precargar una imagen JPG con GPS en el dispositivo
> y probar el flujo completo usando el picker de galería (mismo código que la cámara).
> Copilot diagnostica con screenshots, logcat y taps dentro de la app.

## PROMPT PARA COPILOT

```
Eres un desarrollador .NET MAUI senior y QA engineer.
La app GeoFoto muestra pantalla en blanco después de procesar una foto.

LIMITACIÓN TÉCNICA IMPORTANTE:
adb shell input tap NO puede controlar la cámara nativa de Android ni hacer
que MediaPicker devuelva una foto a la app. Lo que SÍ podés hacer con adb:
  - Precargar una imagen JPG con GPS en el dispositivo (push)
  - Simular taps DENTRO de la app GeoFoto para navegar
  - Simular taps en el selector nativo de archivos (galería)
  - Capturar screenshots en cada etapa
  - Leer logcat para diagnosticar el error

Estrategia de prueba:
  1. Precargar imagen de prueba con GPS en /sdcard/DCIM/GeoFotoTest/
  2. Usar el flujo de SubirFotos.razor (galería) que ejecuta el mismo código
     que el flujo de cámara: ambos llaman a SubirDesdeStreamAsync()
  3. Diagnosticar el error real con logcat
  4. Aplicar el fix correspondiente
  5. Verificar con screenshots que el marker aparece en el mapa

---

## PASO 0 — DETECTAR ADB Y DISPOSITIVO AUTOMÁTICAMENTE

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
& $global:ADB devices -l

$global:DEV = (& $global:ADB devices | Select-String "\sdevice$" |
    Select-Object -First 1) -replace "\s.*","" | ForEach-Object { $_.Trim() }
Write-Host "Dispositivo: $global:DEV"
Write-Host "Modelo: $((& $global:ADB -s $global:DEV shell getprop ro.product.model).Trim())"

$dims = [regex]::Match((& $global:ADB -s $global:DEV shell wm size), "(\d+)x(\d+)")
$global:W = [int]$dims.Groups[1].Value
$global:H = [int]$dims.Groups[2].Value
Write-Host "Pantalla: $($global:W)x$($global:H)"

$global:PKG = (Select-String "ApplicationId" "GeoFoto.Mobile\GeoFoto.Mobile.csproj" |
    Select-Object -First 1) -replace '.*"([\w.]+)".*','$1'
Write-Host "Package: $global:PKG"

& $global:ADB -s $global:DEV reverse tcp:5000 tcp:5000
```

---

## PASO 1 — PREPARAR IMAGEN DE PRUEBA CON GPS

```powershell
New-Item -ItemType Directory -Path "test_assets" -Force | Out-Null

if (-not (Test-Path "test_assets\foto_prueba_gps.jpg")) {
    Write-Host "Descargando imagen con GPS embebido..."
    try {
        # Imagen real de dominio público con EXIF GPS
        Invoke-WebRequest `
            -Uri "https://github.com/ianare/exif-samples/raw/master/jpg/gps/DSCN0010.jpg" `
            -OutFile "test_assets\foto_prueba_gps.jpg" -TimeoutSec 15
        Write-Host "Descargada"
    } catch {
        Write-Host "Sin internet. Creando JPG mínimo válido..."
        # JPG 1x1 pixel, suficiente para probar el flujo de stream
        [IO.File]::WriteAllBytes("test_assets\foto_prueba_gps.jpg",
            [Convert]::FromBase64String(
            "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8U" +
            "HRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/wAARCAABAAEDASIA" +
            "AhEBAxEB/8QAFgABAQEAAAAAAAAAAAAAAAAABgUEB//EAB0QAAICAQUAAAAAAAAAAAAAAAABAgMR" +
            "BBIVIX//xAAUAQEAAAAAAAAAAAAAAAAAAAAA/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQAC" +
            "EQMRAD8ArtVqM8pQhFLtQvDPQAB//9k="))
    }
}

# Copiar al dispositivo y escanear galería
& $global:ADB -s $global:DEV shell mkdir -p "/sdcard/DCIM/GeoFotoTest"
& $global:ADB -s $global:DEV push "test_assets\foto_prueba_gps.jpg" `
    "/sdcard/DCIM/GeoFotoTest/foto_prueba.jpg"
& $global:ADB -s $global:DEV shell am broadcast `
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE `
    -d "file:///sdcard/DCIM/GeoFotoTest/foto_prueba.jpg"
Start-Sleep 3
Write-Host "Imagen de prueba lista en el dispositivo"
```

---

## PASO 2 — FUNCIONES DE UTILIDAD

```powershell
function Take-Screenshot {
    param([string]$nombre)
    New-Item -ItemType Directory -Path "debug_screenshots" -Force | Out-Null
    $ts     = Get-Date -Format "HHmmss"
    $remote = "/sdcard/gf_${nombre}_$ts.png"
    $local  = "debug_screenshots\gf_${nombre}_$ts.png"
    & $global:ADB -s $global:DEV shell screencap -p $remote 2>$null
    & $global:ADB -s $global:DEV pull $remote $local 2>$null
    & $global:ADB -s $global:DEV shell rm $remote 2>$null
    if (Test-Path $local) { Write-Host "  Screenshot: $local"; Start-Process $local }
    return $local
}

function TapPct([float]$px, [float]$py, [string]$d="") {
    $x=[int]($global:W*$px); $y=[int]($global:H*$py)
    Write-Host "  TAP ($x,$y) [$([math]::Round($px*100))%,$([math]::Round($py*100))%] $d"
    & $global:ADB -s $global:DEV shell input tap $x $y; Start-Sleep 1
}
function PressBack { & $global:ADB -s $global:DEV shell input keyevent 4; Start-Sleep 1 }
function LimpiarLogcat { & $global:ADB -s $global:DEV logcat -c 2>$null }
function GetErrores([string]$out="debug_screenshots\logcat_actual.txt") {
    & $global:ADB -s $global:DEV logcat -d 2>&1 | Out-File $out
    Get-Content $out | Select-String `
        "Exception|Error|FATAL|crash|NullRef|ObjectDisposed|TaskCancel|stream|upload|MediaPicker" `
        -CaseSensitive:$false | Select-Object -Last 25
}
```

---

## PASO 3 — CAPTURAR EL BUG

```powershell
Write-Host "=== Reproduciendo bug con imagen de prueba ==="

& $global:ADB -s $global:DEV shell monkey -p $global:PKG -c android.intent.category.LAUNCHER 1 2>$null
Start-Sleep 5
LimpiarLogcat
Take-Screenshot "00_inicio"

# Navegar a /subir (botón en el AppBar ~30% ancho, ~4% alto)
TapPct 0.30 0.04 "boton Subir"
Start-Sleep 3
Take-Screenshot "01_pantalla_subir"

# Tocar el MudFileUpload (~50% ancho, ~35% alto)
TapPct 0.50 0.35 "FileUpload"
Start-Sleep 4
Take-Screenshot "02_picker_abierto"

# En el selector: tocar "Recientes" o primera imagen del grid
TapPct 0.50 0.12 "tab Recientes"
Start-Sleep 2
TapPct 0.17 0.28 "primera imagen (foto de prueba)"
Start-Sleep 10
Take-Screenshot "03_post_seleccion"

Write-Host "=== ERRORES DETECTADOS ==="
GetErrores "debug_screenshots\logcat_bug.txt"
```

---

## PASO 4 — DIAGNOSTICAR Y CORREGIR

Según lo que aparezca en el logcat del PASO 3, aplicá el fix correspondiente:

### FIX A — ObjectDisposedException / Stream closed (MÁS COMÚN)
El IBrowserFile expira. Solución: copiar a MemoryStream antes de cualquier await.

En SubirFotos.razor:
```csharp
private async Task OnFilesChanged(IReadOnlyList<IBrowserFile> files)
{
    // LEER EL STREAM INMEDIATAMENTE antes de que Blazor lo dispose
    var buffers = new List<(string nombre, string tipo, MemoryStream ms)>();
    foreach (var file in files)
    {
        var ms = new MemoryStream();
        await using var s = file.OpenReadStream(maxAllowedSize: 52_428_800);
        await s.CopyToAsync(ms);
        ms.Seek(0, SeekOrigin.Begin);
        buffers.Add((file.Name, file.ContentType ?? "image/jpeg", ms));
    }

    // Procesar siempre en el thread de Blazor
    await InvokeAsync(async () =>
    {
        _resultados.Clear(); StateHasChanged();
        foreach (var (nombre, tipo, ms) in buffers)
        {
            try {
                var r = await UploadStrategy.SubirDesdeStreamAsync(ms, nombre, tipo);
                _resultados.Add(new ResultadoItem(nombre, r?.TeniaGps??false,
                    r?.Latitud, r?.Longitud, r is not null));
            } catch (Exception ex) {
                Logger.LogError(ex, "Error en {Archivo}", nombre);
                _resultados.Add(new ResultadoItem(nombre, false, null, null, false));
            }
            StateHasChanged();
        }
    });
}
```

### FIX B — TaskCanceledException / timeout
En MauiProgram.cs: `c.Timeout = TimeSpan.FromSeconds(60);`

### FIX C — Thread equivocado (pantalla en blanco sin excepción)
Toda lógica de UI post-MediaPicker dentro de `await InvokeAsync(async () => { ... });`

### FIX D — En MobileLayout con la cámara
```csharp
private async Task AbrirCamara()
{
    var foto = await MediaPicker.Default.CapturePhotoAsync();
    if (foto is null) return;

    // Leer INMEDIATAMENTE en el thread actual
    using var ms = new MemoryStream();
    await using var stream = await foto.OpenReadAsync();
    await stream.CopyToAsync(ms); ms.Seek(0, SeekOrigin.Begin);

    // TODO el UI dentro de InvokeAsync
    await InvokeAsync(async () =>
    {
        Snackbar.Add("Procesando...", Severity.Info); StateHasChanged();
        var r = await UploadStrategy.SubirDesdeStreamAsync(
            ms, foto.FileName, foto.ContentType ?? "image/jpeg");
        if (r is not null) { Snackbar.Add("OK", Severity.Success); Nav.NavigateTo("/"); }
        else Snackbar.Add("Error al procesar", Severity.Error);
    });
}
```

---

## PASO 5 — RECOMPILAR, REINSTALAR Y VERIFICAR

```powershell
dotnet build GeoFoto.Shared\GeoFoto.Shared.csproj --configuration Debug --verbosity quiet
dotnet build GeoFoto.Mobile\GeoFoto.Mobile.csproj --configuration Debug --framework net10.0-android --verbosity minimal

$apk = Get-ChildItem "GeoFoto.Mobile\bin\Debug\net10.0-android" -Filter "*-Signed.apk" -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
& $global:ADB -s $global:DEV uninstall $global:PKG 2>$null
& $global:ADB -s $global:DEV install -r $apk.FullName
& $global:ADB -s $global:DEV reverse tcp:5000 tcp:5000
LimpiarLogcat

# Re-ejecutar flujo de prueba
$antes = 0
try { $antes=((Invoke-WebRequest "http://localhost:5000/api/puntos" -UseBasicParsing -TimeoutSec 3).Content|ConvertFrom-Json).Count } catch {}

& $global:ADB -s $global:DEV shell monkey -p $global:PKG -c android.intent.category.LAUNCHER 1 2>$null
Start-Sleep 5
TapPct 0.30 0.04 "Subir"; Start-Sleep 3
TapPct 0.50 0.35 "FileUpload"; Start-Sleep 4
TapPct 0.50 0.12 "Recientes"; Start-Sleep 2
TapPct 0.17 0.28 "foto de prueba"; Start-Sleep 12
Take-Screenshot "fix_resultado"

GetErrores "debug_screenshots\logcat_fix.txt"

$despues = 0
try { $despues=((Invoke-WebRequest "http://localhost:5000/api/puntos" -UseBasicParsing -TimeoutSec 3).Content|ConvertFrom-Json).Count } catch {}

Write-Host "Puntos: $antes -> $despues"
$crashes = (Get-Content "debug_screenshots\logcat_fix.txt" | Select-String "FATAL|AndroidRuntime").Count
$proc = (& $global:ADB -s $global:DEV shell ps -A 2>$null | Select-String "geofoto" -CaseSensitive:$false).Count

if ($crashes -eq 0 -and $proc -gt 0) {
    Write-Host "ÉXITO — Sin crashes, proceso corriendo"
    TapPct 0.10 0.04 "Mapa"; Start-Sleep 4; Take-Screenshot "EXITO_mapa_con_marker"
    
    # Commit del fix
    git add GeoFoto.Shared\Pages\SubirFotos.razor
    git add GeoFoto.Shared\Services\IFotoUploadStrategy.cs
    git add GeoFoto.Mobile\Components\MobileLayout.razor
    git add test_assets\ 2>$null
    git commit -m "fix(GEO-US10): corrige pantalla en blanco - MemoryStream + InvokeAsync"
    git push origin (git rev-parse --abbrev-ref HEAD)
} else {
    Write-Host "Todavía hay problemas. Revisar logcat_fix.txt y volver al PASO 4."
}
```


---

## PASO 6 — MEJORAS DE UX/UI (ejecutar DESPUÉS de resolver el bug)

Una vez que el flujo de fotos funciona sin pantalla en blanco, aplicá estas
mejoras de experiencia de usuario. Todas usan componentes MudBlazor existentes.
Recompilá e instalá después de cada bloque. Capturá screenshots para comparar antes/después.

---

### UX-01 — Feedback visual durante la subida de foto

**Problema:** el usuario no sabe si la app está procesando o colgada.
**Fix:** mostrar overlay de carga con progreso mientras se procesa la foto.

En `SubirFotos.razor`, agregar estado de carga por archivo:

```razor
@* Reemplazar la tabla de resultados por una lista con estados visuales *@

@foreach (var item in _resultados)
{
    <MudListItem>
        <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">

            @* Ícono de estado *@
            @if (item.Procesando)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" Color="Color.Primary" />
            }
            else if (item.Ok)
            {
                <MudIcon Icon="@Icons.Material.Filled.CheckCircle" Color="Color.Success" />
            }
            else
            {
                <MudIcon Icon="@Icons.Material.Filled.Warning" Color="Color.Warning" />
            }

            @* Info del archivo *@
            <MudStack Spacing="0">
                <MudText Typo="Typo.body2">@item.NombreArchivo</MudText>
                @if (item.Procesando)
                {
                    <MudText Typo="Typo.caption" Color="Color.Primary">Procesando...</MudText>
                }
                else if (item.Ok && item.Latitud.HasValue)
                {
                    <MudText Typo="Typo.caption" Color="Color.Success">
                        GPS: @item.Latitud.Value.ToString("F5"), @item.Longitud!.Value.ToString("F5")
                    </MudText>
                }
                else if (!item.Ok)
                {
                    <MudText Typo="Typo.caption" Color="Color.Warning">Sin GPS — guardado en (0,0)</MudText>
                }
            </MudStack>

        </MudStack>
    </MudListItem>
}

@code {
    private record ResultadoItem(
        string NombreArchivo,
        bool Ok,
        decimal? Latitud,
        decimal? Longitud,
        bool Procesando = false);   // <-- nuevo campo

    private List<ResultadoItem> _resultados = new();
    private bool _subiendo = false;

    private async Task OnFilesChanged(IReadOnlyList<IBrowserFile> files)
    {
        _subiendo = true;
        _resultados.Clear();

        // Mostrar todos los archivos como "Procesando" primero
        foreach (var file in files)
            _resultados.Add(new ResultadoItem(file.Name, false, null, null, Procesando: true));
        StateHasChanged();

        // Copiar streams antes de perder referencias
        var buffers = new List<(string nombre, string tipo, MemoryStream ms)>();
        foreach (var file in files)
        {
            var ms = new MemoryStream();
            await using var s = file.OpenReadStream(maxAllowedSize: 52_428_800);
            await s.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            buffers.Add((file.Name, file.ContentType ?? "image/jpeg", ms));
        }

        await InvokeAsync(async () =>
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                var (nombre, tipo, ms) = buffers[i];
                UploadResultDto? resultado = null;
                try
                {
                    resultado = await UploadStrategy.SubirDesdeStreamAsync(ms, nombre, tipo);
                }
                catch (Exception ex) { Logger.LogError(ex, "Error en {A}", nombre); }

                // Actualizar el item en la posición correcta
                _resultados[i] = new ResultadoItem(
                    nombre,
                    resultado is not null,
                    resultado?.Latitud,
                    resultado?.Longitud,
                    Procesando: false);
                StateHasChanged();
            }
            _subiendo = false;
        });
    }
}
```

---

### UX-02 — Estado de sincronización siempre visible con color semántico

**Problema:** el badge de sync es difícil de leer y no comunica urgencia.
**Fix:** usar colores semánticos y tooltip descriptivo según el estado.

En `SyncStatusBadge.razor`:

```razor
@* Determinar el estado compuesto *@
@{
    var (icono, color, tooltip) = _estado switch
    {
        "sinred_pendientes" => (Icons.Material.Filled.CloudOff,  Color.Error,
            $"Sin conexión — {_pendientes} operación(es) esperando sync"),
        "conred_pendientes" => (Icons.Material.Filled.CloudSync, Color.Warning,
            $"{_pendientes} operación(es) pendientes de sincronizar"),
        "sincronizando"     => (Icons.Material.Filled.Sync,      Color.Info,
            "Sincronizando con el servidor..."),
        "sinred_ok"         => (Icons.Material.Filled.CloudOff,  Color.Default,
            "Sin conexión — todo sincronizado"),
        _                   => (Icons.Material.Filled.CloudDone, Color.Success,
            "Todo sincronizado correctamente"),
    };
}

<MudTooltip Text="@tooltip" Placement="Placement.Bottom">
    @if (_pendientes > 0 || _estado == "sincronizando")
    {
        <MudBadge Content="@(_estado == "sincronizando" ? "..." : _pendientes.ToString())"
                  Color="@color" Overlap="true" Dot="@(_estado == "sincronizando")">
            <MudIconButton Icon="@icono" Color="@color" Href="/sync"
                           Size="Size.Medium" />
        </MudBadge>
    }
    else
    {
        <MudIconButton Icon="@icono" Color="@color" Href="/sync" Size="Size.Medium" />
    }
</MudTooltip>

@code {
    private string _estado = "ok";

    // Actualizar _estado según conectividad y pendientes
    private void RecalcularEstado()
    {
        _estado = (_conectado, _pendientes, _sincronizando) switch
        {
            (_, _, true)        => "sincronizando",
            (false, > 0, _)     => "sinred_pendientes",
            (false, 0, _)       => "sinred_ok",
            (true, > 0, _)      => "conred_pendientes",
            _                   => "ok"
        };
    }
}
```

---

### UX-03 — Mapa con clustering inteligente y popup mejorado

**Problema:** con muchos puntos el mapa se satura y los markers se superponen.
**Fix:** habilitar Leaflet.markercluster y mejorar el popup visual.

En `App.razor` / `index.html`, agregar CDN de markercluster:
```html
<link rel="stylesheet" href="https://unpkg.com/leaflet.markercluster@1.5.3/dist/MarkerCluster.css" />
<link rel="stylesheet" href="https://unpkg.com/leaflet.markercluster@1.5.3/dist/MarkerCluster.Default.css" />
<script src="https://unpkg.com/leaflet.markercluster@1.5.3/dist/leaflet.markercluster.js"></script>
```

En `leaflet-interop.js`, reemplazar markers simples por cluster:

```javascript
window.leafletInterop = {
    _map: null,
    _cluster: null,
    _dotnetRef: null,
    _ready: false,

    init: function (elementId, dotnetRef) {
        try {
            if (this._map) { this._map.remove(); this._map = null; }
            this._dotnetRef = dotnetRef;
            const el = document.getElementById(elementId);
            if (!el) return;

            this._map = L.map(elementId, { zoomControl: true }).setView([-34.6, -58.4], 5);

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© <a href="https://openstreetmap.org">OpenStreetMap</a>',
                maxZoom: 19
            }).addTo(this._map);

            // Cluster group con opciones visuales
            this._cluster = L.markerClusterGroup({
                maxClusterRadius: 60,
                spiderfyOnMaxZoom: true,
                showCoverageOnHover: false,
                iconCreateFunction: function (cluster) {
                    const count = cluster.getChildCount();
                    const size  = count < 10 ? 'small' : count < 50 ? 'medium' : 'large';
                    return L.divIcon({
                        html: `<div class="cluster-${size}"><span>${count}</span></div>`,
                        className: 'marker-cluster',
                        iconSize: L.point(40, 40)
                    });
                }
            });
            this._map.addLayer(this._cluster);
            this._ready = true;
        } catch (e) { console.error('[GeoFoto] init:', e); }
    },

    addMarkers: function (puntosJson) {
        if (!this._ready || !this._map) return;
        try {
            const puntos = typeof puntosJson === 'string' ? JSON.parse(puntosJson) : puntosJson;
            this._cluster.clearLayers();

            puntos.forEach(p => {
                // Icono personalizado: verde = synced, naranja = pending, rojo = conflict
                const color = p.syncStatus === 'Synced' || !p.syncStatus ? '#4CAF50'
                            : p.syncStatus === 'Conflict' ? '#F44336' : '#FF9800';

                const icon = L.divIcon({
                    html: `<div style="
                        background:${color}; width:14px; height:14px;
                        border-radius:50%; border:2px solid white;
                        box-shadow:0 1px 4px rgba(0,0,0,.4);">
                    </div>`,
                    className: '',
                    iconSize: [18, 18],
                    iconAnchor: [9, 9]
                });

                const marker = L.marker([parseFloat(p.latitud), parseFloat(p.longitud)], { icon })
                    .on('click', () => {
                        if (this._dotnetRef)
                            this._dotnetRef.invokeMethodAsync('OnMarkerClicked', p.id)
                                .catch(e => console.warn('[GeoFoto]', e));
                    });

                this._cluster.addLayer(marker);
            });

            // Centrar mapa en los puntos si hay alguno
            if (puntos.length > 0 && this._cluster.getBounds().isValid())
                this._map.fitBounds(this._cluster.getBounds(), { padding: [40, 40], maxZoom: 14 });

        } catch (e) { console.error('[GeoFoto] addMarkers:', e); }
    },

    centerOn: function (lat, lng) {
        if (this._map) this._map.flyTo([lat, lng], 16, { duration: 0.8 });
    }
};
```

Agregar estilos del cluster en `geofoto.css`:

```css
/* Cluster personalizado */
.marker-cluster { background: transparent !important; }
.cluster-small, .cluster-medium, .cluster-large {
    display: flex; align-items: center; justify-content: center;
    border-radius: 50%; color: white; font-weight: bold;
    border: 2px solid rgba(255,255,255,0.6);
    box-shadow: 0 2px 8px rgba(0,0,0,.3);
}
.cluster-small  { background: #43A047; width: 34px; height: 34px; font-size: 13px; }
.cluster-medium { background: #FB8C00; width: 42px; height: 42px; font-size: 15px; }
.cluster-large  { background: #E53935; width: 50px; height: 50px; font-size: 17px; }

/* Mapa responsive */
#map { height: calc(100vh - 64px); width: 100%; }
@media (max-width: 960px) { #map { height: 55vh; } }
```

---

### UX-04 — Panel de detalle mejorado con animación y estado de sync visible

En `DetallePunto.razor`, agregar el estado de sync del punto en el header:

```razor
<MudCard Elevation="0" Outlined="true">
    <MudCardHeader>
        <CardHeaderContent>
            <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="1">
                <MudText Typo="Typo.h6">
                    @(Punto?.Nombre ?? "Sin nombre")
                </MudText>
                @* Badge de estado de sync *@
                @{
                    var (chip, chipColor) = (Punto?.SyncStatus ?? "Synced") switch {
                        "Synced"        => ("Sincronizado",  Color.Success),
                        "PendingCreate" => ("Por subir",     Color.Warning),
                        "PendingUpdate" => ("Por actualizar",Color.Warning),
                        "PendingDelete" => ("Por eliminar",  Color.Error),
                        "Conflict"      => ("Conflicto",     Color.Error),
                        "Failed"        => ("Error sync",    Color.Error),
                        _               => ("Local",         Color.Default),
                    };
                }
                <MudChip Size="Size.Small" Color="@chipColor" Variant="Variant.Outlined">
                    @chip
                </MudChip>
            </MudStack>
            <MudText Typo="Typo.caption" Color="Color.Secondary">
                @Punto?.Latitud.ToString("F6")°, @Punto?.Longitud.ToString("F6")°
                @if (Punto?.FechaCreacion is not null)
                { <span> — @Punto.FechaCreacion.ToString("dd/MM/yyyy HH:mm")</span> }
            </MudText>
        </CardHeaderContent>
        <CardHeaderActions>
            <MudIconButton Icon="@Icons.Material.Filled.MyLocation"
                           OnClick="CentrarEnMapa"
                           Title="Centrar en el mapa" />
            <MudIconButton Icon="@Icons.Material.Filled.Close"
                           OnClick="() => OnClose.InvokeAsync()" />
        </CardHeaderActions>
    </MudCardHeader>

    <MudCardContent Class="pa-2">
        <FotoCarousel Fotos="@(Punto?.Fotos.ToList() ?? [])" />

        <MudStack Spacing="2" Class="mt-3">
            <MudTextField @bind-Value="_nombre"
                          Label="Nombre del punto"
                          Variant="Variant.Outlined"
                          Clearable="true"
                          Adornment="Adornment.Start"
                          AdornmentIcon="@Icons.Material.Filled.Label"
                          Margin="Margin.Dense" />

            <MudTextField @bind-Value="_descripcion"
                          Label="Descripción"
                          Variant="Variant.Outlined"
                          Lines="3"
                          Placeholder="Agregá notas sobre este punto..."
                          Margin="Margin.Dense" />
        </MudStack>
    </MudCardContent>

    <MudCardActions Class="px-3 pb-3">
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.Save"
                   OnClick="Guardar"
                   Disabled="_guardando"
                   FullWidth="true">
            @(_guardando ? "Guardando..." : "Guardar cambios")
        </MudButton>
    </MudCardActions>
</MudCard>

@code {
    [Parameter] public EventCallback<(decimal lat, decimal lng)> OnCentrar { get; set; }

    private async Task CentrarEnMapa()
    {
        if (Punto is not null)
            await OnCentrar.InvokeAsync((Punto.Latitud, Punto.Longitud));
    }
}
```

En `Mapa.razor` manejar el evento y llamar a Leaflet:
```csharp
private async Task OnCentrarEnMapa((decimal lat, decimal lng) coords)
{
    await JS.InvokeVoidAsync("leafletInterop.centerOn",
        (double)coords.lat, (double)coords.lng);
}
```

---

### UX-05 — Pantalla de subida con drag & drop y preview de imagen

En `SubirFotos.razor`, mejorar la zona de drop con preview visual:

```razor
<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    <MudText Typo="Typo.h5" GutterBottom="true">
        <MudIcon Icon="@Icons.Material.Filled.AddPhotoAlternate" Class="mr-2" />
        Subir fotografías
    </MudText>

    @* Zona de upload con estado visual *@
    <MudPaper Class="@($"pa-6 mb-4 upload-zone {(_subiendo ? "uploading" : "")}")"
              Outlined="true" Elevation="0">
        <MudFileUpload T="IReadOnlyList<IBrowserFile>"
                       FilesChanged="OnFilesChanged"
                       Accept=".jpg,.jpeg,.png,.webp"
                       Multiple="true"
                       Disabled="_subiendo">
            <ButtonTemplate>
                <MudStack AlignItems="AlignItems.Center" Spacing="2">
                    @if (_subiendo)
                    {
                        <MudProgressCircular Indeterminate="true" Color="Color.Primary" Size="Size.Large" />
                        <MudText Typo="Typo.body1" Color="Color.Primary">Procesando fotos...</MudText>
                    }
                    else
                    {
                        <MudIcon Icon="@Icons.Material.Filled.CloudUpload"
                                 Style="font-size:3rem;" Color="Color.Primary" />
                        <MudText Typo="Typo.body1">
                            <strong>Arrastrá tus fotos aquí</strong> o hacé click para elegir
                        </MudText>
                        <MudText Typo="Typo.caption" Color="Color.Secondary">
                            JPG, PNG, WEBP con datos GPS · Múltiples archivos
                        </MudText>
                        <MudButton HtmlTag="label" Variant="Variant.Outlined"
                                   Color="Color.Primary" for="@context.Id" Class="mt-2">
                            Elegir fotos
                        </MudButton>
                    }
                </MudStack>
            </ButtonTemplate>
        </MudFileUpload>
    </MudPaper>

    @* Resultados con preview en miniatura *@
    @if (_resultados.Any())
    {
        <MudText Typo="Typo.subtitle2" Class="mb-2">
            @_resultados.Count(r => r.Ok) de @_resultados.Count foto(s) procesadas
        </MudText>

        <MudGrid Spacing="2">
            @foreach (var item in _resultados)
            {
                <MudItem xs="12" sm="6">
                    <MudCard Elevation="1" Class="@(item.Ok ? "" : "border-warning")">
                        <MudCardContent Class="pa-2">
                            <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
                                @* Ícono estado *@
                                @if (item.Procesando)
                                { <MudProgressCircular Size="Size.Small" Indeterminate="true" /> }
                                else if (item.Ok)
                                { <MudIcon Icon="@Icons.Material.Filled.CheckCircle" Color="Color.Success" Size="Size.Medium" /> }
                                else
                                { <MudIcon Icon="@Icons.Material.Filled.LocationOff" Color="Color.Warning" Size="Size.Medium" /> }

                                <MudStack Spacing="0" Style="flex:1; min-width:0;">
                                    <MudText Typo="Typo.body2" Style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">
                                        @item.NombreArchivo
                                    </MudText>
                                    @if (item.Procesando)
                                    {
                                        <MudText Typo="Typo.caption" Color="Color.Primary">Procesando...</MudText>
                                    }
                                    else if (item.Ok)
                                    {
                                        <MudText Typo="Typo.caption" Color="Color.Success">
                                            @item.Latitud?.ToString("F4")°, @item.Longitud?.ToString("F4")°
                                        </MudText>
                                    }
                                    else
                                    {
                                        <MudText Typo="Typo.caption" Color="Color.Warning">
                                            Sin GPS — guardado en (0, 0)
                                        </MudText>
                                    }
                                </MudStack>
                            </MudStack>
                        </MudCardContent>
                    </MudCard>
                </MudItem>
            }
        </MudGrid>

        @if (!_subiendo)
        {
            <MudStack Row="true" Justify="Justify.FlexEnd" Class="mt-4" Spacing="2">
                <MudButton Variant="Variant.Outlined" OnClick="LimpiarResultados"
                           StartIcon="@Icons.Material.Filled.Clear">
                    Limpiar
                </MudButton>
                <MudButton Variant="Variant.Filled" Color="Color.Primary"
                           OnClick='() => Nav.NavigateTo("/")'
                           StartIcon="@Icons.Material.Filled.Map">
                    Ver en el mapa
                </MudButton>
            </MudStack>
        }
    }
</MudContainer>
```

Agregar en `geofoto.css`:
```css
.upload-zone {
    border: 2px dashed var(--mud-palette-primary);
    border-radius: 12px;
    transition: border-color .2s, background .2s;
    cursor: pointer;
}
.upload-zone:hover { background: var(--mud-palette-primary-lighten); }
.upload-zone.uploading {
    border-style: solid;
    border-color: var(--mud-palette-primary);
    cursor: default;
}
.border-warning { border-left: 3px solid var(--mud-palette-warning); }
```

---

### UX-06 — Toast de reconexión y sync automático visible

**Problema:** el usuario no sabe cuándo el sistema se sincronizó automáticamente.
**Fix:** mostrar un Snackbar no intrusivo cuando el sync automático termina.

En `GeoFoto.Mobile/Components/MobileLayout.razor` (o en App.razor si aplica):

```razor
@inject ISyncService? SyncService
@inject ISnackbar Snackbar
@implements IDisposable

@code {
    protected override void OnInitialized()
    {
        if (SyncService is not null)
            SyncService.SyncCompleted += OnSyncCompleted;
    }

    private void OnSyncCompleted(object? sender, SyncCompletedEventArgs e)
    {
        InvokeAsync(() =>
        {
            if (e.Successful > 0 && e.Failed == 0)
            {
                Snackbar.Add(
                    $"Sincronización completa — {e.Successful} operación(es) enviadas",
                    Severity.Success,
                    cfg => { cfg.VisibleStateDuration = 3000; cfg.ShowCloseIcon = false; });
            }
            else if (e.Failed > 0)
            {
                Snackbar.Add(
                    $"Sync parcial: {e.Successful} OK, {e.Failed} fallidas. Ver /sync",
                    Severity.Warning,
                    cfg => { cfg.VisibleStateDuration = 5000; cfg.Action = "Ver"; cfg.ActionColor = Color.Warning; });
            }
        });
    }

    public void Dispose()
    {
        if (SyncService is not null)
            SyncService.SyncCompleted -= OnSyncCompleted;
    }
}
```

---

### UX-07 — Página de Sync rediseñada con MudTimeline

En `EstadoSync.razor`, reemplazar la tabla plana por un timeline visual:

```razor
@* Historial como timeline *@
<MudText Typo="Typo.h6" Class="mt-4 mb-2">Historial de operaciones</MudText>

@if (!_operaciones.Any())
{
    <MudAlert Severity="Severity.Info" Variant="Variant.Outlined">
        No hay operaciones registradas. Todo está sincronizado.
    </MudAlert>
}
else
{
    <MudTimeline TimelineAlign="TimelineAlign.Start" TimelinePosition="TimelinePosition.Alternate">
        @foreach (var op in _operaciones.OrderByDescending(o => o.CreatedAt).Take(20))
        {
            <MudTimelineItem Color="@ColorDeEstado(op.Status)"
                             Size="Size.Small"
                             Variant="Variant.Filled">
                <ItemContent>
                    <MudStack Spacing="0">
                        <MudText Typo="Typo.body2">
                            <strong>@op.OperationType</strong> @op.EntityType #@op.LocalId
                        </MudText>
                        <MudText Typo="Typo.caption" Color="Color.Secondary">
                            @FormatFecha(op.CreatedAt)
                            · Intentos: @op.Attempts
                        </MudText>
                        @if (op.ErrorMessage is not null)
                        {
                            <MudText Typo="Typo.caption" Color="Color.Error">
                                @op.ErrorMessage
                            </MudText>
                        }
                    </MudStack>
                </ItemContent>
            </MudTimelineItem>
        }
    </MudTimeline>

    @if (_operaciones.Count > 20)
    {
        <MudText Typo="Typo.caption" Color="Color.Secondary" Class="mt-2">
            Mostrando las últimas 20 de @_operaciones.Count operaciones.
        </MudText>
    }
}

@code {
    private Color ColorDeEstado(string status) => status switch {
        "Done"       => Color.Success,
        "Pending"    => Color.Warning,
        "InProgress" => Color.Info,
        "Failed"     => Color.Error,
        _            => Color.Default
    };
}
```

---

### VERIFICACIÓN DE LAS MEJORAS UX

Después de aplicar todos los cambios de UX, recompilá e instalá:

```powershell
dotnet build GeoFoto.Shared\GeoFoto.Shared.csproj --configuration Debug --verbosity quiet
dotnet build GeoFoto.Mobile\GeoFoto.Mobile.csproj `
    --configuration Debug --framework net10.0-android --verbosity minimal

$apk = Get-ChildItem "GeoFoto.Mobilein\Debug
et10.0-android" `
    -Filter "*-Signed.apk" -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
& $global:ADB -s $global:DEV uninstall $global:PKG 2>$null
& $global:ADB -s $global:DEV install -r $apk.FullName
& $global:ADB -s $global:DEV reverse tcp:5000 tcp:5000

& $global:ADB -s $global:DEV shell monkey -p $global:PKG `
    -c android.intent.category.LAUNCHER 1 2>$null
Start-Sleep 5

# Screenshots de comparación de UX
Take-Screenshot "ux_mapa_cluster"
TapPct 0.30 0.04 "Subir"; Start-Sleep 3
Take-Screenshot "ux_pantalla_subir_mejorada"
TapPct 0.90 0.96 "Sync badge"; Start-Sleep 2
Take-Screenshot "ux_sync_badge"
TapPct 0.70 0.04 "Sync page"; Start-Sleep 2
Take-Screenshot "ux_sync_timeline"

Write-Host "Screenshots de UX guardados en debug_screenshots"
Write-Host "Verificá visualmente cada mejora en las imágenes."

# Commit de las mejoras UX
git add GeoFoto.Shared\Pages\SubirFotos.razor
git add GeoFoto.Shared\Pages\EstadoSync.razor
git add GeoFoto.Shared\Components\DetallePunto.razor
git add GeoFoto.Shared\Components\SyncStatusBadge.razor
git add GeoFoto.Shared\Components\MobileLayout.razor 2>$null
git add GeoFoto.Shared\wwwroot\js\leaflet-interop.js
git add GeoFoto.Shared\wwwroot\css\geofoto.css
git add GeoFoto.Web\Components\App.razor 2>$null
git add GeoFoto.Mobile\wwwroot\index.html 2>$null
git commit -m "feat(UX): mejoras de experiencia de usuario - clustering, feedback visual, timeline sync"
git push origin (git rev-parse --abbrev-ref HEAD)
Write-Host "Mejoras UX commiteadas y pusheadas"
```

---

## CRITERIO DE ÉXITO FINAL (BUG + UX)

  [ ] Bug corregido: foto se procesa sin pantalla en blanco
  [ ] Screenshot ux_mapa_cluster muestra markers con clustering activo
  [ ] Screenshot ux_pantalla_subir_mejorada muestra zona drag&drop con iconos de estado
  [ ] Screenshot ux_sync_badge muestra badge con color semántico correcto
  [ ] Screenshot ux_sync_timeline muestra timeline en lugar de tabla plana
  [ ] Snackbar de sync automático visible al reconectar
  [ ] Panel de detalle muestra chip de estado de sync del punto
  [ ] Commits realizados: fix del bug + mejoras UX por separado


---

## CRITERIO DE ÉXITO COMPLETO

  [ ] Bug corregido: foto se procesa sin pantalla en blanco
  [ ] Screenshot EXITO_mapa_con_marker muestra marker en el mapa
  [ ] logcat_fix.txt sin FATAL ni AndroidRuntime
  [ ] Proceso GeoFoto corriendo después del test
  [ ] Commit y push del fix realizados
  [ ] Mejoras UX aplicadas (UX-01 a UX-07)
  [ ] Commit y push de mejoras UX realizados

Comenzá con el PASO 0 ahora.
```
