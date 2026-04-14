# Arquitectura de la Solución — GeoFoto

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** arquitectura-solucion_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico  

---

## 1. Propósito

Este documento describe la arquitectura técnica del sistema **GeoFoto**, una aplicación de registro georeferenciado de fotografías con capacidad de operación sin conectividad. Se definen los componentes principales de la solución, sus responsabilidades individuales, las dependencias entre proyectos, el flujo de datos completo y las decisiones técnicas que sustentan el diseño.

El objetivo es proporcionar una visión integral de la arquitectura que sirva como referencia para el equipo de desarrollo durante la implementación, las revisiones técnicas y la evolución futura del producto.

---

## 2. Alcance

La arquitectura descrita en este documento cubre:

- Los cuatro proyectos que componen la solución (`GeoFoto.Api`, `GeoFoto.Shared`, `GeoFoto.Web`, `GeoFoto.Mobile`) y sus dependencias.  
- El patrón **offline-first** adoptado para la aplicación móvil.  
- El flujo de datos desde la captura en campo hasta la persistencia centralizada.  
- Las elecciones tecnológicas y sus justificaciones.  
- La estrategia de reutilización de componentes mediante Razor Class Library.  
- La integración de librerías de UI (MudBlazor) y mapas (Leaflet.js).  

**No se incluyen** en este documento:

- Detalles de infraestructura de despliegue (servidores, contenedores, dominios).  
- Configuración de red, firewalls o balanceadores de carga.  
- Procedimientos de operación o monitoreo en producción.  

---

## 3. Estilo Arquitectónico

Se adopta un estilo arquitectónico híbrido que combina los siguientes patrones:

### 3.1 Offline-First

La aplicación móvil opera con **SQLite como fuente de verdad operativa local**. Toda escritura se realiza primero contra la base de datos local del dispositivo, independientemente del estado de la conectividad. Cuando se detecta conexión disponible, un servicio de sincronización transfiere los registros pendientes hacia la API REST centralizada.

### 3.2 API-First

La API REST (`GeoFoto.Api`) constituye el **punto único de integración** entre los clientes y la persistencia centralizada en SQL Server. Toda operación de lectura y escritura desde la aplicación web, y toda operación de sincronización desde la aplicación móvil, se canaliza a través de los endpoints de esta API.

### 3.3 Componentes Compartidos vía RCL

Se utiliza una **Razor Class Library** (`GeoFoto.Shared`) como proyecto compartido que contiene las páginas Blazor, los componentes de UI y los servicios HTTP. Tanto `GeoFoto.Web` como `GeoFoto.Mobile` referencian este proyecto, eliminando la duplicación de código de presentación.

### 3.4 Principios Rectores

| Principio | Descripción |
|---|---|
| Alta cohesión | Cada proyecto agrupa responsabilidades relacionadas y bien delimitadas. |
| Bajo acoplamiento | Las dependencias entre proyectos se establecen mediante interfaces y contratos. |
| Escritura local primero | En el cliente móvil, el dato se persiste localmente antes de cualquier intento de sincronización. |
| Sincronización automática | Al recuperar conectividad, el servicio de sincronización opera sin intervención del usuario. |
| Reutilización de UI | Las páginas y componentes Blazor se escriben una vez y se consumen en ambos hosts. |

---

## 4. Diagrama de Componentes

```text
┌─────────────────────┐         ┌──────────────────────┐
│    GeoFoto.Web      │         │   GeoFoto.Mobile     │
│   (Blazor Host)     │         │   (MAUI Hybrid)      │
│  InteractiveServer  │         │   BlazorWebView      │
└──────────┬──────────┘         └──────────┬───────────┘
           │                               │
           │    ┌──────────────────┐       │
           │    │    SQLite        │◄──────┘ (local)
           │    │  sqlite-net-pcl  │
           │    └──────────────────┘
           │                               │
           └─────────────┬─────────────────┘
                         │ referencia NuGet
             ┌───────────▼───────────┐
             │    GeoFoto.Shared     │
             │  (Razor Class Lib)    │
             │                       │
             │  Pages:               │
             │   Mapa.razor          │
             │   SubirFotos.razor    │
             │   ListaPuntos.razor   │
             │   EstadoSync.razor    │
             │                       │
             │  Components:          │
             │   DetallePunto        │
             │   MarkerPopup         │
             │   FotoCarousel        │
             │   SyncStatusBadge     │
             │                       │
             │  MudBlazor + Leaflet  │
             └───────────┬───────────┘
                         │ HTTP
             ┌───────────▼───────────┐
             │     GeoFoto.Api       │
             │  (ASP.NET Core API)   │
             │                       │
             │  Controllers:         │
             │   PuntosController    │
             │   FotosController     │
             │   SyncController      │
             │                       │
             │  EF Core + SQL Server │
             │  MetadataExtractor    │
             └───────────────────────┘
```

> **Nota:** La aplicación móvil mantiene una conexión directa con SQLite para operaciones locales. La comunicación con la API se realiza exclusivamente a través de HTTP cuando existe conectividad disponible.

---

## 5. Descripción de Cada Proyecto

### 5.1 GeoFoto.Api

**Tipo:** ASP.NET Core Web API (.NET 8)  
**Responsabilidad:** Gestionar la persistencia centralizada, exponer endpoints REST y procesar metadatos de fotografías.

| Aspecto | Detalle |
|---|---|
| **Framework** | ASP.NET Core 8 — Minimal Hosting |
| **ORM** | Entity Framework Core con proveedor SQL Server |
| **Controllers** | `PuntosController` — CRUD de puntos georeferenciados |
|  | `FotosController` — Subida, descarga y eliminación de fotografías |
|  | `SyncController` — Recepción de lotes de sincronización desde clientes móviles |
| **DbContext** | `GeoFotoDbContext` — Mapeo de entidades `Punto`, `Foto`, `SyncLog` |
| **Extracción EXIF** | Librería `MetadataExtractor` para lectura de coordenadas GPS, fecha de captura y orientación desde archivos de imagen |
| **Almacenamiento de archivos** | Carpeta `wwwroot/uploads/` organizada por fecha (`yyyy/MM/dd/`) |
| **Validación** | Data Annotations y FluentValidation en DTOs de entrada |
| **Serialización** | `System.Text.Json` con política camelCase |

**Endpoints principales:**

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/puntos` | Listar todos los puntos georeferenciados |
| `GET` | `/api/puntos/{id}` | Obtener detalle de un punto con sus fotos |
| `POST` | `/api/puntos` | Crear un nuevo punto |
| `PUT` | `/api/puntos/{id}` | Actualizar un punto existente |
| `DELETE` | `/api/puntos/{id}` | Eliminar un punto y sus fotos asociadas |
| `POST` | `/api/fotos` | Subir una o más fotografías asociadas a un punto |
| `DELETE` | `/api/fotos/{id}` | Eliminar una fotografía |
| `POST` | `/api/sync/push` | Recibir lote de registros desde el cliente móvil |
| `GET` | `/api/sync/pull?desde={timestamp}` | Devolver registros modificados desde un timestamp |

---

### 5.2 GeoFoto.Shared

**Tipo:** Razor Class Library (.NET 8)  
**Responsabilidad:** Contener toda la UI compartida (páginas, componentes, estilos) y los contratos de servicios HTTP.

| Aspecto | Detalle |
|---|---|
| **Framework** | Razor Class Library con soporte Blazor |
| **UI Framework** | MudBlazor (componentes Material Design) |
| **Mapas** | Leaflet.js integrado mediante `IJSRuntime` interop |
| **Páginas** | `Mapa.razor` — Visualización de puntos en mapa interactivo |
|  | `SubirFotos.razor` — Captura y subida de fotografías con geolocalización |
|  | `ListaPuntos.razor` — Listado filtrable de puntos registrados |
|  | `EstadoSync.razor` — Panel de estado de sincronización (solo móvil) |
| **Componentes** | `DetallePunto` — Tarjeta con información completa de un punto |
|  | `MarkerPopup` — Popup del marcador en el mapa con resumen del punto |
|  | `FotoCarousel` — Carrusel de fotografías asociadas a un punto |
|  | `SyncStatusBadge` — Indicador visual del estado de sincronización |
| **Servicios (interfaces)** | `IPuntosService` — Operaciones CRUD sobre puntos |
|  | `IFotosService` — Operaciones de subida y gestión de fotos |
| **Assets estáticos** | `wwwroot/js/leafletInterop.js` — Funciones JS para inicializar y controlar el mapa |
|  | `wwwroot/css/geofoto.css` — Estilos específicos del proyecto |

> **Nota:** Las implementaciones concretas de `IPuntosService` e `IFotosService` se registran en cada host. En `GeoFoto.Web` se inyectan implementaciones HTTP que consumen la API. En `GeoFoto.Mobile` se inyectan implementaciones que operan contra SQLite local.

---

### 5.3 GeoFoto.Web

**Tipo:** Blazor Web App (.NET 8)  
**Responsabilidad:** Hospedar la aplicación web de escritorio con renderizado interactivo del lado del servidor.

| Aspecto | Detalle |
|---|---|
| **Framework** | Blazor Web App con `@rendermode InteractiveServer` |
| **Hosting** | Kestrel / IIS como proxy reverso |
| **Referencia** | Proyecto `GeoFoto.Shared` (todas las páginas y componentes) |
| **DI — Servicios HTTP** | `HttpClient` configurado con `BaseAddress` apuntando a `GeoFoto.Api` |
|  | Registro de `IPuntosService` → `PuntosHttpService` |
|  | Registro de `IFotosService` → `FotosHttpService` |
| **MudBlazor** | Registrado en `Program.cs` con `builder.Services.AddMudServices()` |
|  | `MudThemeProvider` configurado en `App.razor` o `MainLayout.razor` |
| **Leaflet.js** | Scripts y hojas de estilo referenciados en `App.razor` (`<head>`) |
| **Enrutamiento** | Enrutamiento Blazor estándar con `<Router>` apuntando a ensamblados de `GeoFoto.Shared` |

**Configuración de `Program.cs` (extracto conceptual):**

```csharp
builder.Services.AddMudServices();
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri("https://localhost:5001") });
builder.Services.AddScoped<IPuntosService, PuntosHttpService>();
builder.Services.AddScoped<IFotosService, FotosHttpService>();
```

---

### 5.4 GeoFoto.Mobile

**Tipo:** .NET MAUI Hybrid — Android  
**Responsabilidad:** Hospedar la aplicación móvil con capacidad offline-first, almacenamiento local y sincronización.

| Aspecto | Detalle |
|---|---|
| **Framework** | .NET MAUI 8 con `BlazorWebView` |
| **Plataforma** | Android (API 24+) |
| **Referencia** | Proyecto `GeoFoto.Shared` (todas las páginas y componentes) |
| **SQLite** | Paquete `sqlite-net-pcl` para persistencia local |
|  | `ILocalDbService` — Interfaz para operaciones CRUD locales |
|  | `LocalDbService` — Implementación con base de datos en `FileSystem.AppDataDirectory` |
| **Sincronización** | `ISyncService` — Contrato para el proceso de sincronización |
|  | `SyncService` — Implementación del patrón Outbox: lee registros pendientes de la tabla `SyncQueue`, los envía a `/api/sync/push` y marca como sincronizados |
| **Conectividad** | `ConnectivityService` — Wrapper sobre `Microsoft.Maui.Networking.Connectivity` para detectar cambios de red |
|  | Dispara sincronización automática al detectar transición de offline a online |
| **Almacenamiento de archivos** | `IFileStorageService` — Abstracción para guardar fotos en almacenamiento local del dispositivo |
|  | `FileStorageService` — Implementación que persiste archivos en `FileSystem.AppDataDirectory/photos/` |
| **Extracción EXIF local** | `IExifService` — Lectura de metadatos GPS desde la foto capturada por la cámara |
|  | `ExifService` — Implementación con `MetadataExtractor` o `ExifLib` |
| **DI — Servicios** | `IPuntosService` → `PuntosLocalService` (opera contra SQLite) |
|  | `IFotosService` → `FotosLocalService` (opera contra SQLite + FileStorage) |
| **Permisos** | Cámara, ubicación (fine + coarse), almacenamiento, acceso a red |

**Modelo de datos local (SQLite):**

| Tabla | Columnas principales |
|---|---|
| `Puntos` | `Id`, `Latitud`, `Longitud`, `Descripcion`, `FechaCreacion`, `Sincronizado` |
| `Fotos` | `Id`, `PuntoId`, `RutaLocal`, `Latitud`, `Longitud`, `FechaCaptura`, `Sincronizado` |
| `SyncQueue` | `Id`, `Entidad`, `EntidadId`, `Operacion`, `Payload`, `FechaCreacion`, `Estado` |

---

## 6. Integración de MudBlazor

MudBlazor se instala como dependencia del proyecto `GeoFoto.Shared`, lo cual permite que tanto `GeoFoto.Web` como `GeoFoto.Mobile` hereden automáticamente los componentes de Material Design sin configuración adicional en cada host.

### 6.1 Configuración del Tema

Se define un tema personalizado en `GeoFoto.Shared` que establece la paleta de colores, tipografía y espaciado coherentes con la identidad visual del proyecto:

```csharp
public static MudTheme GeoFotoTheme => new()
{
    PaletteLight = new PaletteLight
    {
        Primary    = "#2E7D32",   // Verde naturaleza
        Secondary  = "#1565C0",   // Azul cartográfico
        AppbarBackground = "#2E7D32"
    }
};
```

### 6.2 Componentes Utilizados

| Componente MudBlazor | Uso en GeoFoto |
|---|---|
| `MudAppBar` + `MudNavMenu` | Barra de navegación principal y menú lateral |
| `MudCard` | Tarjetas de detalle de punto y de fotografía |
| `MudDataGrid` | Listado de puntos con filtros, ordenamiento y paginación |
| `MudFileUpload` | Selección de archivos de imagen para subida |
| `MudSnackbar` | Notificaciones de éxito, error y estado de sincronización |
| `MudDialog` | Confirmaciones de eliminación y detalle ampliado de foto |
| `MudChip` | Indicadores de estado de sincronización por registro |
| `MudProgressLinear` | Progreso de subida de fotografías y de sincronización |

### 6.3 Registro en Cada Host

En `GeoFoto.Web`, se registra en `Program.cs`:

```csharp
builder.Services.AddMudServices();
```

En `GeoFoto.Mobile`, se registra en `MauiProgram.cs`:

```csharp
builder.Services.AddMudServices();
```

Ambos hosts incluyen la referencia al tema en su layout raíz mediante `<MudThemeProvider Theme="GeoFotoTheme" />`.

---

## 7. Flujo de Datos

El flujo de datos del sistema sigue la ruta completa:

```
Campo (dispositivo) → SQLite local → SyncQueue → API REST → SQL Server
```

### 7.1 Escenario 1 — Operación Web con Conectividad

1. El usuario accede a `GeoFoto.Web` desde un navegador.  
2. Selecciona una ubicación en el mapa (Leaflet.js) o ingresa coordenadas manualmente.  
3. Adjunta una o más fotografías mediante `MudFileUpload`.  
4. Al confirmar, `PuntosHttpService` envía un `POST /api/puntos` con los datos del punto.  
5. `FotosHttpService` envía las imágenes mediante `POST /api/fotos` con `multipart/form-data`.  
6. La API extrae metadatos EXIF de cada imagen mediante `MetadataExtractor`.  
7. Los archivos se almacenan en `wwwroot/uploads/yyyy/MM/dd/`.  
8. Las entidades `Punto` y `Foto` se persisten en SQL Server a través de EF Core.  
9. La UI se actualiza mostrando el nuevo marcador en el mapa y la notificación de éxito.  

### 7.2 Escenario 2 — Captura Móvil sin Conectividad

1. El usuario abre `GeoFoto.Mobile` en un dispositivo Android sin conexión a internet.  
2. Captura una fotografía con la cámara del dispositivo.  
3. `ExifService` extrae las coordenadas GPS de la imagen capturada.  
4. `FileStorageService` almacena la imagen en el sistema de archivos local.  
5. `PuntosLocalService` inserta el registro del punto en la tabla `Puntos` de SQLite con `Sincronizado = false`.  
6. `FotosLocalService` inserta el registro de la foto en la tabla `Fotos` de SQLite con la ruta local.  
7. Se crea un registro en la tabla `SyncQueue` con la operación pendiente (`INSERT`, entidad, payload serializado).  
8. La UI muestra el punto en el mapa local y el badge de sincronización indica "Pendiente".  

### 7.3 Escenario 3 — Sincronización al Recuperar Conectividad

1. `ConnectivityService` detecta una transición de estado de red: offline → online.  
2. Se dispara automáticamente `SyncService.SincronizarAsync()`.  
3. El servicio consulta la tabla `SyncQueue` para obtener registros con `Estado = Pendiente`, ordenados por fecha de creación.  
4. Para cada registro pendiente:  
   a. Se serializa el payload y los archivos asociados.  
   b. Se envía un `POST /api/sync/push` con el lote de operaciones.  
   c. La API procesa cada operación, persiste en SQL Server y devuelve confirmación.  
   d. Al recibir respuesta exitosa, se actualiza `SyncQueue.Estado = Sincronizado` y `Punto.Sincronizado = true`.  
5. Opcionalmente, se ejecuta un `GET /api/sync/pull?desde={ultimaSync}` para obtener registros creados por otros clientes.  
6. La UI actualiza los badges de sincronización a "Sincronizado" y muestra una notificación de éxito.  

---

## 8. Tabla de Decisiones Técnicas

| # | Decisión | Elección | Motivo |
|---|---|---|---|
| DT-01 | Framework de UI | **MudBlazor** | Proporciona componentes Material Design listos para usar, reduciendo el esfuerzo de desarrollo de UI y garantizando consistencia visual. Se descartó CSS personalizado por el costo de mantenimiento. |
| DT-02 | Base de datos local | **SQLite vía sqlite-net-pcl** | Librería ligera, multiplataforma y con API asíncrona nativa. Compatible con .NET MAUI sin dependencias nativas adicionales. |
| DT-03 | Patrón de sincronización | **Outbox Pattern** | Desacopla la escritura local de la transmisión remota. Garantiza que ningún registro se pierda incluso si la sincronización falla a mitad del proceso. La tabla `SyncQueue` actúa como cola persistente. |
| DT-04 | Resolución de conflictos | **Last-Write-Wins (LWW)** | Estrategia simple y predecible. En v1.0 se asume un único usuario por dispositivo, lo que minimiza la probabilidad de conflictos reales. El timestamp de la operación determina la versión ganadora. |
| DT-05 | Reutilización de UI | **Razor Class Library (RCL)** | Permite compartir páginas, componentes y assets estáticos entre el host web y el host móvil sin duplicación. Un único punto de mantenimiento para la capa de presentación. |
| DT-06 | Modo de renderizado web | **InteractiveServer** | Simplifica el modelo de ejecución al mantener el estado en el servidor. Evita la descarga de runtime WebAssembly. Adecuado para el escenario web donde se asume conectividad permanente. |
| DT-07 | Librería de mapas | **Leaflet.js vía IJSRuntime** | Librería de mapas open source, ligera y ampliamente adoptada. La interoperabilidad con Blazor se logra mediante `IJSRuntime.InvokeVoidAsync` para inicialización, creación de marcadores y manejo de eventos. |
| DT-08 | Extracción de metadatos | **MetadataExtractor** | Librería .NET para lectura de metadatos EXIF/GPS de archivos de imagen. Compatible con .NET 8, soporta JPEG y PNG, y no requiere dependencias nativas. |
| DT-09 | ORM servidor | **Entity Framework Core** | ORM estándar del ecosistema .NET. Ofrece migraciones, LINQ, tracking de cambios y soporte nativo para SQL Server. |
| DT-10 | Almacenamiento de fotos (servidor) | **Sistema de archivos (`wwwroot/uploads/`)** | En v1.0 se prioriza la simplicidad. Los archivos se sirven directamente como contenido estático. En v2.0 se prevé migración a Azure Blob Storage. |

---

## 9. Riesgos Arquitectónicos

| # | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|---|
| RA-01 | Rendimiento de BlazorWebView en dispositivos Android de gama baja | Media | Alto | Optimizar la cantidad de componentes renderizados simultáneamente. Implementar virtualización en listados. Realizar pruebas en dispositivos con ≤ 3 GB de RAM. |
| RA-02 | Leaflet.js no puede cargar tiles de mapa sin conexión a internet | Alta | Alto | Implementar caché de tiles descargados previamente. En v1.0 se muestra un mapa vacío con marcadores sobre fondo gris cuando no hay conectividad. En v2.0 se prevé soporte de tiles offline. |
| RA-03 | Acumulación masiva de registros en `SyncQueue` tras períodos prolongados sin conectividad | Media | Medio | Limitar el tamaño del lote de sincronización (máximo 50 registros por solicitud). Implementar reintentos con backoff exponencial. Comprimir las imágenes antes de la transmisión. |
| RA-04 | Tamaño de las fotografías impactando el tiempo y ancho de banda de sincronización | Alta | Medio | Comprimir las imágenes en el dispositivo antes de almacenarlas localmente (calidad 80%, resolución máxima 1920px). Transmitir en segundo plano sin bloquear la UI. |
| RA-05 | Concurrencia en SQLite al ejecutar sincronización mientras el usuario registra datos | Baja | Medio | Utilizar modo WAL (Write-Ahead Logging) en SQLite. Serializar las escrituras mediante `SemaphoreSlim`. Separar las operaciones de lectura del usuario de las escrituras del servicio de sincronización. |
| RA-06 | Pérdida de datos si el usuario desinstala la aplicación antes de sincronizar | Media | Alto | Mostrar advertencia visual permanente cuando existen registros pendientes de sincronización. Implementar confirmación explícita antes de permitir la limpieza de datos locales. |

---

## 10. Evolución Prevista — v2.0

Se identifican las siguientes líneas de evolución para versiones futuras del sistema:

| Área | Mejora prevista | Justificación |
|---|---|---|
| Autenticación | Implementar ASP.NET Core Identity + JWT | Permitir identificación de usuarios y control de acceso por roles. |
| Mapas offline | Descarga y caché de tiles para operación sin red | Eliminar la dependencia de conectividad para la visualización de mapas en el cliente móvil. |
| Multi-usuario | Soporte para múltiples usuarios con resolución de conflictos mejorada | Habilitar escenarios de equipos de campo con dispositivos independientes. |
| Almacenamiento en la nube | Migración de `wwwroot/uploads/` a Azure Blob Storage | Escalar el almacenamiento de fotografías sin impacto en el servidor de aplicaciones. |
| Soporte iOS | Compilación MAUI para iOS | Ampliar la cobertura de dispositivos móviles al ecosistema Apple. |
| Reportes | Generación de informes PDF con mapa y fotografías | Proporcionar documentación exportable de los relevamientos realizados. |
| Notificaciones push | Integración con Firebase Cloud Messaging | Notificar al usuario móvil sobre actualizaciones de datos pendientes de descarga. |

---

## 11. Control de Cambios

| Versión | Fecha | Autor | Cambios |
|---|---|---|---|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial — Arquitectura de la solución GeoFoto |

---

**Fin del documento**

