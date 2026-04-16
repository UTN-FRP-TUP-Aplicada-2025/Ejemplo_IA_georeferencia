namespace GeoFoto.Api.Services;

public interface IFileStorageService
{
    Task<string> SaveAsync(Stream stream, string fileName);
    void Delete(string rutaFisica);
    string GetFullPath(string relativePath);
}

public class FileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public FileStorageService(IConfiguration configuration, IWebHostEnvironment env)
    {
        var configured = configuration["Storage:UploadPath"];
        _basePath = Path.IsPathRooted(configured ?? "")
            ? configured!
            : Path.Combine(env.ContentRootPath, configured ?? "wwwroot/uploads");
    }

    public async Task<string> SaveAsync(Stream stream, string fileName)
    {
        var now = DateTime.UtcNow;
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var relativePath = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"),
            $"{Guid.NewGuid()}{extension}");
        var fullPath = Path.Combine(_basePath, relativePath);

        var directory = Path.GetDirectoryName(fullPath)!;
        if (!System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(fullPath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        return relativePath;
    }

    public void Delete(string rutaFisica)
    {
        var fullPath = Path.Combine(_basePath, rutaFisica);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public string GetFullPath(string relativePath) => Path.Combine(_basePath, relativePath);
}
