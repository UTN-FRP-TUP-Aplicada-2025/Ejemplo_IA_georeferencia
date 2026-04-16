# GeoFoto Sprint 07 - E2E Screenshot Capture v3
# Fixed:
#   - AppBar icons are LEFT-aligned after title, not right-aligned
#   - AppBar center y=120 (was 96); icons: Map x~295, Upload x~355, List x~415, Sync x~475
#   - Navigation path: Lista first -> row tap -> map+popup -> captures 05/06/07
#   - 2x BACK after fullscreen to close viewer+popup before AppBar navigation
# Device: ZY32GSJ88S (1080x2400 @ ~2.5x density)

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
# FASE 1: Mapa inicial + GPS
# ─────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Launching GeoFoto..." -ForegroundColor Cyan
Adb shell am force-stop $PKG | Out-Null
Start-Sleep -Seconds 2
Adb shell am start -n "${PKG}/crc640ec92c3269169255.MainActivity" | Out-Null
Start-Sleep -Seconds 9

# e2e_01: Mapa con tiles OSM
Cap "01" "mapa_inicial" "Mapa visible con tiles OSM"
Start-Sleep -Seconds 2

# e2e_02: Tap GPS FAB (purple, bottom-right ~x=960, y=2080)
Write-Host "[NAV] Tap GPS FAB (purple, ~960,2080)..."
Tap 960 2080
Start-Sleep -Seconds 7
Cap "02" "marcador_gps" "Marcador posicion propia circulo azul"
Start-Sleep -Seconds 1

# e2e_03: Mapa centrado
Cap "03" "mapa_centrado_gps" "Mapa centrado post-GPS zoom 16"
Start-Sleep -Seconds 1

# e2e_04: Zoom out x3 (zoom-out button top-left ~x=72, y=420)
Write-Host "[NAV] Zoom out x3..."
Tap 72 420
Start-Sleep -Seconds 1
Tap 72 420
Start-Sleep -Seconds 1
Tap 72 420
Start-Sleep -Seconds 2
Cap "04" "marker_existente" "Marker de punto existente en mapa"
Start-Sleep -Seconds 1

# ─────────────────────────────────────────────────────────
# FASE 2: Lista -> popup -> carrusel -> fullscreen
# AppBar Dense height: 48dp * 2.5 = 120px + status 24dp*2.5=60px -> center y=120
# Icons left-aligned: GeoFoto(~180px) + Map(100px) + Upload(100px) + List(100px) + Sync(100px)
# List center x ~ 295+100+100 = 495? Try 415 (conservative)
# ─────────────────────────────────────────────────────────

Write-Host "[NAV] AppBar -> Lista (x=415, y=120)..."
Tap 415 120
Start-Sleep -Seconds 4

# e2e_08: Lista de markers
Cap "08" "lista_markers" "Lista de markers con busqueda"
Start-Sleep -Seconds 1

# Tap first row in MudTable
# AppBar ends ~180px, container mt-4~40px, title h5~60px, search~80px, table header~60px -> first row center ~480
Write-Host "[NAV] Tap first marker row (~540, 480)..."
Tap 540 480
Start-Sleep -Seconds 5

# e2e_05: popup del marker (abierto por IrAlMapa -> Mapa.razor con puntoId)
Cap "05" "popup_marker" "Popup del marker abierto MarkerPopup"
Start-Sleep -Seconds 1

# e2e_06: carrusel de fotos en el popup (misma pantalla)
Cap "06" "carrusel_fotos" "Carrusel de fotos visible en popup"
Start-Sleep -Seconds 1

# Tap carousel image to open FotoViewer fullscreen (~center of dialog image area)
Write-Host "[NAV] Tap carousel image (~540, 700)..."
Tap 540 700
Start-Sleep -Seconds 3
Cap "07" "foto_fullscreen" "Foto ampliada fullscreen FotoViewer"
Start-Sleep -Seconds 1

# ─────────────────────────────────────────────────────────
# FASE 3: Cerrar viewer+popup, navegar a Sync
# ─────────────────────────────────────────────────────────

Write-Host "[NAV] BACK x2: close FotoViewer then MarkerPopup..."
Key KEYCODE_BACK
Start-Sleep -Seconds 2
Key KEYCODE_BACK
Start-Sleep -Seconds 2
# Now on Mapa page, no dialogs open

# e2e_09: EstadoSync via AppBar (Sync icon = 4th after title, x~475)
Write-Host "[NAV] AppBar -> Sync (x=475, y=120)..."
Tap 475 120
Start-Sleep -Seconds 4
Cap "09" "pantalla_sync" "Pantalla de sincronizacion EstadoSync"
Start-Sleep -Seconds 1

# e2e_10: Badge synced - back to mapa (Map icon = 1st after title, x~295)
Write-Host "[NAV] AppBar -> Mapa (x=295, y=120)..."
Tap 295 120
Start-Sleep -Seconds 3
Cap "10" "badge_synced" "Badge verde Synced en marker"

# ─────────────────────────────────────────────────────────
# Generar reporte
# ─────────────────────────────────────────────────────────

$reportLines = @(
    "GeoFoto - Reporte E2E Sprint 07",
    "Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "Dispositivo: $DEVICE",
    "Paquete: $PKG",
    "Script: 07e_capture_sprint07_v3.ps1",
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
