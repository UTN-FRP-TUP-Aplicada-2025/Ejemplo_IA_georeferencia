namespace GeoFoto.Api.Models;

public class Foto
{
    public int Id { get; set; }
    public int PuntoId { get; set; }
    public string NombreArchivo { get; set; } = string.Empty;
    public string RutaFisica { get; set; } = string.Empty;
    public DateTime? FechaTomada { get; set; }
    public long TamanoBytes { get; set; }
    public decimal? LatitudExif { get; set; }
    public decimal? LongitudExif { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Punto Punto { get; set; } = null!;
}
