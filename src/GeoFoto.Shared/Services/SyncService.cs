using System.Text.Json;
using GeoFoto.Shared.Models;
using Microsoft.Extensions.Logging;

namespace GeoFoto.Shared.Services;

public class SyncService : ISyncService
{
    private readonly ILocalDbService _localDb;
    private readonly IGeoFotoApiClient _api;
    private readonly IConnectivityService _connectivity;
    private readonly IPreferencesService _preferences;
    private readonly ILogger<SyncService> _logger;
    private bool _isSyncing;

    public bool IsSyncing => _isSyncing;
    public event EventHandler<SyncCompletedEventArgs>? SyncCompleted;

    public SyncService(
        ILocalDbService localDb,
        IGeoFotoApiClient api,
        IConnectivityService connectivity,
        IPreferencesService preferences,
        ILogger<SyncService> logger)
    {
        _localDb = localDb;
        _api = api;
        _connectivity = connectivity;
        _preferences = preferences;
        _logger = logger;
    }

    public async Task StartBackgroundSyncAsync()
    {
        _connectivity.ConnectivityChanged += async (_, connected) =>
        {
            if (connected && !_isSyncing)
            {
                try { await SyncNowAsync(); }
                catch (Exception ex) { _logger.LogError(ex, "Background sync error"); }
            }
        };

        if (_connectivity.IsConnected)
        {
            try { await SyncNowAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Initial sync error"); }
        }
    }

    public async Task SyncNowAsync(CancellationToken ct = default)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        var successful = 0;
        var failed = 0;
        var errors = new List<string>();

        try
        {
            await _localDb.InitializeAsync();

            // PUSH
            var pushResult = await PushAsync(ct);
            successful += pushResult.successful;
            failed += pushResult.failed;
            errors.AddRange(pushResult.errors);

            // PULL
            await PullAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en sync");
            errors.Add(ex.Message);
        }
        finally
        {
            _isSyncing = false;
            SyncCompleted?.Invoke(this, new SyncCompletedEventArgs(successful, failed, errors));
        }
    }

    private async Task<(int successful, int failed, List<string> errors)> PushAsync(CancellationToken ct)
    {
        var successful = 0;
        var failed = 0;
        var errors = new List<string>();

        var pendientes = (await _localDb.GetPendingOperationsAsync())
            .Where(o => o.Status == SyncQueueStatus.Pending)
            .ToList();

        if (pendientes.Count == 0)
            return (successful, failed, errors);

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
                    var item = batch.FirstOrDefault(b => b.LocalId == r.LocalId);
                    if (item is null) continue;

                    if (r.Success)
                    {
                        await _localDb.MarkDoneAsync(item.Id, r.RemoteId);

                        // Update local entity's RemoteId if provided
                        if (r.RemoteId.HasValue && item.EntityType == "Punto")
                        {
                            var punto = await _localDb.GetPuntoAsync(item.LocalId);
                            if (punto is not null)
                            {
                                punto.RemoteId = r.RemoteId;
                                punto.SyncStatus = SyncStatusValues.Synced;
                                await _localDb.UpdatePuntoAsync(punto);
                            }
                        }

                        successful++;
                    }
                    else
                    {
                        await _localDb.IncrementAttemptsAsync(item.Id);

                        if (item.Attempts + 1 >= 3)
                        {
                            await _localDb.MarkFailedAsync(item.Id, r.Error ?? "Max intentos alcanzados");
                            failed++;
                            if (r.Error is not null)
                                errors.Add(r.Error);
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                _logger.LogWarning("Sin red durante push — dejando pendientes");
                break;
            }
        }

        return (successful, failed, errors);
    }

    private async Task PullAsync(CancellationToken ct)
    {
        try
        {
            var lastSync = _preferences.Get("last_sync_utc", string.Empty);
            var delta = await _api.GetSyncDeltaAsync(
                string.IsNullOrEmpty(lastSync) ? null : lastSync, ct);

            foreach (var remotePunto in delta.Puntos)
            {
                var local = await _localDb.FindByRemoteIdAsync(remotePunto.Id);

                if (local is null)
                {
                    // New punto from server
                    var newLocal = new LocalPunto
                    {
                        RemoteId = remotePunto.Id,
                        Latitud = remotePunto.Latitud,
                        Longitud = remotePunto.Longitud,
                        Nombre = remotePunto.Nombre,
                        Descripcion = remotePunto.Descripcion,
                        FechaCreacion = remotePunto.FechaCreacion.ToString("O"),
                        UpdatedAt = DateTime.UtcNow.ToString("O"),
                        SyncStatus = SyncStatusValues.Synced
                    };
                    await _localDb.InsertPuntoAsync(newLocal);
                }
                else
                {
                    // Last-Write-Wins: server version wins if local is synced
                    if (local.SyncStatus == SyncStatusValues.Synced ||
                        local.SyncStatus == SyncStatusValues.Local)
                    {
                        local.Nombre = remotePunto.Nombre;
                        local.Descripcion = remotePunto.Descripcion;
                        local.Latitud = remotePunto.Latitud;
                        local.Longitud = remotePunto.Longitud;
                        local.SyncStatus = SyncStatusValues.Synced;
                        await _localDb.UpdatePuntoAsync(local);
                    }
                    // If local has pending changes, keep local version (will push next sync)
                }
            }

            _preferences.Set("last_sync_utc", DateTime.UtcNow.ToString("O"));
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning("Sin red durante pull — omitiendo");
        }
    }
}
