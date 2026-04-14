@echo off
setlocal enabledelayedexpansion
title GeoFoto — Demo Completa v1.0
color 0A

set ADB=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
set APP_ID=com.companyname.geofoto.mobile
set PC_IP=192.168.1.212
set API_PORT=5000
set WEB_PORT=5001
set MAX_INTENTOS_API=5
set MAX_INTENTOS_APP=3

echo.
echo   ####################################################
echo   #                                                  #
echo   #   GEOFOTO v1.0 — DEMO COMPLETA                  #
echo   #                                                  #
echo   ####################################################
echo.

:: -------------------------------------------------------
:: FASE 1: Verificaciones previas
:: -------------------------------------------------------
echo   [FASE 1/6] Verificaciones previas...

dotnet --version >nul 2>&1
if errorlevel 1 ( echo     [FATAL] .NET SDK no encontrado & pause & exit /b 1 )
echo     [OK] .NET SDK

if not exist "GeoFoto.Api\wwwroot\uploads" mkdir "GeoFoto.Api\wwwroot\uploads"
echo     [OK] Carpeta uploads

:: Verificar ADB
if not exist "%ADB%" (
    echo     [WARN] adb no encontrado en %ADB%
    echo            Se omitira el deploy en Android
    set SKIP_ANDROID=1
) else (
    echo     [OK] adb encontrado
    set SKIP_ANDROID=0
)

:: Liberar puertos si están ocupados
for /f "tokens=5" %%a in ('netstat -ano 2^>nul ^| findstr ":%API_PORT% "') do (
    taskkill /PID %%a /F >nul 2>&1
)
for /f "tokens=5" %%a in ('netstat -ano 2^>nul ^| findstr ":%WEB_PORT% "') do (
    taskkill /PID %%a /F >nul 2>&1
)
echo     [OK] Puertos %API_PORT% y %WEB_PORT% liberados

:: -------------------------------------------------------
:: FASE 2: Base de datos
:: -------------------------------------------------------
echo.
echo   [FASE 2/6] Preparando base de datos...

dotnet ef database update --project GeoFoto.Api\GeoFoto.Api.csproj
if errorlevel 1 (
    echo     [INFO] Creando migration inicial...
    dotnet ef migrations add InitialCreate --project GeoFoto.Api\GeoFoto.Api.csproj >nul 2>&1
    dotnet ef database update --project GeoFoto.Api\GeoFoto.Api.csproj
    if errorlevel 1 ( echo     [FATAL] Base de datos no disponible. Verificá SQL Server. & pause & exit /b 1 )
)
echo     [OK] GeoFotoDB lista

:: -------------------------------------------------------
:: FASE 3: Build y compilacion
:: -------------------------------------------------------
echo.
echo   [FASE 3/6] Compilando proyectos...

dotnet build GeoFoto.Api\GeoFoto.Api.csproj --configuration Debug --verbosity quiet
if errorlevel 1 ( echo     [FATAL] GeoFoto.Api no compila & pause & exit /b 1 )
echo     [OK] GeoFoto.Api

dotnet build GeoFoto.Web\GeoFoto.Web.csproj --configuration Debug --verbosity quiet
if errorlevel 1 ( echo     [FATAL] GeoFoto.Web no compila & pause & exit /b 1 )
echo     [OK] GeoFoto.Web

if "%SKIP_ANDROID%"=="0" (
    echo     [INFO] Compilando APK Android (puede tardar)...
    dotnet build GeoFoto.Mobile\GeoFoto.Mobile.csproj ^
        --configuration Debug --framework net10.0-android --verbosity quiet
    if errorlevel 1 (
        echo     [WARN] GeoFoto.Mobile no compilo. Se omite deploy Android.
        set SKIP_ANDROID=1
    ) else (
        echo     [OK] GeoFoto.Mobile
    )
)

:: -------------------------------------------------------
:: FASE 4: Lanzar servicios
:: -------------------------------------------------------
echo.
echo   [FASE 4/6] Lanzando servicios...

:: Lanzar API en ventana separada
start "GeoFoto.Api" cmd /k "title GeoFoto.Api && cd /d %~dp0GeoFoto.Api && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --configuration Debug --urls http://0.0.0.0:%API_PORT%"
echo     [OK] GeoFoto.Api iniciando en puerto %API_PORT%...

:: Esperar con reintentos hasta que la API responda
set API_OK=0
for /l %%i in (1,1,%MAX_INTENTOS_API%) do (
    if "!API_OK!"=="0" (
        timeout /t 5 /nobreak >nul
        curl -s -o nul -w "%%{http_code}" http://localhost:%API_PORT%/api/puntos 2>nul | findstr "200" >nul
        if not errorlevel 1 (
            set API_OK=1
            echo     [OK] API respondiendo en http://localhost:%API_PORT%
        ) else (
            echo     [INFO] Esperando API... intento %%i/%MAX_INTENTOS_API%
        )
    )
)

if "!API_OK!"=="0" (
    echo.
    echo     [ERROR] La API no responde despues de %MAX_INTENTOS_API% intentos.
    echo             Verificá la ventana "GeoFoto.Api" para ver el error.
    echo             Presiona Enter cuando este lista para continuar...
    pause
)

:: Lanzar Web
start "GeoFoto.Web" cmd /k "title GeoFoto.Web && cd /d %~dp0GeoFoto.Web && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --configuration Debug --urls http://localhost:%WEB_PORT%"
echo     [OK] GeoFoto.Web iniciando en puerto %WEB_PORT%...
timeout /t 8 /nobreak >nul

:: Verificar Web
curl -s -o nul -w "%%{http_code}" http://localhost:%WEB_PORT% 2>nul | findstr "200 302" >nul
if errorlevel 1 (
    echo     [WARN] Web no responde aun. Continua de todas formas.
) else (
    echo     [OK] Web respondiendo en http://localhost:%WEB_PORT%
)

:: Regla de firewall para que el celular acceda a la API
echo     [INFO] Abriendo puerto %API_PORT% en el firewall de Windows...
netsh advfirewall firewall add rule name="GeoFoto API %API_PORT%" ^
    dir=in action=allow protocol=TCP localport=%API_PORT% profile=private >nul 2>&1
echo     [OK] Regla de firewall configurada

:: -------------------------------------------------------
:: FASE 5: Deploy Android
:: -------------------------------------------------------
echo.
echo   [FASE 5/6] Deploy en Android...

if "%SKIP_ANDROID%"=="1" (
    echo     [SKIP] Deploy Android omitido (sin ADB o APK)
    goto :post_android
)

:: Resetear ADB
"%ADB%" kill-server >nul 2>&1
timeout /t 2 /nobreak >nul
"%ADB%" start-server >nul 2>&1
timeout /t 3 /nobreak >nul

:: Detectar dispositivo con reintentos
set DEVICE_ID=
for /l %%i in (1,1,3) do (
    if "!DEVICE_ID!"=="" (
        for /f "skip=1 tokens=1,2" %%a in ('"%ADB%" devices 2^>nul') do (
            if "%%b"=="device" set DEVICE_ID=%%a
        )
        if "!DEVICE_ID!"=="" (
            echo     [INFO] Esperando dispositivo Android... intento %%i/3
            timeout /t 5 /nobreak >nul
        )
    )
)

if "!DEVICE_ID!"=="" (
    echo     [WARN] No se detecto dispositivo Android. Omitiendo deploy.
    set SKIP_ANDROID=1
    goto :post_android
)

echo     [OK] Dispositivo: !DEVICE_ID!

:: Configurar tunel
"%ADB%" -s !DEVICE_ID! reverse tcp:%API_PORT% tcp:%API_PORT% >nul 2>&1
echo     [OK] Tunel adb reverse activo

:: Buscar APK
set APK_PATH=
for /r "GeoFoto.Mobile\bin\Debug\net10.0-android" %%f in (*-Signed.apk) do (
    set APK_PATH=%%f
)

if "!APK_PATH!"=="" (
    echo     [ERROR] APK no encontrado. Verifica la compilacion del Mobile.
    set SKIP_ANDROID=1
    goto :post_android
)

:: Desinstalar + instalar con reintentos
set INSTALL_OK=0
for /l %%i in (1,1,%MAX_INTENTOS_APP%) do (
    if "!INSTALL_OK!"=="0" (
        echo     [INFO] Instalando APK intento %%i/%MAX_INTENTOS_APP%...
        "%ADB%" -s !DEVICE_ID! uninstall %APP_ID% >nul 2>&1
        "%ADB%" -s !DEVICE_ID! install -r "!APK_PATH!" >nul 2>&1
        if not errorlevel 1 (
            set INSTALL_OK=1
            echo     [OK] APK instalada correctamente
        ) else (
            echo     [WARN] Instalacion fallo en intento %%i. Reintentando...
            timeout /t 3 /nobreak >nul
        )
    )
)

if "!INSTALL_OK!"=="0" (
    echo     [ERROR] No se pudo instalar la APK despues de %MAX_INTENTOS_APP% intentos.
) else (
    :: Lanzar app y verificar
    timeout /t 2 /nobreak >nul
    "%ADB%" -s !DEVICE_ID! shell monkey -p %APP_ID% -c android.intent.category.LAUNCHER 1 >nul 2>&1
    echo     [OK] App lanzada en el celular

    timeout /t 10 /nobreak >nul

    :: Verificar proceso
    "%ADB%" -s !DEVICE_ID! shell ps -A 2>nul | findstr /i "geofoto" >nul
    if errorlevel 1 (
        echo     [WARN] Proceso no detectado. Capturando logcat...
        "%ADB%" -s !DEVICE_ID! logcat -d 2>nul | findstr /i "AndroidRuntime fatal crash" > logcat_crash.txt
        type logcat_crash.txt
        echo     Revisando la pantalla...
    ) else (
        echo     [OK] Proceso GeoFoto corriendo en el celular
    )

    :: Screenshot de verificacion
    "%ADB%" -s !DEVICE_ID! shell screencap -p /sdcard/demo_final.png >nul 2>&1
    "%ADB%" -s !DEVICE_ID! pull /sdcard/demo_final.png demo_final_screenshot.png >nul 2>&1
    if exist "demo_final_screenshot.png" (
        start "" "demo_final_screenshot.png"
        echo     [OK] Screenshot guardado: demo_final_screenshot.png
    )
)

:post_android

:: -------------------------------------------------------
:: FASE 6: Verificacion final y apertura de browsers
:: -------------------------------------------------------
echo.
echo   [FASE 6/6] Verificacion final...

:: Test rapido de la API
curl -s http://localhost:%API_PORT%/api/puntos >nul 2>&1
if errorlevel 1 (
    echo     [WARN] API no responde en /api/puntos
) else (
    echo     [OK] API: GET /api/puntos responde
)

:: Abrir browsers
echo     [INFO] Abriendo navegadores...
timeout /t 2 /nobreak >nul
start "" "http://localhost:%API_PORT%/swagger"
timeout /t 1 /nobreak >nul
start "" "http://localhost:%WEB_PORT%"

echo.
echo   ####################################################
echo   #                                                  #
echo   #   DEMO GEOFOTO v1.0 LISTA                       #
echo   #                                                  #
echo   #   API:     http://localhost:%API_PORT%            #
echo   #   Swagger: http://localhost:%API_PORT%/swagger    #
echo   #   Web:     http://localhost:%WEB_PORT%            #
if not "%SKIP_ANDROID%"=="1" (
echo   #   Mobile:  Corriendo en !DEVICE_ID!             #
)
echo   #                                                  #
echo   #   Para detener: ejecuta 07_stop_all.bat         #
echo   ####################################################
echo.
echo   [INFO] Ejecutando tests rapidos de humo...
curl -s http://localhost:%API_PORT%/api/puntos >nul 2>&1
if not errorlevel 1 (
    echo     [OK] API health check: OK
) else (
    echo     [WARN] API no responde en /api/puntos
)

:: Preguntar si hacer commit/push
echo.
set /p DO_COMMIT="   Hacer commit y push del trabajo? (S/N): "
if /i "!DO_COMMIT!"=="S" (
    call 08_git_commit_push.bat
)

echo   CHECKLIST DE DEMO MANUAL:
echo.
echo   WEB:
echo   [ ] Ir a /subir y subir una foto JPG con GPS
echo   [ ] Ver el marker en el mapa
echo   [ ] Click en el marker - ver el popup con foto
echo   [ ] Editar nombre/descripcion - guardar
echo   [ ] Ir a /lista - ver la tabla con el punto
echo   [ ] Eliminar el punto - confirmar
echo.
if not "%SKIP_ANDROID%"=="1" (
echo   ANDROID:
echo   [ ] Ver mapa en el celular con markers
echo   [ ] Click en marker - ver detalle
echo   [ ] Ir a /subir o usar boton de camara
echo   [ ] Poner modo avion - subir foto
echo   [ ] Badge muestra 1 pendiente
echo   [ ] Desactivar modo avion - sync automatico
echo   [ ] Badge vuelve a 0
echo.
)
pause
