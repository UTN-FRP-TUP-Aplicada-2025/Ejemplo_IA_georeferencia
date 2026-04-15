using GeoFoto.Shared.Services;

namespace GeoFoto.Mobile.Services;

public class MauiAppConfigService : IAppConfigService
{
    private const string RadioKey = "RadioAgrupacionMetros";

    public Task<int> GetRadioAgrupacionMetrosAsync()
    {
        var valor = Preferences.Default.Get(RadioKey, 50);
        return Task.FromResult(valor);
    }

    public Task SetRadioAgrupacionMetrosAsync(int metros)
    {
        Preferences.Default.Set(RadioKey, metros);
        return Task.CompletedTask;
    }
}
