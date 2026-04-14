using FluentAssertions;
using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace GeoFoto.Tests;

public class SyncServiceTests
{
    private readonly Mock<ILocalDbService> _localDbMock = new();
    private readonly Mock<IGeoFotoApiClient> _apiMock = new();
    private readonly Mock<IConnectivityService> _connMock = new();
    private readonly Mock<IPreferencesService> _prefsMock = new();
    private readonly Mock<ILogger<SyncService>> _loggerMock = new();

    private SyncService CreateService() => new(
        _localDbMock.Object,
        _apiMock.Object,
        _connMock.Object,
        _prefsMock.Object,
        _loggerMock.Object);

    [Fact]
    public async Task SyncNow_CuandoHayPendientes_DebeEnviarAlApi()
    {
        var pendientes = new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 10, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending }
        };
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(pendientes);
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchResultDto([new SyncOperationResultDto(10, true, 100, null)]));
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto([], []));

        var svc = CreateService();
        await svc.SyncNowAsync();

        _apiMock.Verify(a => a.SyncBatchAsync(
            It.Is<IReadOnlyList<SyncOperationDto>>(ops => ops.Count == 1 && ops[0].LocalId == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncNow_CuandoApiRetorna200_DebeMarcarDone()
    {
        var pendientes = new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 10, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending }
        };
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(pendientes);
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchResultDto([new SyncOperationResultDto(10, true, 100, null)]));
        _localDbMock.Setup(d => d.GetPuntoAsync(10)).ReturnsAsync(new LocalPunto { LocalId = 10 });
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto([], []));

        var svc = CreateService();
        await svc.SyncNowAsync();

        _localDbMock.Verify(d => d.MarkDoneAsync(1, 100), Times.Once);
    }

    [Fact]
    public async Task SyncNow_CuandoApiRetornaError_DebeIncrementarAttempts()
    {
        var pendientes = new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 10, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending, Attempts = 0 }
        };
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(pendientes);
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchResultDto([new SyncOperationResultDto(10, false, null, "Server error")]));
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto([], []));

        var svc = CreateService();
        await svc.SyncNowAsync();

        _localDbMock.Verify(d => d.IncrementAttemptsAsync(1), Times.Once);
    }

    [Fact]
    public async Task SyncNow_CuandoAttempts3_DebeMarcarFailed()
    {
        var pendientes = new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 10, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending, Attempts = 2 }
        };
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(pendientes);
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchResultDto([new SyncOperationResultDto(10, false, null, "Error final")]));
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto([], []));

        var svc = CreateService();
        await svc.SyncNowAsync();

        _localDbMock.Verify(d => d.MarkFailedAsync(1, "Error final"), Times.Once);
    }

    [Fact]
    public async Task SyncNow_SinPendientes_NoDebeEnviarAlApi()
    {
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync([]);
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto([], []));

        var svc = CreateService();
        await svc.SyncNowAsync();

        _apiMock.Verify(a => a.SyncBatchAsync(
            It.IsAny<IReadOnlyList<SyncOperationDto>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Pull_CuandoServidorTieneNuevoPunto_DebeInsertarLocal()
    {
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync([]);
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _localDbMock.Setup(d => d.FindByRemoteIdAsync(1)).ReturnsAsync((LocalPunto?)null);

        var deltaPuntos = new List<PuntoDto>
        {
            new(1, -34.6m, -58.4m, "Remoto", "Desc", DateTime.UtcNow, 0, null)
        };
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto(deltaPuntos, []));

        var svc = CreateService();
        await svc.SyncNowAsync();

        _localDbMock.Verify(d => d.InsertPuntoAsync(It.Is<LocalPunto>(p =>
            p.RemoteId == 1 && p.Nombre == "Remoto" && p.SyncStatus == SyncStatusValues.Synced)),
            Times.Once);
    }

    [Fact]
    public async Task Pull_LastWriteWins_ServidorActualizaSynced_DebeActualizar()
    {
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync([]);
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);

        var localPunto = new LocalPunto
        {
            LocalId = 1,
            RemoteId = 10,
            Nombre = "Local",
            Latitud = -34.6m,
            Longitud = -58.4m,
            SyncStatus = SyncStatusValues.Synced
        };
        _localDbMock.Setup(d => d.FindByRemoteIdAsync(10)).ReturnsAsync(localPunto);

        var deltaPuntos = new List<PuntoDto>
        {
            new(10, -34.7m, -58.5m, "Actualizado", "Desc nueva", DateTime.UtcNow, 0, null)
        };
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto(deltaPuntos, []));

        var svc = CreateService();
        await svc.SyncNowAsync();

        _localDbMock.Verify(d => d.UpdatePuntoAsync(It.Is<LocalPunto>(p =>
            p.Nombre == "Actualizado" && p.SyncStatus == SyncStatusValues.Synced)),
            Times.Once);
    }

    [Fact]
    public async Task Pull_LocalConPendingCreate_NoDebeActualizar()
    {
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync([]);
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);

        var localPunto = new LocalPunto
        {
            LocalId = 1,
            RemoteId = 10,
            Nombre = "Local pendiente",
            Latitud = -34.6m,
            Longitud = -58.4m,
            SyncStatus = SyncStatusValues.PendingCreate
        };
        _localDbMock.Setup(d => d.FindByRemoteIdAsync(10)).ReturnsAsync(localPunto);

        var deltaPuntos = new List<PuntoDto>
        {
            new(10, -34.7m, -58.5m, "Servidor", "Desc", DateTime.UtcNow, 0, null)
        };
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto(deltaPuntos, []));

        var svc = CreateService();
        await svc.SyncNowAsync();

        _localDbMock.Verify(d => d.UpdatePuntoAsync(It.IsAny<LocalPunto>()), Times.Never);
    }

    [Fact]
    public async Task SyncNow_HttpRequestException_NoDebePropagar()
    {
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 10, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending }
        });
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Sin red"));
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Sin red"));

        var svc = CreateService();

        var act = () => svc.SyncNowAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncNow_SyncCompleted_EmiteEvento()
    {
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync([]);
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto([], []));

        var svc = CreateService();
        SyncCompletedEventArgs? args = null;
        svc.SyncCompleted += (_, e) => args = e;

        await svc.SyncNowAsync();

        args.Should().NotBeNull();
        args!.Successful.Should().Be(0);
        args.Failed.Should().Be(0);
    }

    [Fact]
    public async Task SyncNow_YaSincronizando_NoDebeReejecutar()
    {
        var tcs = new TaskCompletionSource();
        _localDbMock.Setup(d => d.InitializeAsync()).Returns(tcs.Task);

        var svc = CreateService();

        // Start first sync (will block on InitializeAsync)
        var firstSync = svc.SyncNowAsync();
        svc.IsSyncing.Should().BeTrue();

        // Second sync should return immediately
        await svc.SyncNowAsync();

        // Unblock first sync
        tcs.SetResult();
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync([]);
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto([], []));
        await firstSync;

        _localDbMock.Verify(d => d.InitializeAsync(), Times.Once);
    }

    [Fact]
    public async Task SyncNow_ExitosoConRemoteId_DebeActualizarPuntoLocal()
    {
        var pendientes = new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 10, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending }
        };
        var localPunto = new LocalPunto { LocalId = 10, Nombre = "Test" };

        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(pendientes);
        _localDbMock.Setup(d => d.GetPuntoAsync(10)).ReturnsAsync(localPunto);
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BatchResultDto([new SyncOperationResultDto(10, true, 200, null)]));
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto([], []));

        var svc = CreateService();
        await svc.SyncNowAsync();

        _localDbMock.Verify(d => d.UpdatePuntoAsync(It.Is<LocalPunto>(p =>
            p.RemoteId == 200 && p.SyncStatus == SyncStatusValues.Synced)), Times.Once);
    }

    [Fact]
    public async Task Pull_GuardaTimestamp_DespuesDeSync()
    {
        _localDbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync([]);
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncDeltaDto([], []));

        var svc = CreateService();
        await svc.SyncNowAsync();

        _prefsMock.Verify(p => p.Set("last_sync_utc", It.Is<string>(s => !string.IsNullOrEmpty(s))), Times.Once);
    }
}
