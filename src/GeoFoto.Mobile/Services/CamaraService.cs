using GeoFoto.Shared.Services;

namespace GeoFoto.Mobile;

public class CamaraService : ICamaraService
{
    public async Task<Stream?> TomarFotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported) return null;
        var foto = await MediaPicker.Default.CapturePhotoAsync();
        return foto is null ? null : await foto.OpenReadAsync();
    }

    public async Task<Stream?> ElegirDeGaleriaAsync()
    {
        var fotos = await MediaPicker.Default.PickPhotosAsync();
        var foto = fotos?.FirstOrDefault();
        return foto is null ? null : await foto.OpenReadAsync();
    }
}
