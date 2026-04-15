using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Integration.Mobile;

/// <summary>
/// Tests de integración — flujo foto ↔ marker con SQLite real.
/// Verifica que la primera foto crea un marker, la segunda se asocia
/// al marker cercano, y que eliminar fotos no elimina el punto.
/// </summary>
public class FotoMarkerFlowTests : IAsyncLifetime, IDisposable
{
    private ILocalDbService _db = null!;
    private readonly string _dbPath;

    public FotoMarkerFlowTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"geofoto_flow_{Guid.NewGuid():N}.db3");
    }

    public async Task InitializeAsync()
    {
        _db = new LocalDbService(_dbPath);
        await _db.InitializeAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        try { if (File.Exists(_dbPath)) File.Delete(_dbPath); } catch { }
    }

    // ──────────────────────────────────────────
    // Primera foto sin marker cercano → crea marker nuevo
    // ──────────────────────────────────────────
    [Fact]
    public async Task PrimeraFoto_SinMarkerCercano_CreaMarkerNuevo()
    {
        // No markers in DB — FindPuntoCercano debe retornar null
        var cercano = await _db.FindPuntoCercanoAsync(-34.603722m, -58.381592m);
        cercano.Should().BeNull();

        // Crear nuevo punto (comportamiento del flujo)
        var punto = new LocalPunto
        {
            Latitud    = -34.603722m,
            Longitud   = -58.381592m,
            Nombre     = "Nuevo marker",
            SyncStatus = SyncStatusValues.PendingCreate
        };
        var puntoId = await _db.InsertPuntoAsync(punto);

        // Crear foto asociada al nuevo punto
        var foto = new LocalFoto
        {
            PuntoLocalId  = puntoId,
            NombreArchivo = "foto_001.jpg",
            RutaLocal     = "/tmp/noexiste_001.jpg",
            FechaTomada   = DateTime.UtcNow.ToString("O"),
            TamanoBytes   = 102400,
            SyncStatus    = SyncStatusValues.PendingCreate
        };
        var fotoId = await _db.InsertFotoAsync(foto);

        // Assert
        var puntos = await _db.GetPuntosAsync();
        puntos.Should().HaveCount(1, "debe crearse exactamente un marker nuevo");

        var fotoGuardada = await _db.GetFotoAsync(fotoId);
        fotoGuardada.Should().NotBeNull();
        fotoGuardada!.PuntoLocalId.Should().Be(puntoId);
    }

    // ──────────────────────────────────────────
    // Segunda foto con marker cercano → se asocia al marker existente
    // ──────────────────────────────────────────
    [Fact]
    public async Task SegundaFoto_MarkerCercano_AsociaAlMarkerExistente()
    {
        // Insertar marker existente
        var puntoExistente = new LocalPunto
        {
            Latitud    = -34.603722m,
            Longitud   = -58.381592m,
            Nombre     = "Marker existente",
            SyncStatus = SyncStatusValues.Local
        };
        var puntoExistenteId = await _db.InsertPuntoAsync(puntoExistente);

        // Foto tomada muy cerca (diff < 0.001 grados)
        var latFoto = -34.603700m;  // diff en lat = 0.000022 < 0.001
        var lngFoto = -58.381600m;  // diff en lng = 0.000008 < 0.001

        var cercano = await _db.FindPuntoCercanoAsync(latFoto, lngFoto, 0.001m);
        cercano.Should().NotBeNull("debe encontrar el marker a menos de 0.001 grados");
        cercano!.LocalId.Should().Be(puntoExistenteId);

        // Asociar foto al marker existente
        var foto = new LocalFoto
        {
            PuntoLocalId  = cercano.LocalId,
            NombreArchivo = "foto_002.jpg",
            RutaLocal     = "/tmp/noexiste_002.jpg",
            SyncStatus    = SyncStatusValues.PendingCreate
        };
        var fotoId = await _db.InsertFotoAsync(foto);

        // Assert: solo 1 punto, foto vinculada al marker existente
        var puntos = await _db.GetPuntosAsync();
        puntos.Should().HaveCount(1, "no debe crearse un segundo marker");

        var fotoGuardada = await _db.GetFotoAsync(fotoId);
        fotoGuardada!.PuntoLocalId.Should().Be(puntoExistenteId);
    }

    // ──────────────────────────────────────────
    // Foto lejos (fuera de radio) → crea marker nuevo
    // ──────────────────────────────────────────
    [Fact]
    public async Task FotoLejos_FueraDeRadio_CreaMarkerNuevo()
    {
        // Marker existente en Buenos Aires
        var puntoBA = new LocalPunto
        {
            Latitud    = -34.603722m,
            Longitud   = -58.381592m,
            Nombre     = "Buenos Aires",
            SyncStatus = SyncStatusValues.Local
        };
        await _db.InsertPuntoAsync(puntoBA);

        // Foto tomada en Córdoba (~3.18 grados de diferencia en lat → >> 0.001)
        var latCba = -31.4201m;
        var lngCba = -64.1888m;

        var cercano = await _db.FindPuntoCercanoAsync(latCba, lngCba, 0.001m);
        cercano.Should().BeNull("Córdoba está muy lejos del marker de Buenos Aires");

        // Crear marker nuevo para Córdoba
        var puntoCba = new LocalPunto
        {
            Latitud    = latCba,
            Longitud   = lngCba,
            Nombre     = "Córdoba",
            SyncStatus = SyncStatusValues.PendingCreate
        };
        var puntoCbaId = await _db.InsertPuntoAsync(puntoCba);

        var foto = new LocalFoto
        {
            PuntoLocalId  = puntoCbaId,
            NombreArchivo = "foto_cba.jpg",
            RutaLocal     = "/tmp/noexiste_cba.jpg",
            SyncStatus    = SyncStatusValues.PendingCreate
        };
        await _db.InsertFotoAsync(foto);

        var puntos = await _db.GetPuntosAsync();
        puntos.Should().HaveCount(2, "deben existir 2 markers distintos (BA y Córdoba)");
    }

    // ──────────────────────────────────────────
    // Foto nueva → SyncStatus debe ser PendingCreate
    // ──────────────────────────────────────────
    [Fact]
    public async Task FotoNueva_TieneSyncStatusPendingCreate()
    {
        var punto = new LocalPunto
        {
            Latitud    = -34.6m,
            Longitud   = -58.4m,
            SyncStatus = SyncStatusValues.Local
        };
        var puntoId = await _db.InsertPuntoAsync(punto);

        var foto = new LocalFoto
        {
            PuntoLocalId  = puntoId,
            NombreArchivo = "foto_sync.jpg",
            RutaLocal     = "/tmp/noexiste_sync.jpg",
            FechaTomada   = DateTime.UtcNow.ToString("O"),
            TamanoBytes   = 51200,
            SyncStatus    = SyncStatusValues.PendingCreate
        };
        var fotoId = await _db.InsertFotoAsync(foto);

        var stored = await _db.GetFotoAsync(fotoId);
        stored.Should().NotBeNull();
        stored!.SyncStatus.Should().Be(SyncStatusValues.PendingCreate);
        stored.TamanoBytes.Should().Be(51200);
        stored.FechaTomada.Should().NotBeNullOrWhiteSpace();
    }

    // ──────────────────────────────────────────
    // Eliminar foto no elimina el punto
    // ──────────────────────────────────────────
    [Fact]
    public async Task EliminarFoto_NoEliminaElPunto()
    {
        var punto = new LocalPunto
        {
            Latitud    = -34.6m,
            Longitud   = -58.4m,
            SyncStatus = SyncStatusValues.Local
        };
        var puntoId = await _db.InsertPuntoAsync(punto);

        var foto1 = new LocalFoto
        {
            PuntoLocalId  = puntoId,
            NombreArchivo = "foto_a.jpg",
            RutaLocal     = "/tmp/noexiste_a.jpg",
            SyncStatus    = SyncStatusValues.Local
        };
        var foto2 = new LocalFoto
        {
            PuntoLocalId  = puntoId,
            NombreArchivo = "foto_b.jpg",
            RutaLocal     = "/tmp/noexiste_b.jpg",
            SyncStatus    = SyncStatusValues.Local
        };
        var fotoId1 = await _db.InsertFotoAsync(foto1);
        await _db.InsertFotoAsync(foto2);

        // Eliminar solo foto1
        await _db.DeleteFotoAsync(fotoId1);

        // El punto debe seguir existiendo
        var puntoStored = await _db.GetPuntoAsync(puntoId);
        puntoStored.Should().NotBeNull("el punto no debe eliminarse al borrar una foto");

        // foto2 debe seguir existiendo
        var fotosRestantes = await _db.GetFotosByPuntoAsync(puntoId);
        fotosRestantes.Should().HaveCount(1);
        fotosRestantes[0].NombreArchivo.Should().Be("foto_b.jpg");
    }

    // ──────────────────────────────────────────
    // Múltiples fotos al mismo marker → todas asociadas al mismo punto
    // ──────────────────────────────────────────
    [Fact]
    public async Task MultiplesFotos_MismoMarker_TodasAsociadasAlMismoPunto()
    {
        var punto = new LocalPunto
        {
            Latitud    = -34.603722m,
            Longitud   = -58.381592m,
            SyncStatus = SyncStatusValues.Local
        };
        var puntoId = await _db.InsertPuntoAsync(punto);

        for (int i = 1; i <= 5; i++)
        {
            var foto = new LocalFoto
            {
                PuntoLocalId  = puntoId,
                NombreArchivo = $"foto_{i:D2}.jpg",
                RutaLocal     = $"/tmp/noexiste_{i:D2}.jpg",
                SyncStatus    = SyncStatusValues.PendingCreate
            };
            await _db.InsertFotoAsync(foto);
        }

        var fotos = await _db.GetFotosByPuntoAsync(puntoId);
        fotos.Should().HaveCount(5, "todas las fotos deben estar vinculadas al mismo punto");
        fotos.Should().OnlyContain(f => f.PuntoLocalId == puntoId);
    }
}
