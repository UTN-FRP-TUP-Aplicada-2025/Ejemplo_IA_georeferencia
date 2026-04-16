# GeoFoto Sprint 07 - E2E Screenshot Capture v2
# Device: ZY32GSJ88S (1080x2400)
# Uses PowerShell Start-Sleep for reliable waits

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
function Tap($x,$y)               { Adb shell input tap $x $y }
function Key($k)                  { Adb shell input keyevent $k }

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

# --- Launch GeoFoto on Mapa.razor ---
Write-Host ""
Write-Host "Launching GeoFoto..." -ForegroundColor Cyan
Adb shell am force-stop $PKG | Out-Null
Start-Sleep -Seconds 2
Adb shell am start -n "${PKG}/crc640ec92c3269169255.MainActivity" | Out-Null
Start-Sleep -Seconds 9

# e2e_01: Map with OSM tiles
Cap "01" "mapa_inicial" "Mapa visible con tiles OSM"
Start-Sleep -Seconds 2

# e2e_02: GPS FAB -> user position marker (blue dot)
# GPS FAB is purple circle bottom-right: ~x=960, y=2080 on 1080x2400
Write-Host "[NAV] Tap GPS FAB..."
Tap 960 2080
Start-Sleep -Seconds 7
Cap "02" "marcador_gps" "Marcador posicion propia circulo azul"
Start-Sleep -Seconds 1

# e2e_03: Map centered after GPS
Cap "03" "mapa_centrado_gps" "Mapa centrado post-GPS zoom 16"
Start-Sleep -Seconds 1

# e2e_04: Existing marker visible (zoom out)
Write-Host "[NAV] Zoom out to see existing markers..."
Tap 72 450    # zoom-out button top-left of map
Start-Sleep -Seconds 1
Tap 72 450
Start-Sleep -Seconds 2
Cap "04" "marker_existente" "Marker de punto existente en mapa"
Start-Sleep -Seconds 1

# e2e_05: Open marker popup (tap center of map)
Write-Host "[NAV] Tap marker to open popup..."
Tap 540 1100
Start-Sleep -Seconds 3
Cap "05" "popup_marker" "Popup del marker abierto MarkerPopup"
Start-Sleep -Seconds 1

# e2e_06: Photo carousel in popup
Cap "06" "carrusel_fotos" "Carrusel de fotos visible en popup"
Start-Sleep -Seconds 1

# e2e_07: Fullscreen photo (tap carousel image)
Write-Host "[NAV] Tap carousel image to open FotoViewer..."
Tap 540 600
Start-Sleep -Seconds 2
Cap "07" "foto_fullscreen" "Foto ampliada fullscreen FotoViewer"
Start-Sleep -Seconds 1

# e2e_08: Marker list page
# Close viewer, close popup, tap Lista icon in AppBar
Write-Host "[NAV] Close viewer+popup, navigate to Lista..."
Key KEYCODE_BACK
Start-Sleep -Seconds 1
Key KEYCODE_BACK
Start-Sleep -Seconds 2
# Lista icon in AppBar (3rd icon from right ~x=864, y=96)
Tap 864 96
Start-Sleep -Seconds 3
Cap "08" "lista_markers" "Lista de markers con busqueda"
Start-Sleep -Seconds 1

# e2e_09: Sync status page
# Tap Sync icon in AppBar (last icon right ~x=972, y=96)
Write-Host "[NAV] Navigate to EstadoSync..."
Tap 972 96
Start-Sleep -Seconds 3
Cap "09" "pantalla_sync" "Pantalla de sincronizacion EstadoSync"
Start-Sleep -Seconds 1

# e2e_10: Synced badge on marker (back to map)
Write-Host "[NAV] Back to Mapa for synced badge..."
Key KEYCODE_BACK
Start-Sleep -Seconds 2
Cap "10" "badge_synced" "Badge verde Synced en marker"

# --- Generate report ---
$reportLines = @(
    "GeoFoto - Reporte E2E Sprint 07",
    "Fecha: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "Dispositivo: $DEVICE",
    "Paquete: $PKG",
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
