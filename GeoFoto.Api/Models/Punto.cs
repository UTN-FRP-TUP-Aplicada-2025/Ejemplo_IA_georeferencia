namespace GeoFoto.Api.Models;

public class Punto
{
    public int Id { get; set; }
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Foto> Fotos { get; set; } = new List<Foto>();
}
