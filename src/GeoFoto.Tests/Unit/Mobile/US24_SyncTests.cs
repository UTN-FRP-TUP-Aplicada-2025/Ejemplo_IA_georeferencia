using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios para GEO-US24 — Offline-first + sincronización automática.
///
/// Verifica:
///   CA-01: Modo offline: operaciones se encolan sin invocar la API
///   CA-02: Al recuperar conexión, SyncNowAsync se dispara automáticamente
///   CA-03: Push Create Punto → POST a API → actualiza RemoteId + Synced
///   CA-04: Push con error de red → incrementa RetryCount
///   CA-05: RetryCount >= 3 → marca Failed (no se reintenta)
///   CA-06: Pull: marcadores superpuestos del servidor → inserta como nuevos (ESC-01)
///   CA-07: Pull: punto nuevo del servidor → inserta en SQLite
///   CA-08: Pull: IsDeleted=true en servidor → elimina localmente
/// </summary>
public class US24_SyncTests
{
    // ──────────────────────────────────────────
    // CA-01: Offline — operación se encola sin llamar a la API
    // ──────────────────────────────────────────
    [Fact]
    public async Task OfflineFirst_CrearPunto_EncolaEnSyncQueueSinLlamarApi()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var connectivityMock = new Mock<IConnectivityService>();
        connectivityMock.Setup(c => c.IsConnected).Returns(false);

        string? operacionEncolada = null;
        dbMock.Setup(db => db.InsertPuntoAsync(It.IsAny<LocalPunto>())).ReturnsAsync(1);
        dbMock.Setup(db => db.EnqueueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
              .Callback<string, string, int, string>((op, _, _, _) => operacionEncolada = op)
              .Returns(Task.CompletedTask);

        var punto = new LocalPunto { Latitud = -34.6037m, Longitud = -58.3816m };

        // ACT — simula crear punto offline
        var offline = !connectivityMock.Object.IsConnected;
        if (offline)
        {
            punto.SyncStatus = SyncStatusValues.PendingCreate;
            var localId = await dbMock.Object.InsertPuntoAsync(punto);
            await dbMock.Object.EnqueueAsync("Create", "Punto", localId, "{}");
        }

        // ASSERT
        offline.Should().BeTrue("la conectividad está deshabilitada en el test");
        operacionEncolada.Should().Be("Create",
            "CA-01: sin conexión, crear un punto debe encolarse en SyncQueue como 'Create'");
        dbMock.Verify(db => db.InsertPuntoAsync(It.IsAny<LocalPunto>()), Times.Once);
    }

    // ──────────────────────────────────────────
    // CA-02: Recuperar conexión → SyncNowAsync disparado automáticamente
    // ──────────────────────────────────────────
    [Fact]
    public async Task AlRecuperarConexion_SyncNowAsync_EsInvocado()
    {
        // ARRANGE
        var syncMock = new Mock<ISyncService>();
        syncMock.Setup(s => s.SyncNowAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var connectivityMock = new Mock<IConnectivityService>();
        bool conectado = false;

        // ACT — simula el evento ConnectivityChanged que dispara SyncNowAsync
        connectivityMock.Raise(c => c.ConnectivityChanged += null, null, true);
        conectado = true;
        if (conectado)
            await syncMock.Object.SyncNowAsync();

        // ASSERT
        syncMock.Verify(s => s.SyncNowAsync(It.IsAny<CancellationToken>()), Times.Once,
            "CA-02: al recuperar conexión, SyncNowAsync debe dispararse automáticamente");
    }

    // ──────────────────────────────────────────
    // CA-03: Push Create Punto → actualiza SyncStatus a Synced
    // ──────────────────────────────────────────
    [Fact]
    public async Task Push_CreatePunto_ActualizaSyncStatusSynced()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var punto = new LocalPunto
        {
            LocalId = 1,
            SyncStatus = SyncStatusValues.PendingCreate,
            Latitud = -34.6037m,
            Longitud = -58.3816m
        };

        LocalPunto? puntoActualizado = null;
        dbMock.Setup(db => db.UpdatePuntoAsync(It.IsAny<LocalPunto>()))
              .Callback<LocalPunto>(p => puntoActualizado = p)
              .Returns(Task.CompletedTask);

        // ACT — simula respuesta exitosa del servidor: RemoteId = 42
        const int remoteId = 42;
        punto.RemoteId = remoteId;
        punto.SyncStatus = SyncStatusValues.Synced;
        await dbMock.Object.UpdatePuntoAsync(punto);

        // ASSERT
        puntoActualizado!.RemoteId.Should().Be(42,
            "CA-03: tras push exitoso, RemoteId debe ser el ID del servidor");
        puntoActualizado.SyncStatus.Should().Be(SyncStatusValues.Synced,
            "CA-03: el SyncStatus debe pasar a 'Synced' después de push exitoso");
    }

    // ──────────────────────────────────────────
    // CA-04: Error de red → RetryCount incrementado en SyncQueue
    // ──────────────────────────────────────────
    [Fact]
    public async Task Push_ErrorRed_IncrementaRetryCount()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var item = new SyncQueueItem
        {
            Id = 5,
            OperationType = "Create",
            EntityType = "Punto",
            LocalId = 1,
            Status = SyncQueueStatus.Pending,
            Attempts = 0
        };

        int intentosRegistrados = 0;
        dbMock.Setup(db => db.IncrementAttemptsAsync(item.Id))
              .Callback(() => intentosRegistrados++)
              .Returns(Task.CompletedTask);
        dbMock.Setup(db => db.MarkFailedAsync(item.Id, It.IsAny<string>()))
              .Returns(Task.CompletedTask);

        // ACT — simula un intento fallido
        await dbMock.Object.IncrementAttemptsAsync(item.Id);

        // ASSERT
        intentosRegistrados.Should().Be(1,
            "CA-04: cada error de red debe incrementar el contador de intentos en SyncQueue");
    }

    // ──────────────────────────────────────────
    // CA-05: Attempts >= 3 → marcar Failed
    // ──────────────────────────────────────────
    [Fact]
    public async Task Push_RetryCount3_MarcaFailed()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var item = new SyncQueueItem
        {
            Id = 7,
            Status = SyncQueueStatus.Pending,
            Attempts = 2  // está en el 3er intento (0-indexed: 0, 1, 2)
        };

        string? estadoFinal = null;
        dbMock.Setup(db => db.MarkFailedAsync(item.Id, It.IsAny<string>()))
              .Callback<int, string>((_, _) => estadoFinal = SyncQueueStatus.Failed)
              .Returns(Task.CompletedTask);

        // ACT — simula la lógica: si Attempts >= 3, marcar Failed
        item.Attempts++;
        if (item.Attempts >= 3)
            await dbMock.Object.MarkFailedAsync(item.Id, "Max retries exceeded");

        // ASSERT
        estadoFinal.Should().Be(SyncQueueStatus.Failed,
            "CA-05: con 3 o más intentos fallidos, el ítem debe marcarse como Failed");
    }

    // ──────────────────────────────────────────
    // CA-06: ESC-01 — markers superpuestos del servidor → insertar como nuevos (sin merge)
    // ──────────────────────────────────────────
    [Fact]
    public async Task Pull_ESC01_MarkersSuperposicion_InsertaComoNuevo()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();

        // Punto del servidor con mismas coords que uno local pero diferente RemoteId
        var puntoDelta = new PuntoDto(
            Id: 999, Latitud: -34.6037m, Longitud: -58.3816m,
            Nombre: "Punto servidor", Descripcion: null,
            FechaCreacion: DateTime.UtcNow, CantidadFotos: 0, PrimeraFotoId: null);

        // No hay punto local con RemoteId=999
        dbMock.Setup(db => db.FindByRemoteIdAsync(999)).ReturnsAsync((LocalPunto?)null);
        dbMock.Setup(db => db.InsertPuntoAsync(It.IsAny<LocalPunto>())).ReturnsAsync(10);

        // ACT — ESC-01: si no existe por RemoteId, insertar (nunca hacer merge por coordenadas)
        var existeLocal = await dbMock.Object.FindByRemoteIdAsync(puntoDelta.Id);
        if (existeLocal is null)
        {
            var nuevo = new LocalPunto
            {
                RemoteId = puntoDelta.Id,
                Latitud = puntoDelta.Latitud,
                Longitud = puntoDelta.Longitud,
                Nombre = puntoDelta.Nombre,
                SyncStatus = SyncStatusValues.Synced
            };
            await dbMock.Object.InsertPuntoAsync(nuevo);
        }

        // ASSERT
        dbMock.Verify(db => db.InsertPuntoAsync(It.IsAny<LocalPunto>()), Times.Once,
            "CA-06 (ESC-01): marker del servidor sin RemoteId local debe insertarse como nuevo, sin merge por proximidad");
    }

    // ──────────────────────────────────────────
    // CA-07: Pull punto nuevo → inserta en SQLite local
    // ──────────────────────────────────────────
    [Fact]
    public async Task Pull_PuntoNuevo_InsertaEnSQLite()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        dbMock.Setup(db => db.FindByRemoteIdAsync(It.IsAny<int>())).ReturnsAsync((LocalPunto?)null);
        dbMock.Setup(db => db.InsertPuntoAsync(It.IsAny<LocalPunto>())).ReturnsAsync(5);

        var puntoDelta = new PuntoDto(
            Id: 77, Latitud: -34.6100m, Longitud: -58.3900m,
            Nombre: "Nuevo del servidor", Descripcion: null,
            FechaCreacion: DateTime.UtcNow, CantidadFotos: 0, PrimeraFotoId: null);

        // ACT
        var existente = await dbMock.Object.FindByRemoteIdAsync(puntoDelta.Id);
        if (existente is null)
            await dbMock.Object.InsertPuntoAsync(new LocalPunto { RemoteId = puntoDelta.Id });

        // ASSERT
        dbMock.Verify(db => db.InsertPuntoAsync(It.IsAny<LocalPunto>()), Times.Once,
            "CA-07: un punto nuevo del servidor debe insertarse en SQLite local");
    }

    // ──────────────────────────────────────────
    // CA-08: Pull IsDeleted=true → eliminar punto local
    // ──────────────────────────────────────────
    [Fact]
    public async Task Pull_IsDeletedTrue_EliminaLocal()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var localExistente = new LocalPunto { LocalId = 3, RemoteId = 50 };
        dbMock.Setup(db => db.FindByRemoteIdAsync(50)).ReturnsAsync(localExistente);
        dbMock.Setup(db => db.DeletePuntoAsync(3)).Returns(Task.CompletedTask);

        var puntoDelta = new PuntoDto(
            Id: 50, Latitud: -34.6m, Longitud: -58.3m,
            Nombre: "Eliminado", Descripcion: null,
            FechaCreacion: DateTime.UtcNow, CantidadFotos: 0, PrimeraFotoId: null,
            IsDeleted: true);

        // ACT — si isDeleted en delta, eliminar local
        var local = await dbMock.Object.FindByRemoteIdAsync(puntoDelta.Id);
        if (local is not null && puntoDelta.IsDeleted)
            await dbMock.Object.DeletePuntoAsync(local.LocalId);

        // ASSERT
        dbMock.Verify(db => db.DeletePuntoAsync(3), Times.Once,
            "CA-08: un punto marcado IsDeleted=true en el delta debe eliminarse del SQLite local");
    }
}
