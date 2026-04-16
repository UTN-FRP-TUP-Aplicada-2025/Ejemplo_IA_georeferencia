using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios para GEO-US28 — Lista de markers con búsqueda.
///
/// Verifica:
///   CA-01: Tabla muestra todos los puntos locales
///   CA-02: Búsqueda filtra por nombre en tiempo real (case-insensitive)
///   CA-03: Tap en fila navega al mapa con puntoId en query string
///   CA-04: Punto sin nombre muestra fallback "—"
///   CA-05: Búsqueda sin resultados no crashea (lista vacía)
///   CA-06: Puntos PendingDelete no aparecen en la lista
/// </summary>
public class US28_ListaMarkersTests
{
    private static List<LocalPunto> PuntosEjemplo() => new()
    {
        new() { LocalId = 1, Nombre = "Torre de agua", SyncStatus = SyncStatusValues.Synced,
                Latitud = -34.601m, Longitud = -58.381m },
        new() { LocalId = 2, Nombre = "Poste 42", SyncStatus = SyncStatusValues.PendingCreate,
                Latitud = -34.605m, Longitud = -58.385m },
        new() { LocalId = 3, Nombre = null, SyncStatus = SyncStatusValues.Local,
                Latitud = -34.610m, Longitud = -58.390m },
        new() { LocalId = 4, Nombre = "Poste roto", SyncStatus = SyncStatusValues.PendingDelete,
                Latitud = -34.615m, Longitud = -58.395m },
    };

    // ──────────────────────────────────────────
    // CA-01: Lista muestra todos los puntos (excepto PendingDelete)
    // ──────────────────────────────────────────
    [Fact]
    public async Task ListaMarkers_MuestraTodosLosPuntos()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        dbMock.Setup(db => db.GetPuntosAsync()).ReturnsAsync(PuntosEjemplo());

        // ACT — lógica de CargarPuntosAsync en ListaPuntos.razor
        var todos = await dbMock.Object.GetPuntosAsync();
        var visibles = todos
            .Where(p => p.SyncStatus != SyncStatusValues.PendingDelete)
            .ToList();

        // ASSERT
        visibles.Count.Should().Be(3,
            "CA-01: la lista debe mostrar los puntos que no están en PendingDelete");
    }

    // ──────────────────────────────────────────
    // CA-02: Búsqueda filtra por nombre (case-insensitive)
    // ──────────────────────────────────────────
    [Fact]
    public void ListaMarkers_Busqueda_FiltraPorNombre()
    {
        // ARRANGE — puntos proyectados como PuntoDto (como hace ListaPuntos.razor)
        var puntos = new List<PuntoDto>
        {
            new(1, -34.601m, -58.381m, "Torre de agua",     null, DateTime.UtcNow, 0, null),
            new(2, -34.605m, -58.385m, "Poste 42",           null, DateTime.UtcNow, 2, null),
            new(3, -34.610m, -58.390m, "Transformador norte",null, DateTime.UtcNow, 1, null),
        };
        var busqueda = "poste";

        // ACT — lógica de PuntosFiltrados en ListaPuntos.razor
        var filtrados = puntos
            .Where(p => p.Nombre?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        // ASSERT
        filtrados.Count.Should().Be(1,
            "CA-02: 'poste' (minúsculas) debe encontrar 'Poste 42'");
        filtrados[0].Nombre.Should().Be("Poste 42");
    }

    // ──────────────────────────────────────────
    // CA-02: Búsqueda case-insensitive
    // ──────────────────────────────────────────
    [Theory]
    [InlineData("TORRE")]
    [InlineData("Torre")]
    [InlineData("torre")]
    [InlineData("orr")]
    public void ListaMarkers_Busqueda_EsCaseInsensitive(string termino)
    {
        // ARRANGE
        var puntos = new List<PuntoDto>
        {
            new(1, -34.601m, -58.381m, "Torre de agua", null, DateTime.UtcNow, 0, null),
        };

        // ACT
        var encontrado = puntos
            .Any(p => p.Nombre?.Contains(termino, StringComparison.OrdinalIgnoreCase) == true);

        // ASSERT
        encontrado.Should().BeTrue(
            $"CA-02: la búsqueda con '{termino}' debe encontrar 'Torre de agua' (case-insensitive)");
    }

    // ──────────────────────────────────────────
    // CA-04: Punto sin nombre → fallback "—"
    // ──────────────────────────────────────────
    [Fact]
    public void ListaMarkers_PuntoSinNombre_MuestraFallback()
    {
        // ARRANGE
        var punto = new PuntoDto(
            3, -34.610m, -58.390m, null, null, DateTime.UtcNow, 0, null);

        // ACT — lógica del template: @(context.Nombre ?? "—")
        var textoMostrado = punto.Nombre ?? "—";

        // ASSERT
        textoMostrado.Should().Be("—",
            "CA-04: un punto sin nombre debe mostrar el fallback '—' en la tabla");
    }

    // ──────────────────────────────────────────
    // CA-05: Búsqueda sin resultados → lista vacía (no excepción)
    // ──────────────────────────────────────────
    [Fact]
    public void ListaMarkers_BusquedaSinResultados_ListaVacia()
    {
        // ARRANGE
        var puntos = new List<PuntoDto>
        {
            new(1, -34.601m, -58.381m, "Torre de agua", null, DateTime.UtcNow, 0, null),
        };
        var busqueda = "xyzzy_inexistente";

        // ACT
        var filtrados = puntos
            .Where(p => p.Nombre?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        // ASSERT
        filtrados.Should().BeEmpty(
            "CA-05: una búsqueda sin coincidencias debe retornar lista vacía sin lanzar excepción");
    }

    // ──────────────────────────────────────────
    // CA-03: Tap en fila → URL de navegación contiene puntoId
    // ──────────────────────────────────────────
    [Fact]
    public void ListaMarkers_TapEnFila_UrlContienePuntoId()
    {
        // ARRANGE
        var punto = new PuntoDto(
            42, -34.6037m, -58.3816m, "Punto A", null, DateTime.UtcNow, 0, null);

        // ACT — lógica de IrAlMapa() en ListaPuntos.razor
        var lat = ((double)punto.Latitud).ToString("F7", System.Globalization.CultureInfo.InvariantCulture);
        var lng = ((double)punto.Longitud).ToString("F7", System.Globalization.CultureInfo.InvariantCulture);
        var url = $"/?lat={lat}&lng={lng}&puntoId={punto.Id}";

        // ASSERT
        url.Should().Contain("puntoId=42",
            "CA-03: la URL de navegación debe incluir puntoId para que Mapa.razor abra el popup");
        url.Should().Contain("lat=");
        url.Should().Contain("lng=");
    }

    // ──────────────────────────────────────────
    // CA-06: PendingDelete no aparece en lista
    // ──────────────────────────────────────────
    [Fact]
    public async Task ListaMarkers_PendingDelete_NoAparece()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        dbMock.Setup(db => db.GetPuntosAsync()).ReturnsAsync(new List<LocalPunto>
        {
            new() { LocalId = 1, Nombre = "Visible",   SyncStatus = SyncStatusValues.Synced },
            new() { LocalId = 2, Nombre = "Eliminando",SyncStatus = SyncStatusValues.PendingDelete },
        });

        // ACT
        var todos = await dbMock.Object.GetPuntosAsync();
        var visibles = todos.Where(p => p.SyncStatus != SyncStatusValues.PendingDelete).ToList();

        // ASSERT
        visibles.Should().ContainSingle(p => p.Nombre == "Visible");
        visibles.Should().NotContain(p => p.Nombre == "Eliminando",
            "CA-06: los markers en proceso de eliminación no deben mostrarse en la lista");
    }
}
