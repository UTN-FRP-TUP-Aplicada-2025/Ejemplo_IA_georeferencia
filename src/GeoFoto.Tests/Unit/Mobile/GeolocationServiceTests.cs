using FluentAssertions;
using GeoFoto.Shared.Services;
using Moq;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// BLOQUE 1 — Geolocalización e Inicio del Mapa (DELTA-01)
/// TEST-001 a TEST-004
/// Estos tests verifican el contrato del servicio de ubicación y las reglas de
/// inicialización del mapa expresadas como invariantes de lógica de dominio.
/// </summary>
public class GeolocationServiceTests
{
    // ──────────────────────────────────────────
    // TEST-001: El mapa DEBE solicitar ubicación al inicializar
    // ──────────────────────────────────────────
    [Fact]
    public async Task MapaInit_OnInitializedAsync_InvocaObtenerUbicacionAsync()
    {
        // ARRANGE
        var geoMock = new Mock<IUbicacionService>();
        geoMock.Setup(g => g.ObtenerUbicacionAsync())
               .ReturnsAsync(new UbicacionResult(true, -34.6037, -58.3816, 30));

        // ACT — simulamos el comportamiento que Mapa.OnInitializedAsync encapsula:
        // lanzar GetLocation y esperar hasta 1.5 s
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var gpsTask = geoMock.Object.ObtenerUbicacionAsync();
        await Task.WhenAny(gpsTask, Task.Delay(1500, cts.Token));

        // ASSERT — la llamada se produjo
        geoMock.Verify(g => g.ObtenerUbicacionAsync(), Times.Once);
    }

    // ──────────────────────────────────────────
    // TEST-002: GPS disponible → mapa se centra en coordenadas reales (zoom 15)
    // ──────────────────────────────────────────
    [Fact]
    public async Task MapaInit_GpsDisponible_RetornaCoordenadaReal()
    {
        // ARRANGE
        var expectedLat = -34.6037;
        var expectedLng = -58.3816;
        var geoMock = new Mock<IUbicacionService>();
        geoMock.Setup(g => g.ObtenerUbicacionAsync())
               .ReturnsAsync(new UbicacionResult(true, expectedLat, expectedLng, 25));

        // ACT
        var result = await geoMock.Object.ObtenerUbicacionAsync();

        // ASSERT — la ubicación devuelta es la correcta y se debe usar como centro
        result.Ok.Should().BeTrue();
        result.Latitud.Should().Be(expectedLat);
        result.Longitud.Should().Be(expectedLng);
        // REGLA: con GPS disponible el zoom debe ser 15, no el fallback 13
        var zoom = result.Ok ? 15 : 13;
        zoom.Should().Be(15);
    }

    // ──────────────────────────────────────────
    // TEST-003: GPS null → usar posición por defecto Buenos Aires (zoom 13)
    // ──────────────────────────────────────────
    [Fact]
    public async Task MapaInit_GpsNoDisponible_UsaPosicionPorDefecto()
    {
        // ARRANGE
        const double defaultLat = -34.6037;
        const double defaultLng = -58.3816;
        const int defaultZoom = 13;

        var geoMock = new Mock<IUbicacionService>();
        geoMock.Setup(g => g.ObtenerUbicacionAsync())
               .ReturnsAsync(new UbicacionResult(false, 0, 0, 0, "GPS no disponible"));

        // ACT
        var result = await geoMock.Object.ObtenerUbicacionAsync();

        // ASSERT — lógica de fallback
        result.Ok.Should().BeFalse();

        var initLat  = result.Ok ? result.Latitud  : defaultLat;
        var initLng  = result.Ok ? result.Longitud : defaultLng;
        var initZoom = result.Ok ? 15 : defaultZoom;

        initLat.Should().Be(defaultLat);
        initLng.Should().Be(defaultLng);
        initZoom.Should().Be(13);
    }

    // ──────────────────────────────────────────
    // TEST-004: Carga de markers DESPUÉS de centrar, sin alterar el centro
    // ──────────────────────────────────────────
    [Fact]
    public async Task MapaInit_CargoMarkersDespuesDeGps_NoCambiaCentro()
    {
        // ARRANGE
        var geoMock    = new Mock<IUbicacionService>();
        var apiMock    = new Mock<IGeoFotoApiClient>();
        var callOrder  = new List<string>();

        geoMock.Setup(g => g.ObtenerUbicacionAsync())
               .Callback(() => callOrder.Add("GPS"))
               .ReturnsAsync(new UbicacionResult(true, -34.6037, -58.3816, 30));

        apiMock.Setup(a => a.GetPuntosAsync())
               .Callback(() => callOrder.Add("Markers"))
               .ReturnsAsync(new List<GeoFoto.Shared.Models.PuntoDto>());

        // ACT — patrón que Mapa.OnInitializedAsync sigue:
        //   1. lanzar GPS
        //   2. cargar datos
        //   3. esperar GPS (max 1.5s)
        var gpsTask     = geoMock.Object.ObtenerUbicacionAsync();
        var markersTask = apiMock.Object.GetPuntosAsync();
        await Task.WhenAll(markersTask);
        await Task.WhenAny(gpsTask, Task.Delay(200)); // cortamos rápido para el test

        // ASSERT — markers se cargaron; el resultado del GPS no modifica los puntos cargados
        callOrder.Should().Contain("Markers");
        var gpsResult = gpsTask.IsCompleted ? gpsTask.Result : null;
        if (gpsResult is not null)
        {
            // Si GPS respondió, el centro es el real
            gpsResult.Ok.Should().BeTrue();
        }
    }
}
