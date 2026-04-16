using GeoFoto.Api.Data;
using GeoFoto.Api.Dtos;
using GeoFoto.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoFoto.Api.Services;

public interface IPuntosService
{
    Task<IReadOnlyList<PuntoDto>> GetAllAsync(CancellationToken ct = default);
    Task<PuntoDetalleDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PuntoDto?> UpdateAsync(int id, ActualizarPuntoRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    /// <summary>Retorna (zipBytes, nombreArchivo) o null si el punto no existe.
    /// Retorna array vacío si el punto existe pero no tiene fotos.</summary>
    Task<(byte[] ZipBytes, string FileName)?> DescargarFotosZipAsync(int id, CancellationToken ct = default);
}

public class PuntosService : IPuntosService
{
    private readonly GeoFotoDbContext _db;
    private readonly IFileStorageService _storage;

    public PuntosService(GeoFotoDbContext db, IFileStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<IReadOnlyList<PuntoDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Puntos
            .OrderByDescending(p => p.FechaCreacion)
            .Select(p => new PuntoDto(
                p.Id, p.Latitud, p.Longitud,
                p.Nombre, p.Descripcion, p.FechaCreacion,
                p.Fotos.Count,
                p.Fotos.OrderBy(f => f.Id).Select(f => (int?)f.Id).FirstOrDefault()))
            .ToListAsync(ct);
    }

    public async Task<PuntoDetalleDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var punto = await _db.Puntos
            .Include(p => p.Fotos)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (punto is null) return null;

        return new PuntoDetalleDto(
            punto.Id, punto.Latitud, punto.Longitud,
            punto.Nombre, punto.Descripcion, punto.FechaCreacion, punto.UpdatedAt,
            punto.Fotos.Select(f => new FotoDto(
                f.Id, f.PuntoId, f.NombreArchivo,
                f.FechaTomada, f.TamanoBytes,
                f.LatitudExif, f.LongitudExif)).ToList());
    }

    public async Task<PuntoDto?> UpdateAsync(int id, ActualizarPuntoRequest request, CancellationToken ct = default)
    {
        var punto = await _db.Puntos
            .Include(p => p.Fotos)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (punto is null) return null;

        punto.Nombre = request.Nombre;
        punto.Descripcion = request.Descripcion;
        punto.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new PuntoDto(
            punto.Id, punto.Latitud, punto.Longitud,
            punto.Nombre, punto.Descripcion, punto.FechaCreacion,
            punto.Fotos.Count,
            punto.Fotos.OrderBy(f => f.Id).Select(f => (int?)f.Id).FirstOrDefault());
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var punto = await _db.Puntos
            .Include(p => p.Fotos)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (punto is null) return false;

        foreach (var foto in punto.Fotos)
            _storage.Delete(foto.RutaFisica);

        _db.Puntos.Remove(punto);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(byte[] ZipBytes, string FileName)?> DescargarFotosZipAsync(
        int id, CancellationToken ct = default)
    {
        var punto = await _db.Puntos.FindAsync([id], ct);
        if (punto is null) return null;

        var fotos = await _db.Fotos
            .Where(f => f.PuntoId == id && !f.IsDeleted)
            .ToListAsync(ct);

        if (fotos.Count == 0)
            return ([], punto.Nombre ?? $"punto_{id}");

        using var ms = new System.IO.MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(
                   ms, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            var nombreBase = punto.Nombre ?? $"punto_{id}";
            foreach (var (foto, n) in fotos.Select((f, i) => (f, i + 1)))
            {
                var ext = Path.GetExtension(foto.NombreArchivo);
                var entryName = $"{nombreBase}_{n}{ext}";
                var entry = zip.CreateEntry(entryName);
                var fullPath = _storage.GetFullPath(foto.RutaFisica);
                if (!System.IO.File.Exists(fullPath)) continue;
                await using var entryStream = entry.Open();
                await using var fs = System.IO.File.OpenRead(fullPath);
                await fs.CopyToAsync(entryStream, ct);
            }
        }

        var fileName = $"{punto.Nombre ?? $"punto_{id}"}.zip";
        return (ms.ToArray(), fileName);
    }
}
