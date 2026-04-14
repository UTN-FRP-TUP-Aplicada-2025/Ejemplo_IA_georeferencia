using FluentAssertions;
using GeoFoto.Api.Controllers;
using GeoFoto.Api.Dtos;
using GeoFoto.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GeoFoto.Tests.Unit.Api;

/// <summary>
/// Tests unitarios de SyncController (TEST-056, 057).
/// </summary>
public class SyncControllerTests
{
    private readonly Mock<ISyncApiService> _svcMock = new();

    private SyncController CreateController() => new(_svcMock.Object);

    // ──────────────────────────────────────────
    // TEST-056: POST /api/sync/batch → procesa lote y retorna mapeo
    // ──────────────────────────────────────────
    [Fact]
    public async Task ProcessBatch_OperacionesValidas_Retorna200ConResultados()
    {
        // ARRANGE
        var ops = new List<SyncOperationDto>
        {
            new("Create", "Punto",  1, "{\"nombre\":\"P1\"}"),
            new("Create", "Foto",   2, "{\"puntoLocalId\":1}"),
            new("Update", "Punto",  1, "{\"nombre\":\"P1 upd\"}")
        };
        var batchResult = new BatchResultDto(
        [
            new SyncOperationResultDto(1, true, 101, null),
            new SyncOperationResultDto(2, true, 201, null),
            new SyncOperationResultDto(1, true, 101, null)
        ]);
        _svcMock.Setup(s => s.ProcessBatchAsync(It.IsAny<IReadOnlyList<SyncOperationDto>>(),
                                                  It.IsAny<CancellationToken>()))
                .ReturnsAsync(batchResult);

        var ctrl = CreateController();

        // ACT
        var actionResult = await ctrl.ProcessBatch(ops, default);
        var result = actionResult.Result as OkObjectResult;

        // ASSERT
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var dto = result.Value as BatchResultDto;
        dto.Should().NotBeNull();
        dto!.Results.Should().HaveCount(3);
        dto.Results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public async Task ProcessBatch_ListaVacia_Retorna400()
    {
        // ARRANGE
        var ctrl = CreateController();

        // ACT
        var actionResult = await ctrl.ProcessBatch(new List<SyncOperationDto>(), default);

        // ASSERT
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ──────────────────────────────────────────
    // TEST-057: GET /api/sync/delta?since= → retorna cambios desde fecha
    // ──────────────────────────────────────────
    [Fact]
    public async Task GetDelta_ConFechaSince_RetornaCambiosCorrectamente()
    {
        // ARRANGE
        const string since  = "2026-04-10T00:00:00Z";
        var puntosNuevos    = new List<PuntoDto>
        {
            new(1, -34.60m, -58.38m, "Nuevo", null, DateTime.UtcNow, 0, null)
        };
        var delta = new SyncDeltaDto(puntosNuevos, []);
        _svcMock.Setup(s => s.GetDeltaAsync(since, It.IsAny<CancellationToken>()))
                .ReturnsAsync(delta);

        var ctrl = CreateController();

        // ACT
        var actionResult = await ctrl.GetDelta(since, default);
        var result = actionResult.Result as OkObjectResult;

        // ASSERT
        result.Should().NotBeNull();
        var dto = result!.Value as SyncDeltaDto;
        dto.Should().NotBeNull();
        dto!.Puntos.Should().HaveCount(1);
        dto.Puntos[0].Nombre.Should().Be("Nuevo");
    }

    [Fact]
    public async Task GetDelta_SinFecha_RetornaTodosLosPuntos()
    {
        // ARRANGE
        var allPuntos = new List<PuntoDto>
        {
            new(1, -34.60m, -58.38m, "A", null, DateTime.UtcNow, 0, null),
            new(2, -34.61m, -58.39m, "B", null, DateTime.UtcNow, 1, 1),
        };
        _svcMock.Setup(s => s.GetDeltaAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SyncDeltaDto(allPuntos, []));

        var ctrl = CreateController();

        // ACT
        var actionResult = await ctrl.GetDelta(null, default);
        var result = actionResult.Result as OkObjectResult;
        var dto    = result!.Value as SyncDeltaDto;

        // ASSERT
        dto!.Puntos.Should().HaveCount(2);
    }
}
