# Prompts de Copilot por Sprint — GeoFoto Walking Skeleton

Cada prompt es autónomo. Pegarlo en Copilot Chat modo **Edits** (`@workspace`)
al inicio de cada sprint. El siguiente sprint solo se inicia cuando el anterior
pasa su checklist de demo completo.

---
---

# SPRINT 01 — Walking Skeleton: subir fotos y ver el mapa

```
Eres un desarrollador .NET 10 senior. Vas a implementar el Sprint 01 de GeoFoto.

LECTURA OBLIGATORIA antes de escribir código:
  docs/07_plan-sprint/plan-iteracion_sprint-01_v1.0.md
  docs/05_arquitectura_tecnica/arquitectura-solucion_v1.0.md
  docs/05_arquitectura_tecnica/modelo-datos-logico_v1.0.md  (solo Sección A — SQL Server)
  docs/05_arquitectura_tecnica/api-rest-spec_v1.0.md        (endpoints de Puntos y Fotos)
  docs/02_especificacion_funcional/especificacion-funcional_v1.0.md (F-01, F-03, F-05, F-06, F-07)
  docs/08_calidad_y_pruebas/definition-of-done_v1.0.md

Confirmá con una línea: "Sprint 01 documentación leída. Comenzando Walking Skeleton."

---

## QUÉ ES EL WALKING SKELETON

El Walking Skeleton es la versión más delgada posible del sistema que atraviesa
todas las capas de extremo a extremo. Al terminar este sprint debe existir:

  Usuario → sube una foto JPG con GPS desde el browser
  → API recibe el archivo, extrae coordenadas del EXIF
  → guarda el Punto en SQL Server y la imagen en disco
  → Blazor Web carga el mapa (Leaflet + MudBlazor layout)
  → el marker aparece en las coordenadas de la foto
  → click en el marker → MudDialog con la foto y las coordenadas

Sin autenticación. Sin paginación. Sin tests de integración complejos.
Solo el flujo principal funcionando de punta a punta.

---

## HISTORIAS DE ESTE SPRINT

GEO-US01 — Solución compilando con los 4 proyectos (5 pts)
GEO-US02 — Base de datos SQL Server con EF Core y migrations (8 pts)
GEO-US03 — MudBlazor integrado con tema base (3 pts)
GEO-US04 — Subir fotos desde web y ver markers en el mapa (8 pts)
GEO-US05 — Click en marker → popup con foto y coordenadas (5 pts)

Total: 29 story points

---

## REGLAS DE IMPLEMENTACIÓN

- Implementá historia por historia en el orden listado.
- No avancés a la siguiente hasta que la anterior compile y funcione.
- Indicá en el chat qué historia estás implementando con su ID Jira.
- Cada commit debe seguir el formato: feat(GEO-USxx): descripción en presente

---

## BLOQUE A — GEO-US01: Solución base (Walking Skeleton structure)

Creá la solución con los 4 proyectos. Verificá que `dotnet build GeoFoto.sln` pase.

Proyectos:
- GeoFoto.Api        → SDK: Microsoft.NET.Sdk.Web, net10.0
- GeoFoto.Shared     → SDK: Microsoft.NET.Sdk.Razor, net10.0
- GeoFoto.Web        → SDK: Microsoft.NET.Sdk.Web, net10.0, ref: GeoFoto.Shared
- GeoFoto.Mobile     → SDK: Microsoft.NET.Sdk, net10.0-android, UseMaui:true, ref: GeoFoto.Shared

NuGet en GeoFoto.Api:
  Microsoft.EntityFrameworkCore.SqlServer  (versión compatible con net10)
  Microsoft.EntityFrameworkCore.Tools
  MetadataExtractor
  Swashbuckle.AspNetCore

NuGet en GeoFoto.Shared:
  MudBlazor  (última versión estable)

Verificación de BLOQUE A:
  dotnet build GeoFoto.sln → 0 errores
  Reportá: "GEO-US01 OK — solución compilando."

---

## BLOQUE B — GEO-US02: Modelos, DbContext y migrations

### B.1 — Modelos de dominio (GeoFoto.Api/Models/)

```csharp
// Punto.cs
public class Punto
{
    public int Id { get; set; }
    public decimal Latitud { get; set; }        // decimal(10,7)
    public decimal Longitud { get; set; }       // decimal(10,7)
    public string? Nombre { get; set; }          // nvarchar(200)
    public string? Descripcion { get; set; }     // nvarchar(1000)
    public DateTime FechaCreacion { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Foto> Fotos { get; set; } = new List<Foto>();
}

// Foto.cs
public class Foto
{
    public int Id { get; set; }
    public int PuntoId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;   // nvarchar(260)
    public string RutaFisica { get; set; } = string.Empty;      // nvarchar(500)
    public DateTime? FechaTomada { get; set; }
    public long TamanoBytes { get; set; }
    public decimal? LatitudExif { get; set; }
    public decimal? LongitudExif { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Punto Punto { get; set; } = null!;
}
```

### B.2 — GeoFotoDbContext con Fluent API

Configurá en OnModelCreating:
- Puntos: decimal(10,7) para Latitud y Longitud, UpdatedAt DEFAULT GETUTCDATE()
- Fotos: FK → Puntos.Id CASCADE DELETE, índice en PuntoId, decimal(10,7) para LatitudExif/LongitudExif
- Índice compuesto en (Latitud, Longitud) para búsquedas geoespaciales

### B.3 — Migration y update

```
dotnet ef migrations add InitialCreate --project GeoFoto.Api
dotnet ef database update --project GeoFoto.Api
```

Verificación de BLOQUE B:
  - Migration creada en Migrations/
  - `dotnet ef database update` retorna "Done"
  - Tablas Puntos y Fotos existen en SQL Server
  Reportá: "GEO-US02 OK — BD lista con tablas Puntos y Fotos."

---

## BLOQUE C — GEO-US03: MudBlazor configurado

En GeoFoto.Shared agrega `_Imports.razor` con:
```razor
@using MudBlazor
```

En GeoFoto.Web/Program.cs:
```csharp
builder.Services.AddMudServices();
```

En GeoFoto.Web/Components/App.razor o en el layout raíz, en <head>:
```html
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
```
Y antes de </body>:
```html
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
```

Creá GeoFoto.Shared/Layouts/MainLayout.razor:
```razor
@inherits LayoutComponentBase

<MudLayout>
    <MudAppBar Color="Color.Primary" Fixed="true">
        <MudText Typo="Typo.h6">GeoFoto</MudText>
        <MudSpacer />
        <MudNavLink Href="/" Icon="@Icons.Material.Filled.Map">Mapa</MudNavLink>
        <MudNavLink Href="/subir" Icon="@Icons.Material.Filled.Upload">Subir</MudNavLink>
        <MudNavLink Href="/lista" Icon="@Icons.Material.Filled.List">Lista</MudNavLink>
        <MudNavLink Href="/sync" Icon="@Icons.Material.Filled.Sync">Sync</MudNavLink>
        <MudSpacer />
        @* SyncStatusBadge irá aquí en Sprint 03 *@
    </MudAppBar>
    <MudMainContent Style="padding-top:64px;">
        @Body
    </MudMainContent>
</MudLayout>

<MudThemeProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

Verificación de BLOQUE C:
  - `dotnet run` en GeoFoto.Web no lanza errores de MudBlazor
  - La barra de navegación se ve con fondo azul Material
  Reportá: "GEO-US03 OK — MudBlazor operativo."

---

## BLOQUE D — GEO-US04: API de fotos + Blazor SubirFotos + Mapa

### D.1 — DTOs (GeoFoto.Api/Dtos/)

```csharp
// Todos como record (inmutables)
public record PuntoDto(int Id, decimal Latitud, decimal Longitud,
    string? Nombre, string? Descripcion, DateTime FechaCreacion, int CantidadFotos);
public record FotoDto(int Id, int PuntoId, string NombreArchivo,
    DateTime? FechaTomada, long TamanoBytes, decimal? LatitudExif,
    decimal? LongitudExif, string UrlImagen);
public record UploadResultDto(int PuntoId, int FotoId, string NombreArchivo,
    bool TeniaGps, decimal? Latitud, decimal? Longitud);
```

### D.2 — Servicios (GeoFoto.Api/Services/)

IExifService / ExifService:
- Usa MetadataExtractor.ImageMetadataReader.ReadMetadata(stream)
- Extrae GpsDirectory → Latitud y Longitud
- Extrae ExifSubIfdDirectory → DateTimeOriginal
- Si no hay GPS: retorna (null, null, null) sin lanzar excepción

IFileStorageService / FileStorageService:
- Guarda en wwwroot/uploads/{año}/{mes}/{Guid}{extension}
- Crea el directorio si no existe
- La ruta base viene de IConfiguration["Storage:UploadPath"]

IFotosService / FotosService:
- UploadAsync(IFormFile):
  1. Llama ExifService.ExtractGeoData()
  2. Llama FileStorageService.SaveAsync()
  3. Busca Punto con |Lat-LatExif| < 0.001 AND |Lng-LngExif| < 0.001
  4. Si no existe: crea nuevo Punto con FechaCreacion=UtcNow, UpdatedAt=UtcNow
  5. Crea Foto vinculada al Punto
  6. Guarda en DbContext
  7. Retorna UploadResultDto con TeniaGps=true/false

IPuntosService / PuntosService:
- GetAllAsync(): proyecta a PuntoDto con CantidadFotos
- GetByIdAsync(id): retorna detalle con fotos o null si no existe

### D.3 — Controladores (GeoFoto.Api/Controllers/)

PuntosController:
  GET  /api/puntos     → 200 IReadOnlyList<PuntoDto>
  GET  /api/puntos/{id} → 200 PuntoDetalleDto | 404 ProblemDetails

FotosController:
  POST /api/fotos/upload → [FromForm] IFormFile[], 201 UploadResultDto[] | 400
    - Valida extensión: jpg, jpeg, png, webp
    - Llama FotosService.UploadAsync por cada archivo
  GET  /api/fotos/{puntoId} → 200 IReadOnlyList<FotoDto> | 404
  GET  /api/fotos/imagen/{id} → PhysicalFile | 404

### D.4 — Program.cs (GeoFoto.Api)

```csharp
builder.Services.AddDbContext<GeoFotoDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("GeoFoto")));
builder.Services.AddScoped<IExifService, ExifService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IPuntosService, PuntosService>();
builder.Services.AddScoped<IFotosService, FotosService>();
builder.Services.AddCors(o => o.AddPolicy("GeoFoto", p =>
    p.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [])
     .AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<FormOptions>(o =>
    o.MultipartBodyLengthLimit = 52_428_800); // 50 MB
// ...
app.UseCors("GeoFoto");
app.UseStaticFiles();
app.UseSwagger(); app.UseSwaggerUI();
```

appsettings.Development.json:
```json
{
  "ConnectionStrings": { "GeoFoto": "Server=localhost;Database=GeoFotoDB;Trusted_Connection=True;TrustServerCertificate=True;" },
  "Storage": { "UploadPath": "wwwroot/uploads" },
  "Cors": { "AllowedOrigins": ["http://localhost:5001", "https://localhost:7001"] },
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://0.0.0.0:5000" } } }
}
```

### D.5 — Cliente HTTP (GeoFoto.Shared/Services/)

IGeoFotoApiClient / GeoFotoApiClient:
- Constructor recibe HttpClient (registrado con BaseAddress)
- GetPuntosAsync() → GET /api/puntos → deserializa IReadOnlyList<PuntoDto>
- UploadFotoAsync(IBrowserFile) → POST /api/fotos/upload multipart → UploadResultDto
- GetImagenUrl(int id) → $"{BaseAddress}api/fotos/imagen/{id}"

DTOs espejo en GeoFoto.Shared/Models/ (mismos records que en Api, namespace distinto).

### D.6 — GeoFoto.Shared/Pages/SubirFotos.razor (@page "/subir")

```razor
@page "/subir"
@inject IGeoFotoApiClient Api
@inject NavigationManager Nav

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    <MudText Typo="Typo.h5" GutterBottom="true">Subir fotografías georeferenciadas</MudText>

    <MudPaper Class="pa-6 mb-4" Outlined="true">
        <MudFileUpload T="IReadOnlyList<IBrowserFile>" FilesChanged="OnFilesChanged"
                       Accept=".jpg,.jpeg,.png,.webp" Multiple="true">
            <ButtonTemplate>
                <MudButton HtmlTag="label" Variant="Variant.Outlined" Color="Color.Primary"
                           StartIcon="@Icons.Material.Filled.CloudUpload" for="@context.Id">
                    Elegir fotos
                </MudButton>
            </ButtonTemplate>
        </MudFileUpload>
        <MudText Typo="Typo.caption" Class="mt-2">
            Acepta JPG, PNG, WEBP con datos GPS en EXIF. Múltiples archivos.
        </MudText>
    </MudPaper>

    @if (_resultados.Any())
    {
        <MudTable Items="_resultados" Dense="true" Hover="true">
            <HeaderContent>
                <MudTh>Archivo</MudTh>
                <MudTh>Estado</MudTh>
                <MudTh>GPS</MudTh>
                <MudTh>Resultado</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>@context.NombreArchivo</MudTd>
                <MudTd>
                    <MudChip Color="@(context.Ok ? Color.Success : Color.Warning)" Size="Size.Small">
                        @(context.Ok ? "OK" : "Sin GPS")
                    </MudChip>
                </MudTd>
                <MudTd>
                    @if (context.Latitud.HasValue)
                    { <span>@context.Latitud.Value.ToString("F4"), @context.Longitud!.Value.ToString("F4")</span> }
                    else { <span>—</span> }
                </MudTd>
                <MudTd>
                    @if (context.Ok)
                    { <MudAlert Severity="Severity.Success" Dense="true">Punto creado</MudAlert> }
                    else
                    { <MudAlert Severity="Severity.Warning" Dense="true">Guardado en (0,0)</MudAlert> }
                </MudTd>
            </RowTemplate>
        </MudTable>
        <MudButton Variant="Variant.Filled" Color="Color.Primary" Class="mt-4"
                   OnClick='() => Nav.NavigateTo("/")'>
            Ver en el mapa
        </MudButton>
    }
</MudContainer>

@code {
    private record ResultadoItem(string NombreArchivo, bool Ok,
        decimal? Latitud, decimal? Longitud);
    private List<ResultadoItem> _resultados = new();

    private async Task OnFilesChanged(IReadOnlyList<IBrowserFile> files)
    {
        _resultados.Clear();
        foreach (var file in files)
        {
            try
            {
                var r = await Api.UploadFotoAsync(file);
                _resultados.Add(new(file.Name, r?.TeniaGps ?? false,
                    r?.Latitud, r?.Longitud));
            }
            catch
            {
                _resultados.Add(new(file.Name, false, null, null));
            }
            StateHasChanged();
        }
    }
}
```

### D.7 — Interop Leaflet (GeoFoto.Shared/wwwroot/)

Estructura:
```
GeoFoto.Shared/wwwroot/
  js/leaflet-interop.js
  css/geofoto.css
```

GeoFoto.Shared/wwwroot/js/leaflet-interop.js:
```javascript
window.leafletInterop = {
    _map: null, _markers: [], _dotnetRef: null, _ready: false,

    init: function (elementId, dotnetRef) {
        try {
            if (this._map) { this._map.remove(); this._map = null; }
            this._dotnetRef = dotnetRef;
            const el = document.getElementById(elementId);
            if (!el) { console.warn('[GeoFoto] #' + elementId + ' no encontrado'); return; }
            this._map = L.map(elementId).setView([-34.6, -58.4], 5);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '© OpenStreetMap contributors', maxZoom: 19
            }).addTo(this._map);
            this._ready = true;
        } catch (e) { console.error('[GeoFoto] init error:', e); }
    },

    addMarkers: function (puntosJson) {
        if (!this._ready || !this._map) return;
        try {
            const puntos = typeof puntosJson === 'string' ? JSON.parse(puntosJson) : puntosJson;
            this._markers.forEach(m => { try { m.remove(); } catch(_){} });
            this._markers = [];
            puntos.forEach(p => {
                const m = L.marker([parseFloat(p.latitud), parseFloat(p.longitud)])
                    .addTo(this._map)
                    .on('click', () => {
                        if (this._dotnetRef)
                            this._dotnetRef.invokeMethodAsync('OnMarkerClicked', p.id)
                                .catch(e => console.warn('[GeoFoto] callback:', e));
                    });
                this._markers.push(m);
            });
            if (puntos.length > 0)
                this._map.setView([parseFloat(puntos[0].latitud),
                    parseFloat(puntos[0].longitud)], 13);
        } catch (e) { console.error('[GeoFoto] addMarkers error:', e); }
    }
};
```

GeoFoto.Shared/wwwroot/css/geofoto.css:
```css
#map { height: 500px; width: 100%; border-radius: 8px; }
.carousel-img { max-width: 100%; max-height: 300px; object-fit: contain; }
```

### D.8 — GeoFoto.Shared/Pages/Mapa.razor (@page "/")

```razor
@page "/"
@implements IDisposable
@inject IGeoFotoApiClient Api
@inject IJSRuntime JS
@inject ILogger<Mapa> Logger

<MudContainer MaxWidth="MaxWidth.False" Class="pa-0">
    <MudGrid Spacing="0">
        <MudItem xs="12" md="8">
            <div id="map"></div>
        </MudItem>
        <MudItem xs="12" md="4">
            @if (_puntoSeleccionado is not null)
            {
                <DetallePunto Punto="_puntoSeleccionado"
                              OnClose="() => _puntoSeleccionado = null"
                              OnGuardado="RecargarPuntos" />
            }
            else
            {
                <MudPaper Class="pa-4 ma-2">
                    <MudText Typo="Typo.subtitle1">@_puntos.Count punto(s) en el mapa</MudText>
                    <MudText Typo="Typo.caption" Color="Color.Secondary">
                        Hacé click en un marker para ver el detalle.
                    </MudText>
                </MudPaper>
            }
        </MudItem>
    </MudGrid>
</MudContainer>

@code {
    private List<PuntoDto> _puntos = new();
    private PuntoDetalleDto? _puntoSeleccionado;
    private DotNetObjectReference<Mapa>? _ref;
    private bool _mapaListo;

    protected override async Task OnInitializedAsync()
    {
        try { _puntos = (await Api.GetPuntosAsync()).ToList(); }
        catch (Exception ex) { Logger.LogError(ex, "Error cargando puntos"); }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _ref = DotNetObjectReference.Create(this);
            await JS.InvokeVoidAsync("leafletInterop.init", "map", _ref);
            _mapaListo = true;
        }
        if (_mapaListo && _puntos.Any())
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                _puntos, new System.Text.Json.JsonSerializerOptions
                { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            await JS.InvokeVoidAsync("leafletInterop.addMarkers", json);
        }
    }

    [JSInvokable]
    public async Task OnMarkerClicked(int puntoId)
    {
        _puntoSeleccionado = await Api.GetPuntoAsync(puntoId);
        StateHasChanged();
    }

    private async Task RecargarPuntos()
    {
        _puntos = (await Api.GetPuntosAsync()).ToList();
        StateHasChanged();
        await JS.InvokeVoidAsync("leafletInterop.addMarkers",
            System.Text.Json.JsonSerializer.Serialize(_puntos,
            new System.Text.Json.JsonSerializerOptions
            { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
    }

    public void Dispose() => _ref?.Dispose();
}
```

### D.9 — GeoFoto.Web/Program.cs

```csharp
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddHttpClient<IGeoFotoApiClient, GeoFotoApiClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!));

app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode()
   .AddAdditionalAssemblies(typeof(GeoFoto.Shared._Imports).Assembly);
```

GeoFoto.Web/Components/App.razor — head:
```html
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
<link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
<link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
<link href="_content/GeoFoto.Shared/css/geofoto.css" rel="stylesheet" />
```
body final:
```html
<script src="_content/MudBlazor/MudBlazor.min.js"></script>
<script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
<script src="_content/GeoFoto.Shared/js/leaflet-interop.js"></script>
```

GeoFoto.Web/appsettings.json:
```json
{ "Api": { "BaseUrl": "http://localhost:5000/" } }
```

Verificación de BLOQUE D:
  1. dotnet build GeoFoto.sln → 0 errores
  2. API corriendo → GET http://localhost:5000/api/puntos → []
  3. POST /api/fotos/upload (Swagger) con foto JPG con GPS → 201 con coords
  4. Web en localhost:5001 → /subir → subir foto → resultado en tabla
  5. Ir a / → marker en el mapa
  Reportá: "GEO-US04 OK — foto subida y marker visible en el mapa."

---

## BLOQUE E — GEO-US05: Popup de detalle al clickear marker

Creá GeoFoto.Shared/Components/DetallePunto.razor:

```razor
@inject IGeoFotoApiClient Api
@inject ISnackbar Snackbar

<MudCard Class="ma-2">
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h6">@(Punto?.Nombre ?? "Sin nombre")</MudText>
            <MudText Typo="Typo.caption">
                Lat: @Punto?.Latitud.ToString("F6") | Lng: @Punto?.Longitud.ToString("F6")
            </MudText>
        </CardHeaderContent>
        <CardHeaderActions>
            <MudIconButton Icon="@Icons.Material.Filled.Close" OnClick="() => OnClose.InvokeAsync()" />
        </CardHeaderActions>
    </MudCardHeader>
    <MudCardContent>
        <FotoCarousel Fotos="@(Punto?.Fotos.ToList() ?? [])" />
        <MudTextField @bind-Value="_nombre" Label="Nombre" Variant="Variant.Outlined"
                      Class="mt-3" Margin="Margin.Dense" />
        <MudTextField @bind-Value="_descripcion" Label="Descripción" Variant="Variant.Outlined"
                      Lines="3" Class="mt-2" Margin="Margin.Dense" />
    </MudCardContent>
    <MudCardActions>
        <MudButton Variant="Variant.Filled" Color="Color.Primary"
                   OnClick="Guardar" Disabled="_guardando">
            @(_guardando ? "Guardando..." : "Guardar")
        </MudButton>
    </MudCardActions>
</MudCard>

@code {
    [Parameter] public PuntoDetalleDto? Punto { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnGuardado { get; set; }

    private string? _nombre;
    private string? _descripcion;
    private bool _guardando;

    protected override void OnParametersSet()
    {
        _nombre = Punto?.Nombre;
        _descripcion = Punto?.Descripcion;
    }

    private async Task Guardar()
    {
        if (Punto is null) return;
        _guardando = true;
        try
        {
            await Api.UpdatePuntoAsync(Punto.Id,
                new ActualizarPuntoRequest(_nombre, _descripcion));
            Snackbar.Add("Guardado correctamente", Severity.Success);
            await OnGuardado.InvokeAsync();
        }
        catch { Snackbar.Add("Error al guardar", Severity.Error); }
        finally { _guardando = false; }
    }
}
```

Creá GeoFoto.Shared/Components/FotoCarousel.razor:
```razor
@inject IGeoFotoApiClient Api

@if (Fotos.Any())
{
    <div class="d-flex flex-column align-items-center">
        <img src="@Api.GetImagenUrl(Fotos[_indice].Id)"
             class="carousel-img" alt="Foto @(_indice+1) de @Fotos.Count" />
        <MudText Typo="Typo.caption" Class="mt-1">
            Foto @(_indice+1) de @Fotos.Count
            @if (Fotos[_indice].FechaTomada.HasValue)
            { <span> — @Fotos[_indice].FechaTomada!.Value.ToString("dd/MM/yyyy HH:mm")</span> }
        </MudText>
        @if (Fotos.Count > 1)
        {
            <MudButtonGroup Class="mt-1">
                <MudIconButton Icon="@Icons.Material.Filled.ChevronLeft"
                               OnClick="Anterior" Disabled="_indice == 0" />
                <MudIconButton Icon="@Icons.Material.Filled.ChevronRight"
                               OnClick="Siguiente" Disabled="_indice >= Fotos.Count - 1" />
            </MudButtonGroup>
        }
    </div>
}
else
{
    <MudText Typo="Typo.caption" Color="Color.Secondary">Sin fotos</MudText>
}

@code {
    [Parameter] public List<FotoDto> Fotos { get; set; } = new();
    private int _indice;
    private void Anterior() { if (_indice > 0) _indice--; }
    private void Siguiente() { if (_indice < Fotos.Count - 1) _indice++; }
}
```

Verificación de BLOQUE E:
  1. Click en marker → panel lateral muestra foto y coordenadas
  2. FotoCarousel navega entre fotos si hay más de una
  3. Guardar nombre/descripción → Snackbar "Guardado correctamente"
  Reportá: "GEO-US05 OK — popup de detalle funcional."

---

## CHECKLIST FINAL DE DEMO — SPRINT 01

Ejecutá cada paso y confirmá que funciona antes de cerrar el sprint:

  [ ] dotnet build GeoFoto.sln → 0 errores, 0 warnings críticos
  [ ] API corre en http://localhost:5000
  [ ] GET /api/puntos → responde JSON (aunque sea [])
  [ ] Swagger accesible en /swagger/index.html
  [ ] Web corre en http://localhost:5001
  [ ] Ir a /subir → zona de upload se muestra con MudBlazor
  [ ] Subir 1 foto JPG con GPS embebido → aparece en tabla con coords
  [ ] Ir a / → mapa de Leaflet visible dentro del layout MudBlazor
  [ ] Marker aparece en las coordenadas de la foto
  [ ] Click en marker → panel lateral con foto y formulario
  [ ] Completar nombre → Guardar → Snackbar confirma
  [ ] Marker sigue visible después de guardar

Si todo pasa: reportá "SPRINT 01 COMPLETO — Walking Skeleton operativo."
Si algo falla: corregí antes de reportar completado.
```

---
---

# SPRINT 02 — Gestión completa web + App Android básica

```
Eres un desarrollador .NET 10 senior. Implementás el Sprint 02 de GeoFoto.
El Sprint 01 está completo: la app web sube fotos y las muestra en el mapa.

LECTURA OBLIGATORIA:
  docs/07_plan-sprint/plan-iteracion_sprint-02_v1.0.md
  docs/06_backlog-tecnico/backlog-tecnico_v1.0.md  (tareas GEO-US06 a GEO-US10)
  docs/08_calidad_y_pruebas/definition-of-done_v1.0.md

Confirmá: "Sprint 02 leído. Estado del sistema heredado verificado."

---

## HISTORIAS DE ESTE SPRINT

GEO-US06 — Editar nombre/descripción desde la lista (5 pts)
GEO-US07 — Ver todos los puntos en MudTable con filtros (3 pts)
GEO-US08 — Eliminar punto con MudDialog de confirmación (3 pts)
GEO-US09 — App MAUI en Android con mapa funcionando (8 pts)
GEO-US10 — Tomar foto con cámara desde MAUI y registrarla (8 pts)

Total: 27 story points

---

## BLOQUE A — GEO-US06/07/08: Gestión web de puntos

### A.1 — Endpoint PUT y DELETE en la API

Agregá en PuntosController:

```csharp
// PUT /api/puntos/{id}
[HttpPut("{id:int}")]
public async Task<ActionResult<PuntoDetalleDto>> Update(
    int id, [FromBody] ActualizarPuntoRequest request, CancellationToken ct)

// DELETE /api/puntos/{id}
[HttpDelete("{id:int}")]
public async Task<IActionResult> Delete(int id, CancellationToken ct)
// DeleteAsync debe: eliminar fotos del disco, luego eliminar el Punto (cascade borra Fotos de BD)
```

En PuntosService agrega:
- UpdateAsync(id, request): actualiza Nombre, Descripcion, UpdatedAt=UtcNow → guarda
- DeleteAsync(id): carga fotos, borra archivos físicos, luego elimina el Punto

En IGeoFotoApiClient agrega:
- UpdatePuntoAsync(int id, ActualizarPuntoRequest) → PUT
- DeletePuntoAsync(int id) → DELETE

### A.2 — GeoFoto.Shared/Pages/ListaPuntos.razor (@page "/lista")

MudTable con:
- Columnas: Miniatura (img tag) / Nombre / Latitud / Longitud / Fotos / Acciones
- Búsqueda con MudTextField → filtra por Nombre
- MudIconButton editar → abre MudDialog con DetallePunto
- MudIconButton eliminar → MudDialog de confirmación → DeletePuntoAsync → recarga

```razor
@page "/lista"
@inject IGeoFotoApiClient Api
@inject IDialogService Dialog
@inject ISnackbar Snackbar

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h5" GutterBottom="true">Puntos registrados</MudText>
    <MudTextField @bind-Value="_busqueda" Label="Buscar por nombre"
                  Adornment="Adornment.Start" AdornmentIcon="@Icons.Material.Filled.Search"
                  Variant="Variant.Outlined" Margin="Margin.Dense" Class="mb-3"
                  Immediate="true" />
    <MudTable Items="PuntosFiltrados" Hover="true" Dense="true" Loading="_cargando">
        <HeaderContent>
            <MudTh>Foto</MudTh>
            <MudTh><MudTableSortLabel SortBy="new Func<PuntoDto,object>(p=>p.Nombre??string.Empty)">Nombre</MudTableSortLabel></MudTh>
            <MudTh>Latitud</MudTh>
            <MudTh>Longitud</MudTh>
            <MudTh>Fotos</MudTh>
            <MudTh>Acciones</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd>
                @if (context.CantidadFotos > 0)
                {
                    <img src="@Api.GetImagenUrl(context.Id)"
                         style="height:40px;width:40px;object-fit:cover;border-radius:4px" />
                }
            </MudTd>
            <MudTd>@(context.Nombre ?? "—")</MudTd>
            <MudTd>@context.Latitud.ToString("F6")</MudTd>
            <MudTd>@context.Longitud.ToString("F6")</MudTd>
            <MudTd><MudChip Size="Size.Small" Color="Color.Info">@context.CantidadFotos</MudChip></MudTd>
            <MudTd>
                <MudIconButton Icon="@Icons.Material.Filled.Edit"
                               OnClick="() => AbrirDetalle(context)" Size="Size.Small" />
                <MudIconButton Icon="@Icons.Material.Filled.Delete" Color="Color.Error"
                               OnClick="() => ConfirmarEliminar(context)" Size="Size.Small" />
            </MudTd>
        </RowTemplate>
    </MudTable>
</MudContainer>

@code {
    private List<PuntoDto> _puntos = new();
    private string? _busqueda;
    private bool _cargando = true;

    private IEnumerable<PuntoDto> PuntosFiltrados => string.IsNullOrWhiteSpace(_busqueda)
        ? _puntos
        : _puntos.Where(p => p.Nombre?.Contains(_busqueda, StringComparison.OrdinalIgnoreCase) == true);

    protected override async Task OnInitializedAsync()
    {
        _puntos = (await Api.GetPuntosAsync()).ToList();
        _cargando = false;
    }

    private async Task AbrirDetalle(PuntoDto punto)
    {
        var detalle = await Api.GetPuntoAsync(punto.Id);
        var parameters = new DialogParameters { ["Punto"] = detalle };
        var dialog = await Dialog.ShowAsync<DetallePunto>("Detalle del punto", parameters);
        var result = await dialog.Result;
        if (!result.Canceled) _puntos = (await Api.GetPuntosAsync()).ToList();
    }

    private async Task ConfirmarEliminar(PuntoDto punto)
    {
        var confirmacion = await Dialog.ShowMessageBox(
            "Eliminar punto",
            $"¿Eliminar '{punto.Nombre ?? "sin nombre"}' y sus {punto.CantidadFotos} foto(s)?",
            yesText: "Eliminar", cancelText: "Cancelar");
        if (confirmacion == true)
        {
            await Api.DeletePuntoAsync(punto.Id);
            _puntos.Remove(punto);
            Snackbar.Add("Punto eliminado", Severity.Success);
        }
    }
}
```

Verificación A:
  - /lista muestra tabla con todos los puntos
  - Búsqueda filtra en tiempo real
  - Editar abre modal con DetallePunto
  - Eliminar pide confirmación y borra el punto
  Reportá: "GEO-US06/07/08 OK — gestión web completa."

---

## BLOQUE B — GEO-US09: App MAUI Android con mapa

### B.1 — GeoFoto.Mobile/MauiProgram.cs

```csharp
builder.Services.AddMauiBlazorWebView();
builder.Services.AddMudServices();

#if DEBUG
builder.Services.AddBlazorWebViewDeveloperTools();
#endif

builder.Services.AddHttpClient<IGeoFotoApiClient, GeoFotoApiClient>(c =>
{
    // Para dispositivo físico en la misma red via adb reverse:
    c.BaseAddress = new Uri("http://localhost:5000/");
    c.Timeout = TimeSpan.FromSeconds(30);
});
```

### B.2 — GeoFoto.Mobile/wwwroot/index.html

```html
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>GeoFoto</title>
    <base href="/" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css" />
    <link href="_content/GeoFoto.Shared/css/geofoto.css" rel="stylesheet" />
</head>
<body>
    <div id="app">Cargando GeoFoto...</div>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
    <script src="_content/GeoFoto.Shared/js/leaflet-interop.js"></script>
    <script src="_framework/blazor.webview.js" autostart="true"></script>
</body>
</html>
```

### B.3 — GeoFoto.Mobile/MainPage.xaml

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:GeoFoto.Mobile.Components"
             x:Class="GeoFoto.Mobile.MainPage"
             BackgroundColor="White">
    <BlazorWebView HostPage="wwwroot/index.html">
        <BlazorWebView.RootComponents>
            <RootComponent Selector="#app" ComponentType="{x:Type local:Routes}" />
        </BlazorWebView.RootComponents>
    </BlazorWebView>
</ContentPage>
```

### B.4 — GeoFoto.Mobile/Components/Routes.razor

```razor
<Router AppAssembly="@typeof(GeoFoto.Shared._Imports).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData"
                   DefaultLayout="@typeof(GeoFoto.Shared.Layouts.MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(GeoFoto.Shared.Layouts.MainLayout)">
            <MudAlert Severity="Severity.Warning">Página no encontrada.</MudAlert>
        </LayoutView>
    </NotFound>
</Router>
```

### B.5 — GeoFoto.Mobile/GeoFoto.Mobile.csproj

Agregá dentro de PropertyGroup:
```xml
<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>
```

### B.6 — AndroidManifest.xml

```xml
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.CAMERA" />
```

### B.7 — Compilar y desplegar

```powershell
$adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
& $adb kill-server; Start-Sleep 2; & $adb start-server
& $adb devices   # verificar dispositivo conectado

# adb reverse para tunelizar localhost
& $adb reverse tcp:5000 tcp:5000

# compilar con assemblies embebidas
dotnet build GeoFoto.Mobile/GeoFoto.Mobile.csproj `
    --configuration Debug --framework net10.0-android

# instalar
$apk = Get-ChildItem "GeoFoto.Mobile/bin/Debug/net10.0-android" `
    -Filter "*-Signed.apk" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1
& $adb install -r $apk.FullName

# lanzar
& $adb shell monkey -p GeoFoto.Mobile -c android.intent.category.LAUNCHER 1
```

Si la pantalla está en blanco después de 15s:
1. Verificar que index.html tiene `blazor.webview.js` (no `blazor.server.js`)
2. Verificar que EmbedAssembliesIntoApk=true en el csproj
3. `& $adb logcat -d | Select-String "AndroidRuntime|fatal" | Select-Object -Last 20`

Verificación B:
  - App abre en Android con mapa Leaflet visible
  - Markers de los puntos del Sprint 01 aparecen en el mapa
  - Click en marker → panel de detalle con foto
  Reportá: "GEO-US09 OK — app MAUI con mapa en Android."

---

## BLOQUE C — GEO-US10: Cámara nativa en MAUI

Creá GeoFoto.Mobile/Services/ICamaraService.cs:

```csharp
public interface ICamaraService
{
    Task<Stream?> TomarFotoAsync();
    Task<Stream?> ElegirDeGaleriaAsync();
}

public class CamaraService : ICamaraService
{
    public async Task<Stream?> TomarFotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported) return null;
        var foto = await MediaPicker.Default.CapturePhotoAsync();
        return foto is null ? null : await foto.OpenReadAsync();
    }

    public async Task<Stream?> ElegirDeGaleriaAsync()
    {
        var foto = await MediaPicker.Default.PickPhotoAsync();
        return foto is null ? null : await foto.OpenReadAsync();
    }
}
```

Registrá en MauiProgram.cs:
```csharp
builder.Services.AddScoped<ICamaraService, CamaraService>();
```

En el MainLayout de Mobile (override o wrapper), agregá un MudFab flotante:

Creá GeoFoto.Mobile/Components/MobileLayout.razor como wrapper:
```razor
@inherits LayoutComponentBase

<GeoFoto.Shared.Layouts.MainLayout>
    @Body
</GeoFoto.Shared.Layouts.MainLayout>

<MudFab Color="Color.Secondary" StartIcon="@Icons.Material.Filled.CameraAlt"
        Style="position:fixed;bottom:24px;right:24px;z-index:1000;"
        OnClick="AbrirCamara" />

@inject ICamaraService Camara
@inject IGeoFotoApiClient Api
@inject ISnackbar Snackbar
@inject NavigationManager Nav

@code {
    private async Task AbrirCamara()
    {
        var stream = await Camara.TomarFotoAsync();
        if (stream is null) return;
        // Convertir stream a IBrowserFile-like para la API
        // Usar MultipartFormDataContent directamente
        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "files", $"foto_{DateTime.Now:yyyyMMddHHmmss}.jpg");
        // Llamar directamente al HttpClient (inyectar o usar HttpClientFactory)
        Snackbar.Add("Foto capturada — subiendo...", Severity.Info);
        Nav.NavigateTo("/");
    }
}
```

Nota: la subida desde cámara nativa en este sprint puede ser simplificada
(mostrar snackbar "Foto capturada"). La subida real al API se completará en
Sprint 03 cuando ILocalDbService maneje el flujo completo offline-first.

Verificación C:
  - Botón de cámara flotante visible en la app Android
  - Al tocar: se abre la cámara nativa
  - Después de tomar la foto: muestra "Foto capturada"
  Reportá: "GEO-US10 OK — cámara nativa integrada."

---

## CHECKLIST DEMO — SPRINT 02

  [ ] ListaPuntos.razor en /lista muestra tabla con todos los puntos
  [ ] Filtro por nombre funciona en tiempo real
  [ ] Editar punto desde lista → modal → guardar → tabla actualizada
  [ ] Eliminar punto → confirmación → borrado → tabla actualizada
  [ ] App Android instalada y corriendo con mapa visible
  [ ] Markers de puntos existentes aparecen en Android
  [ ] Click en marker en Android → detalle con foto
  [ ] Botón de cámara flotante visible en Android
  [ ] Al tocar cámara → abre cámara nativa del celular
  [ ] dotnet build GeoFoto.sln → 0 errores

Si todo pasa: "SPRINT 02 COMPLETO — gestión web + app Android funcionando."
```

---
---

# SPRINT 03 — Offline-First: SQLite y cola de sincronización

```
Eres un desarrollador .NET 10 senior especializado en MAUI y SQLite.
Implementás el Sprint 03 de GeoFoto. El esqueleto web y la app Android funcionan.
Este sprint es el más crítico: agrega el motor offline-first.

LECTURA OBLIGATORIA:
  docs/07_plan-sprint/plan-iteracion_sprint-03_v1.0.md
  docs/05_arquitectura_tecnica/arquitectura-offline-sync_v1.0.md  (documento completo)
  docs/05_arquitectura_tecnica/modelo-datos-logico_v1.0.md        (Sección B — SQLite)
  docs/06_backlog-tecnico/backlog-tecnico_v1.0.md  (tareas GEO-US11 a GEO-US13)

Confirmá: "Sprint 03 leído. Arquitectura offline-sync internalizada."

---

## HISTORIAS DE ESTE SPRINT

GEO-US11 — Guardar fotos y puntos en SQLite aunque no haya internet (13 pts)
GEO-US12 — Ver cuántas operaciones hay pendientes de sync (5 pts)
GEO-US13 — App funciona igual offline y online sin diferencia visible (5 pts)

Total: 23 story points

---

## PRINCIPIO GUÍA DE ESTE SPRINT

Toda escritura va primero a SQLite local. La API es secundaria.
El usuario nunca espera la red para ver resultados de sus acciones.

---

## BLOQUE A — Infraestructura SQLite (GEO-US11, parte 1)

### A.1 — NuGet en GeoFoto.Mobile

```xml
<PackageReference Include="sqlite-net-pcl" Version="1.9.*" />
<PackageReference Include="SQLitePCLRaw.bundle_green" Version="2.*" />
```

### A.2 — Modelos SQLite (GeoFoto.Mobile/LocalData/Models/)

```csharp
// LocalPunto.cs
[Table("Puntos_Local")]
public class LocalPunto
{
    [PrimaryKey, AutoIncrement] public int LocalId { get; set; }
    public int? RemoteId { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public string FechaCreacion { get; set; } = DateTime.UtcNow.ToString("O");
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string SyncStatus { get; set; } = SyncStatusValues.Local;
}

// LocalFoto.cs
[Table("Fotos_Local")]
public class LocalFoto
{
    [PrimaryKey, AutoIncrement] public int LocalId { get; set; }
    public int? RemoteId { get; set; }
    public int PuntoLocalId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaLocal { get; set; } = string.Empty;
    public string? FechaTomada { get; set; }
    public long TamanoBytes { get; set; }
    public decimal? LatitudExif { get; set; }
    public decimal? LongitudExif { get; set; }
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string SyncStatus { get; set; } = SyncStatusValues.Local;
}

// SyncQueueItem.cs
[Table("SyncQueue")]
public class SyncQueueItem
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string OperationType { get; set; } = string.Empty;  // Create/Update/Delete
    public string EntityType { get; set; } = string.Empty;     // Punto/Foto
    public int LocalId { get; set; }
    public string Payload { get; set; } = string.Empty;        // JSON
    public string Status { get; set; } = SyncQueueStatus.Pending;
    public int Attempts { get; set; } = 0;
    public string? LastAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}

// Constantes de estado
public static class SyncStatusValues
{
    public const string Local = "Local";
    public const string Synced = "Synced";
    public const string PendingCreate = "PendingCreate";
    public const string PendingUpdate = "PendingUpdate";
    public const string PendingDelete = "PendingDelete";
    public const string Conflict = "Conflict";
    public const string Failed = "Failed";
}

public static class SyncQueueStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Done = "Done";
    public const string Failed = "Failed";
}
```

### A.3 — ILocalDbService (GeoFoto.Mobile/LocalData/)

```csharp
public interface ILocalDbService
{
    // Inicialización
    Task InitializeAsync();

    // Puntos
    Task<List<LocalPunto>> GetPuntosAsync();
    Task<LocalPunto?> GetPuntoAsync(int localId);
    Task<int> InsertPuntoAsync(LocalPunto punto);
    Task UpdatePuntoAsync(LocalPunto punto);
    Task DeletePuntoAsync(int localId);
    Task<LocalPunto?> FindPuntoCercanoAsync(decimal lat, decimal lng, decimal tolerancia = 0.001m);

    // Fotos
    Task<List<LocalFoto>> GetFotosByPuntoAsync(int puntoLocalId);
    Task<int> InsertFotoAsync(LocalFoto foto);
    Task DeleteFotoAsync(int localId);

    // SyncQueue
    Task EnqueueAsync(string operationType, string entityType, int localId, string payload);
    Task<List<SyncQueueItem>> GetPendingOperationsAsync();
    Task<int> GetPendingCountAsync();
    Task MarkDoneAsync(int queueId, int? remoteId = null);
    Task MarkFailedAsync(int queueId, string error);
    Task IncrementAttemptsAsync(int queueId);
}

public class LocalDbService : ILocalDbService
{
    private SQLiteAsyncConnection? _db;
    private readonly string _dbPath;

    public LocalDbService()
    {
        _dbPath = Path.Combine(
            FileSystem.Current.AppDataDirectory,
            "geofoto.db3");
    }

    public async Task InitializeAsync()
    {
        if (_db is not null) return;
        _db = new SQLiteAsyncConnection(_dbPath);
        await _db.CreateTableAsync<LocalPunto>();
        await _db.CreateTableAsync<LocalFoto>();
        await _db.CreateTableAsync<SyncQueueItem>();
    }

    // Implementar todos los métodos de la interfaz con operaciones CRUD sobre _db
    // FindPuntoCercano: query con Math.Abs(Latitud - lat) < tolerancia AND Math.Abs(Longitud - lng) < tolerancia
    // GetPendingCount: COUNT WHERE Status = Pending
    // EnqueueAsync: InsertAsync new SyncQueueItem con Status=Pending
    // MarkDoneAsync: UpdateAsync Status=Done
    // MarkFailedAsync: UpdateAsync Status=Failed, ErrorMessage=error
    // IncrementAttemptsAsync: UpdateAsync Attempts++, LastAttemptAt=UtcNow
}
```

Registrá en MauiProgram.cs como Singleton:
```csharp
builder.Services.AddSingleton<ILocalDbService, LocalDbService>();
```

### A.4 — IConnectivityService (GeoFoto.Mobile/Services/)

```csharp
public interface IConnectivityService
{
    bool IsConnected { get; }
    event EventHandler<bool> ConnectivityChanged;
}

public class ConnectivityService : IConnectivityService, IDisposable
{
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;

    public ConnectivityService()
    {
        Connectivity.ConnectivityChanged += OnChanged;
    }

    private void OnChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ConnectivityChanged?.Invoke(this, IsConnected);
    }

    public void Dispose() => Connectivity.ConnectivityChanged -= OnChanged;
}
```

Registrá como Singleton en MauiProgram.cs.

### A.5 — Modificar flujo de subida de fotos para que escriba en SQLite primero

En GeoFoto.Shared/Pages/SubirFotos.razor el método OnFilesChanged debe detectar
si está en contexto MAUI (via DI) y usar ILocalDbService en lugar de la API directamente.

La forma más limpia es crear un IFotoUploadStrategy:

```csharp
// GeoFoto.Shared/Services/IFotoUploadStrategy.cs
public interface IFotoUploadStrategy
{
    Task<UploadResultDto?> SubirAsync(IBrowserFile file, CancellationToken ct = default);
}

// GeoFoto.Shared/Services/ApiUploadStrategy.cs (para Web — online)
public class ApiUploadStrategy : IFotoUploadStrategy
{
    private readonly IGeoFotoApiClient _api;
    public ApiUploadStrategy(IGeoFotoApiClient api) => _api = api;
    public Task<UploadResultDto?> SubirAsync(IBrowserFile file, CancellationToken ct)
        => _api.UploadFotoAsync(file, ct);
}
```

En GeoFoto.Mobile registrá la estrategia offline:
```csharp
// LocalUploadStrategy.cs — escribe en SQLite, encola en SyncQueue
public class LocalUploadStrategy : IFotoUploadStrategy
{
    private readonly ILocalDbService _db;
    private readonly IExifMobileService _exif;  // versión MAUI del extractor
    private readonly IFileStorageService _storage;

    // SubirAsync:
    // 1. Guardar el archivo en AppDataDirectory/photos/{guid}.jpg
    // 2. Extraer GPS del stream con SixLabors.ImageSharp.Metadata o con bytes EXIF
    //    (en MAUI usar SkiaSharp o leer EXIF con MetadataExtractor via stream)
    // 3. Buscar Punto cercano en SQLite con FindPuntoCercanoAsync
    // 4. Si no existe: crear LocalPunto con SyncStatus=PendingCreate
    // 5. Insertar LocalPunto y obtener LocalId
    // 6. Crear LocalFoto con PuntoLocalId=localId, SyncStatus=PendingCreate
    // 7. EnqueueAsync("Create", "Punto", localId, JsonSerializer.Serialize(localPunto))
    // 8. EnqueueAsync("Create", "Foto", fotoLocalId, ...)
    // 9. Retornar UploadResultDto con TeniaGps=true/false
}
```

En SubirFotos.razor usá `@inject IFotoUploadStrategy UploadStrategy` en lugar
de `@inject IGeoFotoApiClient Api` para la subida. La estrategia correcta se
inyecta según el host (Web usa ApiUploadStrategy, Mobile usa LocalUploadStrategy).

Registrá en GeoFoto.Web/Program.cs:
```csharp
builder.Services.AddScoped<IFotoUploadStrategy, ApiUploadStrategy>();
```

Registrá en GeoFoto.Mobile/MauiProgram.cs:
```csharp
builder.Services.AddScoped<IFotoUploadStrategy, LocalUploadStrategy>();
```

---

## BLOQUE B — SyncStatusBadge (GEO-US12)

Creá GeoFoto.Shared/Components/SyncStatusBadge.razor:

```razor
@inject IConnectivityService? Connectivity  @* null en Web *@
@inject ILocalDbService? LocalDb            @* null en Web *@
@implements IDisposable

@if (_pendientes > 0)
{
    <MudBadge Content="@_pendientes" Color="Color.Warning" Overlap="true">
        <MudTooltip Text="@($"{_pendientes} operaciones pendientes de sincronizar")">
            <MudIconButton Icon="@Icons.Material.Filled.CloudOff"
                           Color="Color.Warning" Href="/sync" />
        </MudTooltip>
    </MudBadge>
}
else if (Connectivity?.IsConnected == true)
{
    <MudIconButton Icon="@Icons.Material.Filled.CloudDone" Color="Color.Success" />
}
else
{
    <MudIconButton Icon="@Icons.Material.Filled.CloudOff" Color="Color.Default" />
}

@code {
    private int _pendientes;
    private Timer? _timer;

    protected override async Task OnInitializedAsync()
    {
        await ActualizarContador();
        // Polling cada 5 segundos para actualizar el badge
        _timer = new Timer(async _ =>
        {
            await ActualizarContador();
            await InvokeAsync(StateHasChanged);
        }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        if (Connectivity is not null)
            Connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    private async Task ActualizarContador()
    {
        if (LocalDb is null) return;
        _pendientes = await LocalDb.GetPendingCountAsync();
    }

    private void OnConnectivityChanged(object? sender, bool connected)
        => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        _timer?.Dispose();
        if (Connectivity is not null)
            Connectivity.ConnectivityChanged -= OnConnectivityChanged;
    }
}
```

Agregá `<SyncStatusBadge />` en el MudAppBar del MainLayout (antes del cierre de MudAppBar).

---

## BLOQUE C — Verificación offline (GEO-US13)

Para verificar que el flujo offline funciona:

1. Compilar e instalar la app en Android con los cambios del Sprint 03
2. Poner el dispositivo en modo avión
3. Abrir la app → debe cargar normalmente (datos locales de SQLite)
4. Ir a /subir → subir foto usando MudFileUpload
5. El badge debe mostrar "1" (operación pendiente)
6. Ir a /sync → ver la operación en estado "Pending"
7. El punto debe verse en el mapa con los datos locales

Script de verificación post-deploy:
```powershell
$adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
& $adb reverse tcp:5000 tcp:5000
& $adb install -r "GeoFoto.Mobile\bin\Debug\net10.0-android\GeoFoto.Mobile-Signed.apk"
Start-Sleep 3
& $adb shell monkey -p GeoFoto.Mobile -c android.intent.category.LAUNCHER 1
Start-Sleep 10
# Capturar screenshot
& $adb shell screencap -p /sdcard/sprint03_test.png
& $adb pull /sdcard/sprint03_test.png sprint03_verificacion.png
Write-Host "Screenshot guardado: sprint03_verificacion.png"
```

---

## PÁGINA EstadoSync.razor (GEO-US12, complemento)

Creá GeoFoto.Shared/Pages/EstadoSync.razor (@page "/sync"):

```razor
@page "/sync"
@inject ILocalDbService? LocalDb    @* null en Web → mostrar mensaje informativo *@
@inject IConnectivityService? Connectivity

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-4">
    <MudText Typo="Typo.h5" GutterBottom="true">Estado de sincronización</MudText>

    @if (LocalDb is null)
    {
        <MudAlert Severity="Severity.Info">
            La vista de sincronización está disponible solo en la app móvil.
        </MudAlert>
    }
    else
    {
        <MudGrid Class="mb-4">
            <MudItem xs="6" sm="3">
                <MudPaper Class="pa-4 text-center" Outlined="true">
                    <MudText Typo="Typo.h3" Color="Color.Warning">@_pendientes</MudText>
                    <MudText Typo="Typo.caption">Pendientes</MudText>
                </MudPaper>
            </MudItem>
            <MudItem xs="6" sm="3">
                <MudPaper Class="pa-4 text-center" Outlined="true">
                    <MudText Typo="Typo.h3" Color="Color.Success">@_sincronizados</MudText>
                    <MudText Typo="Typo.caption">Sincronizados</MudText>
                </MudPaper>
            </MudItem>
            <MudItem xs="6" sm="3">
                <MudPaper Class="pa-4 text-center" Outlined="true">
                    <MudText Typo="Typo.h3" Color="Color.Error">@_fallidos</MudText>
                    <MudText Typo="Typo.caption">Fallidos</MudText>
                </MudPaper>
            </MudItem>
            <MudItem xs="6" sm="3">
                <MudPaper Class="pa-4 text-center" Outlined="true">
                    <MudIcon Icon="@(_conectado ? Icons.Material.Filled.Wifi : Icons.Material.Filled.WifiOff)"
                             Color="@(_conectado ? Color.Success : Color.Error)" />
                    <MudText Typo="Typo.caption">@(_conectado ? "Conectado" : "Sin conexión")</MudText>
                </MudPaper>
            </MudItem>
        </MudGrid>

        <MudButton Variant="Variant.Filled" Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.Sync"
                   Disabled="!_conectado || _sincronizando"
                   OnClick="SincronizarAhora" Class="mb-4">
            @(_sincronizando ? "Sincronizando..." : "Sincronizar ahora")
        </MudButton>

        @if (_sincronizando)
        {
            <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mb-4" />
        }

        <MudText Typo="Typo.h6" GutterBottom="true">Cola de operaciones</MudText>
        <MudTable Items="_operaciones" Dense="true" Hover="true">
            <HeaderContent>
                <MudTh>Tipo</MudTh>
                <MudTh>Entidad</MudTh>
                <MudTh>Estado</MudTh>
                <MudTh>Intentos</MudTh>
                <MudTh>Fecha</MudTh>
                <MudTh>Error</MudTh>
            </HeaderContent>
            <RowTemplate>
                <MudTd>@context.OperationType</MudTd>
                <MudTd>@context.EntityType #@context.LocalId</MudTd>
                <MudTd>
                    <MudChip Size="Size.Small"
                             Color="@EstadoColor(context.Status)">@context.Status</MudChip>
                </MudTd>
                <MudTd>@context.Attempts</MudTd>
                <MudTd>@FormatFecha(context.CreatedAt)</MudTd>
                <MudTd>
                    @if (context.ErrorMessage is not null)
                    { <MudTooltip Text="@context.ErrorMessage"><MudIcon Icon="@Icons.Material.Filled.Error" Color="Color.Error" /></MudTooltip> }
                </MudTd>
            </RowTemplate>
        </MudTable>
    }
</MudContainer>

@code {
    private int _pendientes, _sincronizados, _fallidos;
    private bool _conectado, _sincronizando;
    private List<SyncQueueItem> _operaciones = new();

    protected override async Task OnInitializedAsync()
    {
        _conectado = Connectivity?.IsConnected ?? true;
        await CargarDatos();
    }

    private async Task CargarDatos()
    {
        if (LocalDb is null) return;
        _operaciones = await LocalDb.GetPendingOperationsAsync();
        _pendientes = _operaciones.Count(o => o.Status == SyncQueueStatus.Pending);
        _sincronizados = _operaciones.Count(o => o.Status == SyncQueueStatus.Done);
        _fallidos = _operaciones.Count(o => o.Status == SyncQueueStatus.Failed);
    }

    private async Task SincronizarAhora()
    {
        _sincronizando = true;
        // En Sprint 04 se llama ISyncService.SyncNowAsync()
        // En Sprint 03 solo mostramos el estado — el servicio se implementa en S04
        await Task.Delay(1000);  // placeholder
        _sincronizando = false;
        await CargarDatos();
    }

    private Color EstadoColor(string status) => status switch
    {
        SyncQueueStatus.Pending => Color.Warning,
        SyncQueueStatus.Done => Color.Success,
        SyncQueueStatus.Failed => Color.Error,
        SyncQueueStatus.InProgress => Color.Info,
        _ => Color.Default
    };

    private string FormatFecha(string iso) =>
        DateTime.TryParse(iso, out var dt) ? dt.ToLocalTime().ToString("dd/MM HH:mm") : iso;
}
```

---

## CHECKLIST DEMO — SPRINT 03

  [ ] dotnet build GeoFoto.sln → 0 errores
  [ ] Tablas SQLite creadas: Puntos_Local, Fotos_Local, SyncQueue
  [ ] App en modo avión → /subir → subir foto → resultado visible
  [ ] SyncStatusBadge muestra número de pendientes en AppBar
  [ ] /sync muestra métricas y tabla de operaciones
  [ ] Al subir foto en modo avión → aparece en SyncQueue con Status=Pending
  [ ] Punto aparece en mapa local aunque no tenga RemoteId
  [ ] Sin modo avión: /subir web (GeoFoto.Web) funciona igual que Sprint 02

Si todo pasa: "SPRINT 03 COMPLETO — offline-first operativo con SQLite y cola."
```

---
---

# SPRINT 04 — Motor de sincronización completo

```
Implementás el Sprint 04 de GeoFoto. La app guarda offline en SQLite.
Este sprint implementa el SyncService que sincroniza automáticamente.

LECTURA OBLIGATORIA:
  docs/07_plan-sprint/plan-iteracion_sprint-04_v1.0.md
  docs/05_arquitectura_tecnica/arquitectura-offline-sync_v1.0.md (COMPLETO — es la spec del SyncService)

Confirmá: "Sprint 04 leído. Flujo push y pull internalizados."

---

## HISTORIAS

GEO-US14 — Sync automático al recuperar red (13 pts)
GEO-US15 — Sync manual desde EstadoSync (5 pts)
GEO-US16 — Historial de operaciones con resultado (5 pts)
GEO-US17 — Resolución de conflictos Last-Write-Wins (8 pts)

Total: 31 story points

---

## BLOQUE A — Endpoints de sync en la API

### A.1 — GET /api/sync/delta?since={iso8601}

```csharp
[HttpGet("delta")]
public async Task<ActionResult<SyncDeltaDto>> GetDelta(
    [FromQuery] string? since, CancellationToken ct)
// Retorna puntos y fotos cuyo UpdatedAt > since (o todos si since es null)
// SyncDeltaDto: record con Puntos: IReadOnlyList<PuntoDto> y Fotos: IReadOnlyList<FotoDto>
```

### A.2 — POST /api/sync/batch

```csharp
[HttpPost("batch")]
public async Task<ActionResult<BatchResultDto>> ProcessBatch(
    [FromBody] IReadOnlyList<SyncOperationDto> operations, CancellationToken ct)
// Para cada operación: procesar Create/Update/Delete según EntityType
// Retornar BatchResultDto: lista de resultados por operación (éxito, error, remoteId)
// Ejecutar todo en una transacción
```

DTOs de sync:
```csharp
public record SyncOperationDto(string OperationType, string EntityType,
    int LocalId, string Payload);  // Payload = JSON de la entidad
public record SyncOperationResultDto(int LocalId, bool Success,
    int? RemoteId, string? Error);
public record BatchResultDto(IReadOnlyList<SyncOperationResultDto> Results);
public record SyncDeltaDto(IReadOnlyList<PuntoDto> Puntos, IReadOnlyList<FotoDto> Fotos);
```

---

## BLOQUE B — ISyncService (GeoFoto.Mobile/Services/)

```csharp
public interface ISyncService
{
    bool IsSyncing { get; }
    event EventHandler<SyncCompletedEventArgs> SyncCompleted;
    Task SyncNowAsync(CancellationToken ct = default);
    Task StartBackgroundSyncAsync();
}

public record SyncCompletedEventArgs(int Successful, int Failed, List<string> Errors);

public class SyncService : ISyncService
{
    private readonly ILocalDbService _localDb;
    private readonly IGeoFotoApiClient _api;
    private readonly IConnectivityService _connectivity;
    private readonly ILogger<SyncService> _logger;
    private bool _isSyncing;

    public bool IsSyncing => _isSyncing;
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    public async Task StartBackgroundSyncAsync()
    {
        // Suscribirse a ConnectivityChanged
        _connectivity.ConnectivityChanged += async (_, connected) =>
        {
            if (connected && !_isSyncing)
                await SyncNowAsync();
        };
        // Si ya hay conexión al iniciar: sync inmediato
        if (_connectivity.IsConnected)
            await SyncNowAsync();
    }

    public async Task SyncNowAsync(CancellationToken ct = default)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        var successful = 0; var failed = 0; var errors = new List<string>();

        try
        {
            // PUSH: enviar operaciones pendientes
            await PushAsync(ct);

            // PULL: descargar cambios del servidor
            await PullAsync(ct);
        }
        catch (Exception ex) { _logger.LogError(ex, "Error en sync"); errors.Add(ex.Message); }
        finally
        {
            _isSyncing = false;
            SyncCompleted?.Invoke(this, new(successful, failed, errors));
        }
    }

    private async Task PushAsync(CancellationToken ct)
    {
        var pendientes = await _localDb.GetPendingOperationsAsync();
        if (!pendientes.Any()) return;

        // Agrupar en batches de 50
        var batches = pendientes.Chunk(50);
        foreach (var batch in batches)
        {
            var operations = batch.Select(op => new SyncOperationDto(
                op.OperationType, op.EntityType, op.LocalId, op.Payload)).ToList();
            try
            {
                var result = await _api.SyncBatchAsync(operations, ct);
                foreach (var r in result.Results)
                {
                    var item = batch.First(b => b.LocalId == r.LocalId);
                    if (r.Success)
                        await _localDb.MarkDoneAsync(item.Id, r.RemoteId);
                    else
                    {
                        item.Attempts++;
                        if (item.Attempts >= 3)
                            await _localDb.MarkFailedAsync(item.Id, r.Error ?? "Max intentos");
                        else
                        {
                            await _localDb.IncrementAttemptsAsync(item.Id);
                            // Backoff: 5s, 30s, 5min
                            var delay = item.Attempts switch { 1 => 5, 2 => 30, _ => 300 };
                            await Task.Delay(TimeSpan.FromSeconds(delay), ct);
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Sin red: dejar todo como Pending para el próximo intento
                break;
            }
        }
    }

    private async Task PullAsync(CancellationToken ct)
    {
        // Obtener timestamp de última sync exitosa (guardar en preferencias locales)
        var lastSync = Preferences.Get("last_sync_utc", string.Empty);
        var delta = await _api.GetSyncDeltaAsync(
            string.IsNullOrEmpty(lastSync) ? null : lastSync, ct);

        // Actualizar puntos locales con Last-Write-Wins
        foreach (var remotePunto in delta.Puntos)
        {
            // Buscar por RemoteId en SQLite
            // Si no existe: insertar con SyncStatus=Synced
            // Si existe: comparar UpdatedAt — si remoto es más reciente, actualizar
        }

        Preferences.Set("last_sync_utc", DateTime.UtcNow.ToString("O"));
    }
}
```

Registrá como Singleton en MauiProgram.cs e iniciá el background sync:
```csharp
builder.Services.AddSingleton<ISyncService, SyncService>();
// En App.xaml.cs o en un handler de startup:
var syncService = app.Services.GetRequiredService<ISyncService>();
await syncService.StartBackgroundSyncAsync();
```

---

## BLOQUE C — Conectar EstadoSync.razor con ISyncService real

En EstadoSync.razor reemplazá el método SincronizarAhora():

```csharp
@inject ISyncService? SyncService

private async Task SincronizarAhora()
{
    if (SyncService is null) return;
    _sincronizando = true;
    try
    {
        await SyncService.SyncNowAsync();
        await CargarDatos();
        Snackbar.Add("Sincronización completada", Severity.Success);
    }
    catch (Exception ex)
    {
        Snackbar.Add($"Error: {ex.Message}", Severity.Error);
    }
    finally { _sincronizando = false; }
}
```

---

## BLOQUE D — Añadir métodos de sync en IGeoFotoApiClient

```csharp
Task<SyncDeltaDto> GetSyncDeltaAsync(string? since, CancellationToken ct = default);
Task<BatchResultDto> SyncBatchAsync(IReadOnlyList<SyncOperationDto> operations, CancellationToken ct = default);
```

Implementar en GeoFotoApiClient con HttpClient.

---

## CHECKLIST DEMO — SPRINT 04

  [ ] API responde GET /api/sync/delta correctamente
  [ ] API procesa POST /api/sync/batch en transacción
  [ ] Capturar puntos en modo avión → badge muestra pendientes
  [ ] Desactivar modo avión → SyncService detecta red → sync automático
  [ ] Badge vuelve a 0 → /sync muestra operaciones en Done
  [ ] Puntos aparecen en la web (GeoFoto.Web) después del sync
  [ ] Botón "Sincronizar ahora" en /sync funciona manualmente
  [ ] Forzar conflicto (editar desde web y mobile) → se resuelve sin crash
  [ ] Operaciones fallidas muestran mensaje de error en /sync
  [ ] dotnet build GeoFoto.sln → 0 errores

Si todo pasa: "SPRINT 04 COMPLETO — motor de sync end-to-end funcionando."
```

---
---

# SPRINT 05 — CI/CD y cobertura de tests del motor de sync

```
Implementás el Sprint 05 de GeoFoto. El sistema funciona completo online y offline.
Este sprint agrega la red de seguridad: pipeline CI/CD y tests del motor crítico.

LECTURA OBLIGATORIA:
  docs/07_plan-sprint/plan-iteracion_sprint-05_v1.0.md
  docs/08_calidad_y_pruebas/estrategia-testing-motor_v1.0.md (si existe, sino la de-done)
  docs/09_devops/pipeline-ci-cd_v1.0.md

Confirmá: "Sprint 05 leído. Estrategia de CI/CD y testing clara."

---

## HISTORIAS

GEO-US18 — Pipeline CI/CD en GitHub Actions (8 pts)
GEO-US19 — Tests de integración del SyncService ≥ 80% cobertura (8 pts)

Total: 16 story points

---

## BLOQUE A — GEO-US18: Pipeline GitHub Actions

Creá `.github/workflows/ci.yml` en la raíz del repositorio:

```yaml
name: GeoFoto CI

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  build-and-test:
    runs-on: windows-latest  # MAUI requiere Windows o macOS
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install MAUI workload
        run: dotnet workload install maui-android --skip-sign-check

      - name: Restore
        run: dotnet restore GeoFoto.sln

      - name: Build (sin Mobile — Mobile requiere Android SDK en CI)
        run: |
          dotnet build GeoFoto.Api/GeoFoto.Api.csproj --no-restore -c Release
          dotnet build GeoFoto.Shared/GeoFoto.Shared.csproj --no-restore -c Release
          dotnet build GeoFoto.Web/GeoFoto.Web.csproj --no-restore -c Release

      - name: Test
        run: dotnet test GeoFoto.Tests/GeoFoto.Tests.csproj --no-restore --verbosity normal
             --collect:"XPlat Code Coverage"

      - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          files: '**/coverage.cobertura.xml'

  build-android:
    runs-on: windows-latest
    needs: build-and-test
    if: github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Install MAUI Android workload
        run: dotnet workload install maui-android
      - name: Build APK
        run: dotnet build GeoFoto.Mobile/GeoFoto.Mobile.csproj
             --configuration Release --framework net10.0-android
             -p:EmbedAssembliesIntoApk=true
      - name: Upload APK artifact
        uses: actions/upload-artifact@v4
        with:
          name: GeoFoto-Android-APK
          path: GeoFoto.Mobile/bin/Release/net10.0-android/*-Signed.apk
          retention-days: 30
```

---

## BLOQUE B — GEO-US19: Tests del motor de sync

Creá el proyecto de tests: `GeoFoto.Tests/GeoFoto.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Moq" Version="4.*" />
    <PackageReference Include="FluentAssertions" Version="6.*" />
    <PackageReference Include="sqlite-net-pcl" Version="1.9.*" />
    <PackageReference Include="SQLitePCLRaw.bundle_green" Version="2.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\GeoFoto.Mobile\GeoFoto.Mobile.csproj" />
    <ProjectReference Include="..\GeoFoto.Api\GeoFoto.Api.csproj" />
  </ItemGroup>
</Project>
```

Tests a implementar (mínimo 15 tests que cubran los escenarios críticos):

```csharp
// GeoFoto.Tests/LocalDbServiceTests.cs
public class LocalDbServiceTests : IAsyncLifetime
{
    private ILocalDbService _db = null!;

    public async Task InitializeAsync()
    {
        // Usar BD SQLite en memoria o en archivo temporal
        _db = new LocalDbService(":memory:");
        await _db.InitializeAsync();
    }

    [Fact] public async Task InsertPunto_DebeAsignarLocalId()
    [Fact] public async Task FindPuntoCercano_ConTolerancia_DebeEncontrar()
    [Fact] public async Task FindPuntoCercano_FueraDeTolerancia_RetornaNull()
    [Fact] public async Task GetPendingCount_DebeContarSoloPending()
    [Fact] public async Task MarkDone_DebeActualizarRemoteId()
    [Fact] public async Task MarkFailed_DebeGuardarMensajeError()
    [Fact] public async Task IncrementAttempts_DebeIncrementarContador()
    [Fact] public async Task Enqueue_DebeCrearItemConStatusPending()
    [Fact] public async Task DeleteFoto_DebeEliminarSoloEsaFoto()

    public Task DisposeAsync() => Task.CompletedTask;
}

// GeoFoto.Tests/SyncServiceTests.cs
public class SyncServiceTests
{
    private readonly Mock<ILocalDbService> _localDbMock = new();
    private readonly Mock<IGeoFotoApiClient> _apiMock = new();
    private readonly Mock<IConnectivityService> _connMock = new();

    [Fact] public async Task SyncNow_CuandoHayPendientes_DebeEnviarAlApi()
    [Fact] public async Task SyncNow_CuandoApiRetorna200_DebeMarcarDone()
    [Fact] public async Task SyncNow_CuandoApiRetorna500_DebeIncrementarAttempts()
    [Fact] public async Task SyncNow_CuandoAttempts3_DebeMarcarFailed()
    [Fact] public async Task SyncNow_SinConexion_NoDebeIntentarApi()
    [Fact] public async Task Pull_CuandoServidorTieneNuevoPunto_DebeInsertarLocal()
    [Fact] public async Task Pull_LastWriteWins_ServidorMasReciente_DebeActualizar()
}

// GeoFoto.Tests/ExifServiceTests.cs
public class ExifServiceTests
{
    [Fact] public void ExtractGeoData_ConArchivoSinExif_RetornaNulls()
    [Fact] public void ExtractGeoData_ConArchivoConGps_RetornaCoordenadas()
    // Usar un JPG de prueba embebido como recurso
}
```

---

## CHECKLIST DEMO — SPRINT 05

  [ ] .github/workflows/ci.yml existe en la raíz
  [ ] Push a develop → Actions en GitHub muestra pipeline verde
  [ ] Todos los tests pasan: dotnet test GeoFoto.Tests
  [ ] Cobertura de SyncService ≥ 80% (verificar con reporte)
  [ ] APK se genera como artifact en el pipeline de main
  [ ] dotnet build GeoFoto.sln → 0 errores

Si todo pasa: "SPRINT 05 COMPLETO — CI/CD verde y tests del sync cubriendo casos críticos."
```

---
---

# SPRINT 06 — Madurez: performance, UX de campo y distribución

```
Implementás el Sprint 06, el sprint final de GeoFoto v1.0.
El sistema está completo. Este sprint lo hace productivo y distribuible.

LECTURA OBLIGATORIA:
  docs/07_plan-sprint/plan-iteracion_sprint-06_v1.0.md
  docs/09_devops/estrategia-deploy_v1.0.md

Confirmá: "Sprint 06 leído. Objetivos de madurez identificados."

---

## HISTORIAS

GEO-US20 — Mapa fluido con 100+ puntos y paginación en lista (5 pts)
+ Hardening: distribución APK, UX de campo, smoke tests (8 pts extras)

Total: ~13 story points + hardening

---

## BLOQUE A — GEO-US20: Clustering en Leaflet y paginación

### A.1 — Clustering de markers en Leaflet

Reemplazá el marcado básico por Leaflet.markercluster.
En index.html (Web y Mobile) agregá CDN de markercluster:
```html
<link rel="stylesheet" href="https://unpkg.com/leaflet.markercluster@1.5.3/dist/MarkerCluster.css" />
<link rel="stylesheet" href="https://unpkg.com/leaflet.markercluster@1.5.3/dist/MarkerCluster.Default.css" />
<script src="https://unpkg.com/leaflet.markercluster@1.5.3/dist/leaflet.markercluster.js"></script>
```

En leaflet-interop.js reemplazá la creación de markers:
```javascript
// Reemplazar array de markers por ClusterGroup
this._clusterGroup = L.markerClusterGroup({ maxClusterRadius: 50 });
this._map.addLayer(this._clusterGroup);

// En addMarkers: usar this._clusterGroup.addLayer(marker) en lugar de marker.addTo(this._map)
```

### A.2 — Paginación en ListaPuntos.razor

Reemplazá MudTable con paginación:
```razor
<MudTable Items="PuntosFiltrados" Hover="true" Dense="true"
          Loading="_cargando" RowsPerPage="25">
    <PagerContent>
        <MudTablePager PageSizeOptions="new int[]{10, 25, 50, 100}" />
    </PagerContent>
    ...
</MudTable>
```

---

## BLOQUE B — Distribución de la APK

### B.1 — GitHub Release con APK

Creá `.github/workflows/release.yml`:

```yaml
name: GeoFoto Release

on:
  push:
    tags:
      - 'v*'

jobs:
  release-android:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Install workloads
        run: dotnet workload install maui-android
      - name: Build APK Release
        run: dotnet build GeoFoto.Mobile/GeoFoto.Mobile.csproj
             --configuration Release --framework net10.0-android
             -p:EmbedAssembliesIntoApk=true
      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: GeoFoto.Mobile/bin/Release/net10.0-android/*-Signed.apk
          name: GeoFoto ${{ github.ref_name }}
          body: |
            ## GeoFoto ${{ github.ref_name }}
            ### Instalación en Android
            1. Descargá el archivo APK
            2. En el celular: Configuración → Instalar apps desconocidas
            3. Abrí el APK descargado e instalá
            4. En la primera apertura: asegurate de tener conexión para sync inicial
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

Para generar el release:
```
git tag v1.0.0
git push origin v1.0.0
```

---

## BLOQUE C — Hardening y UX de campo

### C.1 — Manejo de errores de red en la app

En GeoFotoApiClient, envolvé todas las llamadas con manejo de timeout:
```csharp
catch (TaskCanceledException) { throw new GeoFotoApiException("La solicitud tardó demasiado. Verificá tu conexión."); }
catch (HttpRequestException ex) { throw new GeoFotoApiException($"Error de red: {ex.Message}"); }
```

En las páginas Blazor, mostrá errores con MudAlert en lugar de dejar la pantalla en blanco:
```razor
@if (_error is not null)
{
    <MudAlert Severity="Severity.Error" Class="ma-4">@_error</MudAlert>
}
```

### C.2 — Loading states con MudProgressCircular

En cada página, mientras cargan los datos:
```razor
@if (_cargando)
{
    <MudContainer Class="d-flex justify-center mt-8">
        <MudProgressCircular Indeterminate="true" Color="Color.Primary" Size="Size.Large" />
    </MudContainer>
}
```

### C.3 — Smoke tests manuales — checklist de campo

Documentá en `docs/08_calidad_y_pruebas/smoke-test-campo_v1.0.md`:

Escenario 1 — Flujo online completo:
  [ ] Abrir web → subir 5 fotos con GPS → 5 markers en mapa
  [ ] Click en marker → popup → editar descripción → guardar → confirmar
  [ ] /lista → 5 filas → filtrar por nombre → resultado correcto
  [ ] Eliminar 1 punto → confirmar → 4 en mapa

Escenario 2 — Flujo offline completo:
  [ ] Android en modo avión → abrir app → mapa muestra puntos locales
  [ ] Tomar 3 fotos → badge muestra "3"
  [ ] /sync → 3 operaciones Pending
  [ ] Desactivar modo avión → esperar 10s → badge vuelve a 0
  [ ] /sync → 3 operaciones Done
  [ ] Web → 3 nuevos markers visibles

Escenario 3 — Conflicto:
  [ ] Editar punto desde web mientras celular offline
  [ ] Editar el mismo punto desde celular offline
  [ ] Reconectar → sync → verificar que se resolvió sin crash
  [ ] /sync → ver registro del conflicto resuelto

---

## CHECKLIST DEMO FINAL — SPRINT 06 (Release Demo)

  [ ] Mapa fluido con 100+ puntos (probar cargando datos de prueba)
  [ ] Clustering visible cuando hay markers cercanos
  [ ] Paginación funciona en /lista con 100+ puntos
  [ ] Tag v1.0.0 → pipeline Release → APK en GitHub Releases
  [ ] APK descargable desde el enlace de GitHub Releases
  [ ] Instalar APK desde enlace → abre y funciona sin cable USB
  [ ] Smoke test Escenario 1 completo ✓
  [ ] Smoke test Escenario 2 completo ✓
  [ ] Smoke test Escenario 3 completo ✓
  [ ] dotnet test GeoFoto.Tests → todos verdes
  [ ] dotnet build GeoFoto.sln → 0 errores

Si todo pasa: "SPRINT 06 COMPLETO — GeoFoto v1.0 listo para producción."

---

## ENTREGABLE FINAL — ESTADO DEL SISTEMA v1.0

Al cerrar el Sprint 06, el sistema completo es:

  GeoFoto.Api     → API REST corriendo, SQL Server con datos reales
  GeoFoto.Web     → Blazor InteractiveServer con MudBlazor, mapa, gestión completa
  GeoFoto.Mobile  → APK Android instalable, offline-first, sync automático
  GeoFoto.Shared  → RCL con todas las páginas y componentes compartidos
  CI/CD           → GitHub Actions: build → test → APK en cada push a main
  Release         → APK publicada en GitHub Releases al tagear v*

Capacidades del sistema:
  ✓ Capturar fotos con GPS en campo sin internet
  ✓ Ver todos los puntos en mapa interactivo con clustering
  ✓ Sincronización automática al recuperar conexión
  ✓ Resolución de conflictos Last-Write-Wins
  ✓ Historial de operaciones de sync visible
  ✓ Performance fluida con 100+ puntos
  ✓ Distribución de APK sin infraestructura adicional
```
