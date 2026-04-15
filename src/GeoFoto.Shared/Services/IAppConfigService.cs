namespace GeoFoto.Shared.Services;

public interface IAppConfigService
{
    Task<int> GetRadioAgrupacionMetrosAsync();
    Task SetRadioAgrupacionMetrosAsync(int metros);
}
