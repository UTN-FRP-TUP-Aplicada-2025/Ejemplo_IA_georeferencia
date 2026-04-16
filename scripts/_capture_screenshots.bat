@echo off
setlocal
set ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
set DEVICE=ZY32GSJ88S
set DEST=%~dp0..\test-results\screenshots

if not exist "%DEST%" mkdir "%DEST%"

echo [CAP 1/5] Pantalla inicial...
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/cap01.png
"%ADB%" -s %DEVICE% pull /sdcard/cap01.png "%DEST%\01_inicio.png"
if exist "%DEST%\01_inicio.png" (echo [OK] 01_inicio.png) else (echo [FALTA] 01_inicio.png)
timeout /t 2 /nobreak >nul

echo [CAP 2/5] Mapa (tap centro 540x960)...
"%ADB%" -s %DEVICE% shell input tap 540 960
timeout /t 4 /nobreak >nul
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/cap02.png
"%ADB%" -s %DEVICE% pull /sdcard/cap02.png "%DEST%\02_mapa.png"
if exist "%DEST%\02_mapa.png" (echo [OK] 02_mapa.png) else (echo [FALTA] 02_mapa.png)
timeout /t 2 /nobreak >nul

echo [CAP 3/5] Camara (FAB tap 900x1700)...
"%ADB%" -s %DEVICE% shell input tap 900 1700
timeout /t 5 /nobreak >nul
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/cap03.png
"%ADB%" -s %DEVICE% pull /sdcard/cap03.png "%DEST%\03_camara.png"
if exist "%DEST%\03_camara.png" (echo [OK] 03_camara.png) else (echo [FALTA] 03_camara.png)
timeout /t 2 /nobreak >nul

echo [CAP 4/5] Lista de fotos (BACK + menu top)...
"%ADB%" -s %DEVICE% shell input keyevent KEYCODE_BACK
timeout /t 2 /nobreak >nul
"%ADB%" -s %DEVICE% shell input tap 540 100
timeout /t 3 /nobreak >nul
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/cap04.png
"%ADB%" -s %DEVICE% pull /sdcard/cap04.png "%DEST%\04_lista_fotos.png"
if exist "%DEST%\04_lista_fotos.png" (echo [OK] 04_lista_fotos.png) else (echo [FALTA] 04_lista_fotos.png)
timeout /t 2 /nobreak >nul

echo [CAP 5/5] Estado de sincronizacion (tap menu 540x200)...
"%ADB%" -s %DEVICE% shell input tap 540 200
timeout /t 3 /nobreak >nul
"%ADB%" -s %DEVICE% shell screencap -p /sdcard/cap05.png
"%ADB%" -s %DEVICE% pull /sdcard/cap05.png "%DEST%\05_sync.png"
if exist "%DEST%\05_sync.png" (echo [OK] 05_sync.png) else (echo [FALTA] 05_sync.png)

echo [CLEAN] Limpiando temporales en dispositivo...
"%ADB%" -s %DEVICE% shell rm /sdcard/cap01.png /sdcard/cap02.png /sdcard/cap03.png /sdcard/cap04.png /sdcard/cap05.png >nul 2>&1

echo.
echo === Capturas finalizadas en: %DEST% ===
dir "%DEST%\*.png" 2>nul
exit /b 0
