using System.Text.Json;
using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Microsoft.AspNetCore.Components.Forms;

namespace GeoFoto.Mobile.Services;

public class LocalUploadStrategy : IFotoUploadStrategy
{
    private readonly ILocalDbService _db;

    public LocalUploadStrategy(ILocalDbService db) => _db = db;

    public async Task<UploadResultDto?> SubirAsync(IBrowserFile file, CancellationToken ct = default)
    {
        using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
        return await SubirAsync(stream, file.Name, ct);
    }

    public async Task<UploadResultDto?> SubirAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        await _db.InitializeAsync();

        // 1. Save file locally
        var photosDir = Path.Combine(FileSystem.Current.AppDataDirectory, "photos");
        System.IO.Directory.CreateDirectory(photosDir);

        var ext = Path.GetExtension(fileName);
        var localFileName = $"{Guid.NewGuid()}{ext}";
        var localPath = Path.Combine(photosDir, localFileName);

        // Copy stream to memory for EXIF extraction and file saving
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var fileSize = ms.Length;

        // Save to disk
        ms.Position = 0;
        using (var fs = File.Create(localPath))
        {
            await ms.CopyToAsync(fs, ct);
        }

        // 2. Extract GPS EXIF
        ms.Position = 0;
        var (lat, lng, dateTaken) = ExtractExif(ms);
        var teniaGeo = lat.HasValue && lng.HasValue;

        // 3. Find or create local punto
        LocalPunto? punto = null;
        if (teniaGeo)
            punto = await _db.FindPuntoCercanoAsync(lat!.Value, lng!.Value);

        int puntoLocalId;
        if (punto is null)
        {
            punto = new LocalPunto
            {
                Latitud = lat ?? 0,
                Longitud = lng ?? 0,
                Nombre = teniaGeo ? null : fileName,
                SyncStatus = SyncStatusValues.PendingCreate
            };
            puntoLocalId = await _db.InsertPuntoAsync(punto);
        }
        else
        {
            puntoLocalId = punto.LocalId;
        }

        // 4. Create local foto
        var foto = new LocalFoto
        {
            PuntoLocalId = puntoLocalId,
            NombreArchivo = fileName,
            RutaLocal = localPath,
            FechaTomada = dateTaken?.ToString("O"),
            TamanoBytes = fileSize,
            LatitudExif = lat,
            LongitudExif = lng,
            SyncStatus = SyncStatusValues.PendingCreate
        };
        var fotoLocalId = await _db.InsertFotoAsync(foto);

        // 5. Enqueue sync operations
        await _db.EnqueueAsync("Create", "Punto", puntoLocalId,
            JsonSerializer.Serialize(punto));
        await _db.EnqueueAsync("Create", "Foto", fotoLocalId,
            JsonSerializer.Serialize(foto));

        return new UploadResultDto(
            puntoLocalId, fotoLocalId, fileName,
            teniaGeo, lat, lng);
    }

    private static (decimal? lat, decimal? lng, DateTime? date) ExtractExif(Stream stream)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(stream);
            var gps = directories.OfType<GpsDirectory>().FirstOrDefault();
            var exif = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

            decimal? lat = null, lng = null;
            var location = gps?.GetGeoLocation();
            if (location is { } loc)
            {
                lat = (decimal)loc.Latitude;
                lng = (decimal)loc.Longitude;
            }

            DateTime? date = null;
            if (exif?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dt) == true)
                date = dt;

            return (lat, lng, date);
        }
        catch
        {
            return (null, null, null);
        }
    }
}
