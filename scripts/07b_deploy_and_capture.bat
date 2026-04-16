@echo off
setlocal enabledelayedexpansion
title GeoFoto — Deploy Android + Capturas
color 0D

set ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
set APP_ID=com.companyname.geofoto.mobile
set SCREENSHOTS_DIR=%~dp0..\test-results\screenshots

echo.
echo  =====================================================
echo   GeoFoto — Deploy Android + Capturas de pantalla
echo  =====================================================
echo.

:: Crear directorio de capturas
if not exist "%SCREENSHOTS_DIR%" mkdir "%SCREENSHOTS_DIR%"

:: -------------------------------------------------------
:: PASO 1: Build
:: -------------------------------------------------------
echo   [PASO 1/5] Compilando APK Android...
call "%~dp0\03_build_android.bat"
if errorlevel 1 (
    echo   [ERROR] Build fallido. Abortando.
    pause & exit /b 1
)

:: -------------------------------------------------------
:: PASO 2: Instalar en dispositivo
:: -------------------------------------------------------
echo   [PASO 2/5] Instalando en dispositivo...
call "%~dp0\04_install_android.bat"
if errorlevel 1 (
    echo   [ERROR] Instalacion fallida. Abortando.
    pause & exit /b 1
)

:: -------------------------------------------------------
:: PASO 3: Verificar dispositivo y obtener info
:: -------------------------------------------------------
echo   [PASO 3/5] Verificando dispositivo conectado...

:: Reiniciar ADB para asegurar conexion limpia
"%ADB%" kill-server >nul 2>&1
timeout /t 2 /nobreak >nul
"%ADB%" start-server >nul 2>&1
timeout /t 3 /nobreak >nul

set DEVICE_ID=
for /f "skip=1 tokens=1,2" %%a in ('"%ADB%" devices') do (
    if "%%b"=="device" set DEVICE_ID=%%a
)

if "!DEVICE_ID!"=="" (
    echo   [ERROR] No hay dispositivo Android conectado por USB.
    echo          Verifica USB debugging y acepta el aviso en el celular.
    pause & exit /b 1
)

echo   [OK] Dispositivo: !DEVICE_ID!
for /f "delims=" %%m in ('"%ADB%" -s !DEVICE_ID! shell getprop ro.product.model 2^>nul') do echo   [OK] Modelo: %%m

:: Configurar tunel para que el celular acceda a la API en la PC
echo   [INFO] Configurando tunel adb reverse tcp:5000 -> localhost:5000 ...
"%ADB%" -s !DEVICE_ID! reverse tcp:5000 tcp:5000 >nul 2>&1
if errorlevel 1 (
    echo   [WARN] adb reverse fallo. La app usara localhost:5000 de la PC si esta en la misma red.
) else (
    echo   [OK] Tunel configurado.
)

:: -------------------------------------------------------
:: PASO 4: Lanzar app y esperar inicio
:: -------------------------------------------------------
echo   [PASO 4/5] Lanzando app en el celular...
"%ADB%" -s !DEVICE_ID! shell monkey -p %APP_ID% -c android.intent.category.LAUNCHER 1 >nul 2>&1
echo   [INFO] Esperando que la app cargue (10s)...
timeout /t 10 /nobreak >nul

:: Verificar proceso corriendo
"%ADB%" -s !DEVICE_ID! shell ps -A 2>nul | findstr /i "geofoto" >nul
if errorlevel 1 (
    echo   [WARN] Proceso no detectado en ps. Continuando de todas formas...
) else (
    echo   [OK] Proceso GeoFoto detectado en el celular.
)

:: -------------------------------------------------------
:: PASO 5: Capturas de pantalla por flujo
:: -------------------------------------------------------
echo   [PASO 5/5] Capturando pantallas del flujo principal...
echo.

:: Captura 01 — Pantalla inicial / Login
echo   [CAP 1/5] Pantalla de inicio...
"%ADB%" -s !DEVICE_ID! shell screencap -p /sdcard/geofoto_cap_01_inicio.png >nul 2>&1
"%ADB%" -s !DEVICE_ID! pull /sdcard/geofoto_cap_01_inicio.png "%SCREENSHOTS_DIR%\01_inicio.png" >nul 2>&1
if exist "%SCREENSHOTS_DIR%\01_inicio.png" (echo   [OK] 01_inicio.png) else (echo   [FALTA] 01_inicio.png)

timeout /t 2 /nobreak >nul

:: Captura 02 — Pantalla principal / Mapa (tap centro-pantalla)
echo   [CAP 2/5] Navegando a pantalla de mapa (tap)...
"%ADB%" -s !DEVICE_ID! shell input tap 540 960 >nul 2>&1
timeout /t 3 /nobreak >nul
"%ADB%" -s !DEVICE_ID! shell screencap -p /sdcard/geofoto_cap_02_mapa.png >nul 2>&1
"%ADB%" -s !DEVICE_ID! pull /sdcard/geofoto_cap_02_mapa.png "%SCREENSHOTS_DIR%\02_mapa.png" >nul 2>&1
if exist "%SCREENSHOTS_DIR%\02_mapa.png" (echo   [OK] 02_mapa.png) else (echo   [FALTA] 02_mapa.png)

timeout /t 2 /nobreak >nul

:: Captura 03 — Flujo de nueva foto (boton FAB / camara)
echo   [CAP 3/5] Navegando a camara (tap boton nueva foto)...
"%ADB%" -s !DEVICE_ID! shell input tap 900 1700 >nul 2>&1
timeout /t 3 /nobreak >nul
"%ADB%" -s !DEVICE_ID! shell screencap -p /sdcard/geofoto_cap_03_camara.png >nul 2>&1
"%ADB%" -s !DEVICE_ID! pull /sdcard/geofoto_cap_03_camara.png "%SCREENSHOTS_DIR%\03_camara.png" >nul 2>&1
if exist "%SCREENSHOTS_DIR%\03_camara.png" (echo   [OK] 03_camara.png) else (echo   [FALTA] 03_camara.png)

:: Volver atras
"%ADB%" -s !DEVICE_ID! shell input keyevent KEYCODE_BACK >nul 2>&1
timeout /t 2 /nobreak >nul

:: Captura 04 — Lista de fotos guardadas
echo   [CAP 4/5] Navegando a lista de fotos...
"%ADB%" -s !DEVICE_ID! shell input tap 540 100 >nul 2>&1
timeout /t 2 /nobreak >nul
"%ADB%" -s !DEVICE_ID! shell screencap -p /sdcard/geofoto_cap_04_lista.png >nul 2>&1
"%ADB%" -s !DEVICE_ID! pull /sdcard/geofoto_cap_04_lista.png "%SCREENSHOTS_DIR%\04_lista_fotos.png" >nul 2>&1
if exist "%SCREENSHOTS_DIR%\04_lista_fotos.png" (echo   [OK] 04_lista_fotos.png) else (echo   [FALTA] 04_lista_fotos.png)

timeout /t 2 /nobreak >nul

:: Captura 05 — Pantalla de sincronizacion / estado offline
echo   [CAP 5/5] Pantalla de sincronizacion...
"%ADB%" -s !DEVICE_ID! shell input tap 540 200 >nul 2>&1
timeout /t 2 /nobreak >nul
"%ADB%" -s !DEVICE_ID! shell screencap -p /sdcard/geofoto_cap_05_sync.png >nul 2>&1
"%ADB%" -s !DEVICE_ID! pull /sdcard/geofoto_cap_05_sync.png "%SCREENSHOTS_DIR%\05_sync.png" >nul 2>&1
if exist "%SCREENSHOTS_DIR%\05_sync.png" (echo   [OK] 05_sync.png) else (echo   [FALTA] 05_sync.png)

:: Limpiar archivos temporales en el celular
"%ADB%" -s !DEVICE_ID! shell rm /sdcard/geofoto_cap_*.png >nul 2>&1

echo.
echo  =====================================================
echo   Capturas guardadas en:
echo   %SCREENSHOTS_DIR%
echo  =====================================================
echo.
echo   Continua con: 08_validate_screenshots.bat
echo.
