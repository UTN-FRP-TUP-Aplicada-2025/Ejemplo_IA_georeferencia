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
