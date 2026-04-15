using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Mobile.Services;

public sealed class SyncBackgroundService : IDisposable
{
    private readonly ISyncService _syncService;
    private readonly IConnectivityService _connectivity;
    private readonly ILocalDbService _db;
    private Timer? _timer;
    private bool _syncing;

    public SyncBackgroundService(
        ISyncService syncService,
        IConnectivityService connectivity,
        ILocalDbService db)
    {
        _syncService = syncService;
        _connectivity = connectivity;
        _db = db;

        _connectivity.ConnectivityChanged += async (_, isConnected) =>
        {
            if (isConnected) await TrySyncAsync();
        };

        // Initial delay 30s, then every 5 min
        _timer = new Timer(async _ => await TrySyncAsync(),
            null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
    }

    public async Task TrySyncAsync()
    {
        if (_syncing || !_connectivity.IsConnected) return;

        try
        {
            var pendientes = await _db.GetPendingCountAsync();
            if (pendientes == 0) return;
        }
        catch
        {
            return;
        }

        _syncing = true;
        try
        {
            await _syncService.SyncNowAsync();
        }
        catch
        {
            // errors surfaced via SyncCompleted event
        }
        finally
        {
            _syncing = false;
        }
    }

    public void Dispose() => _timer?.Dispose();
}
