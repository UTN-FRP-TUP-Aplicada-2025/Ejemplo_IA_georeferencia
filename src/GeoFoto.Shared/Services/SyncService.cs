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

        // Order: Create Punto (0) → Create Foto (1) → Update (2) → Delete Foto (3) → Delete Punto (4)
        static int Priority(SyncQueueItem op) => op switch
        {
            { OperationType: "Create", EntityType: "Punto" } => 0,
            { OperationType: "Create", EntityType: "Foto" } => 1,
            { OperationType: "Update" } => 2,
            { OperationType: "Delete", EntityType: "Foto" } => 3,
            { OperationType: "Delete", EntityType: "Punto" } => 4,
            _ => 99
        };

        pendientes = [.. pendientes.OrderBy(Priority)];

        // Separate foto creates — they require direct file upload, not batch
        var fotoCreates = pendientes.Where(p => p is { OperationType: "Create", EntityType: "Foto" }).ToList();
        var batched = pendientes.Except(fotoCreates).ToList();

        // 1) Push non-foto-create operations via batch PRIMERO (Punto Create obtiene RemoteId)
        //    Así cuando subamos fotos en el paso 2, el punto ya existe en el servidor.
        var batches = batched.Chunk(50);
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

        // 2) Push foto creates individualmente DESPUÉS del batch, así el Punto ya tiene RemoteId
        foreach (var op in fotoCreates)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<FotoSyncPayload>(op.Payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload is null)
                {
                    await _localDb.MarkFailedAsync(op.Id, "Payload inválido");
                    failed++;
                    continue;
                }

                // Leer RemoteId del punto (ya debería estar disponible tras el batch)
                var localPunto = await _localDb.GetPuntoAsync(payload.PuntoLocalId);
                if (localPunto?.RemoteId is null)
                {
                    // Punto todavía sin RemoteId (falló el batch o ya existía pendiente) — reintentar en próximo ciclo
                    continue;
                }

                if (!File.Exists(payload.RutaLocal))
                {
                    await _localDb.MarkFailedAsync(op.Id, "Archivo de foto no encontrado");
                    failed++;
                    continue;
                }

                await using var stream = File.OpenRead(payload.RutaLocal);
                var ext = Path.GetExtension(payload.NombreArchivo).ToLowerInvariant();
                var mime = ext == ".png" ? "image/png" : "image/jpeg";

                await _api.AgregarFotoAPuntoAsync(
                    localPunto.RemoteId.Value, stream, payload.NombreArchivo, mime, ct);

                await _localDb.MarkDoneAsync(op.Id);

                var localFoto = await _localDb.GetFotoAsync(op.LocalId);
                if (localFoto is not null)
                {
                    localFoto.SyncStatus = SyncStatusValues.Synced;
                    await _localDb.UpdateFotoAsync(localFoto);
                }

                successful++;
            }
            catch (HttpRequestException)
            {
                _logger.LogWarning("Sin red durante push de foto — dejando pendiente");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al pushear foto {LocalId}", op.LocalId);
                await _localDb.IncrementAttemptsAsync(op.Id);
                if (op.Attempts + 1 >= 3)
                {
                    await _localDb.MarkFailedAsync(op.Id, ex.Message);
                    failed++;
                    errors.Add(ex.Message);
                }
            }
        }

        return (successful, failed, errors);
    }

    private record FotoSyncPayload(int PuntoLocalId, string NombreArchivo, string RutaLocal);

    private async Task PullAsync(CancellationToken ct)
    {
        try
        {
            var lastSync = _preferences.Get("last_sync_utc", string.Empty);
            var delta = await _api.GetSyncDeltaAsync(
                string.IsNullOrEmpty(lastSync) ? null : lastSync, ct);

            // Process deleted entities first
            if (delta.Eliminados is { Count: > 0 })
            {
                foreach (var eliminado in delta.Eliminados)
                {
                    if (eliminado.EntityType == "Foto")
                    {
                        var localFoto = await _localDb.GetFotoByRemoteIdAsync(eliminado.EntityId);
                        if (localFoto is not null)
                            await _localDb.DeleteFotoAsync(localFoto.LocalId);
                    }
                    else if (eliminado.EntityType == "Punto")
                    {
                        var localPunto = await _localDb.FindByRemoteIdAsync(eliminado.EntityId);
                        if (localPunto is not null)
                            await _localDb.DeletePuntoAsync(localPunto.LocalId);
                    }
                }
            }

            // Process puntos
            foreach (var remotePunto in delta.Puntos)
            {
                var local = await _localDb.FindByRemoteIdAsync(remotePunto.Id);

                if (local is null)
                {
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
                else if (local.SyncStatus == SyncStatusValues.Synced ||
                         local.SyncStatus == SyncStatusValues.Local)
                {
                    local.Nombre = remotePunto.Nombre;
                    local.Descripcion = remotePunto.Descripcion;
                    local.Latitud = remotePunto.Latitud;
                    local.Longitud = remotePunto.Longitud;
                    local.SyncStatus = SyncStatusValues.Synced;
                    await _localDb.UpdatePuntoAsync(local);
                }
                else if (local.SyncStatus == SyncStatusValues.PendingUpdate)
                {
                    // LWW: server wins only when its UpdatedAt is strictly newer than local
                    if (remotePunto.UpdatedAt.HasValue &&
                        DateTime.TryParse(local.UpdatedAt, null,
                            System.Globalization.DateTimeStyles.RoundtripKind,
                            out var localTime) &&
                        remotePunto.UpdatedAt.Value > localTime)
                    {
                        // Server version is newer → apply server data and mark as Conflict
                        local.Nombre = remotePunto.Nombre;
                        local.Descripcion = remotePunto.Descripcion;
                        local.Latitud = remotePunto.Latitud;
                        local.Longitud = remotePunto.Longitud;
                        local.SyncStatus = SyncStatusValues.Conflict;
                        await _localDb.UpdatePuntoAsync(local);
                    }
                    // else: local changes are newer or server has no timestamp → local wins
                }
            }

            // Process fotos
            foreach (var remoteFoto in delta.Fotos)
            {
                var localFoto = await _localDb.GetFotoByRemoteIdAsync(remoteFoto.Id);
                if (localFoto is null)
                {
                    // Find the local punto that corresponds to this foto's punto
                    var localPunto = await _localDb.FindByRemoteIdAsync(remoteFoto.PuntoId);
                    if (localPunto is null) continue;

                    var nueva = new LocalFoto
                    {
                        RemoteId = remoteFoto.Id,
                        PuntoLocalId = localPunto.LocalId,
                        NombreArchivo = remoteFoto.NombreArchivo,
                        RutaLocal = string.Empty, // No local file — only available via URL
                        FechaTomada = remoteFoto.FechaTomada?.ToString("O"),
                        SyncStatus = SyncStatusValues.Synced
                    };
                    await _localDb.InsertFotoAsync(nueva);
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
