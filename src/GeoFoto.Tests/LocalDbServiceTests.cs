using FluentAssertions;
using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests;

public class LocalDbServiceTests : IAsyncLifetime, IDisposable
{
    private ILocalDbService _db = null!;
    private readonly string _dbPath;

    public LocalDbServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"geofoto_test_{Guid.NewGuid():N}.db3");
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

    [Fact]
    public async Task InsertPunto_DebeAsignarLocalId()
    {
        var punto = new LocalPunto
        {
            Latitud = -34.603722m,
            Longitud = -58.381592m,
            Nombre = "Test",
            SyncStatus = SyncStatusValues.Local
        };

        var localId = await _db.InsertPuntoAsync(punto);

        localId.Should().BeGreaterThan(0);
        var stored = await _db.GetPuntoAsync(localId);
        stored.Should().NotBeNull();
        stored!.Nombre.Should().Be("Test");
    }

    [Fact]
    public async Task FindPuntoCercano_ConTolerancia_DebeEncontrar()
    {
        var punto = new LocalPunto
        {
            Latitud = -34.603722m,
            Longitud = -58.381592m,
            Nombre = "Cercano",
            SyncStatus = SyncStatusValues.Local
        };
        await _db.InsertPuntoAsync(punto);

        var encontrado = await _db.FindPuntoCercanoAsync(-34.6037m, -58.3816m, 0.001m);

        encontrado.Should().NotBeNull();
        encontrado!.Nombre.Should().Be("Cercano");
    }

    [Fact]
    public async Task FindPuntoCercano_FueraDeTolerancia_RetornaNull()
    {
        var punto = new LocalPunto
        {
            Latitud = -34.603722m,
            Longitud = -58.381592m,
            SyncStatus = SyncStatusValues.Local
        };
        await _db.InsertPuntoAsync(punto);

        var encontrado = await _db.FindPuntoCercanoAsync(-35.0m, -59.0m, 0.001m);

        encontrado.Should().BeNull();
    }

    [Fact]
    public async Task GetPendingCount_DebeContarSoloPending()
    {
        await _db.EnqueueAsync("Create", "Punto", 1, "{}");
        await _db.EnqueueAsync("Create", "Punto", 2, "{}");
        await _db.EnqueueAsync("Create", "Foto", 3, "{}");

        var ops = await _db.GetPendingOperationsAsync();
        await _db.MarkDoneAsync(ops[0].Id);

        var count = await _db.GetPendingCountAsync();

        count.Should().Be(2);
    }

    [Fact]
    public async Task MarkDone_DebeActualizarStatus()
    {
        await _db.EnqueueAsync("Create", "Punto", 1, "{}");
        var ops = await _db.GetPendingOperationsAsync();
        var item = ops.First();

        await _db.MarkDoneAsync(item.Id, 99);

        var allOps = await _db.GetPendingOperationsAsync();
        allOps.First(o => o.Id == item.Id).Status.Should().Be(SyncQueueStatus.Done);
    }

    [Fact]
    public async Task MarkFailed_DebeGuardarMensajeError()
    {
        await _db.EnqueueAsync("Create", "Punto", 1, "{}");
        var ops = await _db.GetPendingOperationsAsync();
        var item = ops.First();

        await _db.MarkFailedAsync(item.Id, "Error de red");

        var allOps = await _db.GetPendingOperationsAsync();
        var updated = allOps.First(o => o.Id == item.Id);
        updated.Status.Should().Be(SyncQueueStatus.Failed);
        updated.ErrorMessage.Should().Be("Error de red");
    }

    [Fact]
    public async Task IncrementAttempts_DebeIncrementarContador()
    {
        await _db.EnqueueAsync("Create", "Punto", 1, "{}");
        var ops = await _db.GetPendingOperationsAsync();
        var item = ops.First();

        await _db.IncrementAttemptsAsync(item.Id);
        await _db.IncrementAttemptsAsync(item.Id);

        var allOps = await _db.GetPendingOperationsAsync();
        allOps.First(o => o.Id == item.Id).Attempts.Should().Be(2);
    }

    [Fact]
    public async Task Enqueue_DebeCrearItemConStatusPending()
    {
        await _db.EnqueueAsync("Update", "Foto", 5, "{\"id\":5}");

        var ops = await _db.GetPendingOperationsAsync();

        ops.Should().ContainSingle();
        var item = ops.First();
        item.Status.Should().Be(SyncQueueStatus.Pending);
        item.OperationType.Should().Be("Update");
        item.EntityType.Should().Be("Foto");
        item.LocalId.Should().Be(5);
        item.Payload.Should().Be("{\"id\":5}");
    }

    [Fact]
    public async Task DeleteFoto_DebeEliminarSoloEsaFoto()
    {
        var punto = new LocalPunto
        {
            Latitud = -34.6m,
            Longitud = -58.4m,
            SyncStatus = SyncStatusValues.Local
        };
        var puntoId = await _db.InsertPuntoAsync(punto);

        var foto1 = new LocalFoto
        {
            PuntoLocalId = puntoId,
            NombreArchivo = "foto1.jpg",
            RutaLocal = "/nonexistent/foto1.jpg",
            SyncStatus = SyncStatusValues.Local
        };
        var foto2 = new LocalFoto
        {
            PuntoLocalId = puntoId,
            NombreArchivo = "foto2.jpg",
            RutaLocal = "/nonexistent/foto2.jpg",
            SyncStatus = SyncStatusValues.Local
        };
        var fotoId1 = await _db.InsertFotoAsync(foto1);
        var fotoId2 = await _db.InsertFotoAsync(foto2);

        await _db.DeleteFotoAsync(fotoId1);

        var fotos = await _db.GetFotosByPuntoAsync(puntoId);
        fotos.Should().ContainSingle();
        fotos.First().NombreArchivo.Should().Be("foto2.jpg");
    }

    [Fact]
    public async Task FindByRemoteIdAsync_DebeEncontrarPorRemoteId()
    {
        var punto = new LocalPunto
        {
            RemoteId = 42,
            Latitud = -34.6m,
            Longitud = -58.4m,
            SyncStatus = SyncStatusValues.Synced
        };
        await _db.InsertPuntoAsync(punto);

        var found = await _db.FindByRemoteIdAsync(42);

        found.Should().NotBeNull();
        found!.RemoteId.Should().Be(42);
    }

    [Fact]
    public async Task FindByRemoteIdAsync_NoExiste_RetornaNull()
    {
        var result = await _db.FindByRemoteIdAsync(999);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeletePuntoAsync_DebeEliminarPuntoYFotos()
    {
        var punto = new LocalPunto
        {
            Latitud = -34.6m,
            Longitud = -58.4m,
            SyncStatus = SyncStatusValues.Local
        };
        var puntoId = await _db.InsertPuntoAsync(punto);
        await _db.InsertFotoAsync(new LocalFoto
        {
            PuntoLocalId = puntoId,
            NombreArchivo = "test.jpg",
            RutaLocal = "/nonexistent/test.jpg",
            SyncStatus = SyncStatusValues.Local
        });

        await _db.DeletePuntoAsync(puntoId);

        var deletedPunto = await _db.GetPuntoAsync(puntoId);
        deletedPunto.Should().BeNull();
        var fotos = await _db.GetFotosByPuntoAsync(puntoId);
        fotos.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdatePuntoAsync_DebeActualizarDatos()
    {
        var punto = new LocalPunto
        {
            Latitud = -34.6m,
            Longitud = -58.4m,
            Nombre = "Original",
            SyncStatus = SyncStatusValues.Local
        };
        var localId = await _db.InsertPuntoAsync(punto);

        var stored = await _db.GetPuntoAsync(localId);
        stored!.Nombre = "Modificado";
        await _db.UpdatePuntoAsync(stored);

        var updated = await _db.GetPuntoAsync(localId);
        updated!.Nombre.Should().Be("Modificado");
    }

    [Fact]
    public async Task GetPuntosAsync_DebeRetornarTodos()
    {
        await _db.InsertPuntoAsync(new LocalPunto { Latitud = -34.6m, Longitud = -58.4m, SyncStatus = SyncStatusValues.Local });
        await _db.InsertPuntoAsync(new LocalPunto { Latitud = -34.7m, Longitud = -58.5m, SyncStatus = SyncStatusValues.Synced });

        var puntos = await _db.GetPuntosAsync();

        puntos.Should().HaveCount(2);
    }
}
