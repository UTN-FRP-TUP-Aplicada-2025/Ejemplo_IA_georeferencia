@echo off
setlocal enabledelayedexpansion
title GeoFoto — Validacion de capturas
color 0A

set SCREENSHOTS_DIR=%~dp0..\test-results\screenshots
set REPORT=%~dp0..\test-results\validation_report.txt
set PASS=0
set FAIL=0

echo.
echo  =====================================================
echo   GeoFoto — Validacion de capturas de pantalla
echo  =====================================================
echo.

if not exist "%SCREENSHOTS_DIR%" (
    echo   [ERROR] No existe el directorio de capturas: %SCREENSHOTS_DIR%
    echo          Ejecuta primero 07b_deploy_and_capture.bat
    pause & exit /b 1
)

:: Crear reporte
echo GeoFoto - Reporte de Validacion Visual > "%REPORT%"
echo Fecha: %DATE% %TIME% >> "%REPORT%"
echo Generado por: 08_validate_screenshots.bat >> "%REPORT%"
echo ======================================================== >> "%REPORT%"
echo. >> "%REPORT%"
echo CAPTURAS ESPERADAS (segun wireframes_pantallas_v1.0.md): >> "%REPORT%"
echo ======================================================== >> "%REPORT%"

:: -------------------------------------------------------
:: Definir pantallas esperadas segun docs/03_ux-ui
:: -------------------------------------------------------
set EXPECTED[0]=01_inicio.png
set EXPECTED[1]=02_mapa.png
set EXPECTED[2]=03_camara.png
set EXPECTED[3]=04_lista_fotos.png
set EXPECTED[4]=05_sync.png

set LABELS[0]=Pantalla de inicio / splash
set LABELS[1]=Vista de mapa con geolocalizacion
set LABELS[2]=Interfaz de camara georreferenciada
set LABELS[3]=Lista de fotos guardadas
set LABELS[4]=Estado de sincronizacion offline/online

:: Verificar cada captura esperada
for /l %%i in (0,1,4) do (
    set FILE=!EXPECTED[%%i]!
    set LABEL=!LABELS[%%i]!
    if exist "%SCREENSHOTS_DIR%\!FILE!" (
        for %%s in ("%SCREENSHOTS_DIR%\!FILE!") do set SIZE=%%~zs
        set /a SIZE_KB=!SIZE! / 1024
        echo   [OK]    !FILE! (!SIZE_KB! KB^) — !LABEL!
        echo   [OK]    !FILE! (!SIZE_KB! KB) — !LABEL! >> "%REPORT%"
        set /a PASS+=1
    ) else (
        echo   [FALTA] !FILE! — !LABEL!
        echo   [FALTA] !FILE! — !LABEL! >> "%REPORT%"
        set /a FAIL+=1
    )
)

:: -------------------------------------------------------
:: Verificar tamaño minimo (capturas corruptas = < 10KB)
:: -------------------------------------------------------
echo. >> "%REPORT%"
echo VERIFICACION DE INTEGRIDAD (tamano minimo 10KB): >> "%REPORT%"
echo ======================================================== >> "%REPORT%"

set CORRUPT=0
for %%f in ("%SCREENSHOTS_DIR%\*.png") do (
    for %%s in ("%%f") do set FSIZE=%%~zs
    set /a FSIZE_KB=!FSIZE! / 1024
    if !FSIZE_KB! LSS 10 (
        echo   [CORRUPCION] %%~nxf solo tiene !FSIZE_KB! KB - posible captura vacia
        echo   [CORRUPCION] %%~nxf solo tiene !FSIZE_KB! KB >> "%REPORT%"
        set /a CORRUPT+=1
    )
)
if !CORRUPT! EQU 0 (
    echo   [OK] Todas las capturas tienen tamano valido
    echo   [OK] Todas las capturas tienen tamano valido >> "%REPORT%"
)

:: -------------------------------------------------------
:: Resumen final
:: -------------------------------------------------------
echo. >> "%REPORT%"
echo RESUMEN: >> "%REPORT%"
echo   Capturas OK:     !PASS! / 5 >> "%REPORT%"
echo   Capturas falta:  !FAIL! >> "%REPORT%"
echo   Posibles corruptas: !CORRUPT! >> "%REPORT%"

echo.
echo  =====================================================
if !FAIL! EQU 0 if !CORRUPT! EQU 0 (
    color 0A
    echo   RESULTADO: VALIDACION EXITOSA — !PASS!/5 pantallas OK
    echo   RESULTADO: EXITOSA >> "%REPORT%"
) else (
    color 0C
    echo   RESULTADO: VALIDACION CON PROBLEMAS
    echo   Capturas faltantes:  !FAIL!
    echo   Posibles corruptas:  !CORRUPT!
    echo   RESULTADO: CON PROBLEMAS >> "%REPORT%"
)
echo  =====================================================
echo.
echo   Reporte completo: test-results\validation_report.txt
echo   Capturas:         test-results\screenshots\
echo.

:: Abrir carpeta de capturas en el explorador
start "" "%SCREENSHOTS_DIR%"
start "" notepad "%REPORT%"

if !FAIL! GTR 0 exit /b 1
if !CORRUPT! GTR 0 exit /b 1
exit /b 0
