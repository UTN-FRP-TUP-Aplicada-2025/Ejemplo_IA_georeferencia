using GeoFoto.Api.Dtos;
using GeoFoto.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeoFoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FotosController : ControllerBase
{
    private readonly IFotosService _svc;

    public FotosController(IFotosService svc) => _svc = svc;

    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<UploadResultDto>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No se envió archivo.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".jpg" and not ".jpeg" and not ".png")
            return BadRequest("Formato no soportado. Solo JPG/PNG.");

        var result = await _svc.UploadAsync(file, ct);
        return Ok(result);
    }

    [HttpPost("upload-to-punto/{puntoId:int}")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<FotoDto>> UploadToPunto(int puntoId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No se envió archivo.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not ".jpg" and not ".jpeg" and not ".png" and not ".webp")
            return BadRequest("Formato no soportado.");

        var dto = await _svc.UploadToPuntoAsync(puntoId, file, ct);
        return Created($"/api/fotos/{puntoId}", dto);
    }

    [HttpGet("{puntoId:int}")]
    public async Task<ActionResult<IReadOnlyList<FotoDto>>> GetByPunto(int puntoId, CancellationToken ct)
        => Ok(await _svc.GetByPuntoIdAsync(puntoId, ct));

    [HttpGet("imagen/{id:int}")]
    public async Task<IActionResult> GetImagen(int id, CancellationToken ct)
    {
        var foto = await _svc.GetFotoAsync(id, ct);
        if (foto is null) return NotFound();

        var path = foto.RutaFisica;
        if (!System.IO.File.Exists(path))
            return NotFound("Archivo no encontrado en disco.");

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            _ => "image/jpeg"
        };

        return PhysicalFile(path, contentType);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await _svc.DeleteFotoAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}
