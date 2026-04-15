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
    LaunchMode = LaunchMode.SingleTop,
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

    protected override void OnResume()
    {
        base.OnResume();
        // Resume el WebView de Android al volver de la cámara u otras Activities.
        // Sin esto, el BlazorWebView puede quedar en blanco al retornar al primer plano.
        ResumeWebView(Window?.DecorView as Android.Views.ViewGroup);
    }

    private static void ResumeWebView(Android.Views.ViewGroup? parent)
    {
        if (parent is null) return;
        for (var i = 0; i < parent.ChildCount; i++)
        {
            var child = parent.GetChildAt(i);
            if (child is Android.Webkit.WebView webView)
            {
                webView.OnResume();
                return;
            }
            if (child is Android.Views.ViewGroup group)
                ResumeWebView(group);
        }
    }
}
