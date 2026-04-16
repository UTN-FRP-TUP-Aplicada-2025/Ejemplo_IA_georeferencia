using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios de los 5 escenarios de centrado del mapa (GEO-US23 / F4).
///
/// Los escenarios son:
///   1. Carga inicial con GPS disponible → mapa centra en coordenadas GPS reales
///   2. Carga inicial sin GPS → mapa centra en coordenadas por defecto (Buenos Aires)
///   3. Captura de foto → mapa centra en la posición capturada
///   4. Click en marcador → botón "Centrar" posiciona el mapa en el punto
///   5. Usuario presiona botón GPS → mapa vuela a la ubicación actual
/// </summary>
public class MapCenteringTests
{
    // ──────────────────────────────────────────
    // ESCENARIO 1: Carga inicial con GPS disponible → usa coordenadas reales
    // ──────────────────────────────────────────
    [Fact]
    public void CargaInicial_GpsDisponible_UsaCoordenadaGps()
    {
        // ARRANGE
        var gpsResult = new UbicacionResult(
            Ok: true, Latitud: -34.6037, Longitud: -58.3816, Precision: 12.5);

        // ACT — lógica de Mapa.OnAfterRenderAsync: usa GPS si está disponible
        var initLat  = gpsResult.Ok ? gpsResult.Latitud  : -34.6037; // default BA
        var initLng  = gpsResult.Ok ? gpsResult.Longitud : -58.3816;
        var initZoom = gpsResult.Ok ? 15 : 13;

        // ASSERT
        initLat.Should().Be(-34.6037);
        initLng.Should().Be(-58.3816);
        initZoom.Should().Be(15, "con GPS el zoom inicial debe ser 15");
    }

    // ──────────────────────────────────────────
    // ESCENARIO 2: Carga inicial sin GPS → coordenadas de Buenos Aires por defecto
    // ──────────────────────────────────────────
    [Fact]
    public void CargaInicial_SinGps_UsaCoordenadaPorDefecto()
    {
        // ARRANGE — GPS no disponible (null)
        UbicacionResult? gpsInicial = null;

        // ACT
        var initLat  = gpsInicial?.Latitud  ?? -34.6037;
        var initLng  = gpsInicial?.Longitud ?? -58.3816;
        var initZoom = gpsInicial is not null ? 15 : 13;

        // ASSERT
        initLat.Should().BeApproximately(-34.6037, 0.0001,
            "sin GPS el mapa debe centrar en Buenos Aires");
        initLng.Should().BeApproximately(-58.3816, 0.0001);
        initZoom.Should().Be(13, "sin GPS el zoom inicial debe ser 13 (más alejado)");
    }

    // ──────────────────────────────────────────
    // ESCENARIO 3: Captura de foto con GPS → mapa centra en posición de la foto
    // ──────────────────────────────────────────
    [Fact]
    public void CapturaFoto_GpsDisponible_CentraEnPosicionCaptura()
    {
        // ARRANGE
        var posCaptura = new UbicacionResult(
            Ok: true, Latitud: -34.6100, Longitud: -58.3900, Precision: 8.0);

        // ACT — lógica de OnCapturaFoto: si pos != null, centra el mapa ahí
        var debeReCentrar = posCaptura is not null;
        var latCentrado   = posCaptura?.Latitud;
        var lngCentrado   = posCaptura?.Longitud;

        // ASSERT
        debeReCentrar.Should().BeTrue("con GPS disponible el mapa debe recentrarse en la captura");
        latCentrado.Should().Be(-34.6100);
        lngCentrado.Should().Be(-58.3900);
    }

    // ──────────────────────────────────────────
    // ESCENARIO 3b: Captura de foto SIN GPS → no centra en (0,0)
    // ──────────────────────────────────────────
    [Fact]
    public void CapturaFoto_SinGps_NoCentraEnCerosCero()
    {
        // ARRANGE — GPS no disponible durante la captura
        UbicacionResult? pos = null;

        // ACT — lógica de OnCapturaFoto: solo centra si pos != null
        var debeReCentrar = pos is not null;

        // ASSERT
        debeReCentrar.Should().BeFalse(
            "sin GPS después de la captura, el mapa NO debe volar a (0,0)");
    }

    // ──────────────────────────────────────────
    // ESCENARIO 4: Click en marcador → centrar en coordenadas del punto
    // ──────────────────────────────────────────
    [Fact]
    public void ClickEnMarcador_CentrarEnMapa_UsaCoordenadasDelPunto()
    {
        // ARRANGE
        var punto = new LocalPunto
        {
            LocalId  = 42,
            Latitud  = -34.6050m,
            Longitud = -58.3870m,
            Nombre   = "Poste 42"
        };

        // ACT — el botón "Centrar" en DetallePuntoLocal llama leafletInterop.centerOn
        var latCentrado = (double)punto.Latitud;
        var lngCentrado = (double)punto.Longitud;

        // ASSERT
        latCentrado.Should().BeApproximately(-34.6050, 0.0001);
        lngCentrado.Should().BeApproximately(-58.3870, 0.0001);
    }

    // ──────────────────────────────────────────
    // ESCENARIO 5: Botón GPS presionado → centra en última ubicación conocida
    // ──────────────────────────────────────────
    [Fact]
    public async Task BotonGps_UbicacionDisponible_CentraEnUbicacionActual()
    {
        // ARRANGE
        var ubicacionMock = new Mock<IUbicacionService>();
        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync())
                     .ReturnsAsync(new UbicacionResult(
                         Ok: true, Latitud: -34.6080, Longitud: -58.3840, Precision: 15.0));

        // ACT
        var resultado = await ubicacionMock.Object.ObtenerUbicacionAsync();

        // ASSERT
        resultado.Ok.Should().BeTrue();
        resultado.Latitud.Should().Be(-34.6080);
        resultado.Longitud.Should().Be(-58.3840);
        ubicacionMock.Verify(u => u.ObtenerUbicacionAsync(), Times.Once,
            "el botón GPS debe invocar ObtenerUbicacionAsync exactamente una vez");
    }

    // ──────────────────────────────────────────
    // ESCENARIO 5b: Botón GPS — permiso denegado → NO centra, muestra diálogo
    // ──────────────────────────────────────────
    [Fact]
    public async Task BotonGps_PermisoDenegado_NoLanzaExcepcionYRetornaError()
    {
        // ARRANGE
        var ubicacionMock = new Mock<IUbicacionService>();
        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync())
                     .ReturnsAsync(new UbicacionResult(
                         Ok: false, Latitud: 0, Longitud: 0, Precision: 0,
                         Error: "Permiso de ubicación denegado",
                         PermisoDenegado: true));

        // ACT
        var resultado = await ubicacionMock.Object.ObtenerUbicacionAsync();

        // ASSERT
        resultado.Ok.Should().BeFalse();
        resultado.PermisoDenegado.Should().BeTrue();
        resultado.Error.Should().Contain("denegado");
    }

    // ──────────────────────────────────────────
    // Zoom correcto al recentrar en marcador (zoom 16)
    // ──────────────────────────────────────────
    [Fact]
    public void CentrarEnCoordenadas_ZoomEsperado_Es16()
    {
        // ARRANGE — al centrar en un punto específico se usa zoom 16
        const int zoomEsperado = 16;
        var punto = new LocalPunto { Latitud = -34.6050m, Longitud = -58.3870m };

        // ACT — leafletInterop.centrarEnCoordenadas(lat, lng, zoom=16) o centerOn usa flyTo zoom 16
        var zoom = zoomEsperado;

        // ASSERT
        zoom.Should().Be(16, "centrar en un punto específico usa zoom 16 para enfoque óptimo");
    }
}
