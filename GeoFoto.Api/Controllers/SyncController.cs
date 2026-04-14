using GeoFoto.Api.Dtos;
using GeoFoto.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeoFoto.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly ISyncApiService _svc;

    public SyncController(ISyncApiService svc) => _svc = svc;

    [HttpGet("delta")]
    public async Task<ActionResult<SyncDeltaDto>> GetDelta(
        [FromQuery] string? since, CancellationToken ct)
    {
        var delta = await _svc.GetDeltaAsync(since, ct);
        return Ok(delta);
    }

    [HttpPost("batch")]
    public async Task<ActionResult<BatchResultDto>> ProcessBatch(
        [FromBody] IReadOnlyList<SyncOperationDto> operations, CancellationToken ct)
    {
        if (operations is null || operations.Count == 0)
            return BadRequest("No se enviaron operaciones.");

        var result = await _svc.ProcessBatchAsync(operations, ct);
        return Ok(result);
    }
}
