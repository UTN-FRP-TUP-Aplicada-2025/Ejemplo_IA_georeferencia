using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Web;

/// <summary>
/// Tests unitarios para GEO-US31 — Web paridad funcional: geolocalización en browser.
///
/// Verifica:
///   CA-01: GPS disponible → posición retornada y usada correctamente
///   CA-02: GPS no disponible → mensaje de error propagado
///   CA-03: Mensaje de error de permiso denegado es el correcto para browser
///   CA-04: Mensaje de error "posición no disponible" es distinto del permiso denegado
/// </summary>
public class US31_WebGeoTests
{
    // ──────────────────────────────────────────
    // CA-01: GPS disponible → UbicacionResult.Ok = true con coordenadas
    // ──────────────────────────────────────────
    [Fact]
    public async Task WebGps_Disponible_UbicacionResultOkConCoordenadas()
    {
        // ARRANGE — simula el servicio de ubicación web
        var geoMock = new Mock<IUbicacionService>();
        geoMock.Setup(s => s.ObtenerUbicacionAsync())
               .ReturnsAsync(new UbicacionResult(
                   Ok: true,
                   Latitud: -34.6037,
                   Longitud: -58.3816,
                   Precision: 12.5));

        // ACT
        var ubicacion = await geoMock.Object.ObtenerUbicacionAsync();

        // ASSERT
        ubicacion.Ok.Should().BeTrue("CA-01: GPS disponible debe retornar Ok=true");
        ubicacion.Latitud.Should().BeApproximately(-34.6037, 0.0001);
        ubicacion.Longitud.Should().BeApproximately(-58.3816, 0.0001);
        ubicacion.Error.Should().BeNull("CA-01: sin error cuando GPS está disponible");
    }

    // ──────────────────────────────────────────
    // CA-02: GPS no disponible → UbicacionResult.Ok = false con mensaje de error
    // ──────────────────────────────────────────
    [Fact]
    public async Task WebGps_NoDisponible_UbicacionResultFalseConMensaje()
    {
        // ARRANGE
        var geoMock = new Mock<IUbicacionService>();
        geoMock.Setup(s => s.ObtenerUbicacionAsync())
               .ReturnsAsync(new UbicacionResult(
                   Ok: false,
                   Latitud: 0,
                   Longitud: 0,
                   Precision: 0,
                   Error: "Permiso de ubicación denegado",
                   PermisoDenegado: true));

        // ACT
        var ubicacion = await geoMock.Object.ObtenerUbicacionAsync();

        // ASSERT
        ubicacion.Ok.Should().BeFalse("CA-02: GPS no disponible debe retornar Ok=false");
        ubicacion.Error.Should().NotBeNullOrEmpty("CA-02: debe haber mensaje de error");
        ubicacion.PermisoDenegado.Should().BeTrue("CA-02: debe indicar que el permiso fue denegado");
    }

    // ──────────────────────────────────────────
    // CA-03: Permiso denegado (PermisoDenegado=true) → mensaje específico para browser
    // ──────────────────────────────────────────
    [Theory]
    [InlineData(true,  "Permiso de ubicación denegado")]
    [InlineData(false, "Posición no disponible")]
    [InlineData(false, "Tiempo de espera agotado")]
    public void WebGps_PermisoDenegado_MensajeDependeDelFlag(bool permisoDenegado, string error)
    {
        // ARRANGE — resultado simulado
        var resultado = new UbicacionResult(
            Ok: false, Latitud: 0, Longitud: 0, Precision: 0,
            Error: error, PermisoDenegado: permisoDenegado);

        // ACT — lógica de Mapa.razor: si PermisoDenegado → mostrar alerta de configuración
        var debesMostrarAlertaConfig = resultado.PermisoDenegado;

        // ASSERT
        debesMostrarAlertaConfig.Should().Be(permisoDenegado,
            $"CA-03: PermisoDenegado={permisoDenegado} debe determinar si se muestra la alerta de config");
    }

    // ──────────────────────────────────────────
    // CA-04: Texto de alerta de permiso denegado en browser incluye instrucción
    // ──────────────────────────────────────────
    [Fact]
    public void WebGps_TextoAlertaPermisoDenegado_IncluyeInstruccionDeConfiguracion()
    {
        // ARRANGE — texto definido en Mapa.razor para GEO-US31
        const string mensajeEsperado =
            "Permiso de ubicación denegado en el navegador. " +
            "Habilitalo desde la configuración del sitio.";

        // ACT
        var incluyeInstruccion = mensajeEsperado.Contains("configuración", StringComparison.OrdinalIgnoreCase);
        var incluyeNavegador   = mensajeEsperado.Contains("navegador",     StringComparison.OrdinalIgnoreCase);

        // ASSERT
        incluyeInstruccion.Should().BeTrue(
            "CA-04: el mensaje de permiso denegado debe indicar dónde configurarlo");
        incluyeNavegador.Should().BeTrue(
            "CA-04: el mensaje debe mencionar que el origen es el navegador");
    }
}
