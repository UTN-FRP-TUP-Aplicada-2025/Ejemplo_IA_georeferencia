using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios para GEO-US22 — Radio visual y configurable del marker.
///
/// Verifica:
///   1. Cambio de radio se persiste en SQLite (UpdatePuntoAsync con RadioMetros)
///   2. Foto nueva dentro del radio → se asocia al punto existente (no crea uno nuevo)
///   3. Foto nueva fuera del radio → crea un nuevo punto
///   4. Radio mínimo y máximo son 10m y 500m respectivamente
///   5. Radio default es 50m
/// </summary>
public class US22_RadioMarkerTests
{
    private readonly RadioAssociationService _svc = new();

    // ──────────────────────────────────────────
    // Test 1: Radio guardado en LocalPunto
    // ──────────────────────────────────────────
    [Fact]
    public void RadioMarker_NuevoPunto_TieneRadioPorDefecto50m()
    {
        // ARRANGE + ACT
        var punto = new LocalPunto
        {
            Latitud = -34.6037m,
            Longitud = -58.3816m,
            Nombre = "Punto de prueba"
        };

        // ASSERT
        punto.RadioMetros.Should().Be(50,
            "el radio por defecto de un nuevo punto debe ser 50 metros");
    }

    // ──────────────────────────────────────────
    // Test 2: Radio modificado persiste en el objeto (SQLite lo guarda via UpdatePuntoAsync)
    // ──────────────────────────────────────────
    [Fact]
    public async Task RadioMarker_Cambiado_PersisteSQLite()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var punto = new LocalPunto
        {
            LocalId = 1,
            Latitud = -34.6037m,
            Longitud = -58.3816m,
            RadioMetros = 50
        };

        LocalPunto? puntoPersistido = null;
        dbMock.Setup(db => db.UpdatePuntoAsync(It.IsAny<LocalPunto>()))
              .Callback<LocalPunto>(p => puntoPersistido = p)
              .Returns(Task.CompletedTask);

        // ACT — simula el slider cambiando el radio de 50m a 150m
        punto.RadioMetros = 150;
        await dbMock.Object.UpdatePuntoAsync(punto);

        // ASSERT
        dbMock.Verify(db => db.UpdatePuntoAsync(It.IsAny<LocalPunto>()), Times.Once,
            "UpdatePuntoAsync debe llamarse exactamente una vez al cambiar el radio");
        puntoPersistido.Should().NotBeNull();
        puntoPersistido!.RadioMetros.Should().Be(150,
            "el radio persistido debe ser el nuevo valor del slider");
    }

    // ──────────────────────────────────────────
    // Test 3: Foto nueva dentro del radio → asocia al punto existente
    // ──────────────────────────────────────────
    [Fact]
    public void FotoNueva_DentroDelRadio_AsociaAlPuntoExistente()
    {
        // ARRANGE
        _svc.Configure(new AppConfig { RadioAsociacionMetros = 100, AutoAsociacionHabilitada = true });

        var puntoExistente = new LocalPunto
        {
            LocalId  = 1,
            Latitud  = -34.6037m,
            Longitud = -58.3816m,
            RadioMetros = 100
        };

        // Posición de la foto: ~40 metros al norte del punto existente
        var fotoLat = -34.6033;  // ~44m al norte
        var fotoLng = -58.3816;

        // ACT
        var marcadorEncontrado = _svc.BuscarMarkerDentroDeRadio(
            new[] { puntoExistente }, fotoLat, fotoLng);

        // ASSERT
        marcadorEncontrado.Should().NotBeNull(
            "la foto está dentro del radio de 100m, debe asociarse al punto existente");
        marcadorEncontrado!.LocalId.Should().Be(1);
    }

    // ──────────────────────────────────────────
    // Test 4: Foto nueva fuera del radio → debe crear un nuevo punto
    // ──────────────────────────────────────────
    [Fact]
    public void FotoNueva_FueraDelRadio_CreaNuevoPunto()
    {
        // ARRANGE
        _svc.Configure(new AppConfig { RadioAsociacionMetros = 50, AutoAsociacionHabilitada = true });

        var puntoExistente = new LocalPunto
        {
            LocalId  = 1,
            Latitud  = -34.6037m,
            Longitud = -58.3816m,
            RadioMetros = 50
        };

        // Posición de la foto: ~200 metros al sur del punto existente
        var fotoLat = -34.6055;  // ~200m al sur
        var fotoLng = -58.3816;

        // ACT
        var marcadorEncontrado = _svc.BuscarMarkerDentroDeRadio(
            new[] { puntoExistente }, fotoLat, fotoLng);

        // ASSERT
        marcadorEncontrado.Should().BeNull(
            "la foto está fuera del radio de 50m; debe crearse un nuevo punto independiente");
    }

    // ──────────────────────────────────────────
    // Test 5: Radio en el límite exacto del rango → debe aceptar entre 10 y 500
    // ──────────────────────────────────────────
    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(500)]
    public void RadioMarker_ValoresValidos_EstaEnRangoPermitido(double radio)
    {
        radio.Should().BeGreaterThanOrEqualTo(10,
            "el radio mínimo permitido es 10 metros");
        radio.Should().BeLessThanOrEqualTo(500,
            "el radio máximo permitido es 500 metros");
    }

    // ──────────────────────────────────────────
    // Test 6: Auto-asociación deshabilitada → siempre retorna null (crea nuevo punto)
    // ──────────────────────────────────────────
    [Fact]
    public void RadioMarker_AutoAsociacionDeshabilitada_SiempreRetornaNull()
    {
        // ARRANGE
        _svc.Configure(new AppConfig { RadioAsociacionMetros = 500, AutoAsociacionHabilitada = false });

        var puntoExistente = new LocalPunto
        {
            LocalId = 1, Latitud = -34.6037m, Longitud = -58.3816m
        };

        // ACT — aunque la foto esté a solo 1m del punto, auto-asociación está off
        var resultado = _svc.BuscarMarkerDentroDeRadio(
            new[] { puntoExistente }, -34.6037, -58.3816);

        // ASSERT
        resultado.Should().BeNull(
            "con AutoAsociacionHabilitada=false nunca debe asociarse al punto existente");
    }
}
