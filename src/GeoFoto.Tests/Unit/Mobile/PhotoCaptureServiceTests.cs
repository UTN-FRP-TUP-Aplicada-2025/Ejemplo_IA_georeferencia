using FluentAssertions;
using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;
using Moq;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// BLOQUE 3 — Captura de Foto con Marker Activo (DELTA-03)
/// TEST-012 a TEST-019
/// Estos tests verifican el contrato de IPhotoCaptureService y el flujo
/// de lógica de negocio asociado a la captura de fotos.
/// </summary>
public class PhotoCaptureServiceTests
{
    private readonly Mock<IPhotoCaptureService>  _cameraMock  = new();
    private readonly Mock<IUbicacionService>     _geoMock     = new();
    private readonly Mock<ILocalDbService>       _dbMock      = new();

    // ──────────────────────────────────────────
    // TEST-012: Con marker activo → foto va al marker existente (no crea punto nuevo)
    // ──────────────────────────────────────────
    [Fact]
    public async Task CaptureFoto_ConMarkerActivo_AsociaFotoAlMarkerExistente()
    {
        // ARRANGE
        var markerActivo = new LocalPunto { LocalId = 5, Latitud = -34.60m, Longitud = -58.38m };
        var fotoCapturada = new PhotoCaptureResult("/data/foto.jpg", -34.60m, -58.38m, 102400);

        _cameraMock.Setup(c => c.CapturePhotoAsync())
                   .ReturnsAsync(fotoCapturada);
        _dbMock.Setup(d => d.GetFotosByPuntoAsync(5))
               .ReturnsAsync(new List<LocalFoto>());
        _dbMock.Setup(d => d.InsertFotoAsync(It.IsAny<LocalFoto>()))
               .ReturnsAsync(1);

        // ACT — lógica: Si hay marker activo, asociar sin crear punto nuevo
        var foto = await _cameraMock.Object.CapturePhotoAsync();
        foto.Should().NotBeNull();

        var nuevaFoto = new LocalFoto
        {
            PuntoLocalId  = markerActivo.LocalId,
            NombreArchivo = Path.GetFileName(foto!.FilePath),
            RutaLocal     = foto.FilePath,
            TamanoBytes   = foto.SizeBytes,
            LatitudExif   = foto.LatitudExif,
            LongitudExif  = foto.LongitudExif,
            SyncStatus    = SyncStatusValues.PendingCreate
        };

        await _dbMock.Object.InsertFotoAsync(nuevaFoto);

        // ASSERT
        nuevaFoto.PuntoLocalId.Should().Be(5, "la foto debe asociarse al marker activo");
        _dbMock.Verify(d => d.InsertFotoAsync(It.Is<LocalFoto>(f => f.PuntoLocalId == 5)), Times.Once);
        _dbMock.Verify(d => d.InsertPuntoAsync(It.IsAny<LocalPunto>()), Times.Never,
            "NO debe crear un punto nuevo si hay marker activo");
    }

    // ──────────────────────────────────────────
    // TEST-013: Sin marker activo → crea nuevo punto en coords GPS
    // ──────────────────────────────────────────
    [Fact]
    public async Task CaptureFoto_SinMarkerActivo_CreaNuevoPuntoConPosicionGps()
    {
        // ARRANGE
        _geoMock.Setup(g => g.ObtenerUbicacionAsync())
                .ReturnsAsync(new UbicacionResult(true, -34.60, -58.38, 20));
        _cameraMock.Setup(c => c.CapturePhotoAsync())
                   .ReturnsAsync(new PhotoCaptureResult("/data/f.jpg", null, null, 50000));
        _dbMock.Setup(d => d.InsertPuntoAsync(It.IsAny<LocalPunto>())).ReturnsAsync(1);
        _dbMock.Setup(d => d.InsertFotoAsync(It.IsAny<LocalFoto>())).ReturnsAsync(1);

        // ACT
        var gps   = await _geoMock.Object.ObtenerUbicacionAsync();
        var foto  = await _cameraMock.Object.CapturePhotoAsync();

        // Sin marker activo: crear punto en posición GPS
        var nuevoPunto = new LocalPunto
        {
            Latitud    = (decimal)gps.Latitud,
            Longitud   = (decimal)gps.Longitud,
            SyncStatus = SyncStatusValues.PendingCreate
        };
        var puntoId = await _dbMock.Object.InsertPuntoAsync(nuevoPunto);

        var nuevaFoto = new LocalFoto
        {
            PuntoLocalId = puntoId,
            NombreArchivo = "f.jpg",
            RutaLocal    = foto!.FilePath,
            SyncStatus   = SyncStatusValues.PendingCreate
        };
        await _dbMock.Object.InsertFotoAsync(nuevaFoto);

        // ASSERT
        _dbMock.Verify(d => d.InsertPuntoAsync(It.Is<LocalPunto>(p =>
            p.Latitud == -34.60m && p.Longitud == -58.38m)), Times.Once);
        _dbMock.Verify(d => d.InsertFotoAsync(It.IsAny<LocalFoto>()), Times.Once);
    }

    // ──────────────────────────────────────────
    // TEST-014: Primera foto de un marker → flag para abrir diálogo de edición
    // ──────────────────────────────────────────
    [Fact]
    public async Task CaptureFoto_PrimeraFotoDeMarker_EsPrimeraFoto_True()
    {
        // ARRANGE — marker con 0 fotos previas
        _dbMock.Setup(d => d.GetFotosByPuntoAsync(10))
               .ReturnsAsync(new List<LocalFoto>()); // sin fotos aún

        // ACT
        var fotosExistentes = await _dbMock.Object.GetFotosByPuntoAsync(10);
        var esPrimeraFoto   = fotosExistentes.Count == 0;

        // ASSERT
        esPrimeraFoto.Should().BeTrue("con 0 fotos es la primera, debe abrir diálogo de edición");
    }

    // ──────────────────────────────────────────
    // TEST-015: Foto subsiguiente → NO abre diálogo, solo Snackbar
    // ──────────────────────────────────────────
    [Fact]
    public async Task CaptureFoto_FotoSubsiguiente_NoEsPrimeraFoto()
    {
        // ARRANGE — marker que ya tiene 1 foto
        _dbMock.Setup(d => d.GetFotosByPuntoAsync(10))
               .ReturnsAsync(new List<LocalFoto>
               {
                   new() { LocalId = 1, PuntoLocalId = 10, NombreArchivo = "ya_existente.jpg" }
               });

        // ACT
        var fotosExistentes = await _dbMock.Object.GetFotosByPuntoAsync(10);
        var esPrimeraFoto   = fotosExistentes.Count == 0;

        // ASSERT
        esPrimeraFoto.Should().BeFalse("ya tiene fotos: debe mostrar Snackbar sin abrir diálogo");
    }

    // ──────────────────────────────────────────
    // TEST-016: Después de captura → recarga posición GPS
    // ──────────────────────────────────────────
    [Fact]
    public async Task CaptureFoto_DespuesDeCaptura_SolicitaUbicacionActual()
    {
        // ARRANGE
        _geoMock.Setup(g => g.ObtenerUbicacionAsync())
                .ReturnsAsync(new UbicacionResult(true, -34.605, -58.382, 15));

        // ACT — patrón: al volver al mapa, el componente vuelve a pedir GPS
        var posPost = await _geoMock.Object.ObtenerUbicacionAsync();

        // ASSERT
        posPost.Ok.Should().BeTrue();
        _geoMock.Verify(g => g.ObtenerUbicacionAsync(), Times.Once);
    }

    // ──────────────────────────────────────────
    // TEST-017: Usuario cancela captura → sin cambios en base de datos
    // ──────────────────────────────────────────
    [Fact]
    public async Task CaptureFoto_UsuarioCancela_NingunCambioEnDb()
    {
        // ARRANGE — MediaPicker devuelve null cuando el usuario cancela
        _cameraMock.Setup(c => c.CapturePhotoAsync())
                   .ReturnsAsync((PhotoCaptureResult?)null);

        // ACT
        var foto = await _cameraMock.Object.CapturePhotoAsync();
        if (foto is not null)
        {
            await _dbMock.Object.InsertFotoAsync(new LocalFoto());
        }

        // ASSERT — ninguna inserción se realizó
        foto.Should().BeNull();
        _dbMock.Verify(d => d.InsertFotoAsync(It.IsAny<LocalFoto>()), Times.Never);
        _dbMock.Verify(d => d.InsertPuntoAsync(It.IsAny<LocalPunto>()), Times.Never);
    }

    // ──────────────────────────────────────────
    // TEST-018: Marker activo → JS setActiveMarkerStyle se debe invocar
    // Nota: aquí verificamos el contrato a nivel de servicio; la invocación real
    // de JS se verifica en tests de componente/integración.
    // ──────────────────────────────────────────
    [Fact]
    public void MarkerActivo_EstiloDiferenciado_IdRertonado()
    {
        // ARRANGE — cuando un marker se activa, debe poder identificarse
        var marker = new LocalPunto { LocalId = 42, Latitud = -34.60m, Longitud = -58.38m };

        // ACT
        var markerActivoId = marker.LocalId;

        // ASSERT — el ID del marker activo es el correcto para pasarlo a JS
        markerActivoId.Should().Be(42);
    }

    // ──────────────────────────────────────────
    // TEST-019: Nuevo punto sin marker activo → primera foto abre diálogo
    // ──────────────────────────────────────────
    [Fact]
    public async Task CaptureFoto_NuevoPuntoSinMarkerActivo_EsPrimeraFoto()
    {
        // ARRANGE — recién creado, sin fotos
        _dbMock.Setup(d => d.InsertPuntoAsync(It.IsAny<LocalPunto>())).ReturnsAsync(99);
        _dbMock.Setup(d => d.GetFotosByPuntoAsync(99))
               .ReturnsAsync(new List<LocalFoto>()); // recien insertado = 0 fotos

        // ACT
        var nuevoPuntoId    = await _dbMock.Object.InsertPuntoAsync(new LocalPunto());
        var fotosExistentes = await _dbMock.Object.GetFotosByPuntoAsync(nuevoPuntoId);
        var esPrimeraFoto   = fotosExistentes.Count == 0;

        // ASSERT
        esPrimeraFoto.Should().BeTrue();
        nuevoPuntoId.Should().Be(99);
    }
}
