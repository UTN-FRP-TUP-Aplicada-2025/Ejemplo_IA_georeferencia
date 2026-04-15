# Prompt para GitHub Copilot — Suite de Tests: Fotos, Markers y Sincronización

> Pegá este prompt en Copilot Chat con modo **Edits** (`@workspace`).
> Prerequisito: la solución compila (0 errores). Los 34 tests actuales pasan.
> API corriendo en localhost:5000. Mobile instalada en el dispositivo.

---

## PROMPT

```
Sos un QA engineer senior y desarrollador .NET 10. Tu tarea es crear una suite
de tests completa que cubra los flujos críticos que están fallando en GeoFoto:

  1. Primera foto crea un marker nuevo
  2. Segunda foto se asocia al marker existente
  3. Sincronización bidireccional Mobile ↔ API ↔ Web

El proyecto ya tiene 34 tests en GeoFoto.Tests (13.8% cobertura).
Tu objetivo es llevar la cobertura a ≥ 50% global y ≥ 80% en los flujos críticos,
SIN romper los tests existentes.

---

## FASE 0 — DESCUBRIR EL CÓDIGO REAL

Antes de escribir UN SOLO test, leé todo el código existente.
Esto es OBLIGATORIO — el proyecto pasó por múltiples sprints y fixes.

### 0.1 — Leer los tests actuales

```powershell
# Ver qué tests ya existen
Get-ChildItem -Recurse -Path "src\GeoFoto.Tests" -Filter "*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" } |
    ForEach-Object {
        Write-Host "`n=== $($_.Name) ==="
        Get-Content $_.FullName
    }
```

### 0.2 — Leer los servicios que vas a testear

```powershell
# LocalDbService completo (SQLite — CRÍTICO)
Get-ChildItem -Recurse -Filter "LocalDbService.cs","ILocalDbService.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } |
    ForEach-Object { Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName }

# Modelos SQLite (PuntoLocal, FotoLocal, SyncQueueItem)
Get-ChildItem -Recurse -Filter "PuntoLocal.cs","FotoLocal.cs","SyncQueue*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } |
    ForEach-Object { Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName }

# SyncService completo
Get-ChildItem -Recurse -Filter "*SyncService*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } |
    ForEach-Object { Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName }

# Handler de captura de foto (donde está el bug)
Get-ChildItem -Recurse -Filter "MobileLayout.razor","Mapa.razor" |
    ForEach-Object { Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName }

# Estrategia de upload (LocalUploadStrategy o equivalente)
Get-ChildItem -Recurse -Filter "*Upload*Strategy*.cs","*Upload*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } |
    ForEach-Object { Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName }

# FindPuntoCercano — lógica de asociación por radio
Get-ChildItem -Recurse -Include "*.cs" |
    Select-String -Pattern "FindPuntoCercano|PuntoCercano|CalcularDistancia" |
    ForEach-Object { Write-Host "$($_.Filename):$($_.LineNumber) → $($_.Line.Trim())" }
```

### 0.3 — Leer la API

```powershell
# Controladores
Get-ChildItem -Recurse -Filter "PuntosController.cs","FotosController.cs","SyncController.cs" |
    ForEach-Object { Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName }

# DbContext y modelos del servidor
Get-ChildItem -Recurse -Filter "*DbContext*.cs","GeoFotoContext*.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } |
    ForEach-Object { Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName }

# Program.cs de la API (para WebApplicationFactory)
Get-Content "src\GeoFoto.Api\Program.cs"
```

### 0.4 — Leer el cliente HTTP

```powershell
Get-ChildItem -Recurse -Filter "GeoFotoApiClient.cs","IGeoFotoApiClient.cs" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } |
    ForEach-Object { Write-Host "=== $($_.FullName) ==="; Get-Content $_.FullName }
```

### 0.5 — Verificar la API real

```powershell
# Endpoints disponibles
try {
    $swagger = (Invoke-WebRequest "http://localhost:5000/swagger/v1/swagger.json" -UseBasicParsing).Content | ConvertFrom-Json
    $swagger.paths.PSObject.Properties | ForEach-Object {
        $path = $_.Name
        $_.Value.PSObject.Properties | ForEach-Object { Write-Host "$($_.Name.ToUpper()) $path" }
    }
} catch { Write-Host "API no responde — lanzala con 01_start_api.bat" }
```

ANOTÁ TODO. Necesitás saber:
  - Qué métodos tiene ILocalDbService (InsertPuntoAsync, InsertFotoAsync, FindPuntoCercano, etc.)
  - Cómo se llaman los modelos reales (PuntoLocal, FotoLocal, SyncQueueItem)
  - Qué hace el handler de captura de foto paso a paso
  - Si FindPuntoCercano existe y cómo calcula la distancia
  - Qué endpoints tiene la API real
  - Cómo está configurado Program.cs (para WebApplicationFactory)
  - Qué tests ya existen (para NO duplicar)

Confirmá: "Fase 0 completa. Código real leído. Tests existentes identificados."

---

## REGLAS PARA LOS TESTS

1. Convención de nombres: `Clase_Metodo_Escenario` 
   Ejemplo: `LocalDbService_InsertFoto_PrimerFoto_CreaMarkerNuevo`
2. Framework: xUnit + Moq + FluentAssertions (ya están en el csproj)
3. SQLite: usar archivo temporal (no :memory: que puede tener limitaciones con sqlite-net-pcl)
4. API: usar WebApplicationFactory<Program> con BD in-memory
5. NO modificar los 34 tests existentes — solo AGREGAR nuevos
6. Cada test debe ser independiente (setup/teardown propio)
7. Cada test debe verificar UNA cosa (single assertion concept)
8. Los tests deben correr sin API ni dispositivo (unit tests + integration con mocks/in-memory)
9. Compilar y correr después de cada grupo de tests

---

## GRUPO 1 — TESTS DE BASE DE DATOS LOCAL (SQLite)

### Objetivo
Probar que LocalDbService maneja correctamente el flujo de fotos y markers.

### Tests a crear

```csharp
// src/GeoFoto.Tests/FotoMarkerFlowTests.cs
// Estos tests prueban el flujo EXACTO que falla en la app

public class FotoMarkerFlowTests : IAsyncLifetime
{
    private ILocalDbService _db;
    // Usar archivo temporal para la BD
    private string _dbPath;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"geofoto_test_{Guid.NewGuid()}.db3");
        _db = new LocalDbService(_dbPath);
        await _db.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        // Limpiar archivo temporal
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        return Task.CompletedTask;
    }

    // --- ESCENARIO 1: Primera foto CREA un marker nuevo ---

    [Fact]
    public async Task PrimeraFoto_SinMarkerCercano_CreaMarkerNuevo()
    {
        // DADO: no hay markers en la BD
        var puntosCantidadAntes = await _db.GetAllPuntosAsync();
        Assert.Empty(puntosCantidadAntes);

        // CUANDO: creo un punto con coordenadas y una foto
        var punto = new PuntoLocal
        {
            Latitud = -34.6037,
            Longitud = -58.3816,
            Nombre = "Punto Test 1",
            FechaCreacion = DateTime.UtcNow,
            SyncStatus = "PendingCreate"
        };
        await _db.InsertPuntoAsync(punto);

        var foto = new FotoLocal
        {
            PuntoLocalId = punto.LocalId,
            NombreArchivo = "test_foto_1.jpg",
            RutaLocal = "/fake/path/test_foto_1.jpg",
            FechaTomada = DateTime.UtcNow,
            TamanoBytes = 1024,
            SyncStatus = "PendingCreate"
        };
        await _db.InsertFotoAsync(foto);

        // ENTONCES: hay exactamente 1 punto y 1 foto
        var puntos = await _db.GetAllPuntosAsync();
        Assert.Single(puntos);
        Assert.Equal(-34.6037, puntos[0].Latitud, 4);

        var fotos = await _db.GetFotosByPuntoAsync(punto.LocalId);
        Assert.Single(fotos);
        Assert.Equal(punto.LocalId, fotos[0].PuntoLocalId);
    }

    // --- ESCENARIO 2: Segunda foto se ASOCIA al marker existente ---

    [Fact]
    public async Task SegundaFoto_ConMarkerCercano_SeAsociaAlMarker()
    {
        // DADO: ya existe un marker en (-34.6037, -58.3816)
        var punto = new PuntoLocal
        {
            Latitud = -34.6037,
            Longitud = -58.3816,
            Nombre = "Punto Existente",
            FechaCreacion = DateTime.UtcNow,
            SyncStatus = "PendingCreate"
        };
        await _db.InsertPuntoAsync(punto);

        var foto1 = new FotoLocal
        {
            PuntoLocalId = punto.LocalId,
            NombreArchivo = "foto_1.jpg",
            RutaLocal = "/fake/foto_1.jpg",
            TamanoBytes = 1024,
            SyncStatus = "PendingCreate"
        };
        await _db.InsertFotoAsync(foto1);

        // CUANDO: busco un punto cercano (dentro del radio) y asocio otra foto
        var puntoCercano = await _db.FindPuntoCercanoAsync(-34.6037, -58.3816);
        // NOTA: si FindPuntoCercanoAsync no existe con esa firma, usar la real

        Assert.NotNull(puntoCercano);
        Assert.Equal(punto.LocalId, puntoCercano.LocalId);

        var foto2 = new FotoLocal
        {
            PuntoLocalId = puntoCercano.LocalId, // ← ASOCIAR al existente, no crear nuevo
            NombreArchivo = "foto_2.jpg",
            RutaLocal = "/fake/foto_2.jpg",
            TamanoBytes = 2048,
            SyncStatus = "PendingCreate"
        };
        await _db.InsertFotoAsync(foto2);

        // ENTONCES: sigue habiendo 1 solo punto, pero ahora tiene 2 fotos
        var puntos = await _db.GetAllPuntosAsync();
        Assert.Single(puntos); // ← NO debe haber duplicado

        var fotos = await _db.GetFotosByPuntoAsync(punto.LocalId);
        Assert.Equal(2, fotos.Count);
    }

    // --- ESCENARIO 3: Foto LEJOS del marker existente crea marker NUEVO ---

    [Fact]
    public async Task TerceraFoto_FueraDelRadio_CreaMarkerNuevo()
    {
        // DADO: existe un marker en Buenos Aires
        var punto1 = new PuntoLocal
        {
            Latitud = -34.6037,
            Longitud = -58.3816,
            Nombre = "Buenos Aires",
            FechaCreacion = DateTime.UtcNow,
            SyncStatus = "PendingCreate"
        };
        await _db.InsertPuntoAsync(punto1);

        // CUANDO: busco un punto cercano desde Córdoba (600km lejos)
        var puntoCercano = await _db.FindPuntoCercanoAsync(-31.4201, -64.1888);

        // ENTONCES: no hay punto cercano
        Assert.Null(puntoCercano);

        // Debo crear un nuevo punto
        var punto2 = new PuntoLocal
        {
            Latitud = -31.4201,
            Longitud = -64.1888,
            Nombre = "Córdoba",
            FechaCreacion = DateTime.UtcNow,
            SyncStatus = "PendingCreate"
        };
        await _db.InsertPuntoAsync(punto2);

        var puntos = await _db.GetAllPuntosAsync();
        Assert.Equal(2, puntos.Count); // ← 2 markers distintos
    }

    // --- ESCENARIO 4: SyncQueue se genera correctamente ---

    [Fact]
    public async Task CrearPuntoConFoto_GeneraDosSyncQueueItems()
    {
        var punto = new PuntoLocal
        {
            Latitud = -34.6037, Longitud = -58.3816,
            Nombre = "Test", SyncStatus = "PendingCreate"
        };
        await _db.InsertPuntoAsync(punto);
        await _db.EnqueueAsync("Create", "Punto", punto.LocalId, "{}");

        var foto = new FotoLocal
        {
            PuntoLocalId = punto.LocalId,
            NombreArchivo = "test.jpg",
            RutaLocal = "/fake/test.jpg",
            TamanoBytes = 512,
            SyncStatus = "PendingCreate"
        };
        await _db.InsertFotoAsync(foto);
        await _db.EnqueueAsync("Create", "Foto", foto.LocalId, "{}");

        var pendientes = await _db.GetPendingOperationsAsync();
        Assert.Equal(2, pendientes.Count);

        // El punto debe estar PRIMERO (para sync en orden correcto)
        var primera = pendientes.OrderBy(p => p.CreatedAt).First();
        Assert.Equal("Punto", primera.EntityType);
    }

    // --- ESCENARIO 5: Eliminar foto no elimina el punto ---

    [Fact]
    public async Task EliminarFoto_NoEliminaElPunto()
    {
        var punto = new PuntoLocal
        {
            Latitud = -34.6037, Longitud = -58.3816,
            Nombre = "Punto con fotos", SyncStatus = "Synced"
        };
        await _db.InsertPuntoAsync(punto);

        var foto1 = new FotoLocal { PuntoLocalId = punto.LocalId, NombreArchivo = "a.jpg", RutaLocal = "/a.jpg", TamanoBytes = 100, SyncStatus = "Synced" };
        var foto2 = new FotoLocal { PuntoLocalId = punto.LocalId, NombreArchivo = "b.jpg", RutaLocal = "/b.jpg", TamanoBytes = 200, SyncStatus = "Synced" };
        await _db.InsertFotoAsync(foto1);
        await _db.InsertFotoAsync(foto2);

        // Eliminar UNA foto
        await _db.DeleteFotoLocalAsync(foto1.LocalId);

        // El punto debe seguir existiendo con 1 foto
        var puntoPost = await _db.GetPuntoByLocalIdAsync(punto.LocalId);
        Assert.NotNull(puntoPost);

        var fotosPost = await _db.GetFotosByPuntoAsync(punto.LocalId);
        Assert.Single(fotosPost);
        Assert.Equal("b.jpg", fotosPost[0].NombreArchivo);
    }

    // --- ESCENARIO 6: Agregar foto a punto con RemoteId ya asignado ---

    [Fact]
    public async Task AgregarFoto_APuntoSincronizado_MantieneSyncStatus()
    {
        var punto = new PuntoLocal
        {
            Latitud = -34.6037, Longitud = -58.3816,
            Nombre = "Synced", RemoteId = 42, SyncStatus = "Synced"
        };
        await _db.InsertPuntoAsync(punto);

        var fotoNueva = new FotoLocal
        {
            PuntoLocalId = punto.LocalId,
            NombreArchivo = "nueva.jpg",
            RutaLocal = "/nueva.jpg",
            TamanoBytes = 300,
            SyncStatus = "PendingCreate" // ← la foto es nueva, el punto no
        };
        await _db.InsertFotoAsync(fotoNueva);

        // El punto sigue Synced (no cambiar a PendingUpdate por agregar foto)
        var puntoPost = await _db.GetPuntoByLocalIdAsync(punto.LocalId);
        Assert.Equal("Synced", puntoPost.SyncStatus);

        // La foto es PendingCreate
        Assert.Equal("PendingCreate", fotoNueva.SyncStatus);
    }
}
```

IMPORTANTE: Adaptá los nombres de métodos (InsertPuntoAsync, FindPuntoCercanoAsync,
GetFotosByPuntoAsync, etc.) a los que REALMENTE existen en ILocalDbService.
Si un método no existe, ANOTALO — lo crearemos después.

### Compilar y correr

```powershell
dotnet test src\GeoFoto.Tests\GeoFoto.Tests.csproj --filter "FullyQualifiedName~FotoMarkerFlowTests" --verbosity normal
```

Todos deben pasar. Si alguno falla, ESE ES EL BUG — reportá qué método falla y por qué.

---

## GRUPO 2 — TESTS DE INTEGRACIÓN API (WebApplicationFactory)

### Objetivo
Probar que la API REST maneja correctamente fotos, puntos y la relación entre ambos.

### Tests a crear

```csharp
// src/GeoFoto.Tests/ApiIntegrationTests.cs

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Configurar para usar BD in-memory en lugar de SQL Server real
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remover DbContext registrado con SQL Server
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<GeoFotoContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Agregar DbContext con SQLite in-memory (o InMemory provider)
                services.AddDbContext<GeoFotoContext>(options =>
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));
            });
        }).CreateClient();
    }

    // --- CRUD DE PUNTOS ---

    [Fact]
    public async Task GetPuntos_SinDatos_RetornaListaVacia()
    {
        var response = await _client.GetAsync("/api/puntos");
        response.EnsureSuccessStatusCode();
        var puntos = await response.Content.ReadFromJsonAsync<List<PuntoDto>>();
        Assert.NotNull(puntos);
        Assert.Empty(puntos);
    }

    [Fact]
    public async Task PostPunto_CreaYRetornaPunto()
    {
        var punto = new { Latitud = -34.6037, Longitud = -58.3816, Nombre = "Test API" };
        var response = await _client.PostAsJsonAsync("/api/puntos", punto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<PuntoDto>();
        Assert.NotNull(created);
        Assert.Equal("Test API", created.Nombre);
    }

    [Fact]
    public async Task PutPunto_ActualizaNombreYDescripcion()
    {
        // Crear
        var punto = new { Latitud = -34.6037, Longitud = -58.3816, Nombre = "Original" };
        var createResp = await _client.PostAsJsonAsync("/api/puntos", punto);
        var created = await createResp.Content.ReadFromJsonAsync<PuntoDto>();

        // Actualizar
        var update = new { Nombre = "Modificado", Descripcion = "Desc nueva" };
        var updateResp = await _client.PutAsJsonAsync($"/api/puntos/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        // Verificar
        var getResp = await _client.GetFromJsonAsync<PuntoDto>($"/api/puntos/{created.Id}");
        Assert.Equal("Modificado", getResp.Nombre);
        Assert.Equal("Desc nueva", getResp.Descripcion);
    }

    [Fact]
    public async Task DeletePunto_EliminaPuntoYFotosAsociadas()
    {
        // Crear punto
        var punto = new { Latitud = -34.6037, Longitud = -58.3816, Nombre = "A borrar" };
        var createResp = await _client.PostAsJsonAsync("/api/puntos", punto);
        var created = await createResp.Content.ReadFromJsonAsync<PuntoDto>();

        // Eliminar
        var deleteResp = await _client.DeleteAsync($"/api/puntos/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // Verificar que no existe
        var getResp = await _client.GetAsync($"/api/puntos/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    // --- UPLOAD DE FOTOS ---

    [Fact]
    public async Task UploadFoto_SinGps_CreaFotoYPunto()
    {
        // Crear imagen fake (1x1 pixel JPEG)
        var jpegBytes = CreateMinimalJpeg();
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(jpegBytes), "file", "test_nogps.jpg");

        var response = await _client.PostAsync("/api/fotos/upload", content);
        // Puede ser 200 o 201 según la implementación
        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task UploadFoto_APuntoExistente_AsociaCorrectamente()
    {
        // Crear punto primero
        var punto = new { Latitud = -34.6037, Longitud = -58.3816, Nombre = "Con foto" };
        var createResp = await _client.PostAsJsonAsync("/api/puntos", punto);
        var created = await createResp.Content.ReadFromJsonAsync<PuntoDto>();

        // Subir foto asociada al punto
        var jpegBytes = CreateMinimalJpeg();
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(jpegBytes), "file", "test_foto.jpg");
        content.Add(new StringContent(created.Id.ToString()), "puntoId");

        var uploadResp = await _client.PostAsync("/api/fotos/upload", content);
        Assert.True(uploadResp.IsSuccessStatusCode);

        // Verificar que el punto tiene fotos
        var puntoDetalle = await _client.GetFromJsonAsync<PuntoDetalleDto>($"/api/puntos/{created.Id}");
        Assert.True(puntoDetalle.CantidadFotos >= 1);
    }

    [Fact]
    public async Task DeleteFoto_EliminaFotoPeroNoPunto()
    {
        // Crear punto + foto
        var punto = new { Latitud = -34.6037, Longitud = -58.3816, Nombre = "Punto persist" };
        var pResp = await _client.PostAsJsonAsync("/api/puntos", punto);
        var pCreated = await pResp.Content.ReadFromJsonAsync<PuntoDto>();

        var jpegBytes = CreateMinimalJpeg();
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(jpegBytes), "file", "borrable.jpg");
        content.Add(new StringContent(pCreated.Id.ToString()), "puntoId");
        var fResp = await _client.PostAsync("/api/fotos/upload", content);
        var fCreated = await fResp.Content.ReadFromJsonAsync<FotoUploadResultDto>();

        // Eliminar la foto
        var delResp = await _client.DeleteAsync($"/api/fotos/{fCreated.FotoId}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        // El punto debe seguir existiendo
        var puntoPost = await _client.GetAsync($"/api/puntos/{pCreated.Id}");
        Assert.Equal(HttpStatusCode.OK, puntoPost.StatusCode);
    }

    // Helper: JPEG mínimo válido (1x1 pixel)
    private static byte[] CreateMinimalJpeg()
    {
        // Smallest valid JPEG: FF D8 FF E0 (SOI + APP0) ... FF D9 (EOI)
        return new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
            0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
            0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
            0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20,
            0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
            0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
            0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
            0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x1F, 0x00, 0x00,
            0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0xFF, 0xC4, 0x00, 0xB5, 0x10, 0x00, 0x02, 0x01, 0x03,
            0x03, 0x02, 0x04, 0x03, 0x05, 0x05, 0x04, 0x04, 0x00, 0x00, 0x01, 0x7D,
            0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0x7B, 0x40,
            0x1B, 0xFF, 0xD9
        };
    }
}
```

IMPORTANTE: Adaptá los DTOs (PuntoDto, PuntoDetalleDto, FotoUploadResultDto)
a los nombres reales que encontraste en la Fase 0. Si Program.cs no tiene
el builder accesible para WebApplicationFactory, ajustá según la estructura real.

### Compilar y correr

```powershell
dotnet test src\GeoFoto.Tests\GeoFoto.Tests.csproj --filter "FullyQualifiedName~ApiIntegrationTests" --verbosity normal
```

---

## GRUPO 3 — TESTS DE SINCRONIZACIÓN

### Objetivo
Probar que SyncService sincroniza correctamente en ambas direcciones
y resuelve conflictos.

### Tests a crear

```csharp
// src/GeoFoto.Tests/SyncFlowTests.cs

public class SyncFlowTests
{
    private readonly Mock<IGeoFotoApiClient> _apiMock = new();
    private readonly Mock<IConnectivityService> _connMock = new();
    private ILocalDbService _db;
    private string _dbPath;

    public SyncFlowTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"sync_test_{Guid.NewGuid()}.db3");
        _db = new LocalDbService(_dbPath);
        _db.InitializeAsync().GetAwaiter().GetResult();
        _connMock.Setup(c => c.IsConnected).Returns(true);
    }

    // --- PUSH: Mobile → API ---

    [Fact]
    public async Task Push_PuntoNuevo_EnviaPostYActualizaRemoteId()
    {
        // DADO: un punto pendiente de crear
        var punto = new PuntoLocal
        {
            Latitud = -34.6037, Longitud = -58.3816,
            Nombre = "Nuevo", SyncStatus = "PendingCreate"
        };
        await _db.InsertPuntoAsync(punto);
        await _db.EnqueueAsync("Create", "Punto", punto.LocalId, "{}");

        // MOCK: la API retorna éxito con RemoteId = 99
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchResultDto { Results = new[] {
                new SyncResultItemDto { LocalId = punto.LocalId, Success = true, RemoteId = 99 }
            }});

        // CUANDO: ejecuto sync
        var syncService = CreateSyncService();
        await syncService.SyncNowAsync();

        // ENTONCES: el punto tiene RemoteId = 99 y SyncStatus = Synced
        var puntoPost = await _db.GetPuntoByLocalIdAsync(punto.LocalId);
        Assert.Equal(99, puntoPost.RemoteId);
        Assert.Equal("Synced", puntoPost.SyncStatus);
    }

    [Fact]
    public async Task Push_FotaDePuntoNoSincronizado_EsperaAlProximoCiclo()
    {
        // DADO: punto PendingCreate (sin RemoteId) y foto PendingCreate
        var punto = new PuntoLocal
        {
            Latitud = -34.6037, Longitud = -58.3816,
            Nombre = "Sin sync", SyncStatus = "PendingCreate"
            // RemoteId = null (no sincronizado)
        };
        await _db.InsertPuntoAsync(punto);

        var foto = new FotoLocal
        {
            PuntoLocalId = punto.LocalId,
            NombreArchivo = "pendiente.jpg",
            RutaLocal = "/fake/pendiente.jpg",
            TamanoBytes = 100,
            SyncStatus = "PendingCreate"
        };
        await _db.InsertFotoAsync(foto);
        await _db.EnqueueAsync("Create", "Foto", foto.LocalId, "{}");

        // CUANDO: intento sincronizar la foto
        // ENTONCES: no debe fallar, debe dejar la operación para después
        // (porque el punto padre no tiene RemoteId todavía)
    }

    [Fact]
    public async Task Push_EliminarFoto_EnviaDeleteYLimpiaLocal()
    {
        // DADO: foto sincronizada con RemoteId
        var punto = new PuntoLocal
        {
            Latitud = -34.6037, Longitud = -58.3816,
            Nombre = "Con foto", RemoteId = 10, SyncStatus = "Synced"
        };
        await _db.InsertPuntoAsync(punto);

        var foto = new FotoLocal
        {
            PuntoLocalId = punto.LocalId, RemoteId = 55,
            NombreArchivo = "borrar.jpg", RutaLocal = "/fake/borrar.jpg",
            TamanoBytes = 100, SyncStatus = "PendingDelete"
        };
        await _db.InsertFotoAsync(foto);
        await _db.EnqueueAsync("Delete", "Foto", foto.LocalId, null);

        // MOCK: DELETE exitoso
        _apiMock.Setup(a => a.DeleteFotoAsync(55, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // CUANDO: sync
        var syncService = CreateSyncService();
        await syncService.SyncNowAsync();

        // ENTONCES: foto eliminada localmente
        var fotoPost = await _db.GetFotoByLocalIdAsync(foto.LocalId);
        Assert.Null(fotoPost);
    }

    // --- PULL: API → Mobile ---

    [Fact]
    public async Task Pull_PuntoNuevoEnServidor_SeInsertaLocal()
    {
        // MOCK: delta retorna un punto nuevo del servidor
        var delta = new DeltaResponse
        {
            Puntos = new List<PuntoDeltaDto>
            {
                new() { Id = 100, Latitud = -31.4201, Longitud = -64.1888,
                        Nombre = "Desde servidor", UpdatedAt = DateTime.UtcNow }
            },
            Fotos = new List<FotoDeltaDto>(),
            Timestamp = DateTime.UtcNow
        };
        _apiMock.Setup(a => a.GetDeltaAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(delta);

        // CUANDO: pull
        var syncService = CreateSyncService();
        await syncService.SyncNowAsync();

        // ENTONCES: punto existe localmente con RemoteId = 100
        var local = await _db.GetPuntoByRemoteIdAsync(100);
        Assert.NotNull(local);
        Assert.Equal("Desde servidor", local.Nombre);
        Assert.Equal("Synced", local.SyncStatus);
    }

    // --- CONFLICTOS ---

    [Fact]
    public async Task Conflicto_LWW_ServidorMasReciente_ActualizaLocal()
    {
        // DADO: punto local editado hace 1 hora
        var punto = new PuntoLocal
        {
            Latitud = -34.6037, Longitud = -58.3816,
            Nombre = "Local viejo", RemoteId = 50,
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            SyncStatus = "PendingUpdate"
        };
        await _db.InsertPuntoAsync(punto);

        // MOCK: servidor tiene versión más reciente (hace 5 min)
        var delta = new DeltaResponse
        {
            Puntos = new List<PuntoDeltaDto>
            {
                new() { Id = 50, Latitud = -34.6037, Longitud = -58.3816,
                        Nombre = "Server reciente",
                        UpdatedAt = DateTime.UtcNow.AddMinutes(-5) }
            },
            Fotos = new List<FotoDeltaDto>(),
            Timestamp = DateTime.UtcNow
        };
        _apiMock.Setup(a => a.GetDeltaAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(delta);

        // CUANDO: pull con LWW
        var syncService = CreateSyncService();
        await syncService.SyncNowAsync();

        // ENTONCES: gana el servidor (más reciente)
        var local = await _db.GetPuntoByRemoteIdAsync(50);
        Assert.Equal("Server reciente", local.Nombre);
    }

    [Fact]
    public async Task Conflicto_LWW_LocalMasReciente_NoSobreescribe()
    {
        // DADO: punto local editado hace 5 min (más reciente)
        var punto = new PuntoLocal
        {
            Latitud = -34.6037, Longitud = -58.3816,
            Nombre = "Local reciente", RemoteId = 60,
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5),
            SyncStatus = "PendingUpdate"
        };
        await _db.InsertPuntoAsync(punto);

        // MOCK: servidor tiene versión vieja (hace 1 hora)
        var delta = new DeltaResponse
        {
            Puntos = new List<PuntoDeltaDto>
            {
                new() { Id = 60, Latitud = -34.6037, Longitud = -58.3816,
                        Nombre = "Server viejo",
                        UpdatedAt = DateTime.UtcNow.AddHours(-1) }
            },
            Fotos = new List<FotoDeltaDto>(),
            Timestamp = DateTime.UtcNow
        };
        _apiMock.Setup(a => a.GetDeltaAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(delta);

        // CUANDO: pull con LWW
        var syncService = CreateSyncService();
        await syncService.SyncNowAsync();

        // ENTONCES: gana el local (más reciente), no se sobreescribe
        var local = await _db.GetPuntoByLocalIdAsync(punto.LocalId);
        Assert.Equal("Local reciente", local.Nombre);
    }

    [Fact]
    public async Task Push_3Fallos_MarcaFailed()
    {
        var punto = new PuntoLocal
        {
            Latitud = -34.6037, Longitud = -58.3816,
            Nombre = "Fallará", SyncStatus = "PendingCreate"
        };
        await _db.InsertPuntoAsync(punto);
        await _db.EnqueueAsync("Create", "Punto", punto.LocalId, "{}");

        // MOCK: API siempre falla
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Server error"));

        // CUANDO: sync 3 veces
        var syncService = CreateSyncService();
        for (int i = 0; i < 3; i++)
        {
            try { await syncService.SyncNowAsync(); } catch { }
        }

        // ENTONCES: operación marcada como Failed
        var ops = await _db.GetPendingOperationsAsync();
        // Verificar que Attempts >= 3 o Status == Failed
    }

    [Fact]
    public async Task Push_SinConexion_NoIntentaSync()
    {
        _connMock.Setup(c => c.IsConnected).Returns(false);

        var punto = new PuntoLocal
        {
            Latitud = -34.6037, Longitud = -58.3816,
            Nombre = "Offline", SyncStatus = "PendingCreate"
        };
        await _db.InsertPuntoAsync(punto);
        await _db.EnqueueAsync("Create", "Punto", punto.LocalId, "{}");

        var syncService = CreateSyncService();
        await syncService.SyncNowAsync();

        // La API nunca fue llamada
        _apiMock.Verify(
            a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Helper: crear SyncService con los mocks y la BD real
    private ISyncService CreateSyncService()
    {
        // ADAPTÁ ESTO al constructor real del SyncService
        // Puede necesitar ILogger, IAppConfigService, etc.
        return new SyncService(_db, _apiMock.Object, _connMock.Object,
            Mock.Of<ILogger<SyncService>>());
    }
}
```

IMPORTANTE: Adaptá TODOS los tipos (SyncOperationDto, BatchResultDto,
DeltaResponse, PuntoDeltaDto, FotoDeltaDto) a los nombres reales.
Si el SyncService no usa SyncBatchAsync sino un flujo operación por operación,
adaptá los mocks a la interfaz real.

### Compilar y correr

```powershell
dotnet test src\GeoFoto.Tests\GeoFoto.Tests.csproj --filter "FullyQualifiedName~SyncFlowTests" --verbosity normal
```

---

## GRUPO 4 — TEST DE INTEGRACIÓN END-TO-END MOBILE (ADB)

### Objetivo
Verificar en el dispositivo real que tomar foto crea marker y que
la segunda foto se asocia correctamente.

Este NO es un test xUnit — es un script PowerShell de verificación.

```powershell
# src/GeoFoto.Tests/Scripts/test_foto_marker_e2e.ps1

param(
    [string]$ADB = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe",
    [string]$PKG = "com.companyname.geofoto.mobile"
)

Write-Host "=== TEST E2E: Foto → Marker → Segunda Foto ==="
Write-Host ""

# 1. Verificar que la API responde
Write-Host "[1/7] Verificando API..."
$puntosAntes = 0
try {
    $resp = Invoke-WebRequest "http://localhost:5000/api/puntos" -UseBasicParsing -TimeoutSec 5
    $puntosAntes = ($resp.Content | ConvertFrom-Json).Count
    Write-Host "  OK — $puntosAntes puntos existentes"
} catch {
    Write-Host "  ERROR — API no responde. Ejecutá 01_start_api.bat primero."
    exit 1
}

# 2. Verificar que el dispositivo está conectado
Write-Host "[2/7] Verificando dispositivo..."
$dev = (& $ADB devices | Select-String "device$" | Select-Object -First 1) -replace '\s.*',''
if (-not $dev) { Write-Host "  ERROR — Dispositivo no encontrado"; exit 1 }
Write-Host "  OK — Dispositivo: $dev"

# 3. Configurar tunnel y lanzar app
Write-Host "[3/7] Configurando tunnel y lanzando app..."
& $ADB -s $dev reverse tcp:5000 tcp:5000
& $ADB -s $dev shell monkey -p $PKG -c android.intent.category.LAUNCHER 1 2>$null
Start-Sleep 8

# 4. Contar puntos en SQLite local ANTES
Write-Host "[4/7] Estado SQLite local ANTES de la foto..."
& $ADB -s $dev shell "run-as $PKG find files/ -name '*.db3' 2>/dev/null"
# Extraer count si es posible

# 5. Tomar primera foto
Write-Host "[5/7] Tomando PRIMERA foto (debe crear marker nuevo)..."
Write-Host "  [MANUAL] Tocá el botón de cámara, sacá una foto, confirmá."
Write-Host "  Presioná Enter cuando hayas tomado la foto..."
Read-Host

Start-Sleep 5

# Verificar que se creó un punto nuevo en la API (después del sync)
$puntosDesp1 = 0
try {
    # Esperar sync (hasta 30 segundos)
    for ($i = 0; $i -lt 6; $i++) {
        $resp = Invoke-WebRequest "http://localhost:5000/api/puntos" -UseBasicParsing -TimeoutSec 5
        $puntosDesp1 = ($resp.Content | ConvertFrom-Json).Count
        if ($puntosDesp1 -gt $puntosAntes) { break }
        Write-Host "  Esperando sync... ($i/6)"
        Start-Sleep 5
    }
} catch {}

if ($puntosDesp1 -gt $puntosAntes) {
    Write-Host "  ✅ PASO — Marker creado ($puntosAntes → $puntosDesp1 puntos)"
} else {
    Write-Host "  ❌ FALLO — No se creó marker nuevo ($puntosAntes → $puntosDesp1)"
    Write-Host "  Capturando logcat..."
    & $ADB -s $dev logcat -d | Select-String "\[GeoFoto\]" | Select-Object -Last 20
}

# 6. Tomar segunda foto (debe asociarse al marker existente)
Write-Host "[6/7] Tomando SEGUNDA foto (debe asociarse al marker, NO crear uno nuevo)..."
Write-Host "  [MANUAL] SIN moverte, tocá cámara de nuevo, sacá otra foto."
Write-Host "  Presioná Enter cuando hayas tomado la foto..."
Read-Host

Start-Sleep 5

$puntosDesp2 = 0
try {
    for ($i = 0; $i -lt 6; $i++) {
        $resp = Invoke-WebRequest "http://localhost:5000/api/puntos" -UseBasicParsing -TimeoutSec 5
        $puntosDesp2 = ($resp.Content | ConvertFrom-Json).Count
        Start-Sleep 5
    }
} catch {}

if ($puntosDesp2 -eq $puntosDesp1) {
    # Verificar que el marker tiene 2 fotos
    $ultimoPunto = ($resp.Content | ConvertFrom-Json) | Sort-Object -Property id -Descending | Select-Object -First 1
    try {
        $detalle = (Invoke-WebRequest "http://localhost:5000/api/puntos/$($ultimoPunto.id)" -UseBasicParsing).Content | ConvertFrom-Json
        $cantFotos = if ($detalle.fotos) { $detalle.fotos.Count } else { $detalle.cantidadFotos }
        if ($cantFotos -ge 2) {
            Write-Host "  ✅ PASO — Foto asociada al marker existente ($cantFotos fotos)"
        } else {
            Write-Host "  ⚠️ WARN — Mismo count de puntos pero solo $cantFotos foto(s)"
        }
    } catch {
        Write-Host "  ⚠️ WARN — No se pudo verificar cantidad de fotos"
    }
} elseif ($puntosDesp2 -gt $puntosDesp1) {
    Write-Host "  ❌ FALLO — Se creó un marker NUEVO en vez de asociar ($puntosDesp1 → $puntosDesp2)"
} else {
    Write-Host "  ❌ FALLO — No se registró la segunda foto"
}

# 7. Resumen
Write-Host ""
Write-Host "=== RESUMEN ==="
Write-Host "  Puntos antes:         $puntosAntes"
Write-Host "  Puntos post-foto-1:   $puntosDesp1"
Write-Host "  Puntos post-foto-2:   $puntosDesp2"
Write-Host ""
```

---

## VERIFICACIÓN FINAL

```powershell
# Correr TODA la suite de tests
dotnet test src\GeoFoto.Tests\GeoFoto.Tests.csproj --verbosity normal --collect:"XPlat Code Coverage"

# Debe mostrar:
#   - Los 34 tests originales siguen pasando
#   - Los nuevos tests de FotoMarkerFlowTests pasan
#   - Los nuevos tests de ApiIntegrationTests pasan
#   - Los nuevos tests de SyncFlowTests pasan
#   - Cobertura ≥ 40% (subió desde 13.8%)
```

### Checklist final

```
GRUPO 1 — SQLite / Flujo de fotos y markers:
  [ ] Primera foto sin marker cercano → crea marker nuevo
  [ ] Segunda foto con marker cercano → se asocia al existente
  [ ] Foto fuera del radio → crea marker nuevo
  [ ] SyncQueue genera items en orden correcto
  [ ] Eliminar foto no elimina el punto
  [ ] Agregar foto a punto sincronizado mantiene SyncStatus

GRUPO 2 — API REST:
  [ ] GET /api/puntos retorna lista
  [ ] POST /api/puntos crea punto
  [ ] PUT /api/puntos/{id} actualiza
  [ ] DELETE /api/puntos/{id} elimina en cascada
  [ ] POST /api/fotos/upload sin GPS funciona
  [ ] POST /api/fotos/upload con puntoId asocia correctamente
  [ ] DELETE /api/fotos/{id} no elimina el punto padre

GRUPO 3 — Sincronización:
  [ ] Push: punto nuevo → API → RemoteId asignado
  [ ] Push: foto de punto no sincronizado → espera sin fallar
  [ ] Push: eliminar foto → DELETE en API + limpieza local
  [ ] Pull: punto nuevo del servidor → insertado localmente
  [ ] Conflicto LWW: servidor más reciente gana
  [ ] Conflicto LWW: local más reciente no se sobreescribe
  [ ] 3 fallos consecutivos → operación marcada Failed
  [ ] Sin conexión → no intenta sync

GRUPO 4 — E2E Mobile:
  [ ] Primera foto real → marker aparece en mapa
  [ ] Segunda foto desde misma posición → se asocia al marker

COBERTURA:
  [ ] 34 tests originales siguen verdes
  [ ] Nuevos tests > 20 tests adicionales
  [ ] Cobertura subió de 13.8% a ≥ 40%
  [ ] dotnet build GeoFoto.sln → 0 errores

Si todo pasa: "SUITE DE TESTS COMPLETA — flujos de fotos, markers y sync verificados."
```
```
