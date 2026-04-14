using GeoFoto.Shared.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace GeoFoto.Mobile.Services;

public class MauiUbicacionService : IUbicacionService
{
    public async Task<UbicacionResult> ObtenerUbicacionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await MainThread.InvokeOnMainThreadAsync(
                    () => Permissions.RequestAsync<Permissions.LocationWhenInUse>());
            }

            if (status != PermissionStatus.Granted)
            {
                return new UbicacionResult(false, 0, 0, 0,
                    "Permiso de ubicación denegado", PermisoDenegado: true);
            }

            // Intentar posición cacheada primero — respuesta instantánea para la carga inicial del mapa
            var lastKnown = await Geolocation.Default.GetLastKnownLocationAsync();
            if (lastKnown is not null && lastKnown.Accuracy is <= 200)
            {
                return new UbicacionResult(true,
                    lastKnown.Latitude, lastKnown.Longitude, lastKnown.Accuracy ?? 50);
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location is null)
            {
                return new UbicacionResult(false, 0, 0, 0,
                    "No se pudo obtener la ubicación");
            }

            return new UbicacionResult(true,
                location.Latitude,
                location.Longitude,
                location.Accuracy ?? 0);
        }
        catch (PermissionException)
        {
            return new UbicacionResult(false, 0, 0, 0,
                "Permiso de ubicación denegado", PermisoDenegado: true);
        }
        catch (FeatureNotSupportedException)
        {
            return new UbicacionResult(false, 0, 0, 0,
                "GPS no soportado en este dispositivo");
        }
        catch (Exception ex)
        {
            return new UbicacionResult(false, 0, 0, 0, ex.Message);
        }
    }

    public Task AbrirConfiguracionApp()
    {
        AppInfo.Current.ShowSettingsUI();
        return Task.CompletedTask;
    }
}
