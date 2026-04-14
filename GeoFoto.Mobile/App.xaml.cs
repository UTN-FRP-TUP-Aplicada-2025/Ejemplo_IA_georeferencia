using GeoFoto.Shared.Services;

namespace GeoFoto.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Start background sync after window creation
		var window = new Window(new MainPage()) { Title = "GeoFoto.Mobile" };

		window.Created += async (_, _) =>
		{
			var syncService = Handler?.MauiContext?.Services.GetService<ISyncService>();
			if (syncService is not null)
			{
				try { await syncService.StartBackgroundSyncAsync(); }
				catch { /* will retry on next connectivity change */ }
			}
		};

		return window;
	}
}
