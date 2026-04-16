# GeoFoto Sprint 07 - E2E Screenshot Capture v4
# Key fixes from v3:
#   - Density override = 540 -> scale 3.375x confirmed via GPS FAB at (960,2080)
#   - AppBar center y = 81(statusbar) + 81(half Dense AppBar) = 162
#   - List icon x = CSS 209 * 3.375 = ~720; Sync x = CSS 249 * 3.375 = ~840; Map x = ~460
#   - Lista first row y = ~750 (AppBar 243 + mt-4 54 + title 135 + search 175 + header 121 + half-row 18 = ~746)
#   - After e2e_07 (FotoViewer): ONE BACK only, then tap AppBar Sync (popup is non-blocking per e2e_10)
# Device: ZY32GSJ88S (1080x2400)

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
# AppBar icon positions (density 540, scale 3.375x):
#   y = 162  (status 81 + half AppBar 81)
#   Map   x = 460  (CSS 129 * 3.375 = 435, touch area +/- 68px)
#   Upload x = 600 (CSS 169 * 3.375 = 570)
#   List  x = 720  (CSS 209 * 3.375 = 705)
#   Sync  x = 840  (CSS 249 * 3.375 = 840)
#   SyncBadge x ~985 (rightmost, display-only)
# ─────────────────────────────────────────────────────────

# ─────────────────────────────────────────────────────────
# FASE 1: Lanzar app + mapa inicial
# ─────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Launching GeoFoto..." -ForegroundColor Cyan
Adb shell am force-stop $PKG | Out-Null
Start-Sleep -Seconds 2
Adb shell am start -n "${PKG}/crc640ec92c3269169255.MainActivity" | Out-Null
Start-Sleep -Seconds 9

# e2e_01: Mapa visible con tiles OSM
Cap "01" "mapa_inicial" "Mapa visible con tiles OSM"
Start-Sleep -Seconds 2

# ─────────────────────────────────────────────────────────
# FASE 2: GPS
# ─────────────────────────────────────────────────────────

# GPS FAB confirmed at (960, 2080) - purple FAB bottom-right
Write-Host "[NAV] Tap GPS FAB (960, 2080)..."
Tap 960 2080
Start-Sleep -Seconds 7

# e2e_02: Marcador GPS azul
Cap "02" "marcador_gps" "Marcador posicion propia circulo azul"
Start-Sleep -Seconds 1

# e2e_03: Mapa centrado
Cap "03" "mapa_centrado_gps" "Mapa centrado post-GPS zoom 16"
Start-Sleep -Seconds 1

# ─────────────────────────────────────────────────────────
# FASE 3: Zoom out
# ─────────────────────────────────────────────────────────
Write-Host "[NAV] Zoom out x3 (72, 420)..."
Tap 72 420; Start-Sleep -Seconds 1
Tap 72 420; Start-Sleep -Seconds 1
Tap 72 420; Start-Sleep -Seconds 2

# e2e_04: Mapa con marker existente
Cap "04" "marker_existente" "Marker de punto existente en mapa"
Start-Sleep -Seconds 1

# ─────────────────────────────────────────────────────────
# FASE 4: Lista -> tap row -> popup -> carrusel -> fullscreen
# Captures order: e2e_08 first, then 05, 06, 07
# ─────────────────────────────────────────────────────────

# Navigate to Lista via AppBar (x=720, y=162)
Write-Host "[NAV] AppBar -> Lista (720, 162)..."
Tap 720 162
Start-Sleep -Seconds 4

# e2e_08: Lista de markers con busqueda
Cap "08" "lista_markers" "Lista de markers con busqueda"
Start-Sleep -Seconds 1

# Tap first data row in MudTable
# Row y estimate: AppBar(243) + mt-4(54) + h5(135) + search(175) + table-header(121) + half-row(18) = 746
Write-Host "[NAV] Tap first marker row (~540, 750)..."
Tap 540 750
Start-Sleep -Seconds 5

# e2e_05: Popup del marker abierto (map centered on marker, popup/card visible)
Cap "05" "popup_marker" "Popup del marker abierto MarkerPopup"
Start-Sleep -Seconds 1

# e2e_06: Carrusel de fotos (same popup, carousel visible)
Cap "06" "carrusel_fotos" "Carrusel de fotos visible en popup"
Start-Sleep -Seconds 1

# Tap popup/carousel to open FotoViewer
# Popup appears near marker center; tap center-lower area of popup card
Write-Host "[NAV] Tap popup image to open FotoViewer (~540, 900)..."
Tap 540 900
Start-Sleep -Seconds 3

# e2e_07: Foto ampliada fullscreen
Cap "07" "foto_fullscreen" "Foto ampliada fullscreen FotoViewer"
Start-Sleep -Seconds 1

# ─────────────────────────────────────────────────────────
# FASE 5: Cerrar viewer (1x BACK), navegar a Sync
# NOTE: popup is non-blocking (AppBar accessible per evidence from e2e_10)
# One BACK closes FotoViewer; then AppBar Sync tap navigates away
# ─────────────────────────────────────────────────────────

Write-Host "[NAV] BACK x1: close FotoViewer..."
Key KEYCODE_BACK
Start-Sleep -Seconds 2

# Navigate to EstadoSync (Sync icon, x=840, y=162)
Write-Host "[NAV] AppBar -> Sync (840, 162)..."
Tap 840 162
Start-Sleep -Seconds 4

# e2e_09: Pantalla de sincronizacion
Cap "09" "pantalla_sync" "Pantalla de sincronizacion EstadoSync"
Start-Sleep -Seconds 1

# ─────────────────────────────────────────────────────────
# FASE 6: Volver a mapa para badge
# ─────────────────────────────────────────────────────────

# Navigate back to Mapa (Map icon, x=460, y=162)
Write-Host "[NAV] AppBar -> Mapa (460, 162)..."
Tap 460 162
Start-Sleep -Seconds 3

# e2e_10: Mapa con badge synced
Cap "10" "badge_synced" "Badge verde Synced en marker"

# ─────────────────────────────────────────────────────────
# Generar reporte
# ─────────────────────────────────────────────────────────

$reportLines = @(
    "GeoFoto - Reporte E2E Sprint 07",
    "Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "Dispositivo: $DEVICE",
    "Paquete: $PKG",
    "Script: 07f_capture_sprint07_v4.ps1",
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
