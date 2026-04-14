@echo off
setlocal enabledelayedexpansion
title GeoFoto — Instalando en Android
color 0D

set ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
set APP_ID=com.companyname.geofoto.mobile

echo.
echo  =====================================================
echo   GeoFoto.Mobile — Deploy en Android
echo  =====================================================
echo.

:: Navegar al directorio raiz del repositorio
cd /d "%~dp0.."

:: Reiniciar ADB
echo   [INFO] Reiniciando servidor ADB...
"%ADB%" kill-server >nul 2>&1
timeout /t 2 /nobreak >nul
"%ADB%" start-server >nul 2>&1
timeout /t 3 /nobreak >nul

:: Detectar dispositivo
echo   [INFO] Detectando dispositivo...
"%ADB%" devices

set DEVICE_ID=
for /f "skip=1 tokens=1,2" %%a in ('"%ADB%" devices') do (
    if "%%b"=="device" set DEVICE_ID=%%a
)

if "!DEVICE_ID!"=="" (
    echo.
    echo   [ERROR] No se detecto ningun dispositivo Android.
    echo          Verificá USB debugging y acepta el aviso en el celular.
    "%ADB%" devices
    pause & exit /b 1
)

echo   [OK] Dispositivo: !DEVICE_ID!
"%ADB%" -s !DEVICE_ID! shell getprop ro.product.model
echo.

:: Configurar tunel adb reverse (celular accede a PC por USB)
echo   [INFO] Configurando tunel adb reverse tcp:5000...
"%ADB%" -s !DEVICE_ID! reverse tcp:5000 tcp:5000
if errorlevel 1 (
    echo   [WARN] adb reverse fallo. Intentando con IP directa...
    echo          (Configurá la URL en MauiProgram.cs si estan en la misma red WiFi)
)
echo   [OK] Tunel configurado

:: Buscar APK más reciente
set APK_PATH=
for /r "src\GeoFoto.Mobile\bin\Debug\net10.0-android" %%f in (*-Signed.apk) do (
    set APK_PATH=%%f
)

if "!APK_PATH!"=="" (
    echo   [ERROR] No se encontro el APK. Ejecuta primero: 03_build_android.bat
    pause & exit /b 1
)

echo   [INFO] APK encontrado: !APK_PATH!

:: Desinstalar version anterior
echo   [INFO] Desinstalando version anterior...
"%ADB%" -s !DEVICE_ID! uninstall %APP_ID% >nul 2>&1

:: Instalar
echo   [INFO] Instalando APK (puede tardar 30-60s)...
"%ADB%" -s !DEVICE_ID! install -r "!APK_PATH!"
if errorlevel 1 (
    echo   [INFO] Reintentando con flag -d (downgrade)...
    "%ADB%" -s !DEVICE_ID! install -r -d "!APK_PATH!"
    if errorlevel 1 (
        echo   [ERROR] Instalacion fallida. Verificá que la app no este abierta.
        pause & exit /b 1
    )
)
echo   [OK] APK instalada correctamente

:: Lanzar la app
echo   [INFO] Lanzando la app en el celular...
timeout /t 2 /nobreak >nul
"%ADB%" -s !DEVICE_ID! shell monkey -p %APP_ID% -c android.intent.category.LAUNCHER 1 >nul 2>&1
echo   [OK] App lanzada

:: Verificar que el proceso esta corriendo
timeout /t 8 /nobreak >nul
"%ADB%" -s !DEVICE_ID! shell ps -A 2>nul | findstr /i "GeoFoto" >nul
if errorlevel 1 (
    echo   [WARN] El proceso no aparece en ps. Verificando logcat...
) else (
    echo   [OK] Proceso GeoFoto corriendo en el celular
)

:: Capturar screenshot para verificacion visual
echo   [INFO] Capturando screenshot...
"%ADB%" -s !DEVICE_ID! shell screencap -p /sdcard/geofoto_verify.png >nul 2>&1
"%ADB%" -s !DEVICE_ID! pull /sdcard/geofoto_verify.png geofoto_screenshot.png >nul 2>&1
if exist "geofoto_screenshot.png" (
    echo   [OK] Screenshot guardado: geofoto_screenshot.png
    start "" "geofoto_screenshot.png"
)

echo.
echo  =====================================================
echo   App instalada. Verificá la pantalla del celular.
echo   Si ves pantalla en blanco, ejecuta: 04b_fix_android.bat
echo  =====================================================
pause
