using FluentAssertions;
using GeoFoto.Api.Controllers;
using GeoFoto.Api.Dtos;
using GeoFoto.Api.Models;
using GeoFoto.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GeoFoto.Tests.Unit.Api;

/// <summary>
/// Tests unitarios de FotosController.
/// </summary>
public class FotosControllerTests
{
    private readonly Mock<IFotosService> _svcMock = new();

    private FotosController CreateController() => new(_svcMock.Object);

    // ──────────────────────────────────────────
    // TEST-055: POST foto → 201 Created con datos EXIF
    // ──────────────────────────────────────────
    [Fact]
    public async Task Upload_ArchivoValido_Retorna200ConDto()
    {
        // ARRANGE
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("foto.jpg");
        fileMock.Setup(f => f.Length).Returns(102400);
        fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[100]));

        var uploadResult = new UploadResultDto(1, 1, "foto.jpg", true, -34.60m, -58.38m);
        _svcMock.Setup(s => s.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(uploadResult);

        var ctrl = CreateController();

        // ACT
        var actionResult = await ctrl.Upload(fileMock.Object, default);
        var result = actionResult.Result as OkObjectResult;

        // ASSERT
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);
        var dto = result.Value as UploadResultDto;
        dto.Should().NotBeNull();
        dto!.TeniaGps.Should().BeTrue();
        dto.Latitud.Should().Be(-34.60m);
    }

    [Fact]
    public async Task Upload_ArchivoNulo_Retorna400()
    {
        // ARRANGE
        var ctrl = CreateController();

        // ACT
        var actionResult = await ctrl.Upload(null!, default);

        // ASSERT
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Upload_ExtensionNoSoportada_Retorna400()
    {
        // ARRANGE
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("foto.bmp");  // bmp no soportado
        fileMock.Setup(f => f.Length).Returns(5000);

        var ctrl = CreateController();

        // ACT
        var actionResult = await ctrl.Upload(fileMock.Object, default);

        // ASSERT
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ──────────────────────────────────────────
    // POST /api/fotos/upload-to-punto/{id} → 201 Created
    // ──────────────────────────────────────────
    [Fact]
    public async Task UploadToPunto_Valido_Retorna201()
    {
        // ARRANGE
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("foto.jpg");
        fileMock.Setup(f => f.Length).Returns(80000);
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[100]));

        var fotoDto = new FotoDto(1, 5, "foto.jpg", DateTime.UtcNow, 80000, null, null);
        _svcMock.Setup(s => s.UploadToPuntoAsync(5, It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fotoDto);

        var ctrl = CreateController();

        // ACT
        var actionResult = await ctrl.UploadToPunto(5, fileMock.Object, default);
        var result = actionResult.Result;

        // ASSERT
        result.Should().BeOfType<CreatedResult>()
            .Which.StatusCode.Should().Be(201);
    }

    // GET /api/fotos/imagen/{id} → 404 si no existe
    [Fact]
    public async Task GetImagen_FotoNoExiste_Retorna404()
    {
        // ARRANGE
        _svcMock.Setup(s => s.GetFotoAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Foto?)null);

        var ctrl = CreateController();

        // ACT
        var result = await ctrl.GetImagen(999, default);

        // ASSERT
        result.Should().BeOfType<NotFoundResult>();
    }
}
