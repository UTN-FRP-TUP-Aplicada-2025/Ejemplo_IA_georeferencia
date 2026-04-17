$ADB  = 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
$DEST = 'E:\repos\tup\utn\aplicada\2025\utn\ia\Ejemplo_IA_georeferencia\test-results'

function Cap($name) {
    $file = Join-Path $DEST "$name.png"
    & $ADB -s ZY32GSJ88S shell screencap -p /sdcard/cap_tmp.png | Out-Null
    Start-Sleep -Seconds 1
    & $ADB -s ZY32GSJ88S pull /sdcard/cap_tmp.png $file | Out-Null
    & $ADB -s ZY32GSJ88S shell rm /sdcard/cap_tmp.png 2>$null | Out-Null
    Write-Host "[CAP] $name"
}

Cap "fix_01_mapa_inicial"
Write-Host "Listo."
