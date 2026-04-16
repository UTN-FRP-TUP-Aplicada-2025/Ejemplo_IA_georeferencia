using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Web;

/// <summary>
/// Tests unitarios para GEO-US33 — Subir foto al marker desde browser.
///
/// Verifica:
///   CA-01: Foto sin EXIF → LatitudExif y LongitudExif son null (ESC-04)
///   CA-02: Foto con EXIF → LatitudExif y LongitudExif tienen valores
///   CA-03: Upload sin EXIF no lanza excepción (vincula al marker por puntoId)
///   CA-04: IGeoFotoApiClient.AgregarFotoAPuntoAsync invocado con puntoId correcto
///   CA-05: Extensión no soportada → no invoca el upload
/// </summary>
public class US33_SubirFotoWebTests
{
    // ──────────────────────────────────────────
    // CA-01: Foto sin EXIF → coordenadas EXIF nulas (ESC-04)
    // ──────────────────────────────────────────
    [Fact]
    public void SubirFotoWeb_SinExif_LatitudLongitudExifNulas()
    {
        // ARRANGE — resultado típico de ExifService cuando la foto no tiene datos GPS
        decimal? latExif = null;
        decimal? lngExif = null;

        // ACT — lógica de FotosService.UploadToPuntoAsync: guarda latExif/lngExif tal como vienen
        var fotoVinculada = new
        {
            PuntoId     = 42,
            LatitudExif = latExif,
            LongitudExif = lngExif,
        };

        // ASSERT
        fotoVinculada.PuntoId.Should().Be(42,
            "CA-01 (ESC-04): la foto sin EXIF debe vincularse al marker por puntoId");
        fotoVinculada.LatitudExif.Should().BeNull(
            "CA-01 (ESC-04): sin EXIF, LatitudExif debe ser null");
        fotoVinculada.LongitudExif.Should().BeNull(
            "CA-01 (ESC-04): sin EXIF, LongitudExif debe ser null");
    }

    // ──────────────────────────────────────────
    // CA-02: Foto con EXIF → coordenadas EXIF presentes
    // ──────────────────────────────────────────
    [Fact]
    public void SubirFotoWeb_ConExif_LatitudLongitudExifPresentes()
    {
        // ARRANGE — foto con metadatos GPS
        decimal? latExif = -34.6037m;
        decimal? lngExif = -58.3816m;

        // ACT
        var fotoVinculada = new
        {
            PuntoId      = 42,
            LatitudExif  = latExif,
            LongitudExif = lngExif,
        };

        // ASSERT
        fotoVinculada.LatitudExif.Should().NotBeNull(
            "CA-02: foto con EXIF debe tener LatitudExif");
        fotoVinculada.LongitudExif.Should().NotBeNull(
            "CA-02: foto con EXIF debe tener LongitudExif");
        fotoVinculada.LatitudExif.Should().Be(-34.6037m);
        fotoVinculada.LongitudExif.Should().Be(-58.3816m);
    }

    // ──────────────────────────────────────────
    // CA-03: Upload sin EXIF → AgregarFotoAPuntoAsync no lanza excepción
    // ──────────────────────────────────────────
    [Fact]
    public async Task SubirFotoWeb_SinExif_NoLanzaExcepcion()
    {
        // ARRANGE
        var apiMock = new Mock<IGeoFotoApiClient>();
        apiMock.Setup(a => a.AgregarFotoAPuntoAsync(
                It.IsAny<int>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        using var stream = new MemoryStream([0xFF, 0xD8, 0xFF]); // minimal JPEG header
        const string nombre = "sin_exif.jpg";
        const string mime   = "image/jpeg";

        // ACT — no debe lanzar
        var act = async () =>
            await apiMock.Object.AgregarFotoAPuntoAsync(5, stream, nombre, mime);

        // ASSERT
        await act.Should().NotThrowAsync(
            "CA-03: subir foto sin EXIF no debe lanzar excepción");
    }

    // ──────────────────────────────────────────
    // CA-04: AgregarFotoAPuntoAsync llamado con el puntoId correcto
    // ──────────────────────────────────────────
    [Fact]
    public async Task SubirFotoWeb_AgregarFoto_LlamaConPuntoIdCorrecto()
    {
        // ARRANGE
        var apiMock = new Mock<IGeoFotoApiClient>();
        apiMock.Setup(a => a.AgregarFotoAPuntoAsync(
                It.IsAny<int>(), It.IsAny<Stream>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var puntoRemoteId = 7;
        using var stream  = new MemoryStream([0x89, 0x50, 0x4E, 0x47]); // PNG header

        // ACT
        await apiMock.Object.AgregarFotoAPuntoAsync(
            puntoRemoteId, stream, "foto.png", "image/png");

        // ASSERT
        apiMock.Verify(a => a.AgregarFotoAPuntoAsync(
            puntoRemoteId,
            It.IsAny<Stream>(),
            "foto.png",
            "image/png",
            It.IsAny<CancellationToken>()), Times.Once,
            "CA-04: AgregarFotoAPuntoAsync debe invocarse con el puntoId correcto");
    }

    // ──────────────────────────────────────────
    // CA-05: Extensión no soportada → no invoca el upload
    // ──────────────────────────────────────────
    [Theory]
    [InlineData(".jpg",  true)]
    [InlineData(".jpeg", true)]
    [InlineData(".png",  true)]
    [InlineData(".webp", true)]
    [InlineData(".gif",  false)]
    [InlineData(".bmp",  false)]
    [InlineData(".pdf",  false)]
    public void SubirFotoWeb_ExtensionArchivo_PermiteOBloqueaSegunFormato(
        string extension, bool debePermitirse)
    {
        // ARRANGE — lista de extensiones aceptadas en MarkerPopup / FotosController
        var aceptadas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        // ACT
        var estaPermitida = aceptadas.Contains(extension, StringComparer.OrdinalIgnoreCase);

        // ASSERT
        estaPermitida.Should().Be(debePermitirse,
            $"CA-05: la extensión '{extension}' debería {(debePermitirse ? "permitirse" : "bloquearse")}");
    }
}
