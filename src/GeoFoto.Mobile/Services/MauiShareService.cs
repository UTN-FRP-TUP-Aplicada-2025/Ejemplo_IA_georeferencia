using GeoFoto.Shared.Services;

namespace GeoFoto.Mobile.Services;

public class MauiShareService : IShareService
{
    public Task ShareFileAsync(string title, string filePath)
        => Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(filePath)
        });
}
