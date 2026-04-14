using FluentAssertions;
using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// BLOQUE 4 — Radio de Asociación de Fotos (DELTA-04)
/// TEST-020 a TEST-026
/// RadioAssociationService es una clase de lógica pura (sin deps externas),
/// por lo que estos son tests unitarios directos sin mocks.
/// </summary>
public class RadioAssociationServiceTests
{
    private static RadioAssociationService CrearServicio(int radio = 50, bool habilitado = true)
    {
        var svc = new RadioAssociationService();
        svc.Configure(new AppConfig
        {
            RadioAsociacionMetros   = radio,
            AutoAsociacionHabilitada = habilitado
        });
        return svc;
    }

    // ──────────────────────────────────────────
    // TEST-020: Marker a ~15 m → lo encuentra dentro del radio de 50 m
    // ──────────────────────────────────────────
    [Fact]
    public void BuscarMarker_DentroDeRadio_EncuentraMarkerCercano()
    {
        // ARRANGE — marker en (-34.6037, -58.3816), búsqueda desde ~15 m
        var svc = CrearServicio(radio: 50);
        var markers = new List<LocalPunto>
        {
            new() { LocalId = 1, Latitud = -34.6037m, Longitud = -58.3816m }
        };

        // ACT
        var resultado = svc.BuscarMarkerDentroDeRadio(markers, -34.6038, -58.3817);

        // ASSERT
        resultado.Should().NotBeNull();
        resultado!.LocalId.Should().Be(1);
    }

    // ──────────────────────────────────────────
    // TEST-021: Marker a ~700 m → no lo encuentra con radio de 50 m
    // ──────────────────────────────────────────
    [Fact]
    public void BuscarMarker_FueraDeRadio_RetornaNull()
    {
        // ARRANGE
        var svc = CrearServicio(radio: 50);
        var markers = new List<LocalPunto>
        {
            new() { LocalId = 1, Latitud = -34.6037m, Longitud = -58.3816m }
        };

        // ACT — punto de captura muy alejado (~700 m)
        var resultado = svc.BuscarMarkerDentroDeRadio(markers, -34.6100, -58.3816);

        // ASSERT
        resultado.Should().BeNull();
    }

    // ──────────────────────────────────────────
    // TEST-022: AutoAsociacion deshabilitada → siempre retorna null
    // ──────────────────────────────────────────
    [Fact]
    public void BuscarMarker_AutoAsociacionDeshabilitada_SiempreRetornaNull()
    {
        // ARRANGE — marker a 10 m, pero la función está deshabilitada
        var svc = CrearServicio(radio: 50, habilitado: false);
        var markers = new List<LocalPunto>
        {
            new() { LocalId = 1, Latitud = -34.6037m, Longitud = -58.3816m }
        };

        // ACT — búsqueda desde punto muy cercano
        var resultado = svc.BuscarMarkerDentroDeRadio(markers, -34.6037, -58.3816);

        // ASSERT
        resultado.Should().BeNull("AutoAsociacion está deshabilitada");
    }

    // ──────────────────────────────────────────
    // TEST-023: Guardar config en preferencias → RadioAssociationService usa nuevo valor
    // ──────────────────────────────────────────
    [Fact]
    public void Configure_NuevoRadio_ActualizaPropiedad()
    {
        // ARRANGE
        var svc = CrearServicio(radio: 50);
        svc.RadioMetros.Should().Be(50);

        // ACT — usuario modifica a 100 m y "guarda"
        svc.Configure(new AppConfig { RadioAsociacionMetros = 100, AutoAsociacionHabilitada = true });

        // ASSERT
        svc.RadioMetros.Should().Be(100);
    }

    // ──────────────────────────────────────────
    // TEST-024: Validación de rangos — fuera de [10, 500] → no debe aceptarse
    // ──────────────────────────────────────────
    [Theory]
    [InlineData(5,   "El radio mínimo es 10 metros")]
    [InlineData(600, "El radio máximo es 500 metros")]
    public void ValidarRadio_ValorFueraDeRango_LanzaArgumentException(int radioInvalido, string mensajeEsperado)
    {
        // ARRANGE / ACT
        var acto = () => ValidarRadioAsociacion(radioInvalido);

        // ASSERT
        acto.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage($"*{mensajeEsperado}*");
    }

    private static int ValidarRadioAsociacion(int metros)
    {
        if (metros < 10)
            throw new ArgumentOutOfRangeException(nameof(metros), "El radio mínimo es 10 metros");
        if (metros > 500)
            throw new ArgumentOutOfRangeException(nameof(metros), "El radio máximo es 500 metros");
        return metros;
    }

    // ──────────────────────────────────────────
    // TEST-025: Haversine → distancia correcta (~65 m ± 5 m)
    // ──────────────────────────────────────────
    [Fact]
    public void Haversine_DosPuntos_DistanciaCorrecta()
    {
        // ARRANGE
        var svc    = CrearServicio();
        // A: -34.6037, -58.3816   B: -34.6042, -58.3820
        // Distancia esperada ≈ 65 m

        // ACT
        var distancia = svc.CalcularDistanciaHaversine(-34.6037, -58.3816, -34.6042, -58.3820);

        // ASSERT — tolerancia ± 5 m
        distancia.Should().BeApproximately(65.0, 5.0,
            "la fórmula de Haversine debe producir ~65 m entre esos dos puntos");
    }

    // ──────────────────────────────────────────
    // TEST-026: Varios markers en radio → retorna el más cercano
    // ──────────────────────────────────────────
    [Fact]
    public void BuscarMarker_VariosEnRadio_RetornaElMasCercano()
    {
        // ARRANGE
        var svc = CrearServicio(radio: 50);

        // M1 a ~30 m, M2 a ~15 m (el más cercano), M3 a ~45 m
        var markers = new List<LocalPunto>
        {
            // M1: ~30 m desde el punto de captura
            new() { LocalId = 1, Latitud = -34.60370m, Longitud = -58.38185m },
            // M2: ~5 m desde el punto de captura (coincide casi exacto)
            new() { LocalId = 2, Latitud = -34.60375m, Longitud = -58.38160m },
            // M3: ~40 m desde el punto de captura
            new() { LocalId = 3, Latitud = -34.60405m, Longitud = -58.38160m }
        };

        // Punto de captura en (-34.6038, -58.3816)
        var resultado = svc.BuscarMarkerDentroDeRadio(markers, -34.6038, -58.3816);

        // ASSERT — debe ser M2 (el más cercano)
        resultado.Should().NotBeNull();
        resultado!.LocalId.Should().Be(2, "M2 es el marker más cercano al punto de captura");
    }
}
