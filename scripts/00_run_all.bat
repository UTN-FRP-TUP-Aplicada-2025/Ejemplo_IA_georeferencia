@echo off
setlocal enabledelayedexpansion
title GeoFoto — Pipeline QA Completo
color 0F

echo.
echo  ########################################################
echo   GeoFoto — Pipeline QA Completo
echo   Tests + Servicios + Deploy Android + Capturas + Validacion
echo  ########################################################
echo.
echo   Este script ejecuta el pipeline completo en orden:
echo     [1] Tests automatizados (unit + integracion + e2e)
echo     [2] Levantar API (puerto 5000) y Web (puerto 5001)
echo     [3] Build + Deploy en Android + capturas de pantalla
echo     [4] Validacion de capturas contra documentacion
echo.
echo   Verifica que tengas:
echo     - Un celular Android conectado por USB con USB Debugging activo
echo     - .NET SDK instalado
echo     - ADB en C:\Program Files (x86)\Android\android-sdk\platform-tools\
echo.
pause

:: Crear directorio de resultados
if not exist "%~dp0..\test-results" mkdir "%~dp0..\test-results"
if not exist "%~dp0..\test-results\screenshots" mkdir "%~dp0..\test-results\screenshots"

:: Registrar inicio
set START_TIME=%TIME%
echo   Pipeline iniciado: %DATE% %TIME%
echo.

:: -------------------------------------------------------
:: FASE 1 — Tests automatizados
:: -------------------------------------------------------
echo  ########################################################
echo   FASE 1/4 — Tests automatizados
echo  ########################################################
call "%~dp0\05_run_tests.bat"
if errorlevel 1 (
    echo.
    echo   [ERROR] Tests fallaron. Pipeline detenido.
    echo   Revisa la salida de 05_run_tests.bat y corrige los errores.
    goto :error
)
echo   [OK] Fase 1 completada.
echo.

:: -------------------------------------------------------
:: FASE 2 — Levantar servicios
:: -------------------------------------------------------
echo  ########################################################
echo   FASE 2/4 — Levantando servicios API + Web
echo  ########################################################
call "%~dp0\06b_start_services.bat"
if errorlevel 1 (
    echo.
    echo   [ERROR] No se pudieron levantar los servicios. Pipeline detenido.
    goto :error
)
echo   [OK] Fase 2 completada.
echo.

:: -------------------------------------------------------
:: FASE 3 — Deploy Android + Capturas
:: -------------------------------------------------------
echo  ########################################################
echo   FASE 3/4 — Deploy Android + Capturas de pantalla
echo  ########################################################
call "%~dp0\07b_deploy_and_capture.bat"
if errorlevel 1 (
    echo.
    echo   [ERROR] Deploy o captura de pantallas fallaron. Pipeline detenido.
    goto :error
)
echo   [OK] Fase 3 completada.
echo.

:: -------------------------------------------------------
:: FASE 4 — Validacion de capturas
:: -------------------------------------------------------
echo  ########################################################
echo   FASE 4/4 — Validacion de capturas
echo  ########################################################
call "%~dp0\08_validate_screenshots.bat"
if errorlevel 1 (
    echo.
    echo   [WARN] Validacion con advertencias. Revisa el reporte.
    goto :warning
)
echo   [OK] Fase 4 completada.
echo.

:: -------------------------------------------------------
:: EXITO
:: -------------------------------------------------------
color 0A
echo.
echo  ########################################################
echo   PIPELINE COMPLETADO EXITOSAMENTE
echo   Inicio: %START_TIME%  |  Fin: %TIME%
echo  ########################################################
echo.
echo   Resultados:
echo     Tests:      test-results\results.trx
echo     Capturas:   test-results\screenshots\
echo     Validacion: test-results\validation_report.txt
echo.
start "" "%~dp0..\test-results\screenshots"
goto :end

:warning
color 0E
echo.
echo  ########################################################
echo   PIPELINE COMPLETADO CON ADVERTENCIAS
echo   Revisa test-results\validation_report.txt
echo  ########################################################
goto :end

:error
color 0C
echo.
echo  ########################################################
echo   PIPELINE INTERRUMPIDO POR ERROR
echo   Revisa el mensaje de error arriba e intenta nuevamente.
echo  ########################################################
echo.
pause
exit /b 1

:end
pause
