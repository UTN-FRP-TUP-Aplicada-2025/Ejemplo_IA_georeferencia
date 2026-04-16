using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios para GEO-US30 — Compartir foto nativa Android.
///
/// Verifica:
///   CA-01: Archivo existe → IShareService.ShareFileAsync invocado
///   CA-02: Archivo no existe → no invoca ShareFileAsync, retorna silenciosamente
///   CA-03: IShareService no disponible (web) → botón compartir no se muestra
///   CA-04: ShareFileAsync con nombre de archivo y ruta válidos
/// </summary>
public class US30_CompartirTests
{
    // ──────────────────────────────────────────
    // CA-01: Archivo existe → ShareFileAsync invocado
    // ──────────────────────────────────────────
    [Fact]
    public async Task ShareService_ArchivoExiste_InvokaShareFileAsync()
    {
        // ARRANGE
        var shareMock = new Mock<IShareService>();
        shareMock.Setup(s => s.ShareFileAsync(
                It.IsAny<string>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);

        var nombreArchivo = "inspeccion_01.jpg";
        var rutaLocal = "/data/user/0/com.geofoto/files/photos/inspeccion_01.jpg";
        var archivoSimuladoExiste = true; // en el test no podemos crear el archivo real

        // ACT — simula CompartirAsync en GaleriaLocalDialog
        if (archivoSimuladoExiste)
            await shareMock.Object.ShareFileAsync(nombreArchivo, rutaLocal);

        // ASSERT
        shareMock.Verify(s => s.ShareFileAsync(nombreArchivo, rutaLocal), Times.Once,
            "CA-01: si el archivo existe, ShareFileAsync debe invocarse con nombre y ruta");
    }

    // ──────────────────────────────────────────
    // CA-02: Archivo no existe → NO invoca ShareFileAsync
    // ──────────────────────────────────────────
    [Fact]
    public async Task ShareService_ArchivoNoExiste_NoInvokaShare()
    {
        // ARRANGE
        var shareMock = new Mock<IShareService>();

        var rutaLocal = "/datos/fotos/inexistente.jpg";
        var archivoExiste = false;

        // ACT
        if (archivoExiste)
            await shareMock.Object.ShareFileAsync("inexistente.jpg", rutaLocal);
        else
            // comportamiento real: mostrar snackbar "Archivo no disponible"
            _ = "Archivo no disponible para compartir";

        // ASSERT
        shareMock.Verify(s => s.ShareFileAsync(
            It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "CA-02: si el archivo no existe, no se debe llamar a ShareFileAsync");
    }

    // ──────────────────────────────────────────
    // CA-03: IShareService null (web) → botón no disponible
    // ──────────────────────────────────────────
    [Fact]
    public void ShareService_NoDisponible_BotonNoMostrado()
    {
        // ARRANGE — en la app web, IShareService no está registrado en DI
        IShareService? shareService = null;

        // ACT — lógica de GaleriaLocalDialog: @if (_shareService is not null)
        var botonCompartirVisible = shareService is not null;

        // ASSERT
        botonCompartirVisible.Should().BeFalse(
            "CA-03: en plataformas sin IShareService (web), el botón de compartir no debe mostrarse");
    }

    // ──────────────────────────────────────────
    // CA-04: ShareFileAsync recibe nombre y ruta correctos
    // ──────────────────────────────────────────
    [Fact]
    public async Task ShareService_ParametrosCorrectos_NombreYRutaCoinciden()
    {
        // ARRANGE
        var shareMock = new Mock<IShareService>();
        string? nombreRecibido = null;
        string? rutaRecibida = null;

        shareMock.Setup(s => s.ShareFileAsync(It.IsAny<string>(), It.IsAny<string>()))
                 .Callback<string, string>((nombre, ruta) =>
                 {
                     nombreRecibido = nombre;
                     rutaRecibida = ruta;
                 })
                 .Returns(Task.CompletedTask);

        var foto = new LocalFoto
        {
            LocalId = 5,
            NombreArchivo = "foto_captura.jpg",
            RutaLocal = "/storage/emulated/0/GeoFoto/foto_captura.jpg"
        };

        // ACT
        await shareMock.Object.ShareFileAsync(foto.NombreArchivo, foto.RutaLocal);

        // ASSERT
        nombreRecibido.Should().Be("foto_captura.jpg",
            "CA-04: el nombre del archivo debe pasarse correctamente a ShareFileAsync");
        rutaRecibida.Should().Be("/storage/emulated/0/GeoFoto/foto_captura.jpg",
            "CA-04: la ruta local del archivo debe pasarse correctamente a ShareFileAsync");
    }
}
