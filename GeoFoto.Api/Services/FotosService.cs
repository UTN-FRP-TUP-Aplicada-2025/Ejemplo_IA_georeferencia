using GeoFoto.Api.Data;
using GeoFoto.Api.Dtos;
using GeoFoto.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoFoto.Api.Services;

public interface IFotosService
{
    Task<UploadResultDto> UploadAsync(IFormFile file, CancellationToken ct = default);
    Task<IReadOnlyList<FotoDto>> GetByPuntoIdAsync(int puntoId, CancellationToken ct = default);
    Task<Foto?> GetFotoAsync(int id, CancellationToken ct = default);
}

public class FotosService : IFotosService
{
    private readonly GeoFotoDbContext _db;
    private readonly IExifService _exif;
    private readonly IFileStorageService _storage;

    public FotosService(GeoFotoDbContext db, IExifService exif, IFileStorageService storage)
    {
        _db = db;
        _exif = exif;
        _storage = storage;
    }

    public async Task<UploadResultDto> UploadAsync(IFormFile file, CancellationToken ct = default)
    {
        using var stream = file.OpenReadStream();
        var geo = _exif.ExtractGeoData(stream);

        stream.Position = 0;
        var rutaFisica = await _storage.SaveAsync(stream, file.FileName);

        Punto? punto = null;

        if (geo.Latitud.HasValue && geo.Longitud.HasValue)
        {
            var lat = geo.Latitud.Value;
            var lng = geo.Longitud.Value;

            punto = await _db.Puntos
                .FirstOrDefaultAsync(p =>
                    Math.Abs(p.Latitud - lat) < 0.001m &&
                    Math.Abs(p.Longitud - lng) < 0.001m, ct);
        }

        if (punto is null)
        {
            var now = DateTime.UtcNow;
            punto = new Punto
            {
                Latitud = geo.Latitud ?? 0m,
                Longitud = geo.Longitud ?? 0m,
                FechaCreacion = now,
                UpdatedAt = now
            };
            _db.Puntos.Add(punto);
            await _db.SaveChangesAsync(ct);
        }

        var foto = new Foto
        {
            PuntoId = punto.Id,
            NombreArchivo = file.FileName,
            RutaFisica = rutaFisica,
            FechaTomada = geo.FechaTomada,
            TamanoBytes = file.Length,
            LatitudExif = geo.Latitud,
            LongitudExif = geo.Longitud,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Fotos.Add(foto);
        await _db.SaveChangesAsync(ct);

        return new UploadResultDto(
            punto.Id, foto.Id, file.FileName,
            geo.Latitud.HasValue, geo.Latitud, geo.Longitud);
    }

    public async Task<IReadOnlyList<FotoDto>> GetByPuntoIdAsync(int puntoId, CancellationToken ct = default)
    {
        return await _db.Fotos
            .Where(f => f.PuntoId == puntoId)
            .Select(f => new FotoDto(
                f.Id, f.PuntoId, f.NombreArchivo,
                f.FechaTomada, f.TamanoBytes,
                f.LatitudExif, f.LongitudExif))
            .ToListAsync(ct);
    }

    public async Task<Foto?> GetFotoAsync(int id, CancellationToken ct = default)
    {
        return await _db.Fotos.FindAsync([id], ct);
    }
}
