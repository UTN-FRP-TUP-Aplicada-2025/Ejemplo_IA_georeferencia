namespace GeoFoto.Shared.Services;

public enum LocationPermissionStatus
{
    Unknown,
    Granted,
    Denied,
    DeniedPermanently
}

public interface ILocationPermissionService
{
    Task<LocationPermissionStatus> CheckAndRequestLocationPermissionAsync();
    bool IsPermanentlyDenied();
    void OpenAppSettings();
}
