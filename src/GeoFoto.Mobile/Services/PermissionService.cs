using GeoFoto.Shared.Services;

namespace GeoFoto.Mobile.Services;

public enum PermissionResult
{
    Granted,
    Denied,
    PermanentlyDenied
}

public interface IPermissionService
{
    Task<PermissionResult> CheckAndRequestLocationAsync();
    Task<PermissionResult> CheckAndRequestCameraAsync();
    void OpenAppSettings();
}

public class PermissionService : IPermissionService
{
    public async Task<PermissionResult> CheckAndRequestLocationAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted) return PermissionResult.Granted;

        if (status == PermissionStatus.Denied &&
            !Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
            return PermissionResult.PermanentlyDenied;

        status = await MainThread.InvokeOnMainThreadAsync(
            () => Permissions.RequestAsync<Permissions.LocationWhenInUse>());

        return status switch
        {
            PermissionStatus.Granted => PermissionResult.Granted,
            PermissionStatus.Denied when !Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>()
                => PermissionResult.PermanentlyDenied,
            _ => PermissionResult.Denied
        };
    }

    public async Task<PermissionResult> CheckAndRequestCameraAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status == PermissionStatus.Granted) return PermissionResult.Granted;

        if (status == PermissionStatus.Denied &&
            !Permissions.ShouldShowRationale<Permissions.Camera>())
            return PermissionResult.PermanentlyDenied;

        status = await MainThread.InvokeOnMainThreadAsync(
            () => Permissions.RequestAsync<Permissions.Camera>());

        return status switch
        {
            PermissionStatus.Granted => PermissionResult.Granted,
            PermissionStatus.Denied when !Permissions.ShouldShowRationale<Permissions.Camera>()
                => PermissionResult.PermanentlyDenied,
            _ => PermissionResult.Denied
        };
    }

    public void OpenAppSettings() => AppInfo.Current.ShowSettingsUI();
}
