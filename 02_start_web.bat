@echo off
title GeoFoto.Web — Puerto 5001
color 0E
echo   GeoFoto.Web corriendo en http://localhost:5001
echo   Presiona Ctrl+C para detener
echo.
cd GeoFoto.Web
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --configuration Debug --urls "http://localhost:5001"
