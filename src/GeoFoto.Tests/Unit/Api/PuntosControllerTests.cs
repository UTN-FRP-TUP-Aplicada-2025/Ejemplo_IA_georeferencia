using FluentAssertions;
using GeoFoto.Api.Controllers;
using GeoFoto.Api.Dtos;
using GeoFoto.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GeoFoto.Tests.Unit.Api;

/// <summary>
/// Tests unitarios de PuntosController (TEST-052, 053, 054, 058, 059).
/// </summary>
public class PuntosControllerTests
{
    private readonly Mock<IPuntosService> _svcMock = new();

    private PuntosController CreateController() => new(_svcMock.Object);

    // ──────────────────────────────────────────
    // TEST-052: GET /api/puntos → retorna lista completa
    // ──────────────────────────────────────────
    [Fact]
    public async Task GetAll_ExistenPuntos_Retorna200ConLista()
    {
        // ARRANGE
        var puntos = new List<PuntoDto>
        {
            new(1, -34.60m, -58.38m, "P1", null, DateTime.UtcNow, 0, null),
            new(2, -34.61m, -58.39m, "P2", null, DateTime.UtcNow, 2, 1),
            new(3, -34.62m, -58.40m, "P3", null, DateTime.UtcNow, 1, 3)
        };
        _svcMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(puntos);

        var ctrl = CreateController();

        // ACT
        var actionResult = await ctrl.GetAll(default);
        var result = actionResult.Result as OkObjectResult;

        // ASSERT
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var lista = result.Value as IReadOnlyList<PuntoDto>;
        lista.Should().HaveCount(3);
    }

    // ──────────────────────────────────────────
    // TEST-053: GET /api/puntos/{id} → 200 con detalle
    // ──────────────────────────────────────────
    [Fact]
    public async Task GetById_PuntoExiste_Retorna200()
    {
        // ARRANGE
        var detalle = new PuntoDetalleDto(
            1, -34.60m, -58.38m, "P1", "Desc",
            DateTime.UtcNow, DateTime.UtcNow,
            new List<FotoDto>());
        _svcMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(detalle);

        var ctrl = CreateController();

        // ACT
        var actionResult = await ctrl.GetById(1, default);
        var result = actionResult.Result as OkObjectResult;

        // ASSERT
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetById_PuntoNoExiste_Retorna404()
    {
        // ARRANGE
        _svcMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((PuntoDetalleDto?)null);

        var ctrl = CreateController();

        // ACT
        var result = await ctrl.GetById(999, default);

        // ASSERT
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // ──────────────────────────────────────────
    // TEST-054: DELETE /api/puntos/{id} → 204 si existe, 404 si no
    // ──────────────────────────────────────────
    [Fact]
    public async Task Delete_PuntoExiste_Retorna204()
    {
        // ARRANGE
        _svcMock.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        var ctrl = CreateController();

        // ACT
        var result = await ctrl.Delete(1, default);

        // ASSERT
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_PuntoNoExiste_Retorna404()
    {
        // ARRANGE
        _svcMock.Setup(s => s.DeleteAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        var ctrl = CreateController();

        // ACT
        var result = await ctrl.Delete(999, default);

        // ASSERT
        result.Should().BeOfType<NotFoundResult>();
    }

    // ──────────────────────────────────────────
    // TEST-059: PUT /api/puntos/{id} → actualiza y retorna 204
    // ──────────────────────────────────────────
    [Fact]
    public async Task Update_PuntoExiste_Retorna204()
    {
        // ARRANGE
        var req = new ActualizarPuntoRequest("Nombre nuevo", "Desc nueva");
        _svcMock.Setup(s => s.UpdateAsync(1, req, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PuntoDto(1, -34.60m, -58.38m, "Nombre nuevo", "Desc nueva",
                    DateTime.UtcNow, 0, null));

        var ctrl = CreateController();

        // ACT
        var result = await ctrl.Update(1, req, default);

        // ASSERT
        result.Should().BeOfType<NoContentResult>();
    }
}
