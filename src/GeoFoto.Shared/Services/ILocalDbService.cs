using GeoFoto.Shared.Models;

namespace GeoFoto.Shared.Services;

public interface ILocalDbService
{
    Task InitializeAsync();

    // Puntos
    Task<List<LocalPunto>> GetPuntosAsync();
    Task<LocalPunto?> GetPuntoAsync(int localId);
    Task<int> InsertPuntoAsync(LocalPunto punto);
    Task UpdatePuntoAsync(LocalPunto punto);
    Task DeletePuntoAsync(int localId);
    Task<LocalPunto?> FindPuntoCercanoAsync(decimal lat, decimal lng, decimal tolerancia = 0.001m);
    Task<LocalPunto?> FindByRemoteIdAsync(int remoteId);

    // Fotos
    Task<List<LocalFoto>> GetFotosByPuntoAsync(int puntoLocalId);
    Task<LocalFoto?> GetFotoAsync(int localId);
    Task<LocalFoto?> GetFotoByRemoteIdAsync(int remoteId);
    Task<int> InsertFotoAsync(LocalFoto foto);
    Task UpdateFotoAsync(LocalFoto foto);
    Task DeleteFotoAsync(int localId);

    // SyncQueue
    Task EnqueueAsync(string operationType, string entityType, int localId, string payload);
    Task<List<SyncQueueItem>> GetPendingOperationsAsync();
    Task<int> GetPendingCountAsync();
    Task MarkDoneAsync(int queueId, int? remoteId = null);
    Task MarkFailedAsync(int queueId, string error);
    Task IncrementAttemptsAsync(int queueId);
    Task UpdatePendingCreatePayloadAsync(string entityType, int localId, string payload);
}
