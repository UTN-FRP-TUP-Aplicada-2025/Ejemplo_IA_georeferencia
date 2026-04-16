@echo off
setlocal
set ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
set DEVICE=ZY32GSJ88S
set DEST=%~dp0..\test-results\screenshots
set APP_ID=com.companyname.geofoto.mobile

echo.
echo === Re-lanzando la app desde cero ===
"%ADB%" -s %DEVICE% shell am force-stop %APP_ID%
ping 127.0.0.1 -n 3 >nul

"%ADB%" -s %DEVICE% shell monkey -p %APP_ID% -c android.intent.category.LAUNCHER 1 >nul
echo Esperando 12s para que BlazorWebView cargue...
ping 127.0.0.1 -n 13 >nul

echo.
echo === Navegando al Mapa ===
"%ADB%" -s %DEVICE% shell input tap 343 103
ping 127.0.0.1 -n 4 >nul

echo.
echo === Abriendo camara nativa (FAB) ===
"%ADB%" -s %DEVICE% shell input tap 900 1700
ping 127.0.0.1 -n 5 >nul

echo Captura 03 (camara nativa)...
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/cap03b.png
"%ADB%" -s %DEVICE% pull /sdcard/cap03b.png "%DEST%\03_camara.png"
if exist "%DEST%\03_camara.png" (echo [OK] 03_camara.png recapturada) else (echo [FALTA])

echo.
echo === Volviendo a la app con BACK ===
"%ADB%" -s %DEVICE% shell input keyevent KEYCODE_BACK
echo Esperando 8s para que la app retome el foco y Blazor re-renderice...
ping 127.0.0.1 -n 9 >nul

echo.
echo === Navegando a Lista de Puntos (tap icono lista en AppBar) ===
"%ADB%" -s %DEVICE% shell input tap 556 103
ping 127.0.0.1 -n 4 >nul

echo Captura 04 (lista de fotos)...
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/cap04b.png
"%ADB%" -s %DEVICE% pull /sdcard/cap04b.png "%DEST%\04_lista_fotos.png"
if exist "%DEST%\04_lista_fotos.png" (echo [OK] 04_lista_fotos.png recapturada) else (echo [FALTA])
ping 127.0.0.1 -n 3 >nul

echo.
echo === Navegando a Estado Sync (tap icono sync en AppBar) ===
"%ADB%" -s %DEVICE% shell input tap 660 103
ping 127.0.0.1 -n 5 >nul

echo Captura 05 (estado sync)...
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/cap05b.png
"%ADB%" -s %DEVICE% pull /sdcard/cap05b.png "%DEST%\05_sync.png"
if exist "%DEST%\05_sync.png" (echo [OK] 05_sync.png recapturada) else (echo [FALTA])

echo.
echo === Limpieza ===
"%ADB%" -s %DEVICE% shell rm /sdcard/cap03b.png /sdcard/cap04b.png /sdcard/cap05b.png >nul 2>&1

echo.
echo === Resultado final ===
dir "%DEST%\*.png"
exit /b 0
