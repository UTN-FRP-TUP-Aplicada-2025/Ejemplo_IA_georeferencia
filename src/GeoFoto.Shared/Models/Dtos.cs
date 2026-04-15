namespace GeoFoto.Shared.Models;

public record PuntoDto(int Id, decimal Latitud, decimal Longitud,
    string? Nombre, string? Descripcion, DateTime FechaCreacion,
    int CantidadFotos, int? PrimeraFotoId,
    string? SyncStatus = null);

public record PuntoDetalleDto(
    int Id, decimal Latitud, decimal Longitud,
    string? Nombre, string? Descripcion,
    DateTime FechaCreacion, List<FotoDto> Fotos);

public record FotoDto(
    int Id, int PuntoId, string NombreArchivo,
    DateTime? FechaTomada, long TamanoBytes,
    decimal? LatitudExif, decimal? LongitudExif);

public record UploadResultDto(
    int PuntoId, int FotoId, string NombreArchivo,
    bool TeniaGeo, decimal? Latitud, decimal? Longitud);

public record ActualizarPuntoRequest(string? Nombre, string? Descripcion);

// Sync DTOs
public record SyncOperationDto(string OperationType, string EntityType,
    int LocalId, string Payload);

public record SyncOperationResultDto(int LocalId, bool Success,
    int? RemoteId, string? Error);

public record BatchResultDto(IReadOnlyList<SyncOperationResultDto> Results);

public record SyncDeltaDto(IReadOnlyList<PuntoDto> Puntos, IReadOnlyList<FotoDto> Fotos);
