namespace GeoFoto.Shared.Services;

public interface ICamaraService
{
    Task<Stream?> TomarFotoAsync();
    Task<Stream?> ElegirDeGaleriaAsync();
}
