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
for /f "delims=" %%f in ('dir /s /b "TestResults\coverage.cobertura.xml" 2^>nul') do (
    echo   [INFO] Reporte de cobertura: %%f
    powershell -NoProfile -Command "try { $xml=[xml](Get-Content '%%f'); $rate=$xml.coverage.'line-rate'; Write-Host ('  Cobertura de lineas: {0:N1}%%' -f ([double]$rate*100)) } catch { Write-Host '  [WARN] No se pudo leer cobertura' }"
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
