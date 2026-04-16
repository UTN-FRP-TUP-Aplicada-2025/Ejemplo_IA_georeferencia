using SQLite;

namespace GeoFoto.Shared.Models;

[Table("Puntos_Local")]
public class LocalPunto
{
    [PrimaryKey, AutoIncrement] public int LocalId { get; set; }
    public int? RemoteId { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public double RadioMetros { get; set; } = 50;
    public bool IsDeleted { get; set; } = false;
    public string FechaCreacion { get; set; } = DateTime.UtcNow.ToString("O");
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string SyncStatus { get; set; } = SyncStatusValues.Local;
}

[Table("Fotos_Local")]
public class LocalFoto
{
    [PrimaryKey, AutoIncrement] public int LocalId { get; set; }
    public int? RemoteId { get; set; }
    public int PuntoLocalId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaLocal { get; set; } = string.Empty;
    public string? FechaTomada { get; set; }
    public long TamanoBytes { get; set; }
    public decimal? LatitudExif { get; set; }
    public decimal? LongitudExif { get; set; }
    public string? Comentario { get; set; }
    public bool IsDeleted { get; set; } = false;
    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("O");
    public string SyncStatus { get; set; } = SyncStatusValues.Local;
}

[Table("SyncQueue")]
public class SyncQueueItem
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int LocalId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = SyncQueueStatus.Pending;
    public int Attempts { get; set; }
    public string? LastAttemptAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("O");
}

public static class SyncStatusValues
{
    public const string Local = "Local";
    public const string Synced = "Synced";
    public const string PendingCreate = "PendingCreate";
    public const string PendingUpdate = "PendingUpdate";
    public const string PendingDelete = "PendingDelete";
    public const string Conflict = "Conflict";
    public const string Failed = "Failed";
}

public static class SyncQueueStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Done = "Done";
    public const string Failed = "Failed";
}
