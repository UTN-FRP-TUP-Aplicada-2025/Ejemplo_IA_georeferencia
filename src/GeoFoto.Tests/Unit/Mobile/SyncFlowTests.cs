using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;
using Microsoft.Extensions.Logging;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios del flujo de sincronización (pull y push) de SyncService.
/// Cubre casos NO contemplados por SyncServiceTests.cs ni SyncServiceExtendedTests.cs:
///   - Pull de fotos nuevas desde el servidor
///   - Pull de fotos huérfanas (punto local inexistente)
///   - Pull de entidades eliminadas (Foto y Punto)
///   - Pull con PendingUpdate local → no sobreescritura (LWW)
///   - Pull exitoso → actualiza la preferencia last_sync_utc
///   - Push foto con punto sin RemoteId → skip silencioso
///   - Push foto con archivo físico inexistente → MarkFailed
/// </summary>
public class SyncFlowTests
{
    private readonly Mock<ILocalDbService>     _dbMock     = new();
    private readonly Mock<IGeoFotoApiClient>    _apiMock    = new();
    private readonly Mock<IConnectivityService> _connMock   = new();
    private readonly Mock<IPreferencesService>  _prefsMock  = new();
    private readonly Mock<ILogger<SyncService>> _loggerMock = new();

    private SyncService CreateService() => new(
        _dbMock.Object,
        _apiMock.Object,
        _connMock.Object,
        _prefsMock.Object,
        _loggerMock.Object);

    /// <summary>Configura GetPendingOperationsAsync para devolver lista vacía (sin nada que hacer en push).</summary>
    private void SetupPushEmpty()
    {
        _dbMock.Setup(d => d.GetPendingOperationsAsync())
               .ReturnsAsync(new List<SyncQueueItem>());
    }

    /// <summary>Configura el pull para recibir un delta con los puntos y fotos indicados y sin eliminados.</summary>
    private void SetupPull(
        IReadOnlyList<PuntoDto>? puntos = null,
        IReadOnlyList<FotoDto>? fotos = null,
        IReadOnlyList<DeletedEntityDto>? eliminados = null)
    {
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SyncDeltaDto(
                    puntos ?? [],
                    fotos  ?? [],
                    eliminados));
    }

    // ──────────────────────────────────────────
    // Pull: foto nueva que no existe localmente → InsertFoto
    // ──────────────────────────────────────────
    [Fact]
    public async Task Pull_FotoNueva_NoExisteLocal_InsertaFoto()
    {
        SetupPushEmpty();
        var remoteForotos = new List<FotoDto>
        {
            new(99, 5, "foto_remote.jpg", null, 50000, null, null)
        };
        SetupPull(fotos: remoteForotos);

        // foto no existe localmente
        _dbMock.Setup(d => d.GetFotoByRemoteIdAsync(99)).ReturnsAsync((LocalFoto?)null);
        // punto local asociado al RemoteId 5 → LocalId = 10
        _dbMock.Setup(d => d.FindByRemoteIdAsync(5))
               .ReturnsAsync(new LocalPunto { LocalId = 10, RemoteId = 5 });

        await CreateService().SyncNowAsync();

        _dbMock.Verify(d => d.InsertFotoAsync(It.Is<LocalFoto>(f =>
            f.RemoteId == 99 &&
            f.PuntoLocalId == 10 &&
            f.NombreArchivo == "foto_remote.jpg" &&
            f.SyncStatus == SyncStatusValues.Synced)),
            Times.Once,
            "debe insertar la foto remota vinculada al punto local");
    }

    // ──────────────────────────────────────────
    // Pull: foto nueva pero el punto local no existe → skip (no InsertFoto)
    // ──────────────────────────────────────────
    [Fact]
    public async Task Pull_FotoNueva_PuntoLocalNoExiste_Skip()
    {
        SetupPushEmpty();
        var remoteFotos = new List<FotoDto>
        {
            new(88, 99, "huerfana.jpg", null, 10000, null, null)
        };
        SetupPull(fotos: remoteFotos);

        _dbMock.Setup(d => d.GetFotoByRemoteIdAsync(88)).ReturnsAsync((LocalFoto?)null);
        _dbMock.Setup(d => d.FindByRemoteIdAsync(99)).ReturnsAsync((LocalPunto?)null);

        await CreateService().SyncNowAsync();

        _dbMock.Verify(d => d.InsertFotoAsync(It.IsAny<LocalFoto>()),
            Times.Never,
            "no debe insertar foto si el punto local no existe");
    }

    // ──────────────────────────────────────────
    // Pull: entidad eliminada de tipo Foto → DeleteFotoAsync
    // ──────────────────────────────────────────
    [Fact]
    public async Task Pull_EliminadoFoto_LocalExiste_EliminaFoto()
    {
        SetupPushEmpty();
        var eliminados = new List<DeletedEntityDto> { new("Foto", 77) };
        SetupPull(eliminados: eliminados);

        _dbMock.Setup(d => d.GetFotoByRemoteIdAsync(77))
               .ReturnsAsync(new LocalFoto { LocalId = 15, RemoteId = 77 });

        await CreateService().SyncNowAsync();

        _dbMock.Verify(d => d.DeleteFotoAsync(15), Times.Once,
            "debe eliminar la foto local con LocalId = 15");
    }

    // ──────────────────────────────────────────
    // Pull: entidad eliminada de tipo Punto → DeletePuntoAsync
    // ──────────────────────────────────────────
    [Fact]
    public async Task Pull_EliminadoPunto_LocalExiste_EliminaPunto()
    {
        SetupPushEmpty();
        var eliminados = new List<DeletedEntityDto> { new("Punto", 33) };
        SetupPull(eliminados: eliminados);

        _dbMock.Setup(d => d.FindByRemoteIdAsync(33))
               .ReturnsAsync(new LocalPunto { LocalId = 7, RemoteId = 33 });

        await CreateService().SyncNowAsync();

        _dbMock.Verify(d => d.DeletePuntoAsync(7), Times.Once,
            "debe eliminar el punto local con LocalId = 7");
    }

    // ──────────────────────────────────────────
    // Pull: punto local con SyncStatus = PendingUpdate → no sobreescribir (LWW)
    // ──────────────────────────────────────────
    [Fact]
    public async Task Pull_PuntoLocal_StatusPendingUpdate_NoSobreescribeLocal()
    {
        SetupPushEmpty();
        var remotePuntos = new List<PuntoDto>
        {
            new(5, -34.61m, -58.39m, "Nombre remoto", null, DateTime.UtcNow, 0, null)
        };
        SetupPull(puntos: remotePuntos);

        var localPendiente = new LocalPunto
        {
            LocalId    = 10,
            RemoteId   = 5,
            Nombre     = "Edición local pendiente",
            SyncStatus = SyncStatusValues.PendingUpdate
        };
        _dbMock.Setup(d => d.FindByRemoteIdAsync(5)).ReturnsAsync(localPendiente);

        await CreateService().SyncNowAsync();

        _dbMock.Verify(d => d.UpdatePuntoAsync(It.IsAny<LocalPunto>()),
            Times.Never,
            "no debe sobreescribir un punto local con PendingUpdate (LWW: cambio local gana)");
    }

    // ──────────────────────────────────────────
    // Pull exitoso → actualiza la preferencia last_sync_utc
    // ──────────────────────────────────────────
    [Fact]
    public async Task Pull_Exitoso_ActualizaPreferenciaLastSyncUtc()
    {
        SetupPushEmpty();
        SetupPull();   // delta vacío → pull sin errores

        await CreateService().SyncNowAsync();

        _prefsMock.Verify(p => p.Set("last_sync_utc", It.IsAny<string>()),
            Times.Once,
            "debe persistir el timestamp de la última sincronización exitosa");
    }

    // ──────────────────────────────────────────
    // Push: foto Create con punto sin RemoteId → skip silencioso
    // ──────────────────────────────────────────
    [Fact]
    public async Task Push_FotoCreate_PuntoSinRemoteId_Skip()
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(
            new { PuntoLocalId = 10, NombreArchivo = "foto.jpg", RutaLocal = "/tmp/noexiste_push.jpg" });

        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 5, OperationType = "Create", EntityType = "Foto",
                    Payload = payload, Status = SyncQueueStatus.Pending }
        });
        // Punto local encontrado, pero sin RemoteId
        _dbMock.Setup(d => d.GetPuntoAsync(10))
               .ReturnsAsync(new LocalPunto { LocalId = 10, RemoteId = null });

        SetupPull();

        await CreateService().SyncNowAsync();

        _apiMock.Verify(a => a.AgregarFotoAPuntoAsync(
            It.IsAny<int>(), It.IsAny<Stream>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "no debe intentar subir la foto si el punto aún no tiene RemoteId");
    }

    // ──────────────────────────────────────────
    // Push: foto Create con archivo físico inexistente → MarkFailed
    // ──────────────────────────────────────────
    [Fact]
    public async Task Push_FotoCreate_ArchivoNoExiste_MarkFailed()
    {
        var nonExistentPath = Path.Combine(
            Path.GetTempPath(), $"noexiste_push_{Guid.NewGuid():N}.jpg");

        var payload = System.Text.Json.JsonSerializer.Serialize(
            new { PuntoLocalId = 20, NombreArchivo = "foto.jpg", RutaLocal = nonExistentPath });

        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(new List<SyncQueueItem>
        {
            new() { Id = 2, LocalId = 8, OperationType = "Create", EntityType = "Foto",
                    Payload = payload, Status = SyncQueueStatus.Pending, Attempts = 0 }
        });
        // Punto con RemoteId válido para que pase el check de RemoteId
        _dbMock.Setup(d => d.GetPuntoAsync(20))
               .ReturnsAsync(new LocalPunto { LocalId = 20, RemoteId = 100 });

        SetupPull();

        await CreateService().SyncNowAsync();

        _dbMock.Verify(d => d.MarkFailedAsync(2, "Archivo de foto no encontrado"),
            Times.Once,
            "debe marcar como fallida la operación cuando el archivo físico no existe");
    }
}
