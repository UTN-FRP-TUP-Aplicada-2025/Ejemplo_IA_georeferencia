using GeoFoto.Mobile.Services;
using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace GeoFoto.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddMudServices();

		builder.Services.AddHttpClient<IGeoFotoApiClient, GeoFotoApiClient>(c =>
		{
			c.BaseAddress = new Uri("http://localhost:5000/");
			c.Timeout = TimeSpan.FromSeconds(60);
		});

		builder.Services.AddScoped<ICamaraService, CamaraService>();
		builder.Services.AddScoped<IShareService, MauiShareService>();
		builder.Services.AddScoped<IUbicacionService, MauiUbicacionService>();
		builder.Services.AddScoped<IPermissionService, PermissionService>();
		builder.Services.AddSingleton<IAppConfigService, MauiAppConfigService>();
		builder.Services.AddSingleton<IRadioAssociationService>(_ =>
		{
			var svc = new RadioAssociationService();
			svc.Configure(new AppConfig());
			return svc;
		});

		builder.Services.AddSingleton<ILocalDbService>(_ =>
			new LocalDbService(Path.Combine(FileSystem.Current.AppDataDirectory, "geofoto.db3")));
		builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
		builder.Services.AddSingleton<IPreferencesService, MauiPreferencesService>();
		builder.Services.AddScoped<IFotoUploadStrategy, LocalUploadStrategy>();
		builder.Services.AddSingleton<ISyncService, SyncService>();
		builder.Services.AddSingleton<SyncBackgroundService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
