using GeoFoto.Shared.Services;

namespace GeoFoto.Mobile.Services;

public class MauiPreferencesService : IPreferencesService
{
    public string Get(string key, string defaultValue) => Preferences.Get(key, defaultValue);
    public void Set(string key, string value) => Preferences.Set(key, value);
}
