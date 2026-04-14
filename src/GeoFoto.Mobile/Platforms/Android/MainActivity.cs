using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui;

namespace GeoFoto.Mobile;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges =
        ConfigChanges.ScreenSize | ConfigChanges.Orientation |
        ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Edge-to-edge: el contenido se dibuja detrás de las barras del sistema
        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            Window!.SetDecorFitsSystemWindows(false);
        }
        else
        {
#pragma warning disable CA1422
            Window!.DecorView.SystemUiVisibility =
                (StatusBarVisibility)(
                    SystemUiFlags.LayoutStable |
                    SystemUiFlags.LayoutHideNavigation |
                    SystemUiFlags.LayoutFullscreen);
#pragma warning restore CA1422
        }

        // Barras del sistema semi-transparentes
        Window!.SetStatusBarColor(Android.Graphics.Color.Transparent);
        Window!.SetNavigationBarColor(Android.Graphics.Color.Transparent);
    }
}
