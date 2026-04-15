namespace GeoFoto.Api.Models;

/// <summary>
/// Registro de entidades hard-deleted para sync bidireccional.
/// Permite que el Pull delta informe al cliente qué eliminar localmente.
/// </summary>
public class DeletedEntity
{
    public int Id { get; set; }
    public string EntityType { get; set; } = "";   // "Punto" | "Foto"
    public int EntityId { get; set; }               // PK original de la entidad
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
