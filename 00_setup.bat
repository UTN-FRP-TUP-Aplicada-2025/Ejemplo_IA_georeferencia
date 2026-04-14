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
dotnet ef database update --project GeoFoto.Api\GeoFoto.Api.csproj
if errorlevel 1 (
    echo   [INFO] Intentando crear migration inicial...
    dotnet ef migrations add InitialCreate --project GeoFoto.Api\GeoFoto.Api.csproj
    dotnet ef database update --project GeoFoto.Api\GeoFoto.Api.csproj
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
