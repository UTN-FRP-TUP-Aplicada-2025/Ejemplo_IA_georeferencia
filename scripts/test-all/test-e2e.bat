@echo off
setlocal enabledelayedexpansion
title GeoFoto — Tests Automatizados
color 0F

:: ============================================================
::  test-e2e.bat
::  Ejecuta la suite completa de tests de GeoFoto:
::    - Tests unitarios (Unit/Mobile, Unit/Shared, Unit/Api)
::    - Tests de integración (Integration/)
::
::  Maneja el caso donde GeoFoto.Api.exe está bloqueado
::  por un proceso en ejecución usando -p:BuildProjectReferences=false
::
::  Resultados en: ..\..\test-results\
::
::  Uso: doble click o desde terminal
:: ============================================================

:: Determinar raíz del repo
set REPO_ROOT=%~dp0..\..
set TEST_PROJ=%REPO_ROOT%\src\GeoFoto.Tests\GeoFoto.Tests.csproj
set SHARED_PROJ=%REPO_ROOT%\src\GeoFoto.Shared\GeoFoto.Shared.csproj
set RESULTS_DIR=%REPO_ROOT%\test-results
set TIMESTAMP=%DATE:/=-%_%TIME::=-%
set TIMESTAMP=%TIMESTAMP: =_%

echo.
echo  ########################################################
echo   GeoFoto — Suite de Tests Automatizados
echo  ########################################################
echo.

:: Crear carpeta de resultados
if not exist "%RESULTS_DIR%" mkdir "%RESULTS_DIR%"

:: Verificar .NET SDK
where dotnet >nul 2>&1
if errorlevel 1 (
    echo   [ERROR] .NET SDK no encontrado.
    pause
    exit /b 1
)

echo   Proyecto de tests: %TEST_PROJ%
echo   Resultados:        %RESULTS_DIR%
echo.

:: ──────────────────────────────────────────────────────────
:: PASO 1 — Build GeoFoto.Shared (siempre compila sin lock)
:: ──────────────────────────────────────────────────────────
echo   [1/3] Compilando GeoFoto.Shared...
dotnet build "%SHARED_PROJ%" --verbosity quiet 2>&1
if errorlevel 1 (
    echo.
    echo   [ERROR] GeoFoto.Shared no compila. Revisa los errores arriba.
    goto :error
)
echo   [OK] GeoFoto.Shared compilado.
echo.

:: ──────────────────────────────────────────────────────────
:: PASO 2 — Build GeoFoto.Tests (sin recompilar dependencias)
::          -p:BuildProjectReferences=false evita intentar
::          copiar GeoFoto.Api.exe si el proceso está corriendo
:: ──────────────────────────────────────────────────────────
echo   [2/3] Compilando GeoFoto.Tests...
dotnet build "%TEST_PROJ%" "-p:BuildProjectReferences=false" --verbosity quiet 2>&1
if errorlevel 1 (
    echo.
    echo   [ERROR] GeoFoto.Tests no compila. Revisa los errores arriba.
    goto :error
)
echo   [OK] GeoFoto.Tests compilado.
echo.

:: ──────────────────────────────────────────────────────────
:: PASO 3 — Ejecutar todos los tests
:: ──────────────────────────────────────────────────────────
echo   [3/3] Ejecutando tests...
echo.

set TRX_FILE=%RESULTS_DIR%\test-results.trx
dotnet test "%TEST_PROJ%" ^
    --no-build ^
    --logger "trx;LogFileName=%TRX_FILE%" ^
    --logger "console;verbosity=normal" ^
    2>&1

set TEST_EXIT=%errorlevel%

echo.
if %TEST_EXIT% equ 0 (
    color 0A
    echo  ########################################################
    echo   TODOS LOS TESTS PASARON
    echo  ########################################################
    echo.
    echo   Resultados TRX: %TRX_FILE%
    goto :success
) else (
    color 0C
    echo  ########################################################
    echo   TESTS FALLIDOS — revisa la salida arriba
    echo  ########################################################
    echo.
    echo   Resultados TRX: %TRX_FILE%
    goto :error
)

:: ──────────────────────────────────────────────────────────
:: FILTROS DISPONIBLES (para correr un subconjunto)
:: ──────────────────────────────────────────────────────────
::
::  Para correr solo tests de una US:
::    dotnet test ... --filter "US21|US22"
::
::  Para correr solo tests de integración:
::    dotnet test ... --filter "Category=Integration"
::
::  Para correr tests por nombre:
::    dotnet test ... --filter "FullyQualifiedName~SyncFlow"
::

:success
pause
exit /b 0

:error
pause
exit /b 1
