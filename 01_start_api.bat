@echo off
title GeoFoto.Api — Puerto 5000
color 0B
echo   GeoFoto.Api corriendo en http://0.0.0.0:5000
echo   Swagger: http://localhost:5000/swagger
echo   Presiona Ctrl+C para detener
echo.
cd GeoFoto.Api
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --configuration Debug --urls "http://0.0.0.0:5000"
