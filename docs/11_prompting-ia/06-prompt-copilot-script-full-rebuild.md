# Prompt para GitHub Copilot — Script BAT Orquestador Maestro

> Pegá este prompt en Copilot Chat con modo **Edits** (`@workspace`).
> Prerequisito: los scripts individuales (00–06) ya deben existir en la carpeta `scripts/`.
> El celular debe estar conectado por USB con modo desarrollador activo.

---

## PROMPT

```
Sos un DevOps engineer. Tu tarea es crear UN SOLO script BAT maestro
que orqueste todo el ciclo de build, deploy y lanzamiento de GeoFoto.

LECTURA OBLIGATORIA antes de escribir:
  Leé los scripts existentes en la carpeta scripts/ para entender:
    - La estructura de rutas (src\GeoFoto.Api, src\GeoFoto.Shared, etc.)
    - Los valores de ADB, APP_ID, puertos
    - El patrón de detección de dispositivo Android
    - El patrón de detección de APK
  Leé también:
    docs/10_developer_guide/guia-setup-proyecto_v1.0.md

Confirmá con: "Scripts existentes y guía leídos. Creando orquestador."

---

## QUÉ DEBE HACER EL SCRIPT

El script `07_full_rebuild.bat` va en la carpeta `scripts/`.
Debe ejecutar TODO el ciclo en secuencia, sin pedir confirmación,
deteniéndose solo ante errores críticos.

El flujo completo es:

```
FASE 1 — Verificaciones previas
  ├── Verificar .NET SDK instalado
  ├── Verificar EF Core Tools instalado (instalar si falta)
  ├── Detectar ADB (si no existe, marcar SKIP_ANDROID=1)
  ├── Liberar puertos 5000 y 5001 (matar procesos que los ocupen)
  └── Crear carpeta src\GeoFoto.Api\wwwroot\uploads si no existe

FASE 2 — Base de datos
  ├── Aplicar migrations con dotnet ef database update
  └── Si falla → crear migration inicial y reintentar

FASE 3 — Compilación de toda la solución
  ├── dotnet build src\GeoFoto.Api       --configuration Debug --verbosity quiet
  ├── dotnet build src\GeoFoto.Shared    --configuration Debug --verbosity quiet
  ├── dotnet build src\GeoFoto.Web       --configuration Debug --verbosity quiet
  ├── dotnet build src\GeoFoto.Mobile    --configuration Debug --framework net10.0-android --verbosity minimal
  │     (solo si SKIP_ANDROID == 0)
  └── Si alguno falla → mostrar ERROR y detener

FASE 4 — Lanzar API
  ├── Lanzar en ventana separada: start "GeoFoto.Api" cmd /k "..."
  │     cd src\GeoFoto.Api
  │     set ASPNETCORE_ENVIRONMENT=Development
  │     dotnet run --configuration Debug --urls "http://0.0.0.0:5000" --no-build
  ├── Esperar 10 segundos
  ├── Verificar con: curl -s http://localhost:5000/api/puntos
  │     Si falla → reintentar hasta 5 veces con 5s entre intentos
  └── Si no responde después de 5 intentos → WARN (no detener)

FASE 5 — Lanzar Web
  ├── Lanzar en ventana separada: start "GeoFoto.Web" cmd /k "..."
  │     cd src\GeoFoto.Web
  │     set ASPNETCORE_ENVIRONMENT=Development
  │     dotnet run --configuration Debug --urls "http://localhost:5001" --no-build
  ├── Esperar 8 segundos
  └── Verificar con: curl -s http://localhost:5001 (buscar código 200 o 302)

FASE 6 — Deploy Android (solo si SKIP_ANDROID == 0)
  ├── Reiniciar servidor ADB (kill-server + start-server)
  ├── Detectar dispositivo con 3 reintentos de 5 segundos
  │     Si no encuentra → WARN y saltar fase
  ├── Configurar túnel: adb reverse tcp:5000 tcp:5000
  ├── Buscar APK más reciente en: src\GeoFoto.Mobile\bin\Debug\net10.0-android\*-Signed.apk
  │     Si no existe → ERROR (la compilación de la fase 3 falló)
  ├── Desinstalar versión anterior: adb uninstall APP_ID
  ├── Instalar APK con hasta 3 reintentos
  │     Si falla todos → reintentar con flag -d (downgrade)
  ├── Limpiar cache WebView: adb shell pm clear APP_ID
  │     (esto previene pantalla en blanco tras reinstalación)
  ├── Lanzar la app: adb shell monkey -p APP_ID -c android.intent.category.LAUNCHER 1
  ├── Esperar 8 segundos
  └── Verificar proceso: adb shell ps -A | findstr geofoto

FASE 7 — Verificación final y resumen
  ├── Verificar API: curl http://localhost:5000/api/puntos
  ├── Abrir Swagger: start "" "http://localhost:5000/swagger"
  ├── Abrir Web: start "" "http://localhost:5001"
  └── Mostrar resumen con estado de cada componente
```

---

## REQUISITOS TÉCNICOS DEL SCRIPT

1. Usar `@echo off` y `setlocal enabledelayedexpansion`
2. Usar `cd /d "%~dp0.."` al inicio para posicionar en la raíz del repo
   (los scripts están en scripts/, el código en src/)
3. Variables de configuración al inicio del script:
   - ADB: ruta al adb.exe (leer de scripts/06_demo_completa.bat el valor real)
   - APP_ID: package ID de la app (leer de scripts/06_demo_completa.bat)
   - API_PORT: 5000
   - WEB_PORT: 5001
4. Cada fase debe tener un encabezado claro con número de fase y nombre
5. Cada paso exitoso muestra [OK], cada advertencia [WARN], cada error [ERROR]
6. Los errores en compilación (FASE 3) son fatales — detener el script
7. Los errores en API/Web/Android son warnings — continuar y reportar al final
8. Usar `--no-build` en dotnet run (fases 4 y 5) porque ya compilamos en fase 3
9. La API debe escuchar en 0.0.0.0:5000 (no localhost) para que el celular acceda
10. Agregar regla de firewall para el puerto de la API (profile=private)
11. Al final mostrar un cuadro resumen con URLs y estado de cada componente
12. El script debe ser idempotente: puede correrse múltiples veces sin romper nada

---

## MANEJO DE ERRORES ESPECÍFICOS

### Puerto ocupado
Antes de lanzar API y Web, matar cualquier proceso en esos puertos:
```batch
for /f "tokens=5" %%a in ('netstat -ano 2^>nul ^| findstr ":%API_PORT% "') do (
    taskkill /PID %%a /F >nul 2>&1
)
```

### ADB no encontrado
No es fatal. Setear SKIP_ANDROID=1 y continuar sin la fase 6.
Al final del resumen indicar: "Mobile: OMITIDO (ADB no encontrado)"

### APK no encontrado
Si la compilación de Mobile fue exitosa pero no se encuentra el APK,
buscar recursivamente en toda la carpeta bin/:
```batch
for /r "src\GeoFoto.Mobile\bin" %%f in (*-Signed.apk) do set APK_PATH=%%f
```

### API no responde
Si después de 5 intentos curl sigue fallando:
- Capturar los últimos errores del proceso de la API
- Mostrar WARN pero continuar con Web y Mobile
- Al final del resumen indicar: "API: NO RESPONDE — revisá la ventana de la API"

### Pantalla en blanco en Android
Después de instalar, limpiar cache con `adb shell pm clear APP_ID` ANTES de lanzar.
Esto previene el bug conocido de BlazorWebView tras reinstalación.

---

## ESTRUCTURA DEL RESUMEN FINAL

```
  ####################################################
  #                                                  #
  #   GEOFOTO v1.0 — REBUILD COMPLETO               #
  #                                                  #
  ####################################################

  Componente    Estado      URL
  ──────────    ──────      ───
  API           OK          http://localhost:5000
  Swagger       OK          http://localhost:5000/swagger
  Web           OK          http://localhost:5001
  Mobile        OK          Corriendo en DEVICE_ID
  BD            OK          GeoFotoDB actualizada

  Para detener: cerrá las ventanas de API y Web
  Para relanzar: ejecutá este script nuevamente
```

Si algún componente falló, reemplazar OK con el mensaje de error.

---

## NOMBRE Y UBICACIÓN DEL ARCHIVO

Archivo: scripts/07_full_rebuild.bat
El archivo debe ser autocontenido (no llamar a los otros scripts BAT).
Debe contener toda la lógica internamente para evitar dependencias circulares
y garantizar que los valores de configuración sean consistentes.

---

## VERIFICACIÓN

Después de crear el script:
1. Ejecutalo
2. Verificá que la API responde en http://localhost:5000/swagger
3. Verificá que la Web carga en http://localhost:5001
4. Verificá que la app se instala y abre en el celular
5. Si algo falla, leé el error, corregí el script y reintentá
6. No reportes éxito hasta que los 3 componentes funcionen

Cuando todo funcione: "07_full_rebuild.bat operativo — API + Web + Mobile corriendo."
```
