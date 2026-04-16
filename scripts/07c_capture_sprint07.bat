@echo off
setlocal enabledelayedexpansion

:: ============================================================
::  07c_capture_sprint07.bat
::  Captura 10 pantallas de la app GeoFoto Android (Sprint 07)
::  y genera reporte_e2e.txt con resultado por pantalla.
::
::  Requisito: dispositivo ZY32GSJ88S conectado por ADB
::  Resultado:  test-results\screenshots\sprint07\
::              test-results\reporte_e2e.txt
:: ============================================================

set ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
set DEVICE=ZY32GSJ88S
set DEST=%~dp0..\test-results\screenshots\sprint07
set REPORT=%~dp0..\test-results\reporte_e2e.txt
set OK=0
set FAIL=0

if not exist "%DEST%" mkdir "%DEST%"

:: Verificar dispositivo
"%ADB%" -s %DEVICE% get-state >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Dispositivo %DEVICE% no conectado.
    exit /b 1
)
echo [ADB] Dispositivo conectado: %DEVICE%
echo.

:: ── Helper: capturar pantalla ────────────────────────────────
:: Uso: CALL :CAPTURE <n> <nombre> <descripcion>
goto :main

:CAPTURE
    set N=%~1
    set FILE=e2e_%~1_%~2.png
    set DESC=%~3
    echo [CAP %N%/10] %DESC%...
    "%ADB%" -s %DEVICE% shell screencap -p /sdcard/cap_sprint07_%N%.png
    timeout /t 1 /nobreak >nul
    "%ADB%" -s %DEVICE% pull /sdcard/cap_sprint07_%N%.png "%DEST%\%FILE%" >nul 2>&1
    "%ADB%" -s %DEVICE% shell rm /sdcard/cap_sprint07_%N%.png >nul 2>&1
    if exist "%DEST%\%FILE%" (
        echo   [OK]   %FILE%
        set /a OK+=1
        echo OK    %N%  %DESC% >> "%REPORT%.tmp"
    ) else (
        echo   [FAIL] %FILE% — no se pudo capturar
        set /a FAIL+=1
        echo FAIL  %N%  %DESC% >> "%REPORT%.tmp"
    )
    goto :eof

:: ── Main ─────────────────────────────────────────────────────
:main

if exist "%REPORT%.tmp" del "%REPORT%.tmp"
echo. > "%REPORT%.tmp"

echo GeoFoto — Reporte E2E Sprint 07 > "%REPORT%"
echo Fecha: %DATE% %TIME% >> "%REPORT%"
echo Dispositivo: %DEVICE% >> "%REPORT%"
echo ============================================ >> "%REPORT%"
echo. >> "%REPORT%"

:: e2e_01 — Mapa visible con tiles OSM
:: La app abre directo al mapa
CALL :CAPTURE 01 mapa_inicial "Mapa visible con tiles OSM"
timeout /t 3 /nobreak >nul

:: e2e_02 — FAB GPS: tocar botón centrar → marcador posición propia aparece
:: FAB GPS está en la esquina inferior derecha ~(950,1750)
"%ADB%" -s %DEVICE% shell input tap 950 1750
timeout /t 5 /nobreak >nul
CALL :CAPTURE 02 marcador_gps "Marcador posicion propia (circulo azul)"
timeout /t 2 /nobreak >nul

:: e2e_03 — Mapa centrado tras GPS (zoom nivel 15-16)
CALL :CAPTURE 03 mapa_centrado_gps "Mapa centrado post-GPS"
timeout /t 2 /nobreak >nul

:: e2e_04 — Marker de punto existente visible en el mapa
:: Navegar a /mapa con markers — ya deberían verse si hay datos
CALL :CAPTURE 04 marker_existente "Marker de punto existente visible"
timeout /t 2 /nobreak >nul

:: e2e_05 — Abrir popup del marker (tap en el primer marker visible ~centro pantalla)
"%ADB%" -s %DEVICE% shell input tap 540 960
timeout /t 2 /nobreak >nul
CALL :CAPTURE 05 popup_marker "Popup del marker abierto"
timeout /t 2 /nobreak >nul

:: e2e_06 — Carrusel de fotos visible dentro del popup
CALL :CAPTURE 06 carrusel_fotos "Carrusel de fotos visible en popup"
timeout /t 2 /nobreak >nul

:: e2e_07 — Ampliar foto fullscreen (tap en la imagen del carrusel)
"%ADB%" -s %DEVICE% shell input tap 540 400
timeout /t 2 /nobreak >nul
CALL :CAPTURE 07 foto_fullscreen "Foto ampliada fullscreen (FotoViewer)"
timeout /t 2 /nobreak >nul

:: e2e_08 — Lista de markers (cerrar visor y navegar a /lista)
"%ADB%" -s %DEVICE% shell input keyevent KEYCODE_BACK
timeout /t 1 /nobreak >nul
"%ADB%" -s %DEVICE% shell input keyevent KEYCODE_BACK
timeout /t 1 /nobreak >nul
:: Abrir drawer de navegación (swipe desde borde izquierdo)
"%ADB%" -s %DEVICE% shell input swipe 0 960 350 960 300
timeout /t 2 /nobreak >nul
:: Tap en "Lista" (tercer item del menú, aprox y=500)
"%ADB%" -s %DEVICE% shell input tap 200 500
timeout /t 3 /nobreak >nul
CALL :CAPTURE 08 lista_markers "Lista de markers con busqueda"
timeout /t 2 /nobreak >nul

:: e2e_09 — Pantalla de sincronización
"%ADB%" -s %DEVICE% shell input swipe 0 960 350 960 300
timeout /t 2 /nobreak >nul
:: Tap en "Sincronización" (cuarto item del menú, aprox y=600)
"%ADB%" -s %DEVICE% shell input tap 200 600
timeout /t 3 /nobreak >nul
CALL :CAPTURE 09 pantalla_sync "Pantalla de sincronizacion (EstadoSync)"
timeout /t 2 /nobreak >nul

:: e2e_10 — Badge verde synced (volver al mapa, ver chip verde)
"%ADB%" -s %DEVICE% shell input swipe 0 960 350 960 300
timeout /t 2 /nobreak >nul
"%ADB%" -s %DEVICE% shell input tap 200 300
timeout /t 3 /nobreak >nul
CALL :CAPTURE 10 badge_synced "Badge verde Synced en marker"

:: ── Generar reporte ──────────────────────────────────────────

echo. >> "%REPORT%"
echo Resultados por pantalla: >> "%REPORT%"
echo ---------------------------------------- >> "%REPORT%"
type "%REPORT%.tmp" >> "%REPORT%"
del "%REPORT%.tmp"

echo. >> "%REPORT%"
echo RESUMEN: >> "%REPORT%"
echo   OK:    %OK% / 10 >> "%REPORT%"
echo   FAIL:  %FAIL% / 10 >> "%REPORT%"
echo. >> "%REPORT%"

if %FAIL% equ 0 (
    echo   RESULTADO: SUCCESS — 10/10 capturas OK >> "%REPORT%"
    echo.
    echo  ########################################################
    echo   SUCCESS — 10/10 capturas OK
    echo  ########################################################
) else (
    echo   RESULTADO: PARCIAL — %OK%/10 OK, %FAIL% FALLIDAS >> "%REPORT%"
    echo.
    echo  ########################################################
    echo   PARCIAL — %OK%/10 OK, %FAIL% fallidas
    echo  ########################################################
)

echo.
echo   Capturas: %DEST%
echo   Reporte:  %REPORT%
echo.
type "%REPORT%"

pause
exit /b %FAIL%
