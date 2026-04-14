namespace GeoFoto.Shared.Services;

public record UbicacionResult(
    bool Ok,
    double Latitud,
    double Longitud,
    double Precision,
    string? Error = null,
    bool PermisoDenegado = false);

public interface IUbicacionService
{
    Task<UbicacionResult> ObtenerUbicacionAsync();
    Task AbrirConfiguracionApp();
}
