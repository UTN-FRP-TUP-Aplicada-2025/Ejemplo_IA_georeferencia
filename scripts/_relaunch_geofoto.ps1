$ADB  = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$PKG  = 'com.companyname.geofoto.mobile'
$ACT  = 'crc640ec92c3269169255.MainActivity'
$DEST = 'E:\repos\tup\utn\aplicada\2025\utn\ia\Ejemplo_IA_georeferencia\test-results'

function Cap($name) {
    $file = Join-Path $DEST "$name.png"
    & $ADB -s ZY32GSJ88S shell screencap -p /sdcard/cap_tmp.png | Out-Null
    Start-Sleep -Seconds 1
    & $ADB -s ZY32GSJ88S pull /sdcard/cap_tmp.png $file | Out-Null
    & $ADB -s ZY32GSJ88S shell rm /sdcard/cap_tmp.png 2>$null | Out-Null
    Write-Host "[CAP] $name"
}

# Cerrar lo que haya abierto
Write-Host "[1] Cerrando apps abiertas..."
& $ADB -s ZY32GSJ88S shell input keyevent KEYCODE_BACK | Out-Null
Start-Sleep -Seconds 1
& $ADB -s ZY32GSJ88S shell input keyevent KEYCODE_HOME | Out-Null
Start-Sleep -Seconds 1

# Forzar parada de GeoFoto
& $ADB -s ZY32GSJ88S shell am force-stop $PKG | Out-Null
Start-Sleep -Seconds 1

# Lanzar con am start (componente completo)
Write-Host "[2] Lanzando GeoFoto..."
$component = "$PKG/$ACT"
& $ADB -s ZY32GSJ88S shell am start -n $component
Start-Sleep -Seconds 11

Write-Host "[3] Capturando pantalla inicial..."
Cap "func_01_mapa_inicial"
Write-Host "Listo."
