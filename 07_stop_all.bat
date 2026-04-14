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
