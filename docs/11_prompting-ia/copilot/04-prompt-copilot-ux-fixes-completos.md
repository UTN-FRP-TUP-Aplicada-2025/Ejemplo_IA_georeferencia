# Prompt para GitHub Copilot — UX Fixes Completos + StreamRendering + Pruebas Automatizadas

> Este prompt cubre 8 problemas concretos. Copilot debe resolverlos en orden,
> verificar cada uno con screenshots y taps reales, y no avanzar al siguiente
> hasta confirmar que el anterior funciona.
> La toma de foto con cámara NO se puede automatizar con adb — se prueba manualmente.
> Todo lo demás se verifica de forma automática.

---

## PROMPT

```
Eres un desarrollador .NET MAUI Blazor senior y UX engineer.
Debés corregir y verificar 8 problemas en la app GeoFoto.

REGLA FUNDAMENTAL: Cada problema tiene un fix de código y una verificación
con adb (screenshot + taps). No reportes un problema como resuelto sin
haber capturado el screenshot que lo confirme.

EXCEPCIÓN CONOCIDA: La toma de foto con la cámara nativa NO se puede
automatizar con adb. Esos pasos se marcan con [MANUAL] y se documentan
para que el desarrollador los verifique físicamente.

---

## PASO 0 — DETECTAR ENTORNO

```powershell
# Buscar adb en todas las rutas posibles
$adbRutas = @(
    "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
    "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    "C:\Android\platform-tools\adb.exe",
    (Get-Command adb -ErrorAction SilentlyContinue)?.Source
)
$global:ADB = $adbRutas |
    Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $global:ADB) {
    $global:ADB = Get-ChildItem "$env:LOCALAPPDATA","C:\Program Files (x86)" `
        -Recurse -Filter "adb.exe" -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
}

# Detectar el único dispositivo conectado automáticamente
& $global:ADB kill-server; Start-Sleep 2
& $global:ADB start-server; Start-Sleep 3

$global:DEV = (& $global:ADB devices |
    Select-String "\sdevice$" | Select-Object -First 1) `
    -replace "\s.*","" | ForEach-Object { $_.Trim() }

$dims = [regex]::Match((& $global:ADB -s $global:DEV shell wm size),"(\d+)x(\d+)")
$global:W = [int]$dims.Groups[1].Value
$global:H = [int]$dims.Groups[2].Value

$global:PKG = (Select-String "ApplicationId" "GeoFoto.Mobile\GeoFoto.Mobile.csproj" |
    Select-Object -First 1) -replace '.*"([\w.]+)".*','$1'

# Obtener insets de Android (status bar y navigation bar)
$density = [double]((& $global:ADB -s $global:DEV shell wm density) -replace '.*:\s*','')
$global:DENSITY = $density
Write-Host "Dispositivo : $global:DEV"
Write-Host "Pantalla    : $($global:W)x$($global:H)"
Write-Host "Densidad    : $density dpi"
Write-Host "Package     : $global:PKG"

& $global:ADB -s $global:DEV reverse tcp:5000 tcp:5000
Write-Host "Tunel       : activo"

# Funciones globales
function Take-Screenshot([string]$n) {
    New-Item -Path "ux_shots" -ItemType Directory -Force | Out-Null
    $ts = Get-Date -Format "HHmmss"
    $r  = "/sdcard/gf_${n}_$ts.png"
    $l  = "ux_shots\${n}_$ts.png"
    & $global:ADB -s $global:DEV shell screencap -p $r 2>$null
    & $global:ADB -s $global:DEV pull $r $l 2>$null
    & $global:ADB -s $global:DEV shell rm $r 2>$null
    if (Test-Path $l) { Write-Host "  [SS] $l"; Start-Process $l }
    return $l
}
function TapPct([float]$px,[float]$py,[string]$d="") {
    $x=[int]($global:W*$px); $y=[int]($global:H*$py)
    Write-Host "  [TAP] ($x,$y) — $d"
    & $global:ADB -s $global:DEV shell input tap $x $y; Start-Sleep 1
}
function Swipe([float]$x1,[float]$y1,[float]$x2,[float]$y2,[int]$ms=400,[string]$d="") {
    $ax=[int]($global:W*$x1);$ay=[int]($global:H*$y1)
    $bx=[int]($global:W*$x2);$by=[int]($global:H*$y2)
    Write-Host "  [SWIPE] ($ax,$ay)→($bx,$by) — $d"
    & $global:ADB -s $global:DEV shell input swipe $ax $ay $bx $by $ms; Start-Sleep 1
}
function LimpiarLogcat { & $global:ADB -s $global:DEV logcat -c 2>$null }
function GetErrores([string]$f="ux_shots\logcat.txt") {
    & $global:ADB -s $global:DEV logcat -d 2>&1 | Out-File $f
    Get-Content $f | Select-String "Exception|Error|FATAL|crash|AndroidRuntime|NullRef" `
        -CaseSensitive:$false | Select-Object -Last 20
}
function LaunchApp {
    & $global:ADB -s $global:DEV shell monkey -p $global:PKG `
        -c android.intent.category.LAUNCHER 1 2>$null
    Start-Sleep 5
}
```

---

## FIX 1 — BARRAS DE ANDROID TAPAN LA UI (STATUS BAR + NAVIGATION BAR)

### Problema
La MudAppBar queda debajo de la barra de estado de Android (arriba).
El contenido inferior queda debajo de la barra de navegación (abajo).

### Causa
MAUI Hybrid usa un `BlazorWebView` que por defecto no respeta los
window insets de Android. Hay que configurar edge-to-edge rendering
y compensar con padding en el CSS.

### Fix A — Configurar edge-to-edge en Android (GeoFoto.Mobile)

En `GeoFoto.Mobile/Platforms/Android/MainActivity.cs`:

```csharp
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui;

namespace GeoFoto.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges =
        ConfigChanges.ScreenSize | ConfigChanges.Orientation |
        ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Edge-to-edge: el contenido se dibuja detrás de las barras del sistema
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            Window!.SetDecorFitsSystemWindows(false);
        }
        else
        {
#pragma warning disable CA1422
            Window!.DecorView.SystemUiVisibility =
                (StatusBarVisibility)(
                    SystemUiFlags.LayoutStable |
                    SystemUiFlags.LayoutHideNavigation |
                    SystemUiFlags.LayoutFullscreen);
#pragma warning restore CA1422
        }

        // Barras del sistema semi-transparentes
        Window!.StatusBarColor = Android.Graphics.Color.Transparent;
        Window!.NavigationBarColor = Android.Graphics.Color.Transparent;
    }
}
```

### Fix B — Leer los insets reales del dispositivo desde JS

En `GeoFoto.Shared/wwwroot/js/geofoto-utils.js` (crear si no existe):

```javascript
window.geoFotoUtils = {

    // Obtener los insets reales del sistema (status bar y nav bar)
    getWindowInsets: function () {
        // En MAUI Hybrid, env() CSS funciona con safe-area-inset
        const style = getComputedStyle(document.documentElement);
        return {
            top:    parseInt(style.getPropertyValue('--sat') || '0'),
            bottom: parseInt(style.getPropertyValue('--sab') || '0'),
            left:   parseInt(style.getPropertyValue('--sal') || '0'),
            right:  parseInt(style.getPropertyValue('--sar') || '0')
        };
    },

    // Aplicar insets al body como CSS variables
    applyInsets: function () {
        const meta = document.querySelector('meta[name="viewport"]');
        if (meta) {
            meta.content = 'width=device-width, initial-scale=1.0, viewport-fit=cover';
        }
        // Leer safe area insets
        document.documentElement.style.setProperty(
            '--status-bar-height',
            'env(safe-area-inset-top, 24px)');
        document.documentElement.style.setProperty(
            '--nav-bar-height',
            'env(safe-area-inset-bottom, 16px)');
    }
};

// Aplicar automáticamente al cargar
window.addEventListener('load', () => window.geoFotoUtils.applyInsets());
```

### Fix C — CSS con safe-area-inset para compensar barras

En `GeoFoto.Shared/wwwroot/css/geofoto.css`, reemplazar o agregar:

```css
/* ===================================================
   SAFE AREA — Compensar barras del sistema Android/iOS
   =================================================== */

:root {
    --status-bar-height  : env(safe-area-inset-top,    24px);
    --nav-bar-height     : env(safe-area-inset-bottom, 16px);
    --nav-left           : env(safe-area-inset-left,    0px);
    --nav-right          : env(safe-area-inset-right,   0px);
    --appbar-height      : 56px;
    --total-top-offset   : calc(var(--appbar-height) + var(--status-bar-height));
}

/* AppBar: empujar hacia abajo el alto de la status bar */
.mud-appbar {
    padding-top: var(--status-bar-height) !important;
    height: calc(var(--appbar-height) + var(--status-bar-height)) !important;
}

/* Contenido principal: compensar AppBar + status bar + nav bar */
.mud-main-content {
    padding-top:    var(--total-top-offset)   !important;
    padding-bottom: var(--nav-bar-height)     !important;
    padding-left:   var(--nav-left)           !important;
    padding-right:  var(--nav-right)          !important;
}

/* FAB: no quedar tapado por la nav bar */
.mud-fab-fixed-bottom {
    bottom: calc(24px + var(--nav-bar-height)) !important;
}

/* Mapa: altura total menos las dos barras */
#map {
    height: calc(100dvh - var(--total-top-offset) - var(--nav-bar-height));
    width: 100%;
}

@media (max-width: 960px) {
    #map { height: calc(55dvh - var(--nav-bar-height)); }
}
```

### Fix D — Viewport en index.html del Mobile

En `GeoFoto.Mobile/wwwroot/index.html`, actualizar el meta viewport:

```html
<meta name="viewport"
      content="width=device-width, initial-scale=1.0, maximum-scale=1.0,
               user-scalable=no, viewport-fit=cover" />
```

Y agregar el script de insets antes del cierre de `</head>`:
```html
<script src="_content/GeoFoto.Shared/js/geofoto-utils.js"></script>
```

### Verificación Fix 1

```powershell
# Compilar, instalar y verificar
dotnet build GeoFoto.Mobile\GeoFoto.Mobile.csproj `
    --configuration Debug --framework net10.0-android --verbosity quiet
$apk = Get-ChildItem "GeoFoto.Mobile\bin\Debug\net10.0-android" `
    -Filter "*-Signed.apk" -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
& $global:ADB -s $global:DEV uninstall $global:PKG 2>$null
& $global:ADB -s $global:DEV install -r $apk.FullName
& $global:ADB -s $global:DEV reverse tcp:5000 tcp:5000
LaunchApp
Take-Screenshot "fix1_barras_corregidas"
Write-Host "Verificar: AppBar visible completa SIN quedar debajo de la status bar"
Write-Host "Verificar: contenido inferior NO tapado por navigation bar"
```

---

## FIX 2 — STREAMRENDERING EN LAS PÁGINAS BLAZOR

### Problema
Las páginas cargan en blanco hasta que `OnInitializedAsync` termina,
sin mostrar al usuario que están cargando datos.

### Fix — Agregar `@attribute [StreamRendering]` a cada página del Shared

**Importante:** `StreamRendering` funciona con `InteractiveServer`. Permite
que la página emita HTML inicial inmediatamente (skeleton/loading) y luego
actualice cuando lleguen los datos asíncronos.

En `GeoFoto.Shared/Pages/Mapa.razor`:
```razor
@page "/"
@attribute [StreamRendering]
@implements IDisposable
@inject IGeoFotoApiClient Api
@inject IJSRuntime JS
@inject ILogger<Mapa> Logger

@* Skeleton mientras carga *@
@if (_cargando)
{
    <MudContainer MaxWidth="MaxWidth.False" Class="pa-0">
        <MudSkeleton SkeletonType="SkeletonType.Rectangle"
                     Height="calc(100dvh - var(--total-top-offset) - var(--nav-bar-height))"
                     Width="100%" Animation="Animation.Wave" />
    </MudContainer>
}
else
{
    @* contenido normal del mapa *@
}

@code {
    private bool _cargando = true;

    protected override async Task OnInitializedAsync()
    {
        try { _puntos = (await Api.GetPuntosAsync()).ToList(); }
        catch (Exception ex) { Logger.LogError(ex, "Error cargando puntos"); }
        finally { _cargando = false; }
    }
}
```

En `GeoFoto.Shared/Pages/SubirFotos.razor`:
```razor
@page "/subir"
@attribute [StreamRendering]
```

En `GeoFoto.Shared/Pages/ListaPuntos.razor`:
```razor
@page "/lista"
@attribute [StreamRendering]

@if (_cargando)
{
    <MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
        <MudSkeleton Width="30%" Height="40px" Class="mb-4" />
        @for (int i = 0; i < 5; i++)
        {
            <MudSkeleton SkeletonType="SkeletonType.Rectangle"
                         Height="56px" Class="mb-2" Animation="Animation.Wave" />
        }
    </MudContainer>
}
```

En `GeoFoto.Shared/Pages/EstadoSync.razor`:
```razor
@page "/sync"
@attribute [StreamRendering]
```

En `GeoFoto.Web/Program.cs`, confirmar que está habilitado:
```csharp
// StreamRendering requiere InteractiveServer — ya configurado
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

### Verificación Fix 2

```powershell
LaunchApp
LimpiarLogcat
# Navegar rápido a /lista para capturar el skeleton
TapPct 0.50 0.04 "Lista"
Start-Sleep 1    # capturar durante la carga
Take-Screenshot "fix2_skeleton_loading"
Start-Sleep 4    # esperar que cargue
Take-Screenshot "fix2_datos_cargados"
Write-Host "Verificar: primer screenshot muestra skeleton/spinner"
Write-Host "Verificar: segundo screenshot muestra datos reales"
```

---

## FIX 3 — BOTÓN GPS: CENTRAR MAPA EN UBICACIÓN DEL DISPOSITIVO

### Fix A — Permiso de ubicación en AndroidManifest.xml

```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
```

### Fix B — Geolocalización desde Leaflet vía JS Interop

En `GeoFoto.Shared/wwwroot/js/leaflet-interop.js`, agregar:

```javascript
// Dentro del objeto window.leafletInterop:

centrarEnUbicacion: function (dotnetRef) {
    if (!this._ready || !this._map) return;
    if (!navigator.geolocation) {
        if (dotnetRef) dotnetRef.invokeMethodAsync('OnGpsError', 'GPS no soportado');
        return;
    }
    navigator.geolocation.getCurrentPosition(
        pos => {
            const lat = pos.coords.latitude;
            const lng = pos.coords.longitude;
            const acc = pos.coords.accuracy;

            // Volar suavemente a la posición
            this._map.flyTo([lat, lng], 16, { duration: 1.2 });

            // Mostrar círculo de precisión
            if (this._gpsCircle) this._gpsCircle.remove();
            if (this._gpsMarker) this._gpsMarker.remove();

            this._gpsCircle = L.circle([lat, lng], {
                radius: acc,
                color: '#2196F3', fillColor: '#2196F3',
                fillOpacity: 0.12, weight: 1.5
            }).addTo(this._map);

            // Marcador de posición actual
            const gpsIcon = L.divIcon({
                html: '<div style="width:14px;height:14px;background:#2196F3;' +
                      'border:2px solid white;border-radius:50%;' +
                      'box-shadow:0 0 0 4px rgba(33,150,243,.3)"></div>',
                className: '', iconSize: [18,18], iconAnchor: [9,9]
            });
            this._gpsMarker = L.marker([lat, lng], { icon: gpsIcon })
                .bindPopup(`<b>Tu ubicación</b><br>Precisión: ${Math.round(acc)}m`)
                .addTo(this._map);

            if (dotnetRef)
                dotnetRef.invokeMethodAsync('OnGpsOk', lat, lng, acc);
        },
        err => {
            const msgs = {
                1: 'Permiso de ubicación denegado',
                2: 'Posición no disponible',
                3: 'Tiempo de espera agotado'
            };
            if (dotnetRef)
                dotnetRef.invokeMethodAsync('OnGpsError', msgs[err.code] || err.message);
        },
        { enableHighAccuracy: true, timeout: 10000, maximumAge: 30000 }
    );
},

centrarEnCoordenadas: function (lat, lng, zoom) {
    if (!this._ready || !this._map) return;
    this._map.flyTo([lat, lng], zoom || 16, { duration: 0.8 });
}
```

### Fix C — Botón GPS en Mapa.razor

En `Mapa.razor`, agregar el botón flotante sobre el mapa:

```razor
<div style="position:relative;">
    <div id="map"></div>

    @* Botón GPS — posicionado sobre el mapa, respetando la nav bar *@
    <div style="position:absolute; bottom:calc(16px + var(--nav-bar-height));
                right:16px; z-index:1000;">
        <MudTooltip Text="Centrar en mi ubicación" Placement="Placement.Left">
            <MudFab Color="Color.Primary"
                    StartIcon="@(_buscandoGps
                        ? Icons.Material.Filled.GpsNotFixed
                        : Icons.Material.Filled.GpsFixed)"
                    Size="Size.Medium"
                    OnClick="CentrarEnUbicacion"
                    Disabled="_buscandoGps"
                    Style="box-shadow: 0 2px 8px rgba(0,0,0,.3);" />
        </MudTooltip>
    </div>
</div>

@code {
    private bool _buscandoGps = false;
    private string? _gpsError;

    private async Task CentrarEnUbicacion()
    {
        _buscandoGps = true;
        _gpsError = null;
        StateHasChanged();
        try
        {
            await JS.InvokeVoidAsync("leafletInterop.centrarEnUbicacion", _ref);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error en GPS");
            Snackbar.Add("No se pudo obtener la ubicación", Severity.Warning);
            _buscandoGps = false;
        }
    }

    [JSInvokable]
    public Task OnGpsOk(double lat, double lng, double precision)
    {
        _buscandoGps = false;
        Snackbar.Add($"Ubicación obtenida — precisión: {Math.Round(precision)}m", Severity.Success);
        StateHasChanged();
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnGpsError(string mensaje)
    {
        _buscandoGps = false;
        _gpsError = mensaje;
        Snackbar.Add($"GPS: {mensaje}", Severity.Warning);
        StateHasChanged();
        return Task.CompletedTask;
    }
}
```

### Verificación Fix 3

```powershell
# Otorgar permiso de ubicación vía adb
& $global:ADB -s $global:DEV shell pm grant $global:PKG android.permission.ACCESS_FINE_LOCATION
& $global:ADB -s $global:DEV shell pm grant $global:PKG android.permission.ACCESS_COARSE_LOCATION
Write-Host "Permisos de GPS otorgados"

LaunchApp
Take-Screenshot "fix3_mapa_con_boton_gps"
# El botón GPS debe ser visible en la esquina inferior derecha del mapa
# Tocar el botón GPS (esquina inferior derecha del mapa, encima de la nav bar)
TapPct 0.90 0.82 "boton GPS"
Start-Sleep 6
Take-Screenshot "fix3_mapa_centrado_gps"
Write-Host "Verificar: mapa se centró en la ubicación del dispositivo"
Write-Host "Verificar: marker azul con círculo de precisión visible"
```

---

## FIX 4 — AL SACAR FOTO, MOSTRAR EL MARKER Y AUTOCENTRAR EL MAPA

### Problema
Después de procesar una foto, el mapa no refleja el nuevo punto
y no se centra en la ubicación donde se tomó.

### Fix A — Evento de navegación post-subida con datos del punto creado

En `IFotoUploadStrategy`, el resultado ya incluye `Latitud` y `Longitud`.
El problema es que `Mapa.razor` no recarga los puntos cuando se navega a `/`.

En `Mapa.razor`, detectar que venimos de una subida usando query string:

```razor
@page "/"
@inject NavigationManager Nav

@code {
    [SupplyParameterFromQuery(Name = "puntoId")]
    public int? NuevoPuntoId { get; set; }

    [SupplyParameterFromQuery(Name = "lat")]
    public string? NuevaLat { get; set; }

    [SupplyParameterFromQuery(Name = "lng")]
    public string? NuevaLng { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _puntos = (await Api.GetPuntosAsync()).ToList();
        _cargando = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _ref = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("leafletInterop.init", "map", _ref);
            _mapaListo = true;
        }

        if (_mapaListo && _puntos.Any())
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                _puntos,
                new System.Text.Json.JsonSerializerOptions
                { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            await JS.InvokeVoidAsync("leafletInterop.addMarkers", json);

            // Si venimos de una subida: centrar en el punto recién creado
            if (!string.IsNullOrEmpty(NuevaLat) && !string.IsNullOrEmpty(NuevaLng))
            {
                if (double.TryParse(NuevaLat, out var lat) &&
                    double.TryParse(NuevaLng, out var lng))
                {
                    await JS.InvokeVoidAsync("leafletInterop.centrarEnCoordenadas",
                        lat, lng, 16);

                    // Abrir automáticamente el detalle del punto creado
                    if (NuevoPuntoId.HasValue)
                    {
                        _puntoSeleccionado = await Api.GetPuntoAsync(NuevoPuntoId.Value);
                        StateHasChanged();
                    }
                }
            }
        }
    }
}
```

### Fix B — Navegar al mapa con query string después de subir foto

En `SubirFotos.razor`, modificar la navegación post-subida:

```csharp
// Al terminar de procesar todos los archivos:
var primeroConGps = _resultados.FirstOrDefault(r => r.Ok && r.Latitud.HasValue);
if (primeroConGps is not null)
{
    // Navegar con coordenadas y puntoId para que el mapa se centre y abra el detalle
    var lat = primeroConGps.Latitud!.Value.ToString("F7",
        System.Globalization.CultureInfo.InvariantCulture);
    var lng = primeroConGps.Longitud!.Value.ToString("F7",
        System.Globalization.CultureInfo.InvariantCulture);
    var id  = primeroConGps.PuntoId;
    Nav.NavigateTo($"/?lat={lat}&lng={lng}&puntoId={id}");
}
else
{
    Nav.NavigateTo("/");
}
```

En `MobileLayout.razor`, hacer lo mismo al terminar de procesar la foto de cámara:
```csharp
// Después de SubirDesdeStreamAsync:
if (resultado is not null && resultado.Latitud.HasValue)
{
    var lat = resultado.Latitud!.Value.ToString("F7", System.Globalization.CultureInfo.InvariantCulture);
    var lng = resultado.Longitud!.Value.ToString("F7", System.Globalization.CultureInfo.InvariantCulture);
    await InvokeAsync(() =>
        Nav.NavigateTo($"/?lat={lat}&lng={lng}&puntoId={resultado.PuntoId}"));
}
else
{
    await InvokeAsync(() => Nav.NavigateTo("/"));
}
```

### Verificación Fix 4

```powershell
# Precargar imagen de prueba con GPS
New-Item -Path "test_assets" -ItemType Directory -Force | Out-Null
if (-not (Test-Path "test_assets\foto_gps.jpg")) {
    try {
        Invoke-WebRequest `
            "https://github.com/ianare/exif-samples/raw/master/jpg/gps/DSCN0010.jpg" `
            -OutFile "test_assets\foto_gps.jpg" -TimeoutSec 15
    } catch {
        Write-Host "Descarga fallida — usar imagen propia con GPS en test_assets\foto_gps.jpg"
    }
}

& $global:ADB -s $global:DEV shell mkdir -p "/sdcard/DCIM/GeoFotoTest"
& $global:ADB -s $global:DEV push "test_assets\foto_gps.jpg" "/sdcard/DCIM/GeoFotoTest/gps.jpg"
& $global:ADB -s $global:DEV shell am broadcast `
    -a android.intent.action.MEDIA_SCANNER_SCAN_FILE `
    -d "file:///sdcard/DCIM/GeoFotoTest/gps.jpg" 2>$null
Start-Sleep 2

LaunchApp
TapPct 0.30 0.04 "Subir"
Start-Sleep 3
TapPct 0.50 0.35 "FileUpload"
Start-Sleep 4
TapPct 0.50 0.12 "tab Recientes"
Start-Sleep 2
TapPct 0.17 0.28 "foto de prueba"
Start-Sleep 12

Take-Screenshot "fix4_mapa_nuevo_punto_centrado"
Write-Host "Verificar: mapa centrado en el punto recién creado"
Write-Host "Verificar: panel lateral muestra el detalle del nuevo punto"
Write-Host "[MANUAL] Verificar lo mismo tomando foto real con la cámara"
```

---

## FIX 5 — GALERÍA DE FOTOS AL HACER CLICK EN MARKER

### Problema
El popup/panel de detalle muestra solo la primera foto, sin forma
de navegar entre todas las fotos catalogadas en ese marker.

### Fix — FotoCarousel mejorado con galería expandible

En `GeoFoto.Shared/Components/FotoCarousel.razor`, reescribir completamente:

```razor
@inject IGeoFotoApiClient Api
@inject IDialogService Dialog

<div class="foto-carousel">
    @if (!Fotos.Any())
    {
        <MudPaper Class="pa-4 text-center" Outlined="true">
            <MudIcon Icon="@Icons.Material.Filled.PhotoLibrary"
                     Color="Color.Secondary" Style="font-size:2rem;" />
            <MudText Typo="Typo.caption" Color="Color.Secondary" Class="mt-1">
                Sin fotos
            </MudText>
        </MudPaper>
    }
    else
    {
        @* Imagen principal *@
        <div style="position:relative; cursor:pointer;"
             @onclick="AbrirGaleria">
            <MudImage Src="@Api.GetImagenUrl(Fotos[_indice].Id)"
                      Alt="Foto @(_indice+1) de @Fotos.Count"
                      ObjectFit="ObjectFit.Cover"
                      Class="foto-principal"
                      Elevation="0" />

            @* Overlay con número de fotos *@
            <div style="position:absolute; bottom:8px; right:8px;
                        background:rgba(0,0,0,.55); color:white;
                        padding:3px 8px; border-radius:12px; font-size:12px;">
                📷 @Fotos.Count foto(s)
            </div>

            @* Indicador de foto actual *@
            @if (Fotos.Count > 1)
            {
                <div style="position:absolute; bottom:8px; left:50%;
                            transform:translateX(-50%);
                            display:flex; gap:4px;">
                    @for (int i = 0; i < Fotos.Count; i++)
                    {
                        var idx = i;
                        <div style="width:@(idx==_indice ? 16 : 6)px; height:6px;
                                    border-radius:3px; transition:width .2s;
                                    background:@(idx==_indice ? "white" : "rgba(255,255,255,.5)");
                                    cursor:pointer;"
                             @onclick:stopPropagation="true"
                             @onclick="() => _indice = idx">
                        </div>
                    }
                </div>
            }
        </div>

        @* Miniaturas si hay más de 1 foto *@
        @if (Fotos.Count > 1)
        {
            <div class="foto-thumbnails">
                @foreach (var (foto, i) in Fotos.Select((f,i) => (f,i)))
                {
                    var idx = i;
                    <div class="@($"thumb {(idx==_indice ? "thumb-active" : "")}")"
                         @onclick="() => _indice = idx">
                        <MudImage Src="@Api.GetImagenUrl(foto.Id)"
                                  Alt="Foto @(idx+1)"
                                  ObjectFit="ObjectFit.Cover"
                                  Style="width:100%;height:100%;" />
                    </div>
                }
            </div>
        }

        @* Fecha y metadatos de la foto actual *@
        @if (Fotos[_indice].FechaTomada.HasValue)
        {
            <MudText Typo="Typo.caption" Color="Color.Secondary" Class="mt-1 px-1">
                @Fotos[_indice].FechaTomada!.Value.ToString("dd/MM/yyyy HH:mm")
                @if (Fotos[_indice].LatitudExif.HasValue)
                {
                    <span> · GPS: @Fotos[_indice].LatitudExif!.Value.ToString("F4")°,
                           @Fotos[_indice].LongitudExif!.Value.ToString("F4")°</span>
                }
            </MudText>
        }
    }
</div>

@code {
    [Parameter] public List<FotoDto> Fotos { get; set; } = new();

    private int _indice = 0;

    private async Task AbrirGaleria()
    {
        var params_ = new DialogParameters
        {
            ["Fotos"]        = Fotos,
            ["IndiceInicial"] = _indice
        };
        var opts = new DialogOptions
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseOnEscapeKey = true,
            CloseButton = true
        };
        var dialog = await Dialog.ShowAsync<GaleriaFotosDialog>(
            "Fotos del punto", params_, opts);
        var result = await dialog.Result;
        if (result.Data is int nuevoIndice) _indice = nuevoIndice;
    }
}
```

### Fix — Crear GaleriaFotosDialog.razor

En `GeoFoto.Shared/Components/GaleriaFotosDialog.razor`:

```razor
@inject IGeoFotoApiClient Api

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">
            Foto @(_indice+1) de @Fotos.Count
        </MudText>
    </TitleContent>
    <DialogContent>
        <div style="position:relative; min-height:300px;">
            <MudImage Src="@Api.GetImagenUrl(Fotos[_indice].Id)"
                      Alt="Foto @(_indice+1)"
                      ObjectFit="ObjectFit.Contain"
                      Style="width:100%; max-height:70vh;"
                      Elevation="0" />

            @* Flechas de navegación *@
            @if (_indice > 0)
            {
                <MudIconButton Icon="@Icons.Material.Filled.ChevronLeft"
                               Style="position:absolute;left:8px;top:50%;transform:translateY(-50%);
                                      background:rgba(0,0,0,.45);color:white;border-radius:50%;"
                               OnClick="() => _indice--" />
            }
            @if (_indice < Fotos.Count - 1)
            {
                <MudIconButton Icon="@Icons.Material.Filled.ChevronRight"
                               Style="position:absolute;right:8px;top:50%;transform:translateY(-50%);
                                      background:rgba(0,0,0,.45);color:white;border-radius:50%;"
                               OnClick="() => _indice++" />
            }
        </div>

        @* Metadata de la foto *@
        @{var foto = Fotos[_indice];}
        <MudStack Row="true" Wrap="Wrap.Wrap" Class="mt-2 px-2" Spacing="2">
            @if (foto.FechaTomada.HasValue)
            {
                <MudChip Size="Size.Small" Icon="@Icons.Material.Filled.Schedule">
                    @foto.FechaTomada.Value.ToString("dd/MM/yyyy HH:mm")
                </MudChip>
            }
            @if (foto.LatitudExif.HasValue)
            {
                <MudChip Size="Size.Small" Icon="@Icons.Material.Filled.LocationOn"
                         Color="Color.Primary">
                    @foto.LatitudExif.Value.ToString("F5")°,
                    @foto.LongitudExif!.Value.ToString("F5")°
                </MudChip>
            }
            <MudChip Size="Size.Small" Icon="@Icons.Material.Filled.Photo">
                @foto.NombreArchivo
            </MudChip>
        </MudStack>

        @* Tira de miniaturas *@
        @if (Fotos.Count > 1)
        {
            <div style="display:flex;gap:6px;overflow-x:auto;padding:12px 8px 4px;
                        scrollbar-width:thin;">
                @foreach (var (f, i) in Fotos.Select((f,i) => (f,i)))
                {
                    var idx = i;
                    <div style="flex-shrink:0;width:64px;height:64px;border-radius:6px;
                                overflow:hidden;cursor:pointer;
                                border:2px solid @(idx==_indice ? "var(--mud-palette-primary)" : "transparent");
                                transition:border-color .15s;"
                         @onclick="() => _indice = idx">
                        <MudImage Src="@Api.GetImagenUrl(f.Id)"
                                  ObjectFit="ObjectFit.Cover"
                                  Style="width:100%;height:100%;" />
                    </div>
                }
            </div>
        }
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cerrar" Variant="Variant.Text">Cerrar</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] MudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public List<FotoDto> Fotos { get; set; } = new();
    [Parameter] public int IndiceInicial { get; set; } = 0;

    private int _indice;

    protected override void OnInitialized() => _indice = IndiceInicial;

    private void Cerrar() => MudDialog.Close(DialogResult.Ok(_indice));
}
```

Agregar estilos en `geofoto.css`:

```css
/* Carousel */
.foto-carousel { display: flex; flex-direction: column; gap: 6px; }
.foto-principal {
    width: 100%; height: 200px; object-fit: cover;
    border-radius: 8px; display: block;
}
.foto-thumbnails {
    display: flex; gap: 6px; overflow-x: auto;
    padding-bottom: 4px; scrollbar-width: thin;
}
.thumb {
    flex-shrink: 0; width: 52px; height: 52px;
    border-radius: 6px; overflow: hidden; cursor: pointer;
    border: 2px solid transparent; transition: border-color .15s;
}
.thumb-active { border-color: var(--mud-palette-primary); }
```

### Verificación Fix 5

```powershell
LaunchApp
# Verificar que hay al menos un punto con fotos en el servidor
$puntos = (Invoke-WebRequest "http://localhost:5000/api/puntos" -UseBasicParsing).Content | ConvertFrom-Json
Write-Host "Puntos disponibles: $($puntos.Count)"

Take-Screenshot "fix5_mapa_antes_click"
# Hacer click en un marker — aprox al centro del mapa
TapPct 0.50 0.50 "click en marker"
Start-Sleep 3
Take-Screenshot "fix5_panel_detalle_abierto"
# Click en la imagen del carousel para abrir la galería
TapPct 0.40 0.45 "imagen del carousel"
Start-Sleep 2
Take-Screenshot "fix5_galeria_fotos_abierta"
Write-Host "Verificar: dialog de galería con flechas de navegación y miniaturas"
PressBack
```

---

## FIX 6 — AGREGAR MÁS FOTOS Y COMENTARIOS A UN PUNTO EXISTENTE

### Fix — Botón "Agregar foto" en DetallePunto

En `GeoFoto.Shared/Components/DetallePunto.razor`, agregar sección de acciones:

```razor
@* Sección de acciones: agregar fotos *@
<MudDivider Class="my-3" />

<MudText Typo="Typo.subtitle2" Class="mb-2">Agregar a este punto</MudText>

<MudStack Row="true" Spacing="2">
    @* Subir desde galería *@
    <MudFileUpload T="IReadOnlyList<IBrowserFile>"
                   FilesChanged="OnAgregarFotos"
                   Accept=".jpg,.jpeg,.png,.webp"
                   Multiple="true"
                   Disabled="_agregando">
        <ButtonTemplate>
            <MudButton HtmlTag="label"
                       Variant="Variant.Outlined"
                       Color="Color.Primary"
                       StartIcon="@Icons.Material.Filled.AddPhotoAlternate"
                       Disabled="_agregando"
                       for="@context.Id"
                       Size="Size.Small">
                @(_agregando ? "Agregando..." : "Agregar fotos")
            </MudButton>
        </ButtonTemplate>
    </MudFileUpload>

    @* En MAUI: botón de cámara adicional *@
    @if (MostrarBotonCamara)
    {
        <MudButton Variant="Variant.Outlined"
                   Color="Color.Secondary"
                   StartIcon="@Icons.Material.Filled.CameraAlt"
                   OnClick="AbrirCamaraParaEstePunto"
                   Size="Size.Small">
            Cámara
        </MudButton>
    }
</MudStack>

@if (_agregando)
{
    <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mt-2" />
}

@if (_fotosAgregadas > 0)
{
    <MudAlert Severity="Severity.Success" Dense="true" Class="mt-2">
        @_fotosAgregadas foto(s) agregada(s) al punto
    </MudAlert>
}

@code {
    [Parameter] public bool MostrarBotonCamara { get; set; } = false;
    [Parameter] public EventCallback OnFotosAgregadas { get; set; }

    private bool _agregando = false;
    private int  _fotosAgregadas = 0;

    private async Task OnAgregarFotos(IReadOnlyList<IBrowserFile> files)
    {
        if (Punto is null || !files.Any()) return;
        _agregando = true;
        _fotosAgregadas = 0;
        StateHasChanged();

        var buffers = new List<(string nombre, string tipo, MemoryStream ms)>();
        foreach (var f in files)
        {
            var ms = new MemoryStream();
            await using var s = f.OpenReadStream(maxAllowedSize: 52_428_800);
            await s.CopyToAsync(ms); ms.Seek(0, SeekOrigin.Begin);
            buffers.Add((f.Name, f.ContentType ?? "image/jpeg", ms));
        }

        await InvokeAsync(async () =>
        {
            foreach (var (nombre, tipo, ms) in buffers)
            {
                try
                {
                    // Asociar la foto al punto existente
                    await Api.AgregarFotoAPuntoAsync(Punto.Id, ms, nombre, tipo);
                    _fotosAgregadas++;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error agregando foto a punto {Id}", Punto.Id);
                    Snackbar.Add($"Error: {ex.Message}", Severity.Error);
                }
            }
            _agregando = false;

            // Recargar el detalle del punto para mostrar las nuevas fotos
            Punto = await Api.GetPuntoAsync(Punto.Id);
            await OnFotosAgregadas.InvokeAsync();
            StateHasChanged();
        });
    }
}
```

### Fix — Endpoint para agregar foto a punto existente en la API

En `FotosController.cs`:

```csharp
// POST /api/fotos/upload-to-punto/{puntoId}
[HttpPost("upload-to-punto/{puntoId:int}")]
public async Task<ActionResult<FotoDto>> UploadToPunto(
    int puntoId,
    [FromForm] IFormFile file,
    CancellationToken ct)
{
    // Verificar que el punto existe
    var punto = await _puntosService.GetByIdAsync(puntoId, ct);
    if (punto is null)
        return NotFound(new ProblemDetails { Title = $"Punto {puntoId} no encontrado" });

    // Validar extensión
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
        return BadRequest(new ProblemDetails { Title = "Formato no soportado" });

    // Guardar y asociar al punto
    var dto = await _fotosService.UploadToPuntoAsync(puntoId, file, ct);
    return Created($"/api/fotos/{puntoId}", dto);
}
```

En `IGeoFotoApiClient`:
```csharp
Task AgregarFotoAPuntoAsync(
    int puntoId, Stream stream, string nombre, string tipo,
    CancellationToken ct = default);
```

En `GeoFotoApiClient`:
```csharp
public async Task AgregarFotoAPuntoAsync(
    int puntoId, Stream stream, string nombre, string tipo,
    CancellationToken ct = default)
{
    using var content = new MultipartFormDataContent();
    var sc = new StreamContent(stream);
    sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(tipo);
    content.Add(sc, "file", nombre);
    var response = await _http.PostAsync(
        $"api/fotos/upload-to-punto/{puntoId}", content, ct);
    response.EnsureSuccessStatusCode();
}
```

### Verificación Fix 6

```powershell
LaunchApp
TapPct 0.50 0.50 "click marker"
Start-Sleep 3
Take-Screenshot "fix6_detalle_con_agregar_fotos"
Write-Host "Verificar: botón 'Agregar fotos' visible debajo de las fotos actuales"
Write-Host "Verificar: campo de descripción editable"
```

---

## PASO FINAL — RECOMPILAR, REINSTALAR Y VERIFICAR TODO

```powershell
Write-Host "=== COMPILACIÓN FINAL ==="

dotnet build GeoFoto.Api\GeoFoto.Api.csproj --configuration Debug --verbosity quiet
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR Api"; exit 1 }

dotnet build GeoFoto.Shared\GeoFoto.Shared.csproj --configuration Debug --verbosity quiet
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR Shared"; exit 1 }

dotnet build GeoFoto.Web\GeoFoto.Web.csproj --configuration Debug --verbosity quiet
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR Web"; exit 1 }

dotnet build GeoFoto.Mobile\GeoFoto.Mobile.csproj `
    --configuration Debug --framework net10.0-android --verbosity minimal
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR Mobile"; exit 1 }

Write-Host "=== INSTALAR ==="
$apk = Get-ChildItem "GeoFoto.Mobile\bin\Debug\net10.0-android" `
    -Filter "*-Signed.apk" -Recurse |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
& $global:ADB -s $global:DEV uninstall $global:PKG 2>$null
& $global:ADB -s $global:DEV install -r $apk.FullName
& $global:ADB -s $global:DEV reverse tcp:5000 tcp:5000
& $global:ADB -s $global:DEV shell pm grant $global:PKG android.permission.ACCESS_FINE_LOCATION
& $global:ADB -s $global:DEV shell pm grant $global:PKG android.permission.ACCESS_COARSE_LOCATION

Write-Host "=== TOUR DE VERIFICACIÓN COMPLETO ==="
LaunchApp
Take-Screenshot "FINAL_01_mapa_inicio"

TapPct 0.90 0.82 "GPS"
Start-Sleep 6
Take-Screenshot "FINAL_02_gps_centrado"

TapPct 0.50 0.50 "marker"
Start-Sleep 3
Take-Screenshot "FINAL_03_detalle_abierto"

TapPct 0.40 0.45 "imagen carousel"
Start-Sleep 2
Take-Screenshot "FINAL_04_galeria_fotos"
PressBack; Start-Sleep 1

TapPct 0.30 0.04 "Subir"
Start-Sleep 3
Take-Screenshot "FINAL_05_pantalla_subir"

TapPct 0.70 0.04 "Sync"
Start-Sleep 3
Take-Screenshot "FINAL_06_sync_timeline"

TapPct 0.50 0.04 "Lista"
Start-Sleep 3
Take-Screenshot "FINAL_07_lista_paginada"

# Verificar errores
GetErrores "ux_shots\logcat_final.txt"
$crashes = (Get-Content "ux_shots\logcat_final.txt" |
    Select-String "FATAL|AndroidRuntime").Count
Write-Host "Crashes en logcat: $crashes"

# Commit final
git add -A
git add -- ":(exclude)*.apk" ":(exclude)ux_shots/" ":(exclude)bin/" ":(exclude)obj/"
git commit -m "feat(UX): barras Android, GPS, galería fotos, StreamRendering, agregar fotos a punto"
git push origin (git rev-parse --abbrev-ref HEAD)
Write-Host "Todo commiteado"
```

---

## CHECKLIST FINAL

  [ ] Fix 1: AppBar no tapada por status bar — screenshot FINAL_01
  [ ] Fix 1: contenido no tapado por navigation bar — screenshot FINAL_01
  [ ] Fix 2: skeleton visible al navegar a /lista — screenshot fix2_skeleton
  [ ] Fix 3: botón GPS visible en mapa — screenshot FINAL_02
  [ ] Fix 3: mapa se centra con marker de posición — screenshot FINAL_02
  [ ] Fix 4: subir foto → mapa se centra y muestra el nuevo punto — screenshot fix4
  [ ] Fix 5: click en marker → galería de fotos con miniaturas — screenshot FINAL_04
  [ ] Fix 6: panel detalle tiene botón "Agregar fotos" — screenshot FINAL_03
  [ ] Sin crashes en logcat_final.txt
  [ ] [MANUAL] AppBar visible completa en el celular físico
  [ ] [MANUAL] Sacar foto → marker aparece y mapa se centra
  [ ] Commit y push realizados

Comenzá con el PASO 0.
```
