@echo off
setlocal enabledelayedexpansion
title GeoFoto — Levantar Servicios
color 0F

:: ============================================================
::  launch-all.bat
::  Levanta GeoFoto.Api (puerto 5000) y GeoFoto.Web (5001)
::  en ventanas separadas. Útil para desarrollo y demos.
::
::  Uso: doble click o desde terminal
::  Detener: cerrar las ventanas abiertas o ejecutar
::           ..\07_stop_all.bat
:: ============================================================

echo.
echo  ########################################################
echo   GeoFoto — Levantar todos los servicios
echo  ########################################################
echo.

:: Determinar raíz del repo (dos niveles arriba de este script)
set REPO_ROOT=%~dp0..\..
set REPO_ROOT=%REPO_ROOT:\\=\%

:: Verificar .NET SDK
where dotnet >nul 2>&1
if errorlevel 1 (
    echo   [ERROR] .NET SDK no encontrado. Instalalo desde https://dotnet.microsoft.com
    pause
    exit /b 1
)

echo   Raiz del repo: %REPO_ROOT%
echo.

:: ──── API (puerto 5000) ────────────────────────────────────
echo   Iniciando GeoFoto.Api en puerto 5000...
set API_DIR=%REPO_ROOT%\src\GeoFoto.Api
if not exist "%API_DIR%\GeoFoto.Api.csproj" (
    echo   [ERROR] No se encontro GeoFoto.Api.csproj en %API_DIR%
    pause
    exit /b 1
)

start "GeoFoto.Api [5000]" cmd /k "cd /d "%API_DIR%" && dotnet run --urls http://localhost:5000"
echo   [OK] GeoFoto.Api iniciando (esperar ~10s)...
timeout /t 3 /nobreak >nul

:: ──── Web (puerto 5001) ───────────────────────────────────
echo   Iniciando GeoFoto.Web en puerto 5001...
set WEB_DIR=%REPO_ROOT%\src\GeoFoto.Web
if not exist "%WEB_DIR%\GeoFoto.Web.csproj" (
    echo   [WARN] GeoFoto.Web.csproj no encontrado — saltando.
) else (
    start "GeoFoto.Web [5001]" cmd /k "cd /d "%WEB_DIR%" && dotnet run --urls http://localhost:5001"
    echo   [OK] GeoFoto.Web iniciando (esperar ~15s)...
    timeout /t 3 /nobreak >nul
)

:: ──── Resultado ───────────────────────────────────────────
echo.
echo  ########################################################
echo   Servicios iniciados:
echo     API:  http://localhost:5000
echo     API Swagger: http://localhost:5000/swagger
echo     Web:  http://localhost:5001
echo  ########################################################
echo.
echo   Para detener los servicios: cerrar las ventanas o
echo   ejecutar ..\07_stop_all.bat
echo.

:: Abrir Swagger en el navegador
timeout /t 8 /nobreak >nul
start "" "http://localhost:5000/swagger"

pause
