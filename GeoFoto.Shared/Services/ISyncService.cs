namespace GeoFoto.Shared.Services;

public interface ISyncService
{
    bool IsSyncing { get; }
    event EventHandler<SyncCompletedEventArgs> SyncCompleted;
    Task SyncNowAsync(CancellationToken ct = default);
    Task StartBackgroundSyncAsync();
}

public record SyncCompletedEventArgs(int Successful, int Failed, List<string> Errors);
