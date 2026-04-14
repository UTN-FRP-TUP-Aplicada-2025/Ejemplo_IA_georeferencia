@echo off
setlocal enabledelayedexpansion
title GeoFoto — Diagnostico Android
color 0C

set ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
set APP_ID=com.companyname.geofoto.mobile

:: Detectar dispositivo
set DEVICE_ID=
for /f "skip=1 tokens=1,2" %%a in ('"%ADB%" devices') do (
    if "%%b"=="device" set DEVICE_ID=%%a
)

if "!DEVICE_ID!"=="" ( echo [ERROR] Sin dispositivo & pause & exit /b 1 )

echo.
echo  =====================================================
echo   Diagnostico de pantalla en blanco
echo  =====================================================

echo   [INFO] Capturando logcat de los ultimos errores...
"%ADB%" -s !DEVICE_ID! logcat -d 2>&1 | findstr /i "AndroidRuntime fatal crash exception geofoto" > logcat_error.txt
type logcat_error.txt
echo.

echo   [INFO] Verificando version de WebView...
"%ADB%" -s !DEVICE_ID! shell dumpsys package com.google.android.webview 2>nul | findstr "versionName"

echo.
echo   [INFO] Proceso de la app:
"%ADB%" -s !DEVICE_ID! shell ps -A 2>nul | findstr /i "geofoto"

echo.
echo  --- Aplicando fixes conocidos ---

:: Fix 1: Reinstalar con adb reverse activo
echo   [FIX 1] Reactivando tunel adb reverse...
"%ADB%" -s !DEVICE_ID! reverse tcp:5000 tcp:5000

:: Fix 2: Limpiar cache de la app y relanzar
echo   [FIX 2] Limpiando cache de la app...
"%ADB%" -s !DEVICE_ID! shell pm clear %APP_ID% >nul 2>&1
timeout /t 2 /nobreak >nul

:: Fix 3: Relanzar
echo   [FIX 3] Relanzando la app...
"%ADB%" -s !DEVICE_ID! shell monkey -p %APP_ID% -c android.intent.category.LAUNCHER 1 >nul 2>&1
timeout /t 10 /nobreak >nul

:: Capturar screenshot post-fix
"%ADB%" -s !DEVICE_ID! shell screencap -p /sdcard/geofoto_postfix.png >nul 2>&1
"%ADB%" -s !DEVICE_ID! pull /sdcard/geofoto_postfix.png geofoto_postfix.png >nul 2>&1
if exist "geofoto_postfix.png" (
    start "" "geofoto_postfix.png"
    echo   [INFO] Screenshot post-fix guardado: geofoto_postfix.png
)

echo.
echo   Si sigue en blanco: el log en logcat_error.txt tiene el error real.
echo   Pasa el contenido a Copilot para diagnostico especifico.
pause
