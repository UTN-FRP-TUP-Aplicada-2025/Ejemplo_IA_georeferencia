# Plan de Iteración — Sprint 03

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** plan-iteracion_sprint-03_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico

---

## 1. Información del Sprint

| Campo | Valor |
|-------|-------|
| Sprint | 03 |
| Fase | Fase 1 — Núcleo Online |
| Fecha inicio | 2026-05-18 |
| Fecha fin | 2026-05-31 |
| Duración | 2 semanas |
| Sprint Goal | Eliminación de puntos y app Android funcional: se completa el CRUD online y se lanza la primera versión de la app MAUI con mapa y captura desde cámara. Se demuestra la misma funcionalidad en navegador y en dispositivo Android. |
| Velocidad planificada | 19 pts |

---

## 2. Épicas y User Stories

| Épica | US | Título | Puntos | Prioridad |
|-------|----|--------|--------|-----------|
| GEO-E02 | GEO-US08 | Eliminar punto | 3 | Must |
| GEO-E03 | GEO-US09 | App Android con mapa | 8 | Must |
| GEO-E03 | GEO-US10 | Captura con cámara | 8 | Must |

**Total Story Points:** 19

---

## 3. Objetivo de la Demo

Al cierre del Sprint 03, se podrá demostrar:

1. **Eliminación con confirmación:** En GeoFoto.Web, el usuario elimina un punto; se muestra un MudDialog de confirmación y, al aceptar, se eliminan el punto, sus fotos y los archivos físicos del servidor.
2. **App Android funcional:** GeoFoto.Mobile ejecutándose en emulador o dispositivo Android muestra el mapa con markers (misma UI que Web gracias a Shared).
3. **Captura con cámara:** Desde la app Android, el usuario presiona el MudFab de cámara, se abre la cámara del dispositivo, captura una foto y la sube al servidor con las coordenadas GPS del dispositivo.

---

## 4. Descomposición de Tareas

### GEO-US08 — Eliminar punto (3 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T30 | Crear MudDialog de confirmación de eliminación | Shared | 1h | - | To Do |
| GEO-T31 | Crear endpoint DELETE /api/puntos/{id} con cascade a fotos | Api | 1h | - | To Do |
| GEO-T32 | Eliminar archivos físicos del servidor al borrar punto | Api | 1h | - | To Do |

### GEO-US09 — App Android con mapa (8 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T33 | Configurar GeoFoto.Mobile con BlazorWebView | Mobile | 2h | - | To Do |
| GEO-T34 | Registrar servicios HTTP y DI en MauiProgram.cs | Mobile | 1h | - | To Do |
| GEO-T35 | Configurar permisos Android (cámara, ubicación, internet, almacenamiento) | Mobile | 1h | - | To Do |
| GEO-T36 | Verificar Leaflet.js funcional en WebView Android | Mobile | 2h | - | To Do |
| GEO-T37 | Crear MudAppBar diferenciado para mobile (sin drawer permanente) | Shared | 1h | - | To Do |

### GEO-US10 — Captura con cámara (8 pts)

| ID Jira | Tarea | Componente | Estimación | Responsable | Estado |
|---------|-------|-----------|------------|-------------|--------|
| GEO-T38 | Implementar MudFab con icono cámara en pantalla Mapa (solo mobile) | Shared | 1h | - | To Do |
| GEO-T39 | Implementar MediaPicker.CapturePhotoAsync() para captura nativa | Mobile | 3h | - | To Do |
| GEO-T40 | Conectar captura con POST /api/fotos/upload y geolocalización | Mobile | 2h | - | To Do |

---

## 5. Criterios de Aceptación del Sprint

### CA-S10 — Eliminación con confirmación

```gherkin
Dado que el usuario visualiza un punto en la lista
Cuando hace click en "Eliminar"
Entonces se muestra un MudDialog de confirmación
Y al confirmar, el punto y sus fotos se eliminan del servidor
```

### CA-S11 — App Android con mapa

```gherkin
Dado que GeoFoto.Mobile está instalada en un dispositivo Android
Cuando el usuario abre la app
Entonces se muestra el mapa Leaflet con los markers existentes (requiere conexión)
```

### CA-S12 — Captura con cámara

```gherkin
Dado que el usuario está en la app Android con conexión a internet
Cuando presiona el botón de cámara, captura una foto y confirma
Entonces la foto se sube al servidor con las coordenadas GPS del dispositivo
Y aparece un nuevo marker en el mapa
```

---

## 6. Dependencias y Riesgos

| # | Riesgo / Dependencia | Probabilidad | Impacto | Mitigación |
|---|---------------------|-------------|---------|-----------|
| 1 | Sprint 02 incompleto (API o mapa no funcional) | Media | Crítico | US08, US09 y US10 dependen de la API y Leaflet del Sprint 02 |
| 2 | Emulador Android lento en máquinas de desarrollo | Alta | Medio | Usar dispositivo físico vía USB debugging |
| 3 | MediaPicker no disponible en todos los emuladores | Media | Alto | Probar en dispositivo físico; mock en emulador |
| 4 | Permisos Android denegados por el usuario | Media | Medio | Implementar flujo de solicitud de permisos con mensajes claros |

---

## 7. Definiciones

| Ceremonia | Fecha tentativa | Duración |
|-----------|----------------|----------|
| Sprint Planning | 2026-05-18 | 2h |
| Daily Standup | Lunes a viernes | 15min |
| Sprint Review | 2026-05-31 | 1h |
| Sprint Retrospective | 2026-05-31 | 45min |

---

## 8. Métricas planificadas

| Métrica | Objetivo |
|---------|----------|
| Velocidad | 19 pts |
| Tareas completadas | 11/11 |
| Cobertura de tests | N/A |
| Bugs abiertos al cierre | ≤ 2 |

---

## Control de Cambios

| Versión | Fecha | Autor | Descripción |
|---------|-------|-------|-------------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial |

---

**Fin del documento**
