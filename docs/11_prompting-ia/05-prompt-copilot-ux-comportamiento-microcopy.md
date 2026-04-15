# Prompt para GitHub Copilot — UX Comportamiento + Microcopy Completo

> Pegá este prompt en Copilot Chat con modo **Edits** (`@workspace`).
> Prerequisito: los sprints anteriores (01–04) deben estar funcionales.
> Este prompt modifica comportamiento de flujos existentes y crea componentes nuevos.

---

## PROMPT

```
Sos un desarrollador .NET 10 senior especialista en UX mobile y Blazor Hybrid.
Tu tarea es implementar 7 mejoras de comportamiento UX en GeoFoto y aplicar
la guía de microcopy del proyecto en todos los textos de la interfaz.

LECTURA OBLIGATORIA antes de escribir código:
  docs/03_ux-ui/ux-writing-microcopy_v1.0.md        (TODA la guía de microcopy — es tu fuente de verdad para TODOS los textos)
  docs/03_ux-ui/wireframes-pantallas_v1.0.md
  docs/02_especificacion_funcional/especificacion-funcional_v1.0.md  (F-05, F-06, F-07, F-11)
  docs/05_arquitectura_tecnica/arquitectura-solucion_v1.0.md
  docs/02_especificacion_funcional/casos-de-uso/casos-de-uso_v1.0.md (CU-01, CU-02)
  docs/05_arquitectura_tecnica/arquitectura-offline-sync_v1.0.md     (sección 13 — UI de sync)

Confirmá con: "Documentación UX leída. Comenzando implementación de las 7 mejoras."

---

## REGLA GENERAL DE MICROCOPY

Todos los textos visibles en la UI (labels, snackbars, tooltips, placeholders,
diálogos, alertas, chips, headers de tabla) DEBEN coincidir EXACTAMENTE con
los definidos en docs/03_ux-ui/ux-writing-microcopy_v1.0.md.

Si un texto ya existe en un componente y difiere del catálogo de microcopy,
REEMPLAZALO con la versión del catálogo. Esto aplica a TODOS los componentes
que toques durante esta implementación y a cualquier otro que encuentres
con textos diferentes al catálogo.

Reglas clave del microcopy:
  - Voseo rioplatense: "Arrastrá", "Tocá", "Revisá"
  - Oraciones ≤ 12 palabras en snackbars
  - Verbos en infinitivo para botones: "Guardar", "Cancelar", "Eliminar"
  - Sin puntos finales en labels, botones, chips, badges
  - Sin jerga técnica visible al usuario (no "EXIF", "SQLite", "API", "HTTP")
  - Tooltips: infinitivo sin artículo, máximo 3 palabras
  - Placeholders: sustantivos sin artículos, "(opcional)" para campos no requeridos

---

## MEJORA 1 — MAPA CENTRADO EN UBICACIÓN DEL DISPOSITIVO AL INICIAR

### Comportamiento requerido

Al abrir la app (Mobile) o el frontend (Web), el mapa debe centrarse
automáticamente en la ubicación GPS del dispositivo con zoom nivel 15.

### Implementación Mobile (GeoFoto.Mobile)

Crear o modificar el servicio de geolocalización:

```csharp
// GeoFoto.Mobile/Services/IDeviceLocationService.cs
public interface IDeviceLocationService
{
    Task<(double Lat, double Lng)?> GetCurrentLocationAsync(CancellationToken ct = default);
}

// GeoFoto.Mobile/Services/DeviceLocationService.cs
public class DeviceLocationService : IDeviceLocationService
{
    public async Task<(double Lat, double Lng)?> GetCurrentLocationAsync(CancellationToken ct)
    {
        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request, ct);
            if (location is not null)
                return (location.Latitude, location.Longitude);
        }
        catch (FeatureNotSupportedException) { }
        catch (FeatureNotEnabledException) { }
        catch (PermissionException) { }
        return null;
    }
}
```

Registrá en MauiProgram.cs:
```csharp
builder.Services.AddSingleton<IDeviceLocationService, DeviceLocationService>();
```

### Implementación Web (GeoFoto.Web)

Usar Geolocation API del browser vía JS Interop:

En `geofoto-utils.js`:
```javascript
window.geoFotoUtils = window.geoFotoUtils || {};

window.geoFotoUtils.getCurrentPosition = function () {
    return new Promise((resolve) => {
        if (!navigator.geolocation) {
            resolve(null);
            return;
        }
        navigator.geolocation.getCurrentPosition(
            (pos) => resolve({ lat: pos.coords.latitude, lng: pos.coords.longitude }),
            () => resolve(null),
            { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 }
        );
    });
};
```

### Integración en el Componente de Mapa (GeoFoto.Shared)

En la página del mapa (`Mapa.razor` o equivalente), al inicializar:

1. Llamar al servicio de ubicación (Mobile) o JS Interop (Web).
2. Si se obtiene ubicación → `map.setView([lat, lng], 15)`.
3. Si NO se obtiene ubicación → posición por defecto Buenos Aires (-34.6037, -58.3816) con zoom 12.
4. Después, cargar los markers existentes. Si hay markers, hacer `fitBounds()` incluyendo
   la posición del usuario como un punto más del bounds.

Crear un servicio compartido que abstraiga la diferencia:

```csharp
// GeoFoto.Shared/Services/IMapInitService.cs
public interface IMapInitService
{
    Task<(double Lat, double Lng)?> GetInitialPositionAsync();
}
```

Mobile inyecta la implementación con MAUI Geolocation.
Web inyecta la implementación con IJSRuntime.
Si ambas fallan, retornan null y el mapa usa la posición por defecto.

NO mostrar snackbar cuando la ubicación se obtiene exitosamente.
SÍ mostrar si falla: MudSnackbar Warning:
  "No se pudo obtener la ubicación — mostrando posición predeterminada"

---

## MEJORA 2 — FLUJO DE PERMISOS CON MANEJO DE DENEGACIÓN

### Comportamiento requerido

Al iniciar la app Mobile:
  1. Solicitar permisos de ubicación y cámara.
  2. Si el usuario deniega → continuar con el mapa en posición por defecto.
  3. Si el usuario denegó permanentemente ("No volver a preguntar") →
     mostrar un MudDialog ofreciendo redirigir a la configuración del SO.

### Implementación

Crear servicio de permisos:

```csharp
// GeoFoto.Mobile/Services/IPermissionService.cs
public interface IPermissionService
{
    Task<PermissionResult> CheckAndRequestLocationAsync();
    Task<PermissionResult> CheckAndRequestCameraAsync();
    void OpenAppSettings();
}

public enum PermissionResult
{
    Granted,         // Permiso otorgado
    Denied,          // Denegado esta vez (se puede volver a pedir)
    PermanentlyDenied // Denegado con "No volver a preguntar"
}
```

Implementación con MAUI Essentials:

```csharp
public class PermissionService : IPermissionService
{
    public async Task<PermissionResult> CheckAndRequestLocationAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted) return PermissionResult.Granted;

        // Si ya fue denegado permanentemente
        if (status == PermissionStatus.Denied && !Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
            return PermissionResult.PermanentlyDenied;

        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        return status switch
        {
            PermissionStatus.Granted => PermissionResult.Granted,
            PermissionStatus.Denied when !Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>()
                => PermissionResult.PermanentlyDenied,
            _ => PermissionResult.Denied
        };
    }

    // Análogo para cámara con Permissions.Camera

    public void OpenAppSettings()
    {
        AppInfo.Current.ShowSettingsUI();
    }
}
```

### Diálogos (textos del catálogo de microcopy §4.1)

Permiso de ubicación denegado permanentemente → MudDialog:
  Título: "Ubicación deshabilitada"
  Cuerpo: "GeoFoto necesita acceso a tu ubicación para funcionar correctamente.
           Podés habilitarla desde la configuración del dispositivo."
  Botón primario: "Ir a Configuración"   → llama OpenAppSettings()
  Botón secundario: "Continuar sin ubicación"  → cierra el diálogo, mapa en posición por defecto

Permiso de cámara denegado permanentemente → MudDialog:
  Título: "Cámara deshabilitada"
  Cuerpo: "Para capturar fotos, habilitá el permiso de cámara en la configuración del dispositivo."
  Botón primario: "Ir a Configuración"
  Botón secundario: "Cancelar"

Crear componente reutilizable:

```razor
@* GeoFoto.Shared/Components/PermissionDeniedDialog.razor *@
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">@Title</MudText>
    </TitleContent>
    <DialogContent>
        <MudText>@Message</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">@SecondaryText</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled"
                   OnClick="GoToSettings">@PrimaryText</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public string Title { get; set; } = "";
    [Parameter] public string Message { get; set; } = "";
    [Parameter] public string PrimaryText { get; set; } = "Ir a Configuración";
    [Parameter] public string SecondaryText { get; set; } = "Cancelar";

    private void Cancel() => MudDialog.Cancel();
    private void GoToSettings() => MudDialog.Close(DialogResult.Ok(true));
}
```

### Flujo al iniciar la app

```
App inicia
  → CheckAndRequestLocationAsync()
    → Granted: obtener GPS, centrar mapa
    → Denied: mapa en posición por defecto, snackbar warning
    → PermanentlyDenied: mapa en posición por defecto + MudDialog con "Ir a Configuración"
  → CheckAndRequestCameraAsync() (en paralelo o después)
    → Granted: FAB de cámara habilitado
    → Denied: FAB de cámara habilitado (pedirá de nuevo al tocar)
    → PermanentlyDenied: FAB de cámara muestra MudDialog al tocar
```

---

## MEJORA 3 — FLUJO POST-CAPTURA DE FOTO CON MARKER ACTIVO

### Comportamiento requerido

Cuando el usuario tiene un marker seleccionado (activo) y toma una foto:

1. La foto se asocia al marker activo (no crea un marker nuevo).
2. Después de sacar la foto, el mapa vuelve a centrarse en la posición del dispositivo.
3. Si es la PRIMERA foto de ese marker → abrir MudDialog para poner nombre y descripción.
4. Si ya tiene fotos (segunda foto en adelante) → NO abrir el diálogo, solo snackbar "Foto agregada al punto".

### Implementación

Modificar el flujo del botón MudFab de cámara en Mobile:

```csharp
private async Task OnCapturaFoto()
{
    // 1. Verificar permiso de cámara
    var permiso = await PermissionService.CheckAndRequestCameraAsync();
    if (permiso == PermissionResult.PermanentlyDenied)
    {
        await MostrarDialogoPermisosCamara();
        return;
    }
    if (permiso != PermissionResult.Granted) return;

    // 2. Capturar foto
    var stream = await CamaraService.TomarFotoAsync();
    if (stream is null) return; // usuario canceló

    // 3. Determinar si hay un marker activo
    if (_markerActivo is not null)
    {
        // Asociar foto al marker existente
        var esPrimeraFoto = _markerActivo.CantidadFotos == 0;

        await AgregarFotoAMarker(_markerActivo, stream);

        if (esPrimeraFoto)
        {
            // Abrir diálogo de descripción
            var dialog = await DialogService.ShowAsync<EditarPuntoDialog>(
                "Nuevo punto de registro",
                new DialogParameters
                {
                    { nameof(EditarPuntoDialog.PuntoId), _markerActivo.Id },
                    { nameof(EditarPuntoDialog.NombreInicial), _markerActivo.Nombre },
                    { nameof(EditarPuntoDialog.DescripcionInicial), "" }
                });
            await dialog.Result; // esperar cierre (Guardar u Omitir)
            Snackbar.Add("Foto guardada — completá los datos del punto", Severity.Success);
        }
        else
        {
            Snackbar.Add("Foto agregada al punto", Severity.Success);
        }
    }
    else
    {
        // No hay marker activo → flujo normal: crear punto nuevo
        await CrearPuntoConFoto(stream);
        // Si es punto nuevo, SIEMPRE abrir diálogo de descripción
        // (porque es la primera foto del marker recién creado)
    }

    // 4. Recentrar el mapa en la posición actual del dispositivo
    var pos = await DeviceLocation.GetCurrentLocationAsync();
    if (pos is not null)
        await JSRuntime.InvokeVoidAsync("leafletInterop.setView", pos.Value.Lat, pos.Value.Lng, 15);

    // 5. Refrescar markers del mapa
    await CargarMarkers();
}
```

### Diálogo de primera foto (EditarPuntoDialog.razor)

Usar los textos del catálogo de microcopy §4.3:

```razor
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Nuevo punto de registro</MudText>
    </TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="_nombre" Label="Nombre del punto"
                      Placeholder="Nombre del punto" Variant="Variant.Outlined" />
        <MudTextField @bind-Value="_descripcion" Label="Descripción"
                      Placeholder="Descripción (opcional)" Lines="3"
                      Variant="Variant.Outlined" Class="mt-3" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Omitir">Omitir</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled"
                   OnClick="Guardar">Guardar</MudButton>
    </DialogActions>
</MudDialog>
```

---

## MEJORA 4 — CONFIGURACIÓN GLOBAL DEL RADIO DE AGRUPACIÓN

### Comportamiento requerido

Un diálogo accesible desde la pantalla del mapa permite al usuario definir
el radio (en metros) dentro del cual las fotos se asocian automáticamente
a un marker existente en lugar de crear uno nuevo.

### Implementación

Crear servicio de configuración:

```csharp
// GeoFoto.Shared/Services/IAppConfigService.cs
public interface IAppConfigService
{
    Task<int> GetRadioAgrupacionMetrosAsync();           // default: 50
    Task SetRadioAgrupacionMetrosAsync(int metros);
}
```

En Mobile: persistir en SQLite (tabla AppConfig key-value) o en Preferences de MAUI:
```csharp
public class MauiAppConfigService : IAppConfigService
{
    public Task<int> GetRadioAgrupacionMetrosAsync()
    {
        var valor = Preferences.Default.Get("RadioAgrupacionMetros", 50);
        return Task.FromResult(valor);
    }

    public Task SetRadioAgrupacionMetrosAsync(int metros)
    {
        Preferences.Default.Set("RadioAgrupacionMetros", metros);
        return Task.CompletedTask;
    }
}
```

En Web: persistir via LocalStorage o cookie. Valor default: 50m.

### Componente del diálogo (textos del catálogo de microcopy §4.4)

```razor
@* GeoFoto.Shared/Components/RadioAgrupacionDialog.razor *@
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Radio de agrupación de fotos</MudText>
    </TitleContent>
    <DialogContent>
        <MudText Typo="Typo.body2" Class="mb-4">
            Las fotos capturadas dentro de este radio se asocian automáticamente
            al marker más cercano. Si no hay un marker dentro del radio, se crea uno nuevo.
        </MudText>

        <MudSlider @bind-Value="_radioMetros" Min="10" Max="500" Step="10"
                   Color="Color.Primary" Class="mb-2">
            Radio: @_radioMetros m
        </MudSlider>

        <MudNumericField @bind-Value="_radioMetros" Label="Radio en metros"
                         Min="10" Max="500" Variant="Variant.Outlined" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancelar</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled"
                   OnClick="Guardar">Guardar</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public int RadioActual { get; set; } = 50;

    private int _radioMetros;

    protected override void OnInitialized() => _radioMetros = RadioActual;

    private void Cancel() => MudDialog.Cancel();
    private void Guardar() => MudDialog.Close(DialogResult.Ok(_radioMetros));
}
```

### Botón de acceso en la pantalla del mapa

Agregar un MudIconButton en el MudAppBar o como overlay del mapa:

```razor
<MudTooltip Text="Radio de agrupación">
    <MudIconButton Icon="@Icons.Material.Filled.Radar"
                   Color="Color.Inherit"
                   OnClick="AbrirConfigRadio" />
</MudTooltip>
```

Al guardar, mostrar snackbar: "Radio de agrupación actualizado a {n} m"

### Integración con la lógica de FindPuntoCercano

Modificar el servicio que busca puntos cercanos para usar el radio configurado:

```csharp
public async Task<PuntoLocal?> FindPuntoCercanoAsync(double lat, double lng)
{
    var radioMetros = await _configService.GetRadioAgrupacionMetrosAsync();
    var puntos = await _db.Table<PuntoLocal>().ToListAsync();

    return puntos
        .Select(p => new { Punto = p, Distancia = CalcularDistanciaMetros(lat, lng, p.Latitud, p.Longitud) })
        .Where(x => x.Distancia <= radioMetros)
        .OrderBy(x => x.Distancia)
        .FirstOrDefault()?.Punto;
}

private static double CalcularDistanciaMetros(double lat1, double lng1, double lat2, double lng2)
{
    // Haversine formula
    const double R = 6371000; // radio de la Tierra en metros
    var dLat = (lat2 - lat1) * Math.PI / 180;
    var dLng = (lng2 - lng1) * Math.PI / 180;
    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
            Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
    return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
}
```

---

## MEJORA 5 — LISTADO DE MARKERS DESDE EL FRONT

### Comportamiento requerido

Revisar y mejorar la pantalla de listado de markers (`/lista`) para que:

1. Muestre todos los markers con: miniatura de la primera foto, nombre, coordenadas,
   cantidad de fotos, estado de sincronización, acciones (editar/eliminar).
2. Permita buscar por nombre.
3. Cada fila sea clickeable para navegar al mapa centrado en ese marker.
4. Los chips de estado usen los textos y colores del catálogo de microcopy §4.7.
5. Estados vacíos y sin resultados usen los textos del catálogo.

### Textos (del catálogo §4.7)

- Título: "Puntos registrados"
- Placeholder búsqueda: "Buscar por nombre..."
- Headers: "Foto", "Nombre", "Ubicación", "Fotos", "Estado", "Acciones"
- Chips: "Sincronizado" (verde), "Pendiente" (naranja), "Fallido" (rojo)
- Sin resultados: "No se encontraron puntos con ese nombre"
- Listado vacío: "No hay puntos registrados. Comenzá capturando fotos desde el mapa."
- Tooltips acciones: "Editar punto", "Eliminar punto"

### Implementación

Revisar `ListaPuntos.razor` (o equivalente) y asegurarte de que:

```razor
@* Encabezado *@
<MudText Typo="Typo.h5" Class="mb-4">Puntos registrados</MudText>

@* Buscador *@
<MudTextField @bind-Value="_busqueda"
              Placeholder="Buscar por nombre..."
              Adornment="Adornment.Start"
              AdornmentIcon="@Icons.Material.Filled.Search"
              Immediate="true"
              Class="mb-4" />

@* Tabla *@
<MudTable Items="@_puntosFiltrados" Hover="true" Breakpoint="Breakpoint.Sm"
          OnRowClick="@((TableRowClickEventArgs<PuntoListDto> args) => IrAlMapa(args.Item))"
          RowStyle="cursor:pointer;">
    <HeaderContent>
        <MudTh>Foto</MudTh>
        <MudTh>Nombre</MudTh>
        <MudTh>Ubicación</MudTh>
        <MudTh>Fotos</MudTh>
        <MudTh>Estado</MudTh>
        <MudTh>Acciones</MudTh>
    </HeaderContent>
    <RowTemplate>
        @* ... contenido según wireframe existente ... *@
    </RowTemplate>
    <NoRecordsContent>
        @if (string.IsNullOrEmpty(_busqueda))
        {
            <MudAlert Severity="Severity.Info">
                No hay puntos registrados. Comenzá capturando fotos desde el mapa.
            </MudAlert>
        }
        else
        {
            <MudAlert Severity="Severity.Info">
                No se encontraron puntos con ese nombre
            </MudAlert>
        }
    </NoRecordsContent>
</MudTable>
```

Al hacer click en una fila → `NavigationManager.NavigateTo($"/?lat={punto.Lat}&lng={punto.Lng}")`.
La página del mapa lee los query params y centra el mapa ahí.

---

## MEJORA 6 — CARRUSEL DE FOTOS + VISOR FULLSCREEN

### Comportamiento requerido

1. Al tocar o hacer click en un marker → abrir el drawer/bottom sheet con el detalle.
2. En el detalle, el FotoCarousel muestra las fotos con navegación.
3. Al tocar una foto del carrusel → abrir un visor fullscreen (MudDialog MaxWidth.ExtraExtraLarge).
4. En el visor: foto a pantalla completa, flechas de navegación, indicador "{i} de {n}".
5. Al cerrar el visor → volver al carrusel en la foto que estaba viendo.

### Implementación del Visor (FotoViewer.razor)

Textos del catálogo de microcopy §4.6:

```razor
@* GeoFoto.Shared/Components/FotoViewer.razor *@
@inject IGeoFotoApiClient Api

<MudDialog Style="background:black;">
    <TitleContent>
        <MudStack Row="true" AlignItems="AlignItems.Center" Justify="Justify.SpaceBetween">
            <MudText Typo="Typo.h6" Style="color:white;">
                Fotos de @NombrePunto
            </MudText>
            <MudText Typo="Typo.body2" Style="color:rgba(255,255,255,.7);">
                @(_indice + 1) de @Fotos.Count
            </MudText>
        </MudStack>
    </TitleContent>
    <DialogContent>
        <div style="position:relative; display:flex; align-items:center;
                    justify-content:center; min-height:60vh;">

            @* Botón anterior *@
            <MudIconButton Icon="@Icons.Material.Filled.ChevronLeft"
                           Style="position:absolute; left:8px; color:white;"
                           Size="Size.Large"
                           Disabled="_indice == 0"
                           OnClick="() => _indice--"
                           Title="Foto anterior" />

            @* Imagen *@
            <img src="@Api.GetImagenUrl(Fotos[_indice].Id)"
                 alt="Foto @(_indice + 1)"
                 style="max-width:100%; max-height:70vh; object-fit:contain;" />

            @* Botón siguiente *@
            <MudIconButton Icon="@Icons.Material.Filled.ChevronRight"
                           Style="position:absolute; right:8px; color:white;"
                           Size="Size.Large"
                           Disabled="_indice >= Fotos.Count - 1"
                           OnClick="() => _indice++"
                           Title="Foto siguiente" />
        </div>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cerrar" Style="color:white;">Cerrar</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public List<FotoDto> Fotos { get; set; } = new();
    [Parameter] public string NombrePunto { get; set; } = "";
    [Parameter] public int IndiceInicial { get; set; } = 0;

    private int _indice;

    protected override void OnInitialized() => _indice = IndiceInicial;

    private void Cerrar() => MudDialog.Close(DialogResult.Ok(_indice));
}
```

### Integración en FotoCarousel.razor

Al hacer click/touch en la imagen principal del carrusel:

```csharp
private async Task AbrirVisor()
{
    var options = new DialogOptions
    {
        MaxWidth = MaxWidth.ExtraExtraLarge,
        FullWidth = true,
        NoHeader = false,
        CloseButton = true
    };

    var parameters = new DialogParameters
    {
        { nameof(FotoViewer.Fotos), Fotos },
        { nameof(FotoViewer.NombrePunto), NombrePunto },
        { nameof(FotoViewer.IndiceInicial), _indice }
    };

    var dialog = await DialogService.ShowAsync<FotoViewer>("", parameters, options);
    var result = await dialog.Result;

    // Al cerrar, actualizar el índice del carrusel con la foto que estaba viendo
    if (!result.Canceled && result.Data is int nuevoIndice)
        _indice = nuevoIndice;
}
```

El cursor sobre las fotos del carrusel debe ser `pointer` para indicar que son clickeables.
En mobile, el toque sobre la foto abre el visor directamente.

---

## MEJORA 7 — CONTROL DE AUTOSINCRONIZACIÓN MOBILE ↔ API

### Comportamiento requerido

La app móvil debe sincronizar automáticamente cuando:
  1. Se detecta conexión a internet (evento ConnectivityChanged).
  2. Al iniciar la app si hay conexión disponible.
  3. Periódicamente cada 5 minutos si hay conexión y hay operaciones pendientes.

Debe respetar:
  - Backoff exponencial en errores (5s → 30s → 5min) con máximo 3 intentos.
  - No intentar sync si no hay conexión.
  - Actualizar el SyncStatusBadge en tiempo real.
  - Mostrar snackbars según el catálogo de microcopy §4.10.

### Implementación del SyncService

```csharp
// GeoFoto.Mobile/Services/SyncBackgroundService.cs
public class SyncBackgroundService : IDisposable
{
    private readonly ISyncService _syncService;
    private readonly IConnectivityService _connectivity;
    private readonly ILocalDbService _db;
    private Timer? _timer;
    private bool _syncing;

    public event EventHandler<SyncResultEventArgs>? SyncCompleted;

    public SyncBackgroundService(
        ISyncService syncService,
        IConnectivityService connectivity,
        ILocalDbService db)
    {
        _syncService = syncService;
        _connectivity = connectivity;
        _db = db;

        // Reaccionar a cambios de conectividad
        _connectivity.ConnectivityChanged += async (_, isConnected) =>
        {
            if (isConnected) await TrySyncAsync();
        };

        // Timer periódico cada 5 minutos
        _timer = new Timer(async _ => await TrySyncAsync(),
            null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
    }

    public async Task TrySyncAsync()
    {
        if (_syncing || !_connectivity.IsConnected) return;

        var pendientes = await _db.GetPendingCountAsync();
        if (pendientes == 0) return;

        _syncing = true;
        try
        {
            var result = await _syncService.SyncAsync();
            SyncCompleted?.Invoke(this, new SyncResultEventArgs(result));
        }
        finally
        {
            _syncing = false;
        }
    }

    public void Dispose() => _timer?.Dispose();
}
```

### Snackbars de sincronización (del catálogo §4.10)

En el MainLayout o equivalente, suscribirse a los eventos del SyncBackgroundService:

```csharp
SyncBgService.SyncCompleted += (_, args) => InvokeAsync(() =>
{
    if (args.Success)
        Snackbar.Add($"Sincronización completa: {args.OperacionesProcesadas} operaciones procesadas",
            Severity.Success, config => config.VisibleStateDuration = 5000);
    else
        Snackbar.Add("Error en la sincronización. Revisá el estado en Sync.",
            Severity.Error, config => config.VisibleStateDuration = 10000);

    StateHasChanged();
});

Connectivity.ConnectivityChanged += (_, isConnected) => InvokeAsync(() =>
{
    if (isConnected)
        Snackbar.Add("Conexión restaurada — sincronizando...",
            Severity.Success, config => config.VisibleStateDuration = 3000);
    else
        Snackbar.Add("Sin conexión — los datos se guardan en el dispositivo",
            Severity.Info, config => config.VisibleStateDuration = 5000);

    StateHasChanged();
});
```

---

## AUDITORÍA GENERAL DE MICROCOPY

Después de implementar las 7 mejoras, recorré TODOS los componentes existentes
y verificá que los textos coincidan con el catálogo de microcopy:

1. MudAppBar: título "GeoFoto", links "Mapa", "Subir", "Lista", "Sync"
2. SyncStatusBadge: tooltips exactos según §4.10
3. DetallePunto.razor: placeholders, botones, snackbars según §4.5
4. FotoCarousel.razor: textos según §4.5 ("Sin fotos", "📷 {n} foto(s)")
5. SubirFotos.razor: textos según §4.9
6. ListaPuntos.razor: textos según §4.7
7. EstadoSync.razor: textos según §4.10
8. Diálogos de confirmación: textos según §4.8
9. Mensajes de error genéricos: textos según §4.11

Si encontrás textos que no están en el catálogo, usá el tono y las reglas
de escritura definidas en las secciones 3.1 a 3.3 del documento de microcopy
para generar textos consistentes.

---

## VERIFICACIÓN FINAL

Después de implementar todo, verificá:

```
[ ] App Mobile abre con mapa centrado en GPS del dispositivo
[ ] Sin GPS → mapa en Buenos Aires + snackbar warning
[ ] Permiso denegado permanentemente → MudDialog con "Ir a Configuración"
[ ] Sacar foto con marker activo → foto se asocia al marker
[ ] Primera foto del marker → abre diálogo de descripción
[ ] Segunda foto en adelante → solo snackbar "Foto agregada al punto"
[ ] Después de sacar foto → mapa se recentra en posición del dispositivo
[ ] Diálogo de radio de agrupación accesible desde el mapa
[ ] Slider funcional entre 10m y 500m, valor persiste
[ ] Foto dentro del radio → se asocia al marker cercano
[ ] Foto fuera del radio → crea marker nuevo
[ ] Listado de markers con búsqueda, chips de estado, acciones
[ ] Click en fila → navega al mapa centrado en ese marker
[ ] Listado vacío → mensaje correcto
[ ] Click en marker → drawer con carrusel de fotos
[ ] Click en foto del carrusel → visor fullscreen
[ ] Navegación con flechas en el visor
[ ] Cerrar visor → vuelve al carrusel en la misma foto
[ ] Sync automático al recuperar conexión
[ ] Snackbars de sync con textos del catálogo
[ ] Badge de pendientes se actualiza en tiempo real
[ ] TODOS los textos de la UI coinciden con el catálogo de microcopy
[ ] dotnet build GeoFoto.sln → 0 errores

Si todo pasa: "UX COMPLETO — 7 mejoras implementadas + microcopy aplicado."
```
```
