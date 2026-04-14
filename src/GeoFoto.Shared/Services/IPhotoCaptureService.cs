namespace GeoFoto.Shared.Services;

public record PhotoCaptureResult(
    string FilePath,
    decimal? LatitudExif,
    decimal? LongitudExif,
    long SizeBytes);

public interface IPhotoCaptureService
{
    Task<PhotoCaptureResult?> CapturePhotoAsync();
}
