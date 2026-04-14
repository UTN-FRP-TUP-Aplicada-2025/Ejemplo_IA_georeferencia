using GeoFoto.Shared.Services;

namespace GeoFoto.Mobile.Services;

public class ConnectivityService : IConnectivityService, IDisposable
{
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public event EventHandler<bool>? ConnectivityChanged;

    public ConnectivityService()
    {
        Connectivity.ConnectivityChanged += OnChanged;
    }

    private void OnChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        ConnectivityChanged?.Invoke(this, IsConnected);
    }

    public void Dispose() => Connectivity.ConnectivityChanged -= OnChanged;
}
