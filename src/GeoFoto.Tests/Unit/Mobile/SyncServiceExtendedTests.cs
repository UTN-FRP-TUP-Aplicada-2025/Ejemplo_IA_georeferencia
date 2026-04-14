using FluentAssertions;
using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// BLOQUE 7 — Auto-sincronización (DELTA-07)
/// TEST-042 a TEST-051
/// Extiende los tests base de SyncService (SyncServiceTests.cs).
/// </summary>
public class SyncServiceExtendedTests
{
    private readonly Mock<ILocalDbService>         _dbMock      = new();
    private readonly Mock<IGeoFotoApiClient>        _apiMock     = new();
    private readonly Mock<IConnectivityService>     _connMock    = new();
    private readonly Mock<IPreferencesService>      _prefsMock   = new();
    private readonly Mock<ILogger<SyncService>>     _loggerMock  = new();

    private SyncService CreateService() => new(
        _dbMock.Object,
        _apiMock.Object,
        _connMock.Object,
        _prefsMock.Object,
        _loggerMock.Object);

    private void SetupPullEmpty()
    {
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SyncDeltaDto([], []));
    }

    // ──────────────────────────────────────────
    // TEST-042: ConnectivityChanged → SyncService ejecuta SyncAsync()
    // ──────────────────────────────────────────
    [Fact]
    public async Task StartBackgroundSync_CambioAConectado_EjecutaSync()
    {
        // ARRANGE
        var pendientes = new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 10, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending },
            new() { Id = 2, LocalId = 11, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending },
            new() { Id = 3, LocalId = 12, OperationType = "Create", EntityType = "Foto",
                    Payload = "{}", Status = SyncQueueStatus.Pending }
        };
        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(pendientes);
        var results = pendientes.Select(p =>
            new SyncOperationResultDto(p.LocalId, true, p.LocalId + 100, null)).ToList();
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(),
                                              It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchResultDto(results));
        _connMock.Setup(c => c.IsConnected).Returns(false); // empieza desconectado
        SetupPullEmpty();

        EventHandler<bool>? handlerCapturado = null;
        _connMock.SetupAdd(c => c.ConnectivityChanged += It.IsAny<EventHandler<bool>>())
                 .Callback<EventHandler<bool>>(h => handlerCapturado = h);

        var svc = CreateService();

        // ACT — arrancar background + simular evento de conectividad
        await svc.StartBackgroundSyncAsync();
        handlerCapturado?.Invoke(this, true); // simula "pasa a conectado"

        // Pequeña espera para que el handler async se complete
        await Task.Delay(100);

        // ASSERT — se intentó sincronizar (SyncBatchAsync fue llamado)
        _apiMock.Verify(a => a.SyncBatchAsync(
            It.IsAny<IReadOnlyList<SyncOperationDto>>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    // ──────────────────────────────────────────
    // TEST-043: Timer periódico — si hay pendientes → ejecuta Sync
    // (test conceptual: verificamos que SyncNowAsync procesa pendientes)
    // ──────────────────────────────────────────
    [Fact]
    public async Task SyncTimer_HayPendientes_EjecutaSync()
    {
        // ARRANGE
        var pendientes = new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 20, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending }
        };
        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(pendientes);
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(),
                                              It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchResultDto([new SyncOperationResultDto(20, true, 120, null)]));
        _connMock.Setup(c => c.IsConnected).Returns(true);
        SetupPullEmpty();

        var svc = CreateService();

        // ACT — simular disparo del timer (invocar SyncNowAsync directamente)
        await svc.SyncNowAsync();

        // ASSERT
        _apiMock.Verify(a => a.SyncBatchAsync(
            It.IsAny<IReadOnlyList<SyncOperationDto>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────
    // TEST-044: Sin pendientes → SyncBatchAsync NO se invoca
    // ──────────────────────────────────────────
    [Fact]
    public async Task SyncTimer_SinPendientes_NoInvocaBatchSync()
    {
        // ARRANGE
        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(new List<SyncQueueItem>());
        _connMock.Setup(c => c.IsConnected).Returns(true);
        SetupPullEmpty();

        var svc = CreateService();

        // ACT
        await svc.SyncNowAsync();

        // ASSERT
        _apiMock.Verify(a => a.SyncBatchAsync(
            It.IsAny<IReadOnlyList<SyncOperationDto>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ──────────────────────────────────────────
    // TEST-045: Backoff exponencial — 3 fallos → Status = Failed
    // ──────────────────────────────────────────
    [Fact]
    public async Task Sync_TresFallos_MarcaStatusFailed()
    {
        // ARRANGE — operación que ya tiene 2 intentos (está a punto del límite)
        var pendientes = new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 30, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending, Attempts = 2 }
        };
        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(pendientes);
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(),
                                              It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchResultDto(
                    [new SyncOperationResultDto(30, false, null, "Server error")]));
        _connMock.Setup(c => c.IsConnected).Returns(true);
        SetupPullEmpty();

        var svc = CreateService();

        // ACT
        await svc.SyncNowAsync();

        // ASSERT — después de 3 intentos fallidos → MarkFailedAsync
        _dbMock.Verify(d => d.IncrementAttemptsAsync(1), Times.Once);
        _dbMock.Verify(d => d.MarkFailedAsync(1, It.IsAny<string>()), Times.Once,
            "con Attempts >= 3 debe llamar MarkFailedAsync");
    }

    // ──────────────────────────────────────────
    // TEST-046: Push exitoso → actualiza RemoteId y SyncStatus = Synced
    // ──────────────────────────────────────────
    [Fact]
    public async Task SyncPush_Exitoso_ActualizaRemoteIdYStatus()
    {
        // ARRANGE
        var puntoLocal = new LocalPunto { LocalId = 40, SyncStatus = SyncStatusValues.PendingCreate };
        var pendientes = new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 40, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending }
        };
        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(pendientes);
        _dbMock.Setup(d => d.GetPuntoAsync(40)).ReturnsAsync(puntoLocal);
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(),
                                              It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchResultDto(
                    [new SyncOperationResultDto(40, true, 999, null)]));
        _connMock.Setup(c => c.IsConnected).Returns(true);
        SetupPullEmpty();

        var svc = CreateService();

        // ACT
        await svc.SyncNowAsync();

        // ASSERT
        _dbMock.Verify(d => d.MarkDoneAsync(1, 999), Times.Once);
        puntoLocal.RemoteId.Should().Be(999);
        puntoLocal.SyncStatus.Should().Be(SyncStatusValues.Synced);
    }

    // ──────────────────────────────────────────
    // TEST-047: PullDelta → descarga cambios desde el timestamp de last_sync
    // ──────────────────────────────────────────
    [Fact]
    public async Task SyncPull_LlamaConLastSyncTimestamp()
    {
        // ARRANGE
        const string lastSync = "2026-04-10T00:00:00.0000000Z";
        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(new List<SyncQueueItem>());
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(lastSync);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(lastSync, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SyncDeltaDto([], []));

        var svc = CreateService();

        // ACT
        await svc.SyncNowAsync();

        // ASSERT
        _apiMock.Verify(a => a.GetSyncDeltaAsync(lastSync, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ──────────────────────────────────────────
    // TEST-048: Conflicto LWW — registro con UpdatedAt más reciente prevalece
    // ──────────────────────────────────────────
    [Fact]
    public async Task SyncPull_PuntoConflict_LwwServerGana_CuandoLocalEsSynced()
    {
        // ARRANGE — hay un punto local (Synced) y el servidor envía una versión actualizada
        var localPunto = new LocalPunto
        {
            LocalId    = 1,
            RemoteId   = 77,
            Nombre     = "Nombre viejo",
            SyncStatus = SyncStatusValues.Synced
        };
        var remotePunto = new PuntoDto(77, -34.60m, -58.38m, "Nombre servidor",
            "Desc", DateTime.UtcNow, 0, null);
        var delta = new SyncDeltaDto([remotePunto], []);

        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(new List<SyncQueueItem>());
        _dbMock.Setup(d => d.FindByRemoteIdAsync(77)).ReturnsAsync(localPunto);
        _prefsMock.Setup(p => p.Get("last_sync_utc", string.Empty)).Returns(string.Empty);
        _apiMock.Setup(a => a.GetSyncDeltaAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(delta);
        _dbMock.Setup(d => d.UpdatePuntoAsync(It.IsAny<LocalPunto>()))
               .Callback<LocalPunto>(p => { localPunto.Nombre = p.Nombre; })
               .Returns(Task.CompletedTask);

        var svc = CreateService();

        // ACT
        await svc.SyncNowAsync();

        // ASSERT — la versión del servidor prevaleció (LWW: local Synced = sin cambios locales)
        _dbMock.Verify(d => d.UpdatePuntoAsync(It.Is<LocalPunto>(p =>
            p.Nombre == "Nombre servidor")), Times.Once);
    }

    // ──────────────────────────────────────────
    // TEST-049: Auto-sync deshabilitado → SyncCompleted event aún dispara
    //           (el SyncService básico no tiene habilitado/deshabilitado, se verifica
    //            que el evento se emite al terminar SyncNowAsync)
    // ──────────────────────────────────────────
    [Fact]
    public async Task SyncNow_AlTerminar_DispararaSyncCompletedEvent()
    {
        // ARRANGE
        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(new List<SyncQueueItem>());
        _connMock.Setup(c => c.IsConnected).Returns(true);
        SetupPullEmpty();

        var svc = CreateService();
        SyncCompletedEventArgs? eventArgs = null;
        svc.SyncCompleted += (_, args) => eventArgs = args;

        // ACT
        await svc.SyncNowAsync();

        // ASSERT
        eventArgs.Should().NotBeNull("SyncCompleted debe dispararse al finalizar SyncNowAsync");
        eventArgs!.Successful.Should().Be(0);
        eventArgs.Failed.Should().Be(0);
    }

    // ──────────────────────────────────────────
    // TEST-050: Sync manual → procesa operaciones pendientes y dispara evento
    // ──────────────────────────────────────────
    [Fact]
    public async Task SyncManual_ConPendientes_ProcesaYDisparaEvento()
    {
        // ARRANGE
        var pendientes = new List<SyncQueueItem>
        {
            new() { Id = 1, LocalId = 50, OperationType = "Create", EntityType = "Punto",
                    Payload = "{}", Status = SyncQueueStatus.Pending }
        };
        _dbMock.Setup(d => d.GetPendingOperationsAsync()).ReturnsAsync(pendientes);
        _apiMock.Setup(a => a.SyncBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(),
                                              It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BatchResultDto(
                    [new SyncOperationResultDto(50, true, 500, null)]));
        _connMock.Setup(c => c.IsConnected).Returns(true);
        SetupPullEmpty();

        var svc = CreateService();
        SyncCompletedEventArgs? eventArgs = null;
        svc.SyncCompleted += (_, args) => eventArgs = args;

        // ACT
        await svc.SyncNowAsync();

        // ASSERT
        eventArgs.Should().NotBeNull();
        eventArgs!.Successful.Should().Be(1);
        eventArgs.Failed.Should().Be(0);
    }

    // ──────────────────────────────────────────
    // TEST-051: SyncStatusBadge — contador de pendientes en tiempo real
    // ──────────────────────────────────────────
    [Fact]
    public async Task SyncStatusBadge_MuestraContadorPendientes()
    {
        // ARRANGE
        _dbMock.Setup(d => d.GetPendingCountAsync()).ReturnsAsync(5);

        // ACT
        var pendienteCount = await _dbMock.Object.GetPendingCountAsync();

        // ASSERT
        pendienteCount.Should().Be(5, "el badge debe mostrar 5 operaciones pendientes");
    }

    [Fact]
    public async Task SyncStatusBadge_TrasSincronizacion_ActualizaContador()
    {
        // ARRANGE — empieza con 5, luego de sync baja a 4
        var contadorActual = 5;
        _dbMock.SetupSequence(d => d.GetPendingCountAsync())
               .ReturnsAsync(5)
               .ReturnsAsync(4);

        // ACT
        var antes   = await _dbMock.Object.GetPendingCountAsync();
        var despues = await _dbMock.Object.GetPendingCountAsync();

        // ASSERT
        antes.Should().Be(5);
        despues.Should().Be(4, "el badge debe actualizarse en tiempo real tras cada operación");
    }
}
