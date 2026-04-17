$ADB = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$PKG = 'com.companyname.geofoto.mobile'
$DEST = 'E:\repos\tup\utn\aplicada\2025\utn\ia\Ejemplo_IA_georeferencia\test-results'

function Cap($name) {
    $file = Join-Path $DEST "$name.png"
    & $ADB -s ZY32GSJ88S shell screencap -p /sdcard/cap_tmp.png | Out-Null
    Start-Sleep -Seconds 1
    & $ADB -s ZY32GSJ88S pull /sdcard/cap_tmp.png $file | Out-Null
    & $ADB -s ZY32GSJ88S shell rm /sdcard/cap_tmp.png 2>$null | Out-Null
    Write-Host "[CAP] $name -> $file"
}

function Tap($x, $y) { & $ADB -s ZY32GSJ88S shell input tap $x $y }
function Key($k)     { & $ADB -s ZY32GSJ88S shell input keyevent $k }

# 1. Parar app, conceder permisos, relanzar
Write-Host "[1] Forzando parada..."
& $ADB -s ZY32GSJ88S shell am force-stop $PKG | Out-Null
Start-Sleep -Seconds 2

Write-Host "[2] Concediendo permisos de ubicacion..."
& $ADB -s ZY32GSJ88S shell pm grant $PKG android.permission.ACCESS_FINE_LOCATION 2>$null
& $ADB -s ZY32GSJ88S shell pm grant $PKG android.permission.ACCESS_COARSE_LOCATION 2>$null
& $ADB -s ZY32GSJ88S shell pm grant $PKG android.permission.CAMERA 2>$null
& $ADB -s ZY32GSJ88S shell pm grant $PKG android.permission.READ_EXTERNAL_STORAGE 2>$null
& $ADB -s ZY32GSJ88S shell pm grant $PKG android.permission.WRITE_EXTERNAL_STORAGE 2>$null

Write-Host "[3] Lanzando app..."
& $ADB -s ZY32GSJ88S shell monkey -p $PKG -c android.intent.category.LAUNCHER 1 | Out-Null
Start-Sleep -Seconds 10

Write-Host "[4] Capturando pantalla inicial..."
Cap "func_01_mapa_inicial"
