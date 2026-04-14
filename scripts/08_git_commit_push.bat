@echo off
setlocal enabledelayedexpansion
title GeoFoto — Git Commit y Push
color 0A

echo.
echo  =====================================================
echo   GeoFoto — Commit y Push del sprint actual
echo  =====================================================
echo.

:: Navegar al directorio raiz del repositorio
cd /d "%~dp0.."

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
