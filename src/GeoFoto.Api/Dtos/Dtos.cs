namespace GeoFoto.Api.Dtos;

public record PuntoDto(int Id, decimal Latitud, decimal Longitud,
    string? Nombre, string? Descripcion, DateTime FechaCreacion,
    int CantidadFotos, int? PrimeraFotoId,
    DateTime? UpdatedAt = null,
    double RadioMetros = 50,
    bool IsDeleted = false);

public record PuntoDetalleDto(int Id, decimal Latitud, decimal Longitud,
    string? Nombre, string? Descripcion, DateTime FechaCreacion, DateTime UpdatedAt,
    IReadOnlyList<FotoDto> Fotos,
    double RadioMetros = 50,
    bool IsDeleted = false);

public record FotoDto(int Id, int PuntoId, string NombreArchivo,
    DateTime? FechaTomada, long TamanoBytes, decimal? LatitudExif,
    decimal? LongitudExif,
    string? Comentario = null,
    bool IsDeleted = false);

public record UploadResultDto(int PuntoId, int FotoId, string NombreArchivo,
    bool TeniaGps, decimal? Latitud, decimal? Longitud);

public record ActualizarPuntoRequest(string? Nombre, string? Descripcion,
    double? RadioMetros = null, DateTime? UpdatedAt = null);

public record ActualizarFotoRequest(string? Comentario, DateTime? UpdatedAt = null);

// Sync DTOs
public record SyncOperationDto(string OperationType, string EntityType,
    int LocalId, string Payload);

public record SyncOperationResultDto(int LocalId, bool Success,
    int? RemoteId, string? Error);

public record BatchResultDto(IReadOnlyList<SyncOperationResultDto> Results);

public record SyncDeltaDto(IReadOnlyList<PuntoDto> Puntos, IReadOnlyList<FotoDto> Fotos,
    IReadOnlyList<DeletedEntityDto>? Eliminados = null);

public record DeletedEntityDto(string EntityType, int EntityId);
