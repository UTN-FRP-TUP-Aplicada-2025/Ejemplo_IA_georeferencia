namespace GeoFoto.Shared.Services;

public interface IShareService
{
    Task ShareFileAsync(string title, string filePath);
}
