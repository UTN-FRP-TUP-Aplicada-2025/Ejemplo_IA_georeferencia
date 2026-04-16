@echo off
setlocal
set ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
set DEVICE=ZY32GSJ88S
set DEST=%~dp0..\test-results\screenshots
set APP_ID=com.companyname.geofoto.mobile

:: Coordenadas AppBar (orig 1080x2400): y=110 es centro del AppBar
:: Map=344  Upload=426  List=510  Sync=592
set Y_APP=110
set X_MAP=344
set X_UPLOAD=426
set X_LIST=510
set X_SYNC=592

echo.
echo === [INICIO] Re-lanzando la app ===
"%ADB%" -s %DEVICE% shell am force-stop %APP_ID%
ping 127.0.0.1 -n 3 >nul
"%ADB%" -s %DEVICE% reverse tcp:5000 tcp:5000 >nul
"%ADB%" -s %DEVICE% shell monkey -p %APP_ID% -c android.intent.category.LAUNCHER 1 >nul
echo Esperando 15s para carga inicial de BlazorWebView...
ping 127.0.0.1 -n 16 >nul

:: --- CAPTURA 01: Pantalla inicial (Lista ya visible al abrir) ---
echo.
echo === [CAP 01] Pantalla de inicio ===
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/s01.png
"%ADB%" -s %DEVICE% pull /sdcard/s01.png "%DEST%\01_inicio.png"
if exist "%DEST%\01_inicio.png" (echo [OK] 01_inicio.png) else (echo [FALTA] 01_inicio.png & goto :error)
ping 127.0.0.1 -n 3 >nul

:: --- CAPTURA 02: Mapa ---
echo.
echo === [CAP 02] Navegando a Mapa ===
"%ADB%" -s %DEVICE% shell input tap %X_MAP% %Y_APP%
ping 127.0.0.1 -n 5 >nul
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/s02.png
"%ADB%" -s %DEVICE% pull /sdcard/s02.png "%DEST%\02_mapa.png"
if exist "%DEST%\02_mapa.png" (echo [OK] 02_mapa.png) else (echo [FALTA] 02_mapa.png & goto :error)
ping 127.0.0.1 -n 3 >nul

:: --- CAPTURA 03: SubirFotos (pantalla de camara en la app, sin abrir camara nativa) ---
echo.
echo === [CAP 03] Navegando a Subir Fotos (upload icon en AppBar) ===
"%ADB%" -s %DEVICE% shell input tap %X_UPLOAD% %Y_APP%
ping 127.0.0.1 -n 5 >nul
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/s03.png
"%ADB%" -s %DEVICE% pull /sdcard/s03.png "%DEST%\03_camara.png"
if exist "%DEST%\03_camara.png" (echo [OK] 03_camara.png) else (echo [FALTA] 03_camara.png & goto :error)
ping 127.0.0.1 -n 3 >nul

:: --- CAPTURA 04: Lista de Puntos ---
echo.
echo === [CAP 04] Navegando a Lista de Puntos ===
"%ADB%" -s %DEVICE% shell input tap %X_LIST% %Y_APP%
ping 127.0.0.1 -n 4 >nul
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/s04.png
"%ADB%" -s %DEVICE% pull /sdcard/s04.png "%DEST%\04_lista_fotos.png"
if exist "%DEST%\04_lista_fotos.png" (echo [OK] 04_lista_fotos.png) else (echo [FALTA] 04_lista_fotos.png & goto :error)
ping 127.0.0.1 -n 3 >nul

:: --- CAPTURA 05: Estado Sync ---
echo.
echo === [CAP 05] Navegando a Estado Sync ===
"%ADB%" -s %DEVICE% shell input tap %X_SYNC% %Y_APP%
ping 127.0.0.1 -n 5 >nul
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/s05.png
"%ADB%" -s %DEVICE% pull /sdcard/s05.png "%DEST%\05_sync.png"
if exist "%DEST%\05_sync.png" (echo [OK] 05_sync.png) else (echo [FALTA] 05_sync.png & goto :error)

:: --- LIMPIEZA ---
"%ADB%" -s %DEVICE% shell rm /sdcard/s01.png /sdcard/s02.png /sdcard/s03.png /sdcard/s04.png /sdcard/s05.png >nul 2>&1

echo.
echo === CAPTURAS COMPLETADAS ===
dir "%DEST%\*.png"
exit /b 0

:error
echo.
echo [ERROR] Una captura fallo. Revisa la conexion ADB.
exit /b 1
