@echo off
setlocal enabledelayedexpansion
title GeoFoto — Levantando servicios
color 0B

echo.
echo  =====================================================
echo   GeoFoto — Levantando API + Web
echo  =====================================================
echo.

:: Verificar que los proyectos existen
if not exist "%~dp0..\src\GeoFoto.Api\GeoFoto.Api.csproj" (
    echo   [ERROR] No se encontro src\GeoFoto.Api\GeoFoto.Api.csproj
    pause & exit /b 1
)
if not exist "%~dp0..\src\GeoFoto.Web\GeoFoto.Web.csproj" (
    echo   [ERROR] No se encontro src\GeoFoto.Web\GeoFoto.Web.csproj
    pause & exit /b 1
)

:: Matar procesos anteriores en los puertos 5000 y 5001
echo   [INFO] Liberando puertos 5000 y 5001 si estaban en uso...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5000 " ^| findstr "LISTENING"') do (
    taskkill /f /pid %%a >nul 2>&1
)
for /f "tokens=5" %%a in ('netstat -aon ^| findstr ":5001 " ^| findstr "LISTENING"') do (
    taskkill /f /pid %%a >nul 2>&1
)
timeout /t 2 /nobreak >nul

:: Iniciar API en ventana separada
echo   [INFO] Iniciando GeoFoto.Api en http://localhost:5000 ...
start "GeoFoto-API [5000]" cmd /k "title GeoFoto-API && color 0B && cd /d "%~dp0..\src\GeoFoto.Api" && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --configuration Debug --urls http://0.0.0.0:5000"

:: Iniciar Web en ventana separada
echo   [INFO] Iniciando GeoFoto.Web en http://localhost:5001 ...
start "GeoFoto-Web [5001]" cmd /k "title GeoFoto-Web && color 0E && cd /d "%~dp0..\src\GeoFoto.Web" && set ASPNETCORE_ENVIRONMENT=Development && dotnet run --configuration Debug --urls http://localhost:5001"

:: Esperar que los servicios arranquen
echo   [INFO] Esperando que los servicios inicien (15s)...
timeout /t 15 /nobreak >nul

:: Health check API
echo   [INFO] Verificando API en http://localhost:5000/health ...
curl -s -f -o nul http://localhost:5000/health
if errorlevel 1 (
    echo   [WARN] /health no responde — intentando Swagger...
    curl -s -f -o nul http://localhost:5000/swagger/index.html
    if errorlevel 1 (
        echo   [ERROR] API no responde. Revisa la ventana GeoFoto-API.
        pause & exit /b 1
    )
)
echo   [OK] API responde en http://localhost:5000

:: Health check Web
echo   [INFO] Verificando Web en http://localhost:5001 ...
curl -s -f -o nul http://localhost:5001
if errorlevel 1 (
    echo   [WARN] Web no responde aun, puede necesitar mas tiempo.
) else (
    echo   [OK] Web responde en http://localhost:5001
)

echo.
echo  =====================================================
echo   Servicios levantados:
echo     API  -> http://localhost:5000
echo     Web  -> http://localhost:5001
echo     Swagger -> http://localhost:5000/swagger
echo  =====================================================
echo.
