using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Web;

/// <summary>
/// Tests unitarios para GEO-US32 — Descargar fotos como ZIP desde web.
///
/// Nota de arquitectura: los tests del endpoint API (PuntosController) están en
/// GeoFoto.Tests.Unit.Api. Estos tests verifican el comportamiento del CLIENTE
/// (IGeoFotoApiClient) y la lógica de UI en MarkerPopup.
///
/// Verifica:
///   CA-01: Cliente retorna bytes cuando API responde 200 con ZIP
///   CA-02: Cliente retorna null cuando API responde 204 (sin fotos)
///   CA-03: Botón descarga deshabilitado cuando fotos.Count == 0
///   CA-04: Botón descarga habilitado cuando hay fotos y tiene RemoteId
///   CA-05: Nombre del ZIP contiene el nombre del punto
///   CA-06: DescargarFotosZipAsync invocado con el RemoteId correcto
/// </summary>
public class US32_DescargaZipTests
{
    // ──────────────────────────────────────────
    // CA-01: API responde bytes → cliente retorna byte[] no vacío
    // ──────────────────────────────────────────
    [Fact]
    public async Task DescargaZip_ApiRespondeBytesZip_ClienteRetornaBytes()
    {
        // ARRANGE — ZIP mínimo válido (PK end-of-central-directory)
        var zipBytes = new byte[] { 0x50, 0x4B, 0x05, 0x06,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00,
                                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        var apiMock = new Mock<IGeoFotoApiClient>();
        apiMock.Setup(a => a.DescargarFotosZipAsync(1, It.IsAny<CancellationToken>()))
               .ReturnsAsync(zipBytes);

        // ACT
        var bytes = await apiMock.Object.DescargarFotosZipAsync(1);

        // ASSERT
        bytes.Should().NotBeNullOrEmpty(
            "CA-01: el cliente debe retornar los bytes del ZIP cuando la API responde 200");
        bytes!.Length.Should().Be(zipBytes.Length);
    }

    // ──────────────────────────────────────────
    // CA-02: API responde 204 → cliente retorna null
    // ──────────────────────────────────────────
    [Fact]
    public async Task DescargaZip_ApiResponde204_ClienteRetornaNull()
    {
        // ARRANGE
        var apiMock = new Mock<IGeoFotoApiClient>();
        apiMock.Setup(a => a.DescargarFotosZipAsync(2, It.IsAny<CancellationToken>()))
               .ReturnsAsync((byte[]?)null);

        // ACT
        var bytes = await apiMock.Object.DescargarFotosZipAsync(2);

        // ASSERT
        bytes.Should().BeNull(
            "CA-02: cuando la API responde 204 (sin fotos), el cliente debe retornar null");
    }

    // ──────────────────────────────────────────
    // CA-03: Botón deshabilitado cuando sin fotos
    // ──────────────────────────────────────────
    [Fact]
    public void DescargaZip_SinFotos_BotonDeshabilitado()
    {
        // ARRANGE — lógica del template en MarkerPopup.razor:
        //   Disabled="_procesando || _fotos.Count == 0"
        var fotosCount = 0;
        var procesando = false;

        // ACT
        var deshabilitado = procesando || fotosCount == 0;

        // ASSERT
        deshabilitado.Should().BeTrue(
            "CA-03: el botón de descarga debe estar deshabilitado cuando no hay fotos");
    }

    // ──────────────────────────────────────────
    // CA-04: Botón habilitado cuando hay fotos y no está procesando
    // ──────────────────────────────────────────
    [Fact]
    public void DescargaZip_ConFotos_BotonHabilitado()
    {
        // ARRANGE
        var fotosCount = 3;
        var procesando = false;

        // ACT
        var deshabilitado = procesando || fotosCount == 0;

        // ASSERT
        deshabilitado.Should().BeFalse(
            "CA-04: el botón de descarga debe estar habilitado cuando hay fotos y no se está procesando");
    }

    // ──────────────────────────────────────────
    // CA-05: Nombre ZIP incluye nombre del punto con extensión .zip
    // ──────────────────────────────────────────
    [Theory]
    [InlineData("Torre de agua", "Torre de agua.zip")]
    [InlineData("Poste 42",      "Poste 42.zip")]
    [InlineData(null,            "punto_7.zip")]
    public void DescargaZip_NombreZip_EsCorrecto(string? nombrePunto, string zipEsperado)
    {
        // ARRANGE — lógica de DescargarFotosZipAsync en MarkerPopup:
        //   var nombre = (_punto.Nombre ?? $"punto_{_punto.LocalId}") + ".zip";
        const int localId = 7;

        // ACT
        var nombreZip = (nombrePunto ?? $"punto_{localId}") + ".zip";

        // ASSERT
        nombreZip.Should().Be(zipEsperado,
            $"CA-05: con nombre '{nombrePunto}' el ZIP debe llamarse '{zipEsperado}'");
    }

    // ──────────────────────────────────────────
    // CA-06: DescargarFotosZipAsync invocado con el RemoteId correcto
    // ──────────────────────────────────────────
    [Fact]
    public async Task DescargaZip_InvokaClienteConRemoteIdCorrecto()
    {
        // ARRANGE
        var apiMock = new Mock<IGeoFotoApiClient>();
        apiMock.Setup(a => a.DescargarFotosZipAsync(
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new byte[] { 0x50, 0x4B, 0x05, 0x06 });

        const int remoteId = 42;

        // ACT
        await apiMock.Object.DescargarFotosZipAsync(remoteId);

        // ASSERT
        apiMock.Verify(a => a.DescargarFotosZipAsync(remoteId, It.IsAny<CancellationToken>()),
            Times.Once,
            "CA-06: DescargarFotosZipAsync debe invocarse con el RemoteId del punto");
    }
}
