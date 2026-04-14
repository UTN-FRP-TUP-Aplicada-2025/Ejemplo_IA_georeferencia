# Prompt para GitHub Copilot — Scripts BAT Demo Final + Tests + Deploy Local

> Pegá este prompt en Copilot Chat con modo **Edits** (`@workspace`).
> El celular ya está conectado por USB en modo desarrollador.
> Todos los dispositivos están en la misma red local.
> Este prompt es la tarea final del Sprint 06 — no termina hasta que todo funcione.

---

## PROMPT

```
Eres un DevOps engineer y desarrollador .NET senior. Tu tarea tiene tres fases:

FASE 1: Reconocimiento del entorno (leer la configuración real del proyecto)
FASE 2: Crear todos los scripts BAT necesarios
FASE 3: Ejecutar los scripts en orden y corregir cualquier error hasta que TODO funcione

No reportes éxito hasta que puedas mostrar:
  - API respondiendo
  - Web accesible en el browser
  - App corriendo en el celular Android
  - Todos los tests en verde

No pidas confirmación entre pasos — ejecutá, detectá errores, corregí y reintentá.

---

## FASE 1 — RECONOCIMIENTO DEL ENTORNO

Ejecutá TODOS estos comandos antes de crear cualquier script.
Los resultados determinarán los valores reales que van en cada script.

### 1.1 — Detectar estructura del proyecto

```powershell
# Verificar que la solución existe
Get-ChildItem "*.slnx","*.sln" | Select-Object Name, FullName

# Verificar los 4 proyectos
@("GeoFoto.Api","GeoFoto.Shared","GeoFoto.Web","GeoFoto.Mobile","GeoFoto.Tests") |
  ForEach-Object { [PSCustomObject]@{ Proyecto=$_; Existe=(Test-Path "$_\$_.csproj") } }
```

### 1.2 — Verificar herramientas

```powershell
# .NET
dotnet --version

# EF Core Tools
dotnet ef --version

# ADB
$adbPaths = @(
    "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
    "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe",
    "C:\Android\platform-tools\adb.exe",
    (Get-Command adb -ErrorAction SilentlyContinue)?.Source
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

if ($adbPaths) { Write-Host "ADB: $adbPaths" } else { Write-Host "ADB: NO ENCONTRADO" }
```

### 1.3 — Detectar el celular

```powershell
$adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
& $adb kill-server; Start-Sleep 2; & $adb start-server; Start-Sleep 2
& $adb devices -l

# Obtener ID del dispositivo
$device = (& $adb devices | Select-String "device$" | Select-Object -First 1) -replace '\s.*',''
Write-Host "Device ID: $device"

# Modelo del celular
if ($device) {
    & $adb -s $device shell getprop ro.product.model
    & $adb -s $device shell getprop ro.build.version.release
}
```

### 1.4 — Obtener IP de la PC en la red local

```powershell
# Obtener todas las IPs de red
Get-NetIPAddress -AddressFamily IPv4 |
  Where-Object { $_.IPAddress -notmatch '^127\.' -and $_.IPAddress -notmatch '^169\.' } |
  Select-Object InterfaceAlias, IPAddress, PrefixLength |
  Format-Table -AutoSize
```

### 1.5 — Obtener IP del celular

```powershell
$adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
$device = (& $adb devices | Select-String "device$" | Select-Object -First 1) -replace '\s.*',''
if ($device) {
    & $adb -s $device shell ip route show | Select-String "wlan"
    & $adb -s $device shell ip addr show wlan0 | Select-String "inet "
}
```

### 1.6 — Verificar puertos disponibles

```powershell
$ports = @(5000, 5001, 7000)
foreach ($p in $ports) {
    $ocupado = netstat -an | Select-String ":$p "
    [PSCustomObject]@{ Puerto=$p; Estado=if($ocupado){"OCUPADO"}else{"LIBRE"} }
} | Format-Table
```

### 1.7 — Verificar SQL Server y connection string

```powershell
# Leer connection string actual
$appSettings = Get-Content "GeoFoto.Api\appsettings.Development.json" -ErrorAction SilentlyContinue |
    ConvertFrom-Json -ErrorAction SilentlyContinue
Write-Host "Connection string actual: $($appSettings.ConnectionStrings.GeoFoto)"

# Probar conexión a SQL Server
try {
    $conn = New-Object System.Data.SqlClient.SqlConnection(
        "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True;")
    $conn.Open()
    Write-Host "SQL Server: ACCESIBLE"
    $conn.Close()
} catch { Write-Host "SQL Server: ERROR — $($_.Exception.Message)" }
```

### 1.8 — Verificar migrations y BD

```powershell
dotnet ef migrations list --project GeoFoto.Api/GeoFoto.Api.csproj 2>&1
```

### 1.9 — Verificar package ID de la app Android

```powershell
Select-String "ApplicationId" "GeoFoto.Mobile\GeoFoto.Mobile.csproj"
```

### 1.10 — Verificar si hay APK compilada

```powershell
Get-ChildItem "GeoFoto.Mobile\bin" -Recurse -Filter "*-Signed.apk" |
    Select-Object FullName, @{N='MB';E={[math]::Round($_.Length/1MB,1)}}, LastWriteTime |
    Sort-Object LastWriteTime -Descending
```

Con toda la información recopilada, construí mentalmente los valores reales:
- Ruta exacta de adb.exe
- ID del dispositivo Android
- IP de la PC en la red local (para configurar CORS y para el celular)
- Puerto libre para la API (preferentemente 5000)
- ApplicationId real de la app
- Connection string que funciona

Reportá un resumen de 5 líneas con los valores encontrados antes de continuar.

---

## FASE 2 — CREAR LOS SCRIPTS BAT

Creá todos los archivos en la raíz del repositorio con los valores reales detectados.
Reemplazá TODOS los placeholders ([IP_PC], [ADB_PATH], [DEVICE_ID], [APP_ID])
con los valores reales del entorno antes de guardar los archivos.

---

### SCRIPT 1 — `00_setup.bat`
Configuración inicial: BD, carpetas, verificaciones. Corre UNA SOLA VEZ.

```batch
@echo off
setlocal enabledelayedexpansion
title GeoFoto — Setup inicial
color 0A

echo.
echo  =====================================================
echo   GeoFoto v1.0 — Setup inicial
echo  =====================================================
echo.

:: Verificar .NET
dotnet --version >nul 2>&1
if errorlevel 1 ( echo [ERROR] .NET SDK no encontrado & pause & exit /b 1 )
for /f "tokens=*" %%v in ('dotnet --version') do echo   [OK] .NET %%v

:: Verificar EF Core Tools
dotnet ef --version >nul 2>&1
if errorlevel 1 (
    echo   [INFO] Instalando EF Core Tools...
    dotnet tool install --global dotnet-ef
    if errorlevel 1 ( echo   [ERROR] No se pudo instalar EF Tools & pause & exit /b 1 )
)
echo   [OK] EF Core Tools

:: Crear carpeta uploads
if not exist "GeoFoto.Api\wwwroot\uploads" (
    mkdir "GeoFoto.Api\wwwroot\uploads"
    echo   [OK] Carpeta uploads creada
) else (
    echo   [OK] Carpeta uploads ya existe
)

:: Verificar y aplicar migrations
echo.
echo   [INFO] Verificando base de datos...
dotnet ef database update --project GeoFoto.Api\GeoFoto.Api.csproj --verbosity quiet
if errorlevel 1 (
    echo   [INFO] Intentando crear migration inicial...
    dotnet ef migrations add InitialCreate --project GeoFoto.Api\GeoFoto.Api.csproj --verbosity quiet
    dotnet ef database update --project GeoFoto.Api\GeoFoto.Api.csproj --verbosity quiet
    if errorlevel 1 ( echo   [ERROR] No se pudo crear la BD. Verificá SQL Server. & pause & exit /b 1 )
)
echo   [OK] Base de datos GeoFotoDB lista

:: Build completo (sin Mobile para no requerir Android SDK aquí)
echo.
echo   [INFO] Compilando solución...
dotnet build GeoFoto.Api\GeoFoto.Api.csproj --configuration Debug --verbosity quiet
if errorlevel 1 ( echo   [ERROR] GeoFoto.Api no compila & pause & exit /b 1 )
echo   [OK] GeoFoto.Api

dotnet build GeoFoto.Shared\GeoFoto.Shared.csproj --configuration Debug --verbosity quiet
if errorlevel 1 ( echo   [ERROR] GeoFoto.Shared no compila & pause & exit /b 1 )
echo   [OK] GeoFoto.Shared

dotnet build GeoFoto.Web\GeoFoto.Web.csproj --configuration Debug --verbosity quiet
if errorlevel 1 ( echo   [ERROR] GeoFoto.Web no compila & pause & exit /b 1 )
echo   [OK] GeoFoto.Web

echo.
echo  =====================================================
echo   Setup completado. Ejecutá: 06_demo_completa.bat
echo  =====================================================
pause
```

---

### SCRIPT 2 — `01_start_api.bat`
Lanza solo la API.

```batch
@echo off
title GeoFoto.Api — Puerto 5000
color 0B
echo   GeoFoto.Api corriendo en http://0.0.0.0:5000
echo   Swagger: http://localhost:5000/swagger
echo   Presiona Ctrl+C para detener
echo.
cd GeoFoto.Api
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --configuration Debug --urls "http://0.0.0.0:5000"
```

---

### SCRIPT 3 — `02_start_web.bat`
Lanza solo el frontend Blazor.

```batch
@echo off
title GeoFoto.Web — Puerto 5001
color 0E
echo   GeoFoto.Web corriendo en http://localhost:5001
echo   Presiona Ctrl+C para detener
echo.
cd GeoFoto.Web
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --configuration Debug
```

---

### SCRIPT 4 — `03_build_android.bat`
Compila la APK con assemblies embebidas.

```batch
@echo off
setlocal enabledelayedexpansion
title GeoFoto — Compilando APK Android
color 0D

echo.
echo  =====================================================
echo   Compilando GeoFoto.Mobile para Android
echo  =====================================================
echo.
echo   Esto puede tardar 5-10 minutos la primera vez.
echo.

dotnet build GeoFoto.Mobile\GeoFoto.Mobile.csproj ^
    --configuration Debug ^
    --framework net10.0-android ^
    --verbosity minimal

if errorlevel 1 (
    echo.
    echo   [ERROR] La compilacion fallo. Revisa los errores arriba.
    pause & exit /b 1
)

:: Buscar APK generada
set APK_PATH=
for /r "GeoFoto.Mobile\bin\Debug\net10.0-android" %%f in (*-Signed.apk) do (
    set APK_PATH=%%f
)

if "!APK_PATH!"=="" (
    echo   [ERROR] No se encontro el APK en bin\Debug\net10.0-android\
    pause & exit /b 1
)

:: Mostrar info del APK
for %%f in ("!APK_PATH!") do (
    set APK_SIZE_BYTES=%%~zf
    set /a APK_SIZE_MB=!APK_SIZE_BYTES! / 1048576
)

echo.
echo   [OK] APK generado:
echo        !APK_PATH!
echo        Tamaño: !APK_SIZE_MB! MB
echo.
echo   Ejecuta 04_install_android.bat para instalar en el celular.
pause
```

---

### SCRIPT 5 — `04_install_android.bat`
Detecta el celular, configura el túnel y instala la APK.
(Usar los valores reales de [ADB_PATH] y [APP_ID] detectados en la Fase 1)

```batch
@echo off
setlocal enabledelayedexpansion
title GeoFoto — Instalando en Android
color 0D

set ADB=[ADB_PATH_REAL]
set APP_ID=[APPLICATION_ID_REAL]

echo.
echo  =====================================================
echo   GeoFoto.Mobile — Deploy en Android
echo  =====================================================
echo.

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
for /r "GeoFoto.Mobile\bin\Debug\net10.0-android" %%f in (*-Signed.apk) do (
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
```

---

### SCRIPT 6 — `04b_fix_android.bat`
Diagnóstico y fix automático si la app muestra pantalla en blanco.

```batch
@echo off
setlocal enabledelayedexpansion
title GeoFoto — Diagnostico Android
color 0C

set ADB=[ADB_PATH_REAL]
set APP_ID=[APPLICATION_ID_REAL]

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
```

---

### SCRIPT 7 — `05_run_tests.bat`
Ejecuta todos los tests con reporte detallado.

```batch
@echo off
setlocal enabledelayedexpansion
title GeoFoto — Test Suite Completo
color 0A

echo.
echo  =====================================================
echo   GeoFoto — Ejecutando test suite completo
echo  =====================================================
echo.

:: Verificar que el proyecto de tests existe
if not exist "GeoFoto.Tests\GeoFoto.Tests.csproj" (
    echo   [ERROR] No se encontro GeoFoto.Tests\GeoFoto.Tests.csproj
    echo          Asegurate de haber completado el Sprint 05.
    pause & exit /b 1
)

:: Limpiar resultados anteriores
if exist "TestResults" rmdir /s /q "TestResults"
mkdir "TestResults"

:: Restaurar dependencias
echo   [INFO] Restaurando paquetes...
dotnet restore GeoFoto.Tests\GeoFoto.Tests.csproj --verbosity quiet
if errorlevel 1 ( echo   [ERROR] Restore fallo & pause & exit /b 1 )

:: Compilar
echo   [INFO] Compilando proyecto de tests...
dotnet build GeoFoto.Tests\GeoFoto.Tests.csproj --configuration Debug --verbosity quiet --no-restore
if errorlevel 1 ( echo   [ERROR] Compilacion fallo & pause & exit /b 1 )

:: Ejecutar tests con cobertura
echo.
echo   [INFO] Ejecutando tests (con cobertura de codigo)...
echo.

dotnet test GeoFoto.Tests\GeoFoto.Tests.csproj ^
    --configuration Debug ^
    --no-build ^
    --verbosity normal ^
    --logger "console;verbosity=detailed" ^
    --logger "trx;LogFileName=TestResults\results.trx" ^
    --collect:"XPlat Code Coverage" ^
    --results-directory "TestResults"

set TEST_RESULT=%ERRORLEVEL%

echo.
echo  =====================================================
if %TEST_RESULT% EQU 0 (
    color 0A
    echo   RESULTADO: TODOS LOS TESTS PASARON
) else (
    color 0C
    echo   RESULTADO: HUBO TESTS FALLIDOS
)
echo  =====================================================
echo.

:: Mostrar reporte de cobertura si existe
for /r "TestResults" %%f in (coverage.cobertura.xml) do (
    echo   [INFO] Reporte de cobertura: %%f
    :: Extraer porcentaje de cobertura
    powershell -Command "$xml=[xml](Get-Content '%%f'); $cov=$xml.coverage; Write-Host '  Cobertura de lineas: ' + [math]::Round([double]$cov.'line-rate'*100,1) + '%%'"
)

echo.
echo   Resultados TRX: TestResults\results.trx
echo.
if %TEST_RESULT% NEQ 0 (
    echo   [ACCION REQUERIDA] Revisa los tests fallidos arriba
    echo   y ejecuta este script nuevamente tras corregirlos.
)
pause
exit /b %TEST_RESULT%
```

---

### SCRIPT 8 — `06_demo_completa.bat` ← EL SCRIPT MAESTRO
Lanza todo, instala en Android, abre browsers, verifica que todo funciona.
Este script NO PARA hasta que todo funciona o detecta un error irrecuperable.

```batch
@echo off
setlocal enabledelayedexpansion
title GeoFoto — Demo Completa v1.0
color 0A

set ADB=[ADB_PATH_REAL]
set APP_ID=[APPLICATION_ID_REAL]
set PC_IP=[IP_PC_REAL]
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
    echo     [WARN] adb no encontrado en [ADB_PATH_REAL]
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

dotnet ef database update --project GeoFoto.Api\GeoFoto.Api.csproj --verbosity quiet
if errorlevel 1 (
    echo     [INFO] Creando migration inicial...
    dotnet ef migrations add InitialCreate --project GeoFoto.Api\GeoFoto.Api.csproj --verbosity quiet >nul 2>&1
    dotnet ef database update --project GeoFoto.Api\GeoFoto.Api.csproj --verbosity quiet
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
start "GeoFoto.Web" cmd /k "title GeoFoto.Web && cd /d %~dp0GeoFoto.Web && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --configuration Debug"
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
```

---

### SCRIPT 9 — `07_stop_all.bat`
Detiene todos los servicios limpiamente.

```batch
@echo off
title GeoFoto — Deteniendo servicios
color 0C

echo   [INFO] Deteniendo todos los servicios GeoFoto...

taskkill /FI "WINDOWTITLE eq GeoFoto.Api" /F >nul 2>&1
taskkill /FI "WINDOWTITLE eq GeoFoto.Web" /F >nul 2>&1

for /f "tokens=5" %%a in ('netstat -ano 2^>nul ^| findstr ":5000 "') do (
    taskkill /PID %%a /F >nul 2>&1
)
for /f "tokens=5" %%a in ('netstat -ano 2^>nul ^| findstr ":5001 "') do (
    taskkill /PID %%a /F >nul 2>&1
)

if exist "demo_final_screenshot.png" del "demo_final_screenshot.png"
if exist "geofoto_screenshot.png" del "geofoto_screenshot.png"
if exist "geofoto_postfix.png" del "geofoto_postfix.png"

echo   [OK] Servicios detenidos.
timeout /t 2 /nobreak >nul
```

---

### SCRIPT 10 — `08_git_commit_push.bat`
Hace commit de todo el trabajo del sprint y push al remoto.
Solo corre si todos los tests pasan y la demo está OK.

```batch
@echo off
setlocal enabledelayedexpansion
title GeoFoto — Git Commit y Push
color 0A

echo.
echo  =====================================================
echo   GeoFoto — Commit y Push del sprint actual
echo  =====================================================
echo.

:: Verificar que estamos en un repo git
git status >nul 2>&1
if errorlevel 1 (
    echo   [ERROR] No se encontro repositorio git en el directorio actual.
    pause & exit /b 1
)

:: Mostrar estado actual
echo   [INFO] Estado actual del repositorio:
git status --short
echo.

:: Verificar si hay cambios para commitear
git diff --quiet && git diff --staged --quiet
if not errorlevel 1 (
    echo   [INFO] No hay cambios pendientes. El repositorio esta limpio.
    echo.
    git log --oneline -5
    pause & exit /b 0
)

:: Verificar rama actual
for /f "tokens=*" %%b in ('git rev-parse --abbrev-ref HEAD') do set RAMA=%%b
echo   [INFO] Rama actual: !RAMA!
echo.

:: Pedir mensaje de commit
echo   Ingresa el mensaje del commit (o presiona Enter para usar el default):
echo   Default: "feat: demo final Sprint 06 completa - GeoFoto v1.0"
echo.
set /p COMMIT_MSG="Mensaje: "
if "!COMMIT_MSG!"=="" set COMMIT_MSG=feat: demo final Sprint 06 completa - GeoFoto v1.0

:: Stage de todos los cambios (excluyendo binarios y temporales)
echo   [INFO] Preparando archivos para commit...
git add .
git reset -- "*.apk" >nul 2>&1
git reset -- "*.png" >nul 2>&1
git reset -- "bin/" >nul 2>&1
git reset -- "obj/" >nul 2>&1
git reset -- "TestResults/" >nul 2>&1
git reset -- "logcat_*.txt" >nul 2>&1
git reset -- "*.sqlite" >nul 2>&1

:: Verificar .gitignore existe
if not exist ".gitignore" (
    echo   [INFO] Creando .gitignore basico...
    (
        echo bin/
        echo obj/
        echo TestResults/
        echo *.apk
        echo *.png
        echo logcat_*.txt
        echo *.sqlite
        echo .vs/
        echo **/appsettings.Development.json
    ) > .gitignore
    git add .gitignore
)

:: Mostrar que se va a commitear
echo.
echo   [INFO] Archivos a commitear:
git diff --staged --name-only
echo.

:: Confirmar
set /p CONFIRMAR="   Commitear con mensaje: '!COMMIT_MSG!' ? (S/N): "
if /i "!CONFIRMAR!" NEQ "S" (
    echo   [INFO] Commit cancelado.
    pause & exit /b 0
)

:: Commit
git commit -m "!COMMIT_MSG!"
if errorlevel 1 (
    echo   [ERROR] El commit fallo.
    pause & exit /b 1
)
echo   [OK] Commit creado.

:: Push con reintentos
set PUSH_OK=0
for /l %%i in (1,1,3) do (
    if "!PUSH_OK!"=="0" (
        echo   [INFO] Push a !RAMA!... intento %%i/3
        git push origin !RAMA!
        if not errorlevel 1 (
            set PUSH_OK=1
            echo   [OK] Push exitoso a origin/!RAMA!
        ) else (
            echo   [WARN] Push fallo en intento %%i.
            if %%i LSS 3 (
                echo         Reintentando en 5 segundos...
                timeout /t 5 /nobreak >nul
            )
        )
    )
)

if "!PUSH_OK!"=="0" (
    echo.
    echo   [ERROR] No se pudo hacer push despues de 3 intentos.
    echo          Verificá tu conexion a internet y permisos del repositorio.
    echo.
    echo   El commit esta guardado localmente. Podes hacer push manualmente con:
    echo     git push origin !RAMA!
    pause & exit /b 1
)

echo.
echo   [INFO] Ultimos commits en !RAMA!:
git log --oneline -5
echo.

echo  =====================================================
echo   Push completado exitosamente.
echo  =====================================================
pause
```

---

## FASE 3 — EJECUCIÓN Y CORRECCIÓN HASTA EL ÉXITO

Una vez creados todos los scripts con los valores reales, ejecutá en este orden:

### 3.1 — Setup inicial (una sola vez)

```powershell
Start-Process "00_setup.bat" -Wait
```

Si falla: analizá el error, corregí la configuración (connection string, permisos,
migration) y reintentá. No avancés hasta que termine con "Setup completado".

### 3.2 — Demo completa

```powershell
Start-Process "06_demo_completa.bat" -Wait
```

Monitorá cada fase. Si alguna falla:

**Si falla la API:**
```powershell
# Ver el error real
cd GeoFoto.Api
dotnet run --configuration Debug --urls "http://0.0.0.0:5000" 2>&1 | Select-Object -First 30
```
Corregí el error (migration, connection string, puerto), corregí el script si es necesario, reintentá.

**Si falla el Web:**
```powershell
cd GeoFoto.Web
dotnet run 2>&1 | Select-Object -First 30
```

**Si falla el Android (pantalla en blanco):**
```powershell
Start-Process "04b_fix_android.bat" -Wait
```
Si sigue fallando, ejecutá:
```powershell
$adb = "[ADB_PATH_REAL]"
& $adb -s [DEVICE_ID] logcat -d 2>&1 | Select-Object -Last 40
```
Analizá el error, corregí el código fuente si es necesario, recompilá con `03_build_android.bat`, reinstalá con `04_install_android.bat`.

**Si falla por red (celular no conecta a la API):**
```powershell
# Verificar tunel
$adb = "[ADB_PATH_REAL]"
& $adb reverse --list

# Si no está activo:
& $adb -s [DEVICE_ID] reverse tcp:5000 tcp:5000

# Verificar desde el celular (ejecutar en adb shell):
& $adb -s [DEVICE_ID] shell curl -s http://localhost:5000/api/puntos
```

### 3.3 — Ejecutar todos los tests

```powershell
Start-Process "05_run_tests.bat" -Wait
```

Si hay tests fallidos:
- Leé el output completo
- Identificá qué test falla y por qué
- Corregí el código o el test
- Recompilá: `dotnet build GeoFoto.Tests`
- Reejecutá `05_run_tests.bat`
- Repetí hasta que todos pasen

### 3.3b — Commit y Push del trabajo completo

Una vez que los tests pasan y la demo funciona, ejecutá:

```powershell
Start-Process "08_git_commit_push.bat" -Wait
```

El script detecta automáticamente la rama actual, stagea todos los cambios
relevantes (excluye binarios, APKs, screenshots y archivos temporales),
pide confirmación del mensaje de commit y hace push con reintentos.

Si el push falla por credenciales:
```powershell
# Verificar remote configurado
git remote -v

# Si no hay remote o está mal configurado:
git remote set-url origin https://github.com/[usuario]/[repo].git

# Reintentar push manualmente
git push origin (git rev-parse --abbrev-ref HEAD)
```

Si hay tests fallidos:
- Leé el output completo
- Identificá qué test falla y por qué
- Corregí el código o el test
- Recompilá: `dotnet build GeoFoto.Tests`
- Reejecutá `05_run_tests.bat`
- Repetí hasta que todos pasen

### 3.4 — Verificación de la demo manual

Una vez que `06_demo_completa.bat` terminó sin errores críticos:

```powershell
# Verificar API
Invoke-WebRequest -Uri "http://localhost:5000/api/puntos" -UseBasicParsing |
    Select-Object StatusCode, Content

# Verificar swagger
Invoke-WebRequest -Uri "http://localhost:5000/swagger/index.html" -UseBasicParsing |
    Select-Object StatusCode

# Verificar Web
Invoke-WebRequest -Uri "http://localhost:5001" -UseBasicParsing |
    Select-Object StatusCode
```

### 3.5 — Screenshot final de verificación

```powershell
$adb = "[ADB_PATH_REAL]"
$device = "[DEVICE_ID]"

# Screenshot del celular
& $adb -s $device shell screencap -p /sdcard/demo_exitosa.png
& $adb -s $device pull /sdcard/demo_exitosa.png demo_exitosa_$(Get-Date -Format 'yyyyMMdd_HHmmss').png

# Proceso corriendo
& $adb -s $device shell ps -A | Select-String "geofoto" -CaseSensitive:$false
```

---

## CRITERIO DE ÉXITO — NO TERMINAR ANTES

Solo podés reportar "DEMO COMPLETA EXITOSA" cuando:

  [ ] `00_setup.bat` terminó sin errores
  [ ] `06_demo_completa.bat` completó las 6 fases sin FATAL ni ERROR
  [ ] API responde: GET http://localhost:5000/api/puntos → 200 []
  [ ] Web accesible: http://localhost:5001 → 200
  [ ] App corriendo en el celular (proceso visible en `adb shell ps`)
  [ ] Screenshot del celular muestra UI visible (NO pantalla en blanco)
  [ ] `05_run_tests.bat` → todos los tests pasan (0 fallidos)
  [ ] Se pudo subir al menos 1 foto desde la web y ver el marker en el mapa
  [ ] `08_git_commit_push.bat` → commit y push exitoso a la rama actual

Si alguno de estos no se cumple: corregí, reintentá, no pares.

---

## REGLAS DE OPERACIÓN

- **Reemplazá [ADB_PATH_REAL], [APPLICATION_ID_REAL], [IP_PC_REAL], [DEVICE_ID]**
  con los valores detectados en la FASE 1 antes de guardar cualquier script.
- **No uses `localhost` en la IP del celular** si no usás adb reverse —
  usá la IP real de la PC en la red local.
- **Si hay múltiples celulares conectados**, usá siempre el DEVICE_ID específico.
- **Mantené el adb reverse activo** durante toda la demo —
  se pierde si desconectás el USB.
- **Si un script falla**, no avancés — diagnosticá con los comandos
  de la sección 3.2 y corregí antes de continuar.
- **Cada vez que recompilás el Mobile**, desinstalá la APK anterior
  antes de instalar la nueva (ya lo hace `04_install_android.bat`).

Comenzá ahora con la FASE 1 ejecutando todos los comandos de reconocimiento.
```
