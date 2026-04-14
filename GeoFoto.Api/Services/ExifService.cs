using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace GeoFoto.Api.Services;

public record ExifGeoData(decimal? Latitud, decimal? Longitud, DateTime? FechaTomada);

public interface IExifService
{
    ExifGeoData ExtractGeoData(Stream stream);
}

public class ExifService : IExifService
{
    public ExifGeoData ExtractGeoData(Stream stream)
    {
        try
        {
            var directories = ImageMetadataReader.ReadMetadata(stream);

            decimal? latitud = null;
            decimal? longitud = null;
            DateTime? fechaTomada = null;

            var gpsDir = directories.OfType<GpsDirectory>().FirstOrDefault();
            if (gpsDir is not null)
            {
                var location = gpsDir.GetGeoLocation();
                if (location is { } loc)
                {
                    latitud = (decimal)loc.Latitude;
                    longitud = (decimal)loc.Longitude;
                }
            }

            var exifSubIfd = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exifSubIfd is not null)
            {
                if (exifSubIfd.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var fecha))
                {
                    fechaTomada = fecha;
                }
            }

            return new ExifGeoData(latitud, longitud, fechaTomada);
        }
        catch
        {
            return new ExifGeoData(null, null, null);
        }
    }
}
