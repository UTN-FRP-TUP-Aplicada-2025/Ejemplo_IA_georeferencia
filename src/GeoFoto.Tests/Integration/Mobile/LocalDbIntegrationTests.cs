using FluentAssertions;
using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Integration.Mobile;

/// <summary>
/// BLOQUE 9 — Tests de integración SQLite (DELTA, LocalDbService)
/// TEST-060 a TEST-063
/// Usan SQLite con archivo temporal para alta fidelidad con el SQLite real.
/// </summary>
public class LocalDbIntegrationTests : IAsyncLifetime, IDisposable
{
    private ILocalDbService _db = null!;
    private readonly string _dbPath;

    public LocalDbIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"geo_integ_{Guid.NewGuid():N}.db3");
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
    // TEST-060: CrearPunto → LocalId > 0, SyncStatus = Local (o PendingCreate), RemoteId = null
    // ──────────────────────────────────────────
    [Fact]
    public async Task CrearPunto_GeneraLocalIdYSyncStatusLocal()
    {
        // ACT
        var punto = new LocalPunto
        {
            Latitud    = -34.60m,
            Longitud   = -58.38m,
            Nombre     = "Test integración",
            SyncStatus = SyncStatusValues.PendingCreate
        };
        var localId = await _db.InsertPuntoAsync(punto);
        var stored  = await _db.GetPuntoAsync(localId);

        // ASSERT
        localId.Should().BeGreaterThan(0);
        stored.Should().NotBeNull();
        stored!.RemoteId.Should().BeNull("un punto recién creado no tiene RemoteId del servidor");
        stored.SyncStatus.Should().Be(SyncStatusValues.PendingCreate);
        stored.Nombre.Should().Be("Test integración");
    }

    // ──────────────────────────────────────────
    // TEST-061: CrearPunto → encola operación en SyncQueue
    // ──────────────────────────────────────────
    [Fact]
    public async Task CrearPunto_EncolaSyncQueueConPayload()
    {
        // ARRANGE
        var punto = new LocalPunto
        {
            Latitud    = -34.61m,
            Longitud   = -58.39m,
            SyncStatus = SyncStatusValues.PendingCreate
        };
        var localId = await _db.InsertPuntoAsync(punto);
        var payload = System.Text.Json.JsonSerializer.Serialize(punto);

        // ACT
        await _db.EnqueueAsync("Create", "Punto", localId, payload);
        var queue = await _db.GetPendingOperationsAsync();

        // ASSERT
        queue.Should().NotBeEmpty();
        var item = queue.First(q => q.LocalId == localId && q.EntityType == "Punto");
        item.OperationType.Should().Be("Create");
        item.Status.Should().Be(SyncQueueStatus.Pending);
        item.Payload.Should().NotBeNullOrWhiteSpace();
        item.Payload.Should().Contain(localId.ToString());
    }

    // ──────────────────────────────────────────
    // TEST-062: EliminarPunto → elimina cascada de Fotos_Local
    // ──────────────────────────────────────────
    [Fact]
    public async Task EliminarPunto_EliminaCascadaFotosLocales()
    {
        // ARRANGE — crear punto con 2 fotos (en archivos temporales)
        var punto = new LocalPunto
        {
            Latitud = -34.62m, Longitud = -58.40m,
            SyncStatus = SyncStatusValues.Local
        };
        var localId = await _db.InsertPuntoAsync(punto);

        // Crear archivos físicos temporales para que el delete los limpie
        var tmpFile1 = Path.Combine(Path.GetTempPath(), $"foto_test_{Guid.NewGuid():N}.jpg");
        var tmpFile2 = Path.Combine(Path.GetTempPath(), $"foto_test_{Guid.NewGuid():N}.jpg");
        await File.WriteAllBytesAsync(tmpFile1, new byte[100]);
        await File.WriteAllBytesAsync(tmpFile2, new byte[100]);

        await _db.InsertFotoAsync(new LocalFoto
        {
            PuntoLocalId  = localId,
            NombreArchivo = "f1.jpg",
            RutaLocal     = tmpFile1,
            SyncStatus    = SyncStatusValues.Local
        });
        await _db.InsertFotoAsync(new LocalFoto
        {
            PuntoLocalId  = localId,
            NombreArchivo = "f2.jpg",
            RutaLocal     = tmpFile2,
            SyncStatus    = SyncStatusValues.Local
        });

        var fotosAntes = await _db.GetFotosByPuntoAsync(localId);
        fotosAntes.Should().HaveCount(2);

        // ACT
        await _db.DeletePuntoAsync(localId);

        // ASSERT — punto eliminado
        var puntoEliminado = await _db.GetPuntoAsync(localId);
        puntoEliminado.Should().BeNull();

        // Fotos eliminadas en cascada
        var fotosDespues = await _db.GetFotosByPuntoAsync(localId);
        fotosDespues.Should().BeEmpty("las fotos deben eliminarse en cascada con el punto");

        // Archivos físicos eliminados
        File.Exists(tmpFile1).Should().BeFalse("el archivo físico debe eliminarse");
        File.Exists(tmpFile2).Should().BeFalse("el archivo físico debe eliminarse");
    }

    // ──────────────────────────────────────────
    // TEST-063: ActualizarPunto → SyncStatus cambia a PendingUpdate
    // ──────────────────────────────────────────
    [Fact]
    public async Task ActualizarPunto_CambiaSyncStatusAPendingUpdate()
    {
        // ARRANGE — punto sincronizado
        var punto = new LocalPunto
        {
            Latitud    = -34.63m,
            Longitud   = -58.41m,
            Nombre     = "Original",
            RemoteId   = 77,
            SyncStatus = SyncStatusValues.Synced
        };
        var localId = await _db.InsertPuntoAsync(punto);
        var stored  = await _db.GetPuntoAsync(localId);

        // ACT — actualizar nombre y cambiar status
        stored!.Nombre     = "Actualizado";
        stored.SyncStatus  = SyncStatusValues.PendingUpdate;
        await _db.UpdatePuntoAsync(stored);

        // Encolar operación de update
        var payload = System.Text.Json.JsonSerializer.Serialize(stored);
        await _db.EnqueueAsync("Update", "Punto", localId, payload);

        // ASSERT
        var updated = await _db.GetPuntoAsync(localId);
        updated!.Nombre.Should().Be("Actualizado");
        updated.SyncStatus.Should().Be(SyncStatusValues.PendingUpdate);

        var queue = await _db.GetPendingOperationsAsync();
        queue.Should().Contain(q =>
            q.LocalId == localId &&
            q.OperationType == "Update" &&
            q.EntityType == "Punto");
    }
}
