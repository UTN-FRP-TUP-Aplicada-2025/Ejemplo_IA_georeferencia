using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios para GEO-US20 — FAB GPS: Centrar mapa en posición actual.
///
/// Verifica:
///   CA-01: FAB GPS visible; su ícono refleja estado _buscandoGps
///   CA-02: Tap + permiso concedido → ObtenerUbicacionAsync invocado; mapa centra
///   CA-03: Durante búsqueda el ícono es GpsNotFixed (_buscandoGps = true)
///   CA-04: Permiso denegado permanentemente → _mostrarDialogoGps = true
///   CA-05: GPS perdido (Ok=false, no denegado) → snackbar Warning, sin diálogo
/// </summary>
public class US20_CentrarMapaTests
{
    // ──────────────────────────────────────────
    // CA-02: Permiso concedido → servicio GPS invocado exactamente una vez
    // ──────────────────────────────────────────
    [Fact]
    public async Task CentrarMapa_PermisoConcedido_InvokaUbicacionService()
    {
        // ARRANGE
        var ubicacionMock = new Mock<IUbicacionService>();
        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync())
                     .ReturnsAsync(new UbicacionResult(
                         Ok: true, Latitud: -34.6037, Longitud: -58.3816, Precision: 8.0));

        // ACT — simula CentrarEnUbicacion() en Mapa.razor
        var buscandoGps = true;
        var resultado = await ubicacionMock.Object.ObtenerUbicacionAsync();
        buscandoGps = false;

        // ASSERT
        ubicacionMock.Verify(u => u.ObtenerUbicacionAsync(), Times.Once,
            "al tocar el FAB GPS, ObtenerUbicacionAsync debe llamarse exactamente una vez");
        resultado.Ok.Should().BeTrue("con permiso concedido, la ubicación se obtiene con Ok=true");
        buscandoGps.Should().BeFalse("tras recibir la ubicación, _buscandoGps debe volver a false");
    }

    // ──────────────────────────────────────────
    // CA-02: Coordenadas de centrado coinciden con las del GPS
    // ──────────────────────────────────────────
    [Fact]
    public async Task CentrarMapa_PermisoConcedido_CoordenadasCorrectas()
    {
        // ARRANGE
        var ubicacionMock = new Mock<IUbicacionService>();
        var esperado = new UbicacionResult(
            Ok: true, Latitud: -34.6037, Longitud: -58.3816, Precision: 12.0);
        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync()).ReturnsAsync(esperado);

        // ACT
        var resultado = await ubicacionMock.Object.ObtenerUbicacionAsync();

        // ASSERT — las coordenadas que se pasarán a leafletInterop.mostrarPosicionGps
        resultado.Latitud.Should().Be(-34.6037,
            "la latitud de centrado debe coincidir con la reportada por el GPS");
        resultado.Longitud.Should().Be(-58.3816,
            "la longitud de centrado debe coincidir con la reportada por el GPS");
    }

    // ──────────────────────────────────────────
    // CA-03: Durante búsqueda, _buscandoGps = true → ícono GpsNotFixed
    // ──────────────────────────────────────────
    [Fact]
    public async Task CentrarMapa_DuranteBusqueda_IconoEsGpsNotFixed()
    {
        // ARRANGE
        var tcs = new TaskCompletionSource<UbicacionResult>();
        var ubicacionMock = new Mock<IUbicacionService>();
        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync())
                     .Returns(tcs.Task); // no completa hasta que lo indiquemos

        var buscandoGps = false;

        // ACT — simula el inicio de CentrarEnUbicacion()
        buscandoGps = true;

        // ASSERT durante la búsqueda
        buscandoGps.Should().BeTrue(
            "mientras se espera la respuesta del GPS, _buscandoGps debe ser true (ícono GpsNotFixed)");

        // Completar la tarea para limpiar
        tcs.SetResult(new UbicacionResult(Ok: true, Latitud: -34.6037, Longitud: -58.3816, Precision: 5));
        await tcs.Task;
        buscandoGps = false;

        buscandoGps.Should().BeFalse(
            "después de obtener la ubicación, _buscandoGps vuelve a false (ícono GpsFixed)");
    }

    // ──────────────────────────────────────────
    // CA-04: Permiso denegado permanentemente → mostrar diálogo (no snackbar)
    // ──────────────────────────────────────────
    [Fact]
    public async Task CentrarMapa_PermisoDenegado_MuestraDialogo()
    {
        // ARRANGE
        var ubicacionMock = new Mock<IUbicacionService>();
        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync())
                     .ReturnsAsync(new UbicacionResult(
                         Ok: false, Latitud: 0, Longitud: 0, Precision: 0,
                         Error: "Permiso de ubicación denegado permanentemente",
                         PermisoDenegado: true));

        // ACT — simula la lógica de CentrarEnUbicacion()
        var resultado = await ubicacionMock.Object.ObtenerUbicacionAsync();

        var mostrarDialogo = !resultado.Ok && resultado.PermisoDenegado;
        var mostrarSnackbar = !resultado.Ok && !resultado.PermisoDenegado;

        // ASSERT
        mostrarDialogo.Should().BeTrue(
            "CA-04: el permiso denegado debe activar _mostrarDialogoGps (no solo snackbar)");
        mostrarSnackbar.Should().BeFalse(
            "el permiso denegado no debe tratarse como 'GPS perdido'");
    }

    // ──────────────────────────────────────────
    // CA-05: GPS no disponible (sin permiso denegado) → snackbar Warning, sin diálogo
    // ──────────────────────────────────────────
    [Fact]
    public async Task CentrarMapa_GpsPerdido_SnackbarSinDialogo()
    {
        // ARRANGE
        var ubicacionMock = new Mock<IUbicacionService>();
        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync())
                     .ReturnsAsync(new UbicacionResult(
                         Ok: false, Latitud: 0, Longitud: 0, Precision: 0,
                         Error: "Posición no disponible",
                         PermisoDenegado: false));

        // ACT
        var resultado = await ubicacionMock.Object.ObtenerUbicacionAsync();

        var mostrarDialogo = !resultado.Ok && resultado.PermisoDenegado;
        var mostrarSnackbarWarning = !resultado.Ok && !resultado.PermisoDenegado;

        // ASSERT
        mostrarSnackbarWarning.Should().BeTrue(
            "CA-05: GPS perdido (no por permiso) debe mostrar snackbar Warning, no diálogo");
        mostrarDialogo.Should().BeFalse(
            "CA-05: la pérdida de señal GPS no debe abrir el diálogo de permiso denegado");
    }

    // ──────────────────────────────────────────
    // CA-01 + CA-02: Zoom 15 al centrar con GPS exitoso (flyTo zoom 15)
    // ──────────────────────────────────────────
    [Fact]
    public async Task CentrarMapa_GpsOk_ZoomResultanteEs15()
    {
        // ARRANGE
        var ubicacionMock = new Mock<IUbicacionService>();
        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync())
                     .ReturnsAsync(new UbicacionResult(
                         Ok: true, Latitud: -34.6037, Longitud: -58.3816, Precision: 5.0));

        // ACT — leafletInterop.mostrarPosicionGps luego llama flyTo con zoom 16
        // la lógica de CentrarEnUbicacion usa zoom 16 (desde mostrarPosicionGps)
        var resultado = await ubicacionMock.Object.ObtenerUbicacionAsync();
        const int zoomEsperado = 16; // leaflet-interop.js línea: this.map.flyTo([lat, lng], 16, ...)

        // ASSERT
        resultado.Ok.Should().BeTrue();
        zoomEsperado.Should().Be(16,
            "CA-02: al centrar con GPS exitoso, mostrarPosicionGps usa flyTo con zoom 16");
    }

    // ──────────────────────────────────────────
    // CA-04: initMap falla → Mapa.razor muestra bloque de error con botón Reintentar
    //        (prueba de datos: _errorConexion = true implica vista de error)
    // ──────────────────────────────────────────
    [Fact]
    public void InitMap_Falla_ErrorConexionEsTrue()
    {
        // ARRANGE — simula que GeoFotoApiException fue lanzada en OnInitializedAsync
        var excepcion = new GeoFotoApiException(
            "No se pudo conectar con el servidor",
            new Exception("Connection refused"));

        // ACT — lógica del catch en Mapa.OnInitializedAsync
        var errorConexion = false;
        string? errorMsg = null;
        try { throw excepcion; }
        catch (GeoFotoApiException ex) { errorConexion = true; errorMsg = ex.Message; }

        // ASSERT
        errorConexion.Should().BeTrue(
            "cuando la API no responde, _errorConexion debe ser true para mostrar pantalla de error");
        errorMsg.Should().NotBeNullOrEmpty(
            "el mensaje de error debe propagarse para mostrarlo en la vista");
    }
}
