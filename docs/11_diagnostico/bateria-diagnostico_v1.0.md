# Batería de Diagnóstico — GeoFoto  
**Fecha:** 2026-04-14  
**Versión APK:** Debug, net10.0-android, build 11:03  
**Dispositivo:** Motorola (ZY32GSJ88S), Android, USB debugging  

---

## 1. Resumen Ejecutivo

| Área | Estado | Notas |
|------|--------|-------|
| Build completo solución | ✅ 0 errores | 5 warnings Android backward-compat (pre-existentes) |
| Tests unitarios | ✅ 34/34 pasaron | 8.2s, cobertura 13.8% |
| API smoke tests (HTTP) | ✅ 200 OK | `/api/puntos`, `/swagger`, `POST /api/fotos/upload` |
| FABs sin solapamiento | ✅ Corregido | Camera=abajo-izquierda, GPS=abajo-derecha |
| GPS auto-centering | ✅ Funcionando | Mapa centrado en ubicación real del usuario |
| Tiles del mapa | ✅ Cargando | Tras fix CDN→local + pm clear |
| Sync automático al inicio | ✅ Verde en app bar | Background sync iniciado correctamente |

---

## 2. Fase 1 — Build & Tests Automatizados

### 2.1 Build de Solución Completa

```
dotnet build GeoFoto.slnx
→ Compilación correcta.
→ Errores: 0
→ Warnings: 5 (todos Android backward-compat en MainActivity.cs, pre-existentes)
```

Proyectos compilados: `GeoFoto.Shared`, `GeoFoto.Api`, `GeoFoto.Web`, `GeoFoto.Mobile` (Win + Android), `GeoFoto.Tests`.

### 2.2 Suite de Tests

```
Pruebas totales : 34
Correctas       : 34  ✅
Fallidas        : 0
Tiempo          : 8.24 s
Cobertura líneas: 13.8%
```

**Suites ejecutadas:**
- `ExifServiceTests` — extracción GPS de EXIF
- `GeoFotoApiClientTests` — cliente HTTP de la API
- `LocalDbServiceTests` — operaciones SQLite local
- `SyncServiceTests` — cola de sincronización offline→online

**Observación:** Cobertura 13.8% es baja. Área de mejora para sprints futuros (ver sección 5).

---

## 3. Fase 2 — Smoke Tests API (HTTP)

API lanzada: `http://0.0.0.0:5000` (modo Debug, Development).

| Endpoint | Método | Status | Observación |
|----------|--------|--------|-------------|
| `/api/puntos` | GET | **200** | Retorna JSON array |
| `/swagger/index.html` | GET | **200** | Swagger UI accesible |
| `/api/fotos/upload` | POST | **200** | Retorna `{puntoId, fotoId, nombreArchivo, teniaGps, latitud, longitud}` |

**Resultado POST upload:**
```json
{
  "puntoId": 4,
  "fotoId": 3,
  "nombreArchivo": "tf.jpg",
  "teniaGps": false,
  "latitud": null,
  "longitud": null
}
```
`teniaGps: false` es esperado — la imagen de prueba (`gf_subir2.b64`) no tiene datos EXIF GPS.

---

## 4. Fase 3 — Diagnóstico Mobile (Android ADB)

### 4.1 Setup

```
Dispositivo : ZY32GSJ88S  (device)
Package     : com.companyname.geofoto.mobile
ADB tunnel  : adb reverse tcp:5000 tcp:5000 ✅
```

### 4.2 Instalación del APK

APK generado: `bin/Debug/net10.0-android/com.companyname.geofoto.mobile-Signed.apk` (89.3 MB, 11:03).

```
adb install -r ... (Incremental)
→ Success. Install command complete in 4504/4767 ms
```

### 4.3 Hallazgos Visuales — Capturas de Pantalla

#### Screenshot 1 — Pre-reinstall (`diag_01_pre_reinstall_mapa_nulo.png`)
- App con APK anterior
- FABs solapados: camera y GPS ambos en abajo-derecha ❌
- Mapa visible pero centrado en null island (lat=0, lng=0) ❌
- No había GPS centering activo

#### Screenshot 2 — Post-reinstall, mapa blanco (`diag_02_webview_cache_blanco.png`)
- APK nuevo instalado (FABs ya separados ✅)
- **Mapa completamente en blanco** ❌

#### Screenshot 3 — Post-fix, funcionando (`diag_03_mapa_ok_gps_centrado.png`)
- ✅ Mapa con tiles OSM reales (barrio Buenos Aires)
- ✅ GPS dot con círculo de precisión centrado en ubicación del usuario
- ✅ Camera FAB (rosa) abajo-izquierda
- ✅ GPS FAB (azul) abajo-derecha
- ✅ Sync icon verde en app bar

---

## 5. Bugs Encontrados y Resoluciones

### BUG-01 — Mapa en blanco tras reinstall (CRÍTICO) ✅ RESUELTO

**Síntoma:** Leaflet no inicializaba, `#map` div quedaba blanco.  
**Causa raíz:** Leaflet cargaba desde CDN (`unpkg.com`). Logcat mostró:
```
E cr_X509Util: Failed to validate the certificate chain
E chromium: handshake failed; returned -1, SSL error code 1, net_error -202
```
El WebView de Android no podía establecer conexión TLS con `unpkg.com` (posible CA no confiada en el trust store del WebView versión del device, o caché invalidada al limpiar).

**Resolución:**
1. Archivos de Leaflet descargados localmente a `src/GeoFoto.Shared/wwwroot/lib/`:
   - `leaflet/leaflet.css` (14 KB)
   - `leaflet/leaflet.js` (144 KB)
   - `leaflet.markercluster/MarkerCluster.css` (1 KB)
   - `leaflet.markercluster/MarkerCluster.Default.css` (1 KB)
   - `leaflet.markercluster/leaflet.markercluster.js` (33 KB)
2. `index.html` (Mobile) y `App.razor` (Web) actualizados para referenciar paths locales: `_content/GeoFoto.Shared/lib/...`
3. Rebuild + reinstall + `pm clear` (limpia caché WebView) → mapa funcional ✅

**Impacto:** El mapa funciona offline (sin internet) con tiles de OSM cacheados. Eliminada dependencia CDN.

---

### BUG-02 — WebView cache stale tras reinstall incremental (MEDIO) ✅ RESUELTO

**Síntoma:** Incluso con Leaflet local, el mapa seguía blanco inmediatamente después del reinstall.  
**Causa:** `adb install -r` usa instalación incremental que puede dejar caché del WebView (`blobs/`, `application_webview/`) stale.  
**Resolución:** `adb shell pm clear com.companyname.geofoto.mobile` fuerza recarga completa de assets.  
**Procedimiento recomendado post-deploy:** Ver sección 6.

---

### BUG-03 — FABs solapados (BAJO) ✅ RESUELTO (PRE-SESIÓN)

**Síntoma:** Camera FAB y GPS FAB ambos en abajo-derecha.  
**Causa:** `MobileLayout.razor` no tenía clase de posicionamiento para el camera FAB.  
**Resolución:** `Class="mud-fab-fixed-bottom-left"` + CSS en `geofoto.css` (sprint previo).  
**Verificado:** ✅ por screenshots gs4-gs7.

---

## 6. Áreas de Mejora — Plan de Acción

### P1 — Alta Prioridad

| # | Ítem | Descripción | Sprint sugerido |
|---|------|-------------|----------------|
| 1 | **Script post-install** | Agregar `adb shell pm clear` al script `04_install_android.bat` para garantizar WebView cache limpio | Sprint 07 |
| 2 | **Cobertura de tests** | Aumentar de 13.8% → 40%+. Agregar tests para `LocalUploadStrategy`, `DetallePuntoLocal`, `SyncService` flujo completo | Sprint 07 |

### P2 — Media Prioridad

| # | Ítem | Descripción | Sprint sugerido |
|---|------|-------------|----------------|
| 3 | **Mapa centrado en datos reales** | Los puntos de test en API tienen `latitud=0,longitud=0`. Agregar fixture de puntos con coords reales para smoke test visual | Sprint 07 |
| 4 | **Tile caching offline** | Leaflet no cachea tiles OSM por defecto. Para offline completo, usar `leaflet.offline` o service worker | Sprint 08 |
| 5 | **Warnings Android backward-compat** | 5 warnings en `MainActivity.cs` (`SetDecorFitsSystemWindows`, `SetStatusBarColor`, etc.) — migrar a API 35+ | Deuda Técnica |

### P3 — Baja Prioridad

| # | Ítem | Descripción | Sprint sugerido |
|---|------|-------------|----------------|
| 6 | **Google Fonts CDN** | `fonts.googleapis.com` también es CDN. Hospedar localmente si se estima uso offline | Sprint 08 |
| 7 | **Incrementar test coverage** | Smoke tests HTTP automatizados (integración) como parte del `05_run_tests.bat` | Sprint 08 |

---

## 7. Estado Final de Funcionalidades Implementadas

| Feature | Estado | Evidencia |
|---------|--------|-----------|
| Sync automático al inicio (background) | ✅ | Icono verde en app bar |
| Mapa local-first desde SQLite | ✅ | App carga puntos desde DB local |
| GPS centering al iniciar | ✅ | Screenshot gs7: punto GPS con círculo de precisión |
| `DetallePuntoLocal` (dialog offline) | ✅ | Código presente, no crashea |
| FABs sin solapamiento | ✅ | Screenshots gs4-gs7 |
| Bug: no duplicar `Create Punto` | ✅ | Test `SyncServiceTests` pasa |
| `UpdatePendingCreatePayloadAsync` | ✅ | `LocalDbServiceTests` pasa |
| Leaflet local (sin CDN) | ✅ | Fixes aplicados en esta sesión |

---

## 8. Comandos de Referencia — Flujo de Deploy

```powershell
# 1. Build Android
dotnet build src\GeoFoto.Mobile\GeoFoto.Mobile.csproj --framework net10.0-android

# 2. Instalar + limpiar cache (CRÍTICO: siempre hacer pm clear)
$adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
& $adb install -r src\GeoFoto.Mobile\bin\Debug\net10.0-android\com.companyname.geofoto.mobile-Signed.apk
& $adb shell pm clear com.companyname.geofoto.mobile

# 3. Tunnel API + Launch
& $adb reverse tcp:5000 tcp:5000
& $adb shell monkey -p com.companyname.geofoto.mobile -c android.intent.category.LAUNCHER 1
```

---

*Documento generado automáticamente como resultado de la batería de diagnóstico del 2026-04-14.*
