@echo off
setlocal enabledelayedexpansion
title GeoFoto ? Full Rebuild
color 0A

:: ============================================================
::  GEOFOTO ? ORQUESTADOR COMPLETO DE BUILD, DEPLOY Y LAUNCH
::  Archivo: scripts/07_full_rebuild.bat
::  Uso: ejecutar desde cualquier ubicacion. Es idempotente.
::
::  NOTA CMD: no usar %VAR% con parens (ej. (x86)) dentro de
::  bloques compuestos if (...) porque el ) cierra el bloque.
::  Se usan variables intermedias y set "VAR=val" para esto.
:: ============================================================

:: Posicionarse en la raiz del repositorio
cd /d "%~dp0.."

:: -------------------------------------------------------
:: CONFIGURACION GLOBAL
:: -------------------------------------------------------
set "ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
set "APP_ID=com.companyname.geofoto.mobile"
set "API_PORT=5000"
set "WEB_PORT=5001"
set "MAX_INTENTOS_API=5"
set "MAX_INTENTOS_APP=3"

set "STATUS_API=PENDIENTE"
set "STATUS_WEB=PENDIENTE"
set "STATUS_MOBILE=PENDIENTE"
set "STATUS_BD=PENDIENTE"
set "SKIP_ANDROID=0"
set "API_OK=0"
set "DEVICE_ID="

echo.
echo   ####################################################
echo   #                                                  #
echo   #   GEOFOTO v1.0 - REBUILD COMPLETO               #
echo   #                                                  #
echo   ####################################################
echo.

:: ===========================================================
::  FASE 1 - Verificaciones previas
:: ===========================================================
echo   ====================================================
echo   [FASE 1/7] Verificaciones previas
echo   ====================================================
echo.

dotnet --version >nul 2>&1
if errorlevel 1 (
    echo     [ERROR] .NET SDK no encontrado
    pause & exit /b 1
)
echo     [OK] .NET SDK instalado

dotnet ef --version >nul 2>&1
if errorlevel 1 (
    echo     [INFO] EF Core Tools no encontrado. Instalando...
    dotnet tool install --global dotnet-ef >nul 2>&1
    dotnet ef --version >nul 2>&1
    if errorlevel 1 (
        echo     [ERROR] No se pudo instalar EF Core Tools
        pause & exit /b 1
    )
    echo     [OK] EF Core Tools instalado
) else (
    echo     [OK] EF Core Tools disponibles
)

:: ADB: usar adb version en vez de if exist con ruta (x86)
set "ADB_EXISTS=0"
"%ADB%" version >nul 2>&1
if not errorlevel 1 set "ADB_EXISTS=1"
if "!ADB_EXISTS!"=="0" (
    echo     [WARN] ADB no encontrado. Se omitira deploy Android.
    set "SKIP_ANDROID=1"
) else (
    echo     [OK] ADB encontrado
)

echo     [INFO] Liberando puerto %API_PORT%...
for /f "tokens=5" %%a in ('netstat -ano 2^>nul ^| findstr ":%API_PORT% "') do (
    taskkill /PID %%a /F >nul 2>&1
)
echo     [OK] Puerto %API_PORT% liberado

echo     [INFO] Liberando puerto %WEB_PORT%...
for /f "tokens=5" %%a in ('netstat -ano 2^>nul ^| findstr ":%WEB_PORT% "') do (
    taskkill /PID %%a /F >nul 2>&1
)
echo     [OK] Puerto %WEB_PORT% liberado

if not exist "src\GeoFoto.Api\wwwroot\uploads" mkdir "src\GeoFoto.Api\wwwroot\uploads"
echo     [OK] Carpeta uploads lista
echo.

:: ===========================================================
::  FASE 2 - Base de datos
:: ===========================================================
echo   ====================================================
echo   [FASE 2/7] Base de datos
echo   ====================================================
echo.

dotnet ef database update --project src\GeoFoto.Api\GeoFoto.Api.csproj >nul 2>&1
if errorlevel 1 (
    echo     [INFO] Update fallo. Intentando migration inicial...
    dotnet ef migrations add InitialCreate --project src\GeoFoto.Api\GeoFoto.Api.csproj >nul 2>&1
    dotnet ef database update --project src\GeoFoto.Api\GeoFoto.Api.csproj >nul 2>&1
    if errorlevel 1 (
        echo     [WARN] BD no actualizada. La API reportara el error al arrancar.
        set "STATUS_BD=ERROR - migrations fallidas"
    ) else (
        echo     [OK] Migration inicial aplicada
        set "STATUS_BD=OK"
    )
) else (
    echo     [OK] GeoFotoDB actualizada
    set "STATUS_BD=OK"
)
echo.

:: ===========================================================
::  FASE 3 - Compilacion
:: ===========================================================
echo   ====================================================
echo   [FASE 3/7] Compilacion de proyectos
echo   ====================================================
echo.

echo     [INFO] Compilando GeoFoto.Api...
dotnet build src\GeoFoto.Api\GeoFoto.Api.csproj --configuration Debug --verbosity quiet
if errorlevel 1 ( echo     [ERROR] GeoFoto.Api no compila & pause & exit /b 1 )
echo     [OK] GeoFoto.Api

echo     [INFO] Compilando GeoFoto.Shared...
dotnet build src\GeoFoto.Shared\GeoFoto.Shared.csproj --configuration Debug --verbosity quiet
if errorlevel 1 ( echo     [ERROR] GeoFoto.Shared no compila & pause & exit /b 1 )
echo     [OK] GeoFoto.Shared

echo     [INFO] Compilando GeoFoto.Web...
dotnet build src\GeoFoto.Web\GeoFoto.Web.csproj --configuration Debug --verbosity quiet
if errorlevel 1 ( echo     [ERROR] GeoFoto.Web no compila & pause & exit /b 1 )
echo     [OK] GeoFoto.Web

if "!SKIP_ANDROID!"=="0" (
    echo     [INFO] Compilando GeoFoto.Mobile APK - puede tardar 1-3 min...
    dotnet build src\GeoFoto.Mobile\GeoFoto.Mobile.csproj --configuration Debug --framework net10.0-android --verbosity minimal
    if errorlevel 1 (
        echo     [WARN] GeoFoto.Mobile no compilo. Omitiendo deploy Android.
        set "SKIP_ANDROID=1"
        set "STATUS_MOBILE=OMITIDO - build fallo"
    ) else (
        echo     [OK] GeoFoto.Mobile - APK generado
    )
)
echo.

:: ===========================================================
::  FASE 4 - Lanzar API
:: ===========================================================
echo   ====================================================
echo   [FASE 4/7] Lanzar API
echo   ====================================================
echo.

netsh advfirewall firewall delete rule name="GeoFoto API %API_PORT%" >nul 2>&1
netsh advfirewall firewall add rule name="GeoFoto API %API_PORT%" dir=in action=allow protocol=TCP localport=%API_PORT% profile=private >nul 2>&1
echo     [OK] Regla de firewall para puerto %API_PORT% configurada

start "GeoFoto.Api" cmd /k "title GeoFoto.Api && cd /d %~dp0..\src\GeoFoto.Api && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --configuration Debug --urls http://0.0.0.0:%API_PORT% --no-build"
echo     [OK] GeoFoto.Api lanzada en http://0.0.0.0:%API_PORT%

echo     [INFO] Esperando que la API arranque...
set "API_OK=0"
for /l %%i in (1,1,%MAX_INTENTOS_API%) do (
    if "!API_OK!"=="0" (
        timeout /t 5 /nobreak >nul
        curl -s -o nul -w "%%{http_code}" http://localhost:%API_PORT%/api/puntos 2>nul | findstr "200" >nul
        if not errorlevel 1 (
            set "API_OK=1"
            echo     [OK] API respondiendo en http://localhost:%API_PORT%/api/puntos
        ) else (
            echo     [INFO] API no lista... intento %%i/%MAX_INTENTOS_API%
        )
    )
)
if "!API_OK!"=="0" (
    echo     [WARN] API no responde tras %MAX_INTENTOS_API% intentos.
    set "STATUS_API=WARN - no responde, revisa ventana API"
) else (
    set "STATUS_API=OK"
)
echo.

:: ===========================================================
::  FASE 5 - Lanzar Web
:: ===========================================================
echo   ====================================================
echo   [FASE 5/7] Lanzar Web
echo   ====================================================
echo.

start "GeoFoto.Web" cmd /k "title GeoFoto.Web && cd /d %~dp0..\src\GeoFoto.Web && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --configuration Debug --urls http://localhost:%WEB_PORT% --no-build"
echo     [OK] GeoFoto.Web lanzada en http://localhost:%WEB_PORT%

echo     [INFO] Esperando 8 segundos...
timeout /t 8 /nobreak >nul

curl -s -o nul -w "%%{http_code}" http://localhost:%WEB_PORT% 2>nul | findstr "200 302" >nul
if errorlevel 1 (
    echo     [WARN] Web no responde aun.
    set "STATUS_WEB=WARN - no confirmo respuesta"
) else (
    echo     [OK] Web respondiendo en http://localhost:%WEB_PORT%
    set "STATUS_WEB=OK"
)
echo.

:: ===========================================================
::  FASE 6 - Deploy Android
:: ===========================================================
echo   ====================================================
echo   [FASE 6/7] Deploy Android
echo   ====================================================
echo.

if "!SKIP_ANDROID!"=="1" (
    echo     [SKIP] Deploy Android omitido
    if "!STATUS_MOBILE!"=="PENDIENTE" set "STATUS_MOBILE=OMITIDO - sin ADB"
    goto :post_android
)

echo     [INFO] Reiniciando servidor ADB...
"%ADB%" kill-server >nul 2>&1
timeout /t 2 /nobreak >nul
"%ADB%" start-server >nul 2>&1
timeout /t 3 /nobreak >nul
echo     [OK] ADB reiniciado

set "DEVICE_ID="
for /l %%i in (1,1,3) do (
    if "!DEVICE_ID!"=="" (
        for /f "skip=1 tokens=1,2" %%a in ('"%ADB%" devices 2^>nul') do (
            if "%%b"=="device" set "DEVICE_ID=%%a"
        )
        if "!DEVICE_ID!"=="" (
            echo     [INFO] Buscando dispositivo... intento %%i/3
            timeout /t 5 /nobreak >nul
        )
    )
)

if "!DEVICE_ID!"=="" (
    echo     [WARN] Sin dispositivo Android. Verific? USB debugging.
    set "STATUS_MOBILE=OMITIDO - sin dispositivo"
    goto :post_android
)
echo     [OK] Dispositivo: !DEVICE_ID!

"%ADB%" -s !DEVICE_ID! reverse tcp:%API_PORT% tcp:%API_PORT% >nul 2>&1
if errorlevel 1 (
    echo     [WARN] adb reverse fallo. El celular usara WiFi directa.
) else (
    echo     [OK] Tunel adb reverse tcp:%API_PORT% configurado
)

set "APK_PATH="
for /r "src\GeoFoto.Mobile\bin\Debug\net10.0-android" %%f in (*-Signed.apk) do set "APK_PATH=%%f"
if "!APK_PATH!"=="" (
    for /r "src\GeoFoto.Mobile\bin" %%f in (*-Signed.apk) do set "APK_PATH=%%f"
)

if "!APK_PATH!"=="" (
    echo     [ERROR] APK no encontrado.
    set "STATUS_MOBILE=ERROR - APK no encontrado"
    goto :post_android
)
echo     [OK] APK: !APK_PATH!

echo     [INFO] Desinstalando version anterior...
"%ADB%" -s !DEVICE_ID! uninstall %APP_ID% >nul 2>&1

set "INSTALL_OK=0"
for /l %%i in (1,1,%MAX_INTENTOS_APP%) do (
    if "!INSTALL_OK!"=="0" (
        echo     [INFO] Instalando APK intento %%i/%MAX_INTENTOS_APP%...
        "%ADB%" -s !DEVICE_ID! install -r "!APK_PATH!" >nul 2>&1
        if not errorlevel 1 (
            set "INSTALL_OK=1"
            echo     [OK] APK instalada en intento %%i
        ) else (
            echo     [WARN] Instalacion fallo intento %%i. Reintentando...
            timeout /t 3 /nobreak >nul
        )
    )
)

if "!INSTALL_OK!"=="0" (
    echo     [INFO] Reintentando con flag -d...
    "%ADB%" -s !DEVICE_ID! install -r -d "!APK_PATH!" >nul 2>&1
    if not errorlevel 1 set "INSTALL_OK=1"
)

if "!INSTALL_OK!"=="0" (
    echo     [ERROR] No se pudo instalar la APK.
    set "STATUS_MOBILE=ERROR - instalacion fallida"
    goto :post_android
)

echo     [INFO] Limpiando cache WebView...
"%ADB%" -s !DEVICE_ID! shell pm clear %APP_ID% >nul 2>&1
echo     [OK] Cache WebView limpiado

echo     [INFO] Lanzando GeoFoto en el celular...
"%ADB%" -s !DEVICE_ID! shell monkey -p %APP_ID% -c android.intent.category.LAUNCHER 1 >nul 2>&1
echo     [OK] App lanzada

timeout /t 8 /nobreak >nul
"%ADB%" -s !DEVICE_ID! shell ps -A 2>nul | findstr /i "geofoto" >nul
if errorlevel 1 (
    echo     [WARN] Proceso no detectado. Puede haber crasheado.
    "%ADB%" -s !DEVICE_ID! logcat -d 2>nul | findstr /i "AndroidRuntime fatal" > logcat_crash.txt 2>nul
    if exist logcat_crash.txt type logcat_crash.txt
    set "STATUS_MOBILE=WARN - proceso no detectado, ver logcat_crash.txt"
) else (
    echo     [OK] Proceso GeoFoto corriendo en !DEVICE_ID!
    set "STATUS_MOBILE=OK - corriendo en !DEVICE_ID!"
)

:post_android
echo.

:: ===========================================================
::  FASE 7 - Verificacion final y resumen
:: ===========================================================
echo   ====================================================
echo   [FASE 7/7] Verificacion final
echo   ====================================================
echo.

curl -s http://localhost:%API_PORT%/api/puntos >nul 2>&1
if errorlevel 1 (
    echo     [WARN] API no responde en http://localhost:%API_PORT%/api/puntos
    if "!STATUS_API!"=="OK" set "STATUS_API=WARN - no responde al final"
) else (
    echo     [OK] API: GET /api/puntos OK
    set "STATUS_API=OK"
)

curl -s -o nul -w "%%{http_code}" http://localhost:%WEB_PORT% 2>nul | findstr "200 302" >nul
if errorlevel 1 (
    echo     [WARN] Web no responde en http://localhost:%WEB_PORT%
    if "!STATUS_WEB!"=="OK" set "STATUS_WEB=WARN - no responde al final"
) else (
    echo     [OK] Web: http://localhost:%WEB_PORT% OK
    set "STATUS_WEB=OK"
)

echo.
echo     [INFO] Abriendo Swagger y Web en el navegador...
timeout /t 2 /nobreak >nul
start "" "http://localhost:%API_PORT%/swagger"
timeout /t 1 /nobreak >nul
start "" "http://localhost:%WEB_PORT%"

echo.
echo.
echo   ####################################################
echo   #                                                  #
echo   #   GEOFOTO v1.0 - REBUILD COMPLETO               #
echo   #                                                  #
echo   ####################################################
echo.
echo   Componente    Estado
echo   ----------    ------
echo   API           !STATUS_API!
echo   Swagger       http://localhost:%API_PORT%/swagger
echo   Web           !STATUS_WEB!
echo   Mobile        !STATUS_MOBILE!
echo   BD            !STATUS_BD!
echo.
echo   URLs:
echo     API:     http://localhost:%API_PORT%
echo     Swagger: http://localhost:%API_PORT%/swagger
echo     Web:     http://localhost:%WEB_PORT%
echo.
echo   Para detener: cerra las ventanas de API y Web
echo   Para relanzar: ejecuta este script nuevamente
echo.
echo   ####################################################
echo.
endlocal