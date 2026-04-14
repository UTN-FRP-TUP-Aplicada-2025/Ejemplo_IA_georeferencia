using System.Text.Json;
using GeoFoto.Shared.Models;
using SQLite;

namespace GeoFoto.Shared.Services;

public class LocalDbService : ILocalDbService
{
    private SQLiteAsyncConnection? _db;
    private readonly string _dbPath;

    public LocalDbService(string dbPath)
    {
        _dbPath = dbPath;
    }

    public async Task InitializeAsync()
    {
        if (_db is not null) return;
        _db = new SQLiteAsyncConnection(_dbPath);
        await _db.CreateTableAsync<LocalPunto>();
        await _db.CreateTableAsync<LocalFoto>();
        await _db.CreateTableAsync<SyncQueueItem>();
    }

    private async Task EnsureInitializedAsync()
    {
        if (_db is null) await InitializeAsync();
    }

    // Puntos
    public async Task<List<LocalPunto>> GetPuntosAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<LocalPunto>().ToListAsync();
    }

    public async Task<LocalPunto?> GetPuntoAsync(int localId)
    {
        await EnsureInitializedAsync();
        return await _db!.Table<LocalPunto>()
            .FirstOrDefaultAsync(p => p.LocalId == localId);
    }

    public async Task<int> InsertPuntoAsync(LocalPunto punto)
    {
        await EnsureInitializedAsync();
        await _db!.InsertAsync(punto);
        return punto.LocalId;
    }

    public async Task UpdatePuntoAsync(LocalPunto punto)
    {
        await EnsureInitializedAsync();
        punto.UpdatedAt = DateTime.UtcNow.ToString("O");
        await _db!.UpdateAsync(punto);
    }

    public async Task DeletePuntoAsync(int localId)
    {
        await EnsureInitializedAsync();
        var fotos = await GetFotosByPuntoAsync(localId);
        foreach (var foto in fotos)
        {
            if (File.Exists(foto.RutaLocal))
                File.Delete(foto.RutaLocal);
            await _db!.DeleteAsync(foto);
        }
        await _db!.Table<LocalPunto>().DeleteAsync(p => p.LocalId == localId);
    }

    public async Task<LocalPunto?> FindPuntoCercanoAsync(decimal lat, decimal lng, decimal tolerancia = 0.001m)
    {
        await EnsureInitializedAsync();
        var puntos = await _db!.Table<LocalPunto>().ToListAsync();
        return puntos.FirstOrDefault(p =>
            Math.Abs(p.Latitud - lat) < tolerancia &&
            Math.Abs(p.Longitud - lng) < tolerancia);
    }

    public async Task<LocalPunto?> FindByRemoteIdAsync(int remoteId)
    {
        await EnsureInitializedAsync();
        return await _db!.Table<LocalPunto>()
            .FirstOrDefaultAsync(p => p.RemoteId == remoteId);
    }

    // Fotos
    public async Task<List<LocalFoto>> GetFotosByPuntoAsync(int puntoLocalId)
    {
        await EnsureInitializedAsync();
        return await _db!.Table<LocalFoto>()
            .Where(f => f.PuntoLocalId == puntoLocalId)
            .ToListAsync();
    }

    public async Task<int> InsertFotoAsync(LocalFoto foto)
    {
        await EnsureInitializedAsync();
        await _db!.InsertAsync(foto);
        return foto.LocalId;
    }

    public async Task DeleteFotoAsync(int localId)
    {
        await EnsureInitializedAsync();
        var foto = await _db!.Table<LocalFoto>()
            .FirstOrDefaultAsync(f => f.LocalId == localId);
        if (foto is not null)
        {
            if (File.Exists(foto.RutaLocal))
                File.Delete(foto.RutaLocal);
            await _db!.DeleteAsync(foto);
        }
    }

    // SyncQueue
    public async Task EnqueueAsync(string operationType, string entityType, int localId, string payload)
    {
        await EnsureInitializedAsync();
        var item = new SyncQueueItem
        {
            OperationType = operationType,
            EntityType = entityType,
            LocalId = localId,
            Payload = payload,
            Status = SyncQueueStatus.Pending,
            CreatedAt = DateTime.UtcNow.ToString("O")
        };
        await _db!.InsertAsync(item);
    }

    public async Task<List<SyncQueueItem>> GetPendingOperationsAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<SyncQueueItem>().ToListAsync();
    }

    public async Task<int> GetPendingCountAsync()
    {
        await EnsureInitializedAsync();
        return await _db!.Table<SyncQueueItem>()
            .CountAsync(q => q.Status == SyncQueueStatus.Pending);
    }

    public async Task MarkDoneAsync(int queueId, int? remoteId = null)
    {
        await EnsureInitializedAsync();
        var item = await _db!.Table<SyncQueueItem>()
            .FirstOrDefaultAsync(q => q.Id == queueId);
        if (item is null) return;
        item.Status = SyncQueueStatus.Done;
        await _db!.UpdateAsync(item);
    }

    public async Task MarkFailedAsync(int queueId, string error)
    {
        await EnsureInitializedAsync();
        var item = await _db!.Table<SyncQueueItem>()
            .FirstOrDefaultAsync(q => q.Id == queueId);
        if (item is null) return;
        item.Status = SyncQueueStatus.Failed;
        item.ErrorMessage = error;
        await _db!.UpdateAsync(item);
    }

    public async Task IncrementAttemptsAsync(int queueId)
    {
        await EnsureInitializedAsync();
        var item = await _db!.Table<SyncQueueItem>()
            .FirstOrDefaultAsync(q => q.Id == queueId);
        if (item is null) return;
        item.Attempts++;
        item.LastAttemptAt = DateTime.UtcNow.ToString("O");
        await _db!.UpdateAsync(item);
    }
}
