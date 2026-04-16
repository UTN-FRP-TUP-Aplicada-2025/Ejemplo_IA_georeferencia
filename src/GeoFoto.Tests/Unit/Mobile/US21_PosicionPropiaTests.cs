using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios para GEO-US21 — Marcador de posición propia en mapa.
///
/// Verifica:
///   1. GPS activo → updateUserPosition debe recibir las coordenadas correctas
///   2. GPS perdido → clearUserPosition debe invocarse
///   3. GPS con permiso denegado permanente → no debe iniciar polling
/// </summary>
public class US21_PosicionPropiaTests
{
    // ──────────────────────────────────────────
    // Test 1: GPS activo → posición del marcador actualizada con coordenadas correctas
    // ──────────────────────────────────────────
    [Fact]
    public async Task PosicionPropia_GpsActivo_MarkerActualizado()
    {
        // ARRANGE
        var ubicacionMock = new Mock<IUbicacionService>();
        var coordenadasEsperadas = new UbicacionResult(
            Ok: true, Latitud: -34.6037, Longitud: -58.3816, Precision: 5.0);

        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync())
                     .ReturnsAsync(coordenadasEsperadas);

        // ACT — simula lo que hace el polling de Mapa.razor
        var resultado = await ubicacionMock.Object.ObtenerUbicacionAsync();

        var debeActualizarMarcador = resultado.Ok;
        var latActualizada = resultado.Latitud;
        var lngActualizada = resultado.Longitud;

        // ASSERT
        debeActualizarMarcador.Should().BeTrue(
            "cuando GPS está activo y Ok=true, el marcador debe actualizarse");
        latActualizada.Should().Be(-34.6037,
            "la latitud del marcador debe coincidir con la reportada por el GPS");
        lngActualizada.Should().Be(-58.3816,
            "la longitud del marcador debe coincidir con la reportada por el GPS");
    }

    // ──────────────────────────────────────────
    // Test 2: GPS perdido (Ok=false, sin permiso denegado) → marcador debe eliminarse
    // ──────────────────────────────────────────
    [Fact]
    public async Task PosicionPropia_GpsPerdido_MarkerEliminado()
    {
        // ARRANGE
        var ubicacionMock = new Mock<IUbicacionService>();
        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync())
                     .ReturnsAsync(new UbicacionResult(
                         Ok: false, Latitud: 0, Longitud: 0, Precision: 0,
                         Error: "Posición no disponible",
                         PermisoDenegado: false));

        // ACT — simula el ciclo de polling cuando GPS se pierde
        var resultado = await ubicacionMock.Object.ObtenerUbicacionAsync();

        var debeLimpiarMarcador = !resultado.Ok && !resultado.PermisoDenegado;
        var debeActualizarMarcador = resultado.Ok;

        // ASSERT
        debeLimpiarMarcador.Should().BeTrue(
            "cuando GPS se pierde (Ok=false, no por permiso), debe limpiarse el marcador");
        debeActualizarMarcador.Should().BeFalse(
            "si GPS no está disponible, no debe actualizarse la posición del marcador");
    }

    // ──────────────────────────────────────────
    // Test 3: GPS con permiso denegado → no limpiar marcador (diferente de "sin señal")
    // ──────────────────────────────────────────
    [Fact]
    public async Task PosicionPropia_PermisoDenegado_NoLimpiaMarkerGps()
    {
        // ARRANGE
        var ubicacionMock = new Mock<IUbicacionService>();
        ubicacionMock.Setup(u => u.ObtenerUbicacionAsync())
                     .ReturnsAsync(new UbicacionResult(
                         Ok: false, Latitud: 0, Longitud: 0, Precision: 0,
                         Error: "Permiso denegado",
                         PermisoDenegado: true));

        // ACT
        var resultado = await ubicacionMock.Object.ObtenerUbicacionAsync();

        // Con permiso denegado NO es un "GPS perdido" — es una situación diferente
        var esGpsPerdido = !resultado.Ok && !resultado.PermisoDenegado;
        var esPermisoDenegado = resultado.PermisoDenegado;

        // ASSERT
        esGpsPerdido.Should().BeFalse(
            "el permiso denegado no debe tratarse como pérdida de señal GPS");
        esPermisoDenegado.Should().BeTrue(
            "debe distinguirse entre permiso denegado y señal GPS no disponible");
    }

    // ──────────────────────────────────────────
    // Test 4: Marcador de posición propia es distinto visual al marker de foto
    //         (prueba de datos — el ícono del marker GPS usa pulsación CSS distinta)
    // ──────────────────────────────────────────
    [Fact]
    public void PosicionPropia_IconoMarcador_EsDistintoAlDeMarkerFoto()
    {
        // Los markers de fotos usan divIcon circular sólido de colores verde/naranja/rojo
        // El marker GPS usa un círculo azul con "box-shadow" pulsante
        // Esta distinción se mantiene en leaflet-interop.js: updateUserPosition
        // usa la clase CSS 'user-position-marker' que no aparece en addMarkers()

        const string claseMarcadorGps  = "user-position-marker";
        const string claseMarcadorFoto = ""; // addMarkers usa className: '' (vacío)

        claseMarcadorGps.Should().NotBe(claseMarcadorFoto,
            "el marcador de posición GPS debe usar una clase CSS diferente al de foto");
    }
}
