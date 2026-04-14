using GeoFoto.Api.Dtos;
using GeoFoto.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeoFoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PuntosController : ControllerBase
{
    private readonly IPuntosService _svc;

    public PuntosController(IPuntosService svc) => _svc = svc;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PuntoDto>>> GetAll(CancellationToken ct)
        => Ok(await _svc.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PuntoDetalleDto>> GetById(int id, CancellationToken ct)
    {
        var dto = await _svc.GetByIdAsync(id, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ActualizarPuntoRequest req, CancellationToken ct)
    {
        var result = await _svc.UpdateAsync(id, req, ct);
        return result is not null ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var ok = await _svc.DeleteAsync(id, ct);
        return ok ? NoContent() : NotFound();
    }
}
