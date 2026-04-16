using System.Text.Json;
using GeoFoto.Api.Data;
using GeoFoto.Api.Dtos;
using GeoFoto.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoFoto.Api.Services;

public interface ISyncApiService
{
    Task<SyncDeltaDto> GetDeltaAsync(string? since, CancellationToken ct = default);
    Task<BatchResultDto> ProcessBatchAsync(IReadOnlyList<SyncOperationDto> operations, CancellationToken ct = default);
}

public class SyncApiService : ISyncApiService
{
    private readonly GeoFotoDbContext _db;
    private readonly IFileStorageService _storage;

    public SyncApiService(GeoFotoDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<SyncDeltaDto> GetDeltaAsync(string? since, CancellationToken ct = default)
    {
        DateTime? sinceDate = null;
        if (!string.IsNullOrEmpty(since) && DateTime.TryParse(since, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            sinceDate = parsed;

        var puntosQuery = _db.Puntos.AsQueryable();
        var fotosQuery = _db.Fotos.AsQueryable();
        var deletedQuery = _db.DeletedEntities.AsQueryable();

        if (sinceDate.HasValue)
        {
            puntosQuery = puntosQuery.Where(p => p.UpdatedAt > sinceDate.Value);
            fotosQuery = fotosQuery.Where(f => f.UpdatedAt > sinceDate.Value);
            deletedQuery = deletedQuery.Where(d => d.DeletedAt > sinceDate.Value);
        }

        var puntos = await puntosQuery
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new PuntoDto(
                p.Id, p.Latitud, p.Longitud,
                p.Nombre, p.Descripcion, p.FechaCreacion,
                p.Fotos.Count,
                p.Fotos.OrderBy(f => f.Id).Select(f => (int?)f.Id).FirstOrDefault(),
                p.UpdatedAt))
            .ToListAsync(ct);

        var fotos = await fotosQuery
            .OrderByDescending(f => f.UpdatedAt)
            .Select(f => new FotoDto(
                f.Id, f.PuntoId, f.NombreArchivo,
                f.FechaTomada, f.TamanoBytes,
                f.LatitudExif, f.LongitudExif))
            .ToListAsync(ct);

        var eliminados = await deletedQuery
            .Select(d => new DeletedEntityDto(d.EntityType, d.EntityId))
            .ToListAsync(ct);

        return new SyncDeltaDto(puntos, fotos, eliminados);
    }

    public async Task<BatchResultDto> ProcessBatchAsync(IReadOnlyList<SyncOperationDto> operations, CancellationToken ct = default)
    {
        var results = new List<SyncOperationResultDto>();

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            foreach (var op in operations)
            {
                try
                {
                    var result = op.EntityType switch
                    {
                        "Punto" => await ProcessPuntoOperationAsync(op, ct),
                        "Foto" => await ProcessFotoOperationAsync(op, ct),
                        _ => new SyncOperationResultDto(op.LocalId, false, null, $"EntityType desconocido: {op.EntityType}")
                    };
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    results.Add(new SyncOperationResultDto(op.LocalId, false, null, ex.Message));
                }
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            // Mark all remaining as failed
            for (int i = results.Count; i < operations.Count; i++)
                results.Add(new SyncOperationResultDto(operations[i].LocalId, false, null, ex.Message));
        }

        return new BatchResultDto(results);
    }

    private async Task<SyncOperationResultDto> ProcessPuntoOperationAsync(SyncOperationDto op, CancellationToken ct)
    {
        switch (op.OperationType)
        {
            case "Create":
            {
                var payload = JsonSerializer.Deserialize<PuntoPayload>(op.Payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload is null)
                    return new SyncOperationResultDto(op.LocalId, false, null, "Payload inválido");

                // Check if punto already exists nearby
                var existing = await _db.Puntos.FirstOrDefaultAsync(p =>
                    Math.Abs(p.Latitud - payload.Latitud) < 0.001m &&
                    Math.Abs(p.Longitud - payload.Longitud) < 0.001m, ct);

                if (existing is not null)
                    return new SyncOperationResultDto(op.LocalId, true, existing.Id, null);

                var now = DateTime.UtcNow;
                var punto = new Punto
                {
                    Latitud = payload.Latitud,
                    Longitud = payload.Longitud,
                    Nombre = payload.Nombre,
                    Descripcion = payload.Descripcion,
                    FechaCreacion = now,
                    UpdatedAt = now
                };
                _db.Puntos.Add(punto);
                await _db.SaveChangesAsync(ct);
                return new SyncOperationResultDto(op.LocalId, true, punto.Id, null);
            }
            case "Update":
            {
                var payload = JsonSerializer.Deserialize<PuntoPayload>(op.Payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload?.RemoteId is null)
                    return new SyncOperationResultDto(op.LocalId, false, null, "RemoteId requerido para Update");

                var punto = await _db.Puntos.FindAsync([payload.RemoteId.Value], ct);
                if (punto is null)
                    return new SyncOperationResultDto(op.LocalId, false, null, "Punto no encontrado");

                // Last-Write-Wins: always apply update
                punto.Nombre = payload.Nombre;
                punto.Descripcion = payload.Descripcion;
                punto.UpdatedAt = DateTime.UtcNow;
                return new SyncOperationResultDto(op.LocalId, true, punto.Id, null);
            }
            case "Delete":
            {
                var payload = JsonSerializer.Deserialize<PuntoPayload>(op.Payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload?.RemoteId is null)
                    return new SyncOperationResultDto(op.LocalId, true, null, null); // Already gone

                var punto = await _db.Puntos.Include(p => p.Fotos)
                    .FirstOrDefaultAsync(p => p.Id == payload.RemoteId.Value, ct);

                if (punto is null)
                    return new SyncOperationResultDto(op.LocalId, true, null, null);

                foreach (var foto in punto.Fotos)
                {
                    _storage.Delete(foto.RutaFisica);
                    _db.DeletedEntities.Add(new Models.DeletedEntity { EntityType = "Foto", EntityId = foto.Id, DeletedAt = DateTime.UtcNow });
                }

                _db.DeletedEntities.Add(new Models.DeletedEntity { EntityType = "Punto", EntityId = punto.Id, DeletedAt = DateTime.UtcNow });
                _db.Puntos.Remove(punto);
                return new SyncOperationResultDto(op.LocalId, true, null, null);
            }
            default:
                return new SyncOperationResultDto(op.LocalId, false, null, $"OperationType desconocido: {op.OperationType}");
        }
    }

    private async Task<SyncOperationResultDto> ProcessFotoOperationAsync(SyncOperationDto op, CancellationToken ct)
    {
        switch (op.OperationType)
        {
            case "Create":
            {
                // Foto creation from mobile sends metadata; actual binary is synced via upload endpoint
                // For now, mark as success — the actual file upload happens separately
                return new SyncOperationResultDto(op.LocalId, true, null, null);
            }
            case "Delete":
            {
                var payload = JsonSerializer.Deserialize<FotoPayload>(op.Payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload?.RemoteId is null)
                    return new SyncOperationResultDto(op.LocalId, true, null, null);

                var foto = await _db.Fotos.FindAsync([payload.RemoteId.Value], ct);
                if (foto is not null)
                {
                    _storage.Delete(foto.RutaFisica);
                    _db.DeletedEntities.Add(new Models.DeletedEntity { EntityType = "Foto", EntityId = foto.Id, DeletedAt = DateTime.UtcNow });
                    _db.Fotos.Remove(foto);
                }
                return new SyncOperationResultDto(op.LocalId, true, null, null);
            }
            default:
                return new SyncOperationResultDto(op.LocalId, true, null, null);
        }
    }

    // Payload deserialization models
    private record PuntoPayload(
        int? RemoteId, decimal Latitud, decimal Longitud,
        string? Nombre, string? Descripcion);

    private record FotoPayload(int? RemoteId, int PuntoLocalId, string NombreArchivo);
}
