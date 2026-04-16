# GeoFoto Sprint 07 - E2E Screenshot Capture v5
# CONFIRMED icon positions (from e2e_08/09/10 tooltips in v4 run):
#   Upload: x=720, y=162  (tooltip "Subir"  confirmed v4/e2e_08)
#   List:   x=840, y=162  (tooltip "Lista"  confirmed v4/e2e_09)
#   Map:    x=460, y=162  (tooltip "Mapa"   confirmed v4/e2e_10)
#   Sync:   x=960, y=162  (estimated: 840+120, spacing consistent with Upload->List)
# AppBar y=162: status_bar(81) + half_Dense_AppBar(81) = 162 (density 540, scale 3.375x)
# After e2e_07 (FotoViewer): ONE BACK only, then tap Sync AppBar directly
# Device: ZY32GSJ88S (1080x2400, density override 540)

$ADB    = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$DEVICE = 'ZY32GSJ88S'
$PKG    = 'com.companyname.geofoto.mobile'
$DEST   = Join-Path $PSScriptRoot '..\test-results\screenshots\sprint07'
$REPORT = Join-Path $PSScriptRoot '..\test-results\reporte_e2e.txt'

$ok   = 0
$fail = 0
$log  = @()

if (-not (Test-Path $DEST)) { New-Item -ItemType Directory -Path $DEST | Out-Null }

function Adb { & $ADB -s $DEVICE @args }
function Tap($x,$y) { Adb shell input tap $x $y }
function Key($k)    { Adb shell input keyevent $k }

function Cap($n, $tag, $desc) {
    $file = "e2e_${n}_${tag}.png"
    $path = Join-Path $DEST $file
    Write-Host "[CAP $n/10] $desc..."
    Adb shell screencap -p /sdcard/s7_$n.png | Out-Null
    Start-Sleep -Seconds 1
    Adb pull /sdcard/s7_$n.png $path | Out-Null
    Adb shell rm /sdcard/s7_$n.png 2>$null | Out-Null
    if (Test-Path $path) {
        $kb = [int]((Get-Item $path).Length / 1KB)
        Write-Host "  [OK]  $file  ($kb KB)" -ForegroundColor Green
        $script:ok++
        $script:log += "OK    $n  $desc  ($kb KB)"
    } else {
        Write-Host "  [FAIL] $file" -ForegroundColor Red
        $script:fail++
        $script:log += "FAIL  $n  $desc"
    }
}

# ─────────────────────────────────────────────────────────
# FASE 1: Mapa inicial
# ─────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Launching GeoFoto..." -ForegroundColor Cyan
Adb shell am force-stop $PKG | Out-Null
Start-Sleep -Seconds 2
Adb shell am start -n "${PKG}/crc640ec92c3269169255.MainActivity" | Out-Null
Start-Sleep -Seconds 9

Cap "01" "mapa_inicial" "Mapa visible con tiles OSM"
Start-Sleep -Seconds 2

# ─────────────────────────────────────────────────────────
# FASE 2: GPS
# ─────────────────────────────────────────────────────────
Write-Host "[NAV] Tap GPS FAB (960, 2080) - purple, confirmed..."
Tap 960 2080
Start-Sleep -Seconds 7

Cap "02" "marcador_gps" "Marcador posicion propia circulo azul"
Start-Sleep -Seconds 1

Cap "03" "mapa_centrado_gps" "Mapa centrado post-GPS zoom 16"
Start-Sleep -Seconds 1

# ─────────────────────────────────────────────────────────
# FASE 3: Zoom out
# ─────────────────────────────────────────────────────────
Write-Host "[NAV] Zoom out x3..."
Tap 72 420; Start-Sleep -Seconds 1
Tap 72 420; Start-Sleep -Seconds 1
Tap 72 420; Start-Sleep -Seconds 2

Cap "04" "marker_existente" "Marker de punto existente en mapa"
Start-Sleep -Seconds 1

# ─────────────────────────────────────────────────────────
# FASE 4: Lista (e2e_08) -> row tap -> popup (e2e_05, e2e_06) -> FotoViewer (e2e_07)
# List icon CONFIRMED at x=840, y=162 (tooltip "Lista" in v4/e2e_09)
# ─────────────────────────────────────────────────────────
Write-Host "[NAV] AppBar -> Lista (840, 162) - CONFIRMED position..."
Tap 840 162
Start-Sleep -Seconds 4

# e2e_08: Lista de markers (Puntos registrados page)
Cap "08" "lista_markers" "Lista de markers con busqueda"
Start-Sleep -Seconds 1

# Tap "Prueba" row to navigate to map+popup via IrAlMapa
# Row y estimated: AppBar(243) + mt-4(54) + h5+gutter(135) + search+mb(175) + table-header(121) + half-row(60) = ~788
# Use y=800 safely within first data row, x=400 (left column, away from action buttons at right)
Write-Host "[NAV] Tap first marker row (400, 800) - left of action buttons..."
Tap 400 800
Start-Sleep -Seconds 5

# e2e_05: Popup del marker (map centered on "Prueba", popup/card visible)
Cap "05" "popup_marker" "Popup del marker abierto MarkerPopup"
Start-Sleep -Seconds 1

# e2e_06: Carrusel de fotos (same popup, carousel area visible)
Cap "06" "carrusel_fotos" "Carrusel de fotos visible en popup"
Start-Sleep -Seconds 2

# Tap popup to open FotoViewer (popup should be visible on map near center)
# "Prueba" marker at approx -31.7497, -60.5213 appears near center-left on zoomed-out map
# After IrAlMapa, map is centered at marker coords at zoom 16
Write-Host "[NAV] Tap popup/marker area (~540, 900) for FotoViewer..."
Tap 540 900
Start-Sleep -Seconds 3

Cap "07" "foto_fullscreen" "Foto ampliada fullscreen FotoViewer"
Start-Sleep -Seconds 1

# ─────────────────────────────────────────────────────────
# FASE 5: ONE BACK, then Sync AppBar
# Sync icon ESTIMATED at x=960 (List=840 + spacing 120 = 960)
# ─────────────────────────────────────────────────────────
Write-Host "[NAV] BACK x1: close top layer..."
Key KEYCODE_BACK
Start-Sleep -Seconds 2

Write-Host "[NAV] AppBar -> Sync (960, 162)..."
Tap 960 162
Start-Sleep -Seconds 4

Cap "09" "pantalla_sync" "Pantalla de sincronizacion EstadoSync"
Start-Sleep -Seconds 1

# ─────────────────────────────────────────────────────────
# FASE 6: Map icon -> mapa con badge
# Map icon CONFIRMED at x=460, y=162 (tooltip "Mapa" in v4/e2e_10)
# ─────────────────────────────────────────────────────────
Write-Host "[NAV] AppBar -> Mapa (460, 162) - CONFIRMED position..."
Tap 460 162
Start-Sleep -Seconds 3

Cap "10" "badge_synced" "Badge verde Synced en marker"

# ─────────────────────────────────────────────────────────
# Reporte
# ─────────────────────────────────────────────────────────
$reportLines = @(
    "GeoFoto - Reporte E2E Sprint 07",
    "Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "Dispositivo: $DEVICE",
    "Paquete: $PKG",
    "Script: 07g_capture_sprint07_v5.ps1",
    "============================================",
    "",
    "Resultados por pantalla:",
    "----------------------------------------"
) + $log + @(
    "",
    "RESUMEN:",
    "  OK:   $ok / 10",
    "  FAIL: $fail / 10",
    ""
)

if ($fail -eq 0) {
    $reportLines += "  RESULTADO: SUCCESS -- 10/10 capturas OK"
} else {
    $reportLines += "  RESULTADO: PARCIAL -- $ok/10 OK, $fail FALLIDAS"
}

$reportLines | Out-File -FilePath $REPORT -Encoding utf8

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "CAPTURAS: $ok/10 OK, $fail FALLIDAS"
if ($fail -eq 0) {
    Write-Host "RESULTADO: SUCCESS" -ForegroundColor Green
} else {
    Write-Host "RESULTADO: PARCIAL - revisar capturas" -ForegroundColor Yellow
}
Write-Host "Capturas: $DEST"
Write-Host "Reporte:  $REPORT"
Write-Host "============================================"

exit $fail
