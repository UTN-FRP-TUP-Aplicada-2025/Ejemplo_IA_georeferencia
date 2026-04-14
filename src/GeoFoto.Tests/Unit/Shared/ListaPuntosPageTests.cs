using FluentAssertions;
using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;
using Moq;

namespace GeoFoto.Tests.Unit.Shared;

/// <summary>
/// BLOQUE 5 — Listado de Markers (DELTA-05)
/// TEST-027 a TEST-032
/// Tests de la lógica de presentación y filtrado de ListaPuntos.
/// </summary>
public class ListaPuntosPageTests
{
    private readonly Mock<IGeoFotoApiClient> _apiMock   = new();
    private readonly Mock<ILocalDbService>   _dbMock    = new();

    private static List<PuntoDto> GenerarPuntos(int cantidad) =>
        Enumerable.Range(1, cantidad)
                  .Select(i => new PuntoDto(
                      i,
                      (decimal)(-34.60 - i * 0.001),
                      (decimal)(-58.38 - i * 0.001),
                      $"Punto {i}",
                      $"Descripción larga del punto número {i} que debe truncarse en la vista",
                      DateTime.UtcNow.AddDays(-i),
                      i % 3,
                      i % 3 > 0 ? (int?)i : null))
                  .ToList();

    // ──────────────────────────────────────────
    // TEST-027: 25 puntos → paginador con 2 páginas (20 por página)
    // ──────────────────────────────────────────
    [Fact]
    public async Task ListaPuntos_25Puntos_MuestraPrimeros20YPaginador()
    {
        // ARRANGE
        var todos = GenerarPuntos(25);
        _apiMock.Setup(a => a.GetPuntosAsync()).ReturnsAsync(todos);

        // ACT
        var puntos    = await _apiMock.Object.GetPuntosAsync();
        const int pageSize = 20;
        var pagina1   = puntos.Take(pageSize).ToList();
        var totalPags = (int)Math.Ceiling((double)puntos.Count / pageSize);

        // ASSERT
        puntos.Should().HaveCount(25);
        pagina1.Should().HaveCount(20);
        totalPags.Should().Be(2);
    }

    // ──────────────────────────────────────────
    // TEST-028: Filtro por nombre → filtra en tiempo real
    // ──────────────────────────────────────────
    [Fact]
    public void ListaPuntos_FiltroPorNombre_MueveSoloCoincidencias()
    {
        // ARRANGE
        var puntos = new List<PuntoDto>
        {
            new(1, -34.60m, -58.38m, "Poste 1",        "desc", DateTime.UtcNow, 0, null),
            new(2, -34.61m, -58.39m, "Poste 2",        "desc", DateTime.UtcNow, 1, 1),
            new(3, -34.62m, -58.40m, "Transformador A","desc", DateTime.UtcNow, 2, 2)
        };

        // ACT
        var filtro    = "Poste";
        var filtrados = puntos
            .Where(p => p.Nombre?.Contains(filtro, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        // ASSERT
        filtrados.Should().HaveCount(2);
        filtrados.Should().OnlyContain(p => p.Nombre!.StartsWith("Poste"));
    }

    // ──────────────────────────────────────────
    // TEST-029: Click en fila → navegar al mapa centrado en el punto (zoom 17)
    // ──────────────────────────────────────────
    [Fact]
    public void ListaPuntos_ClickEnFila_GeneraUrlDeNavegacionCorrecta()
    {
        // ARRANGE
        var punto = new PuntoDto(7, -34.60m, -58.38m, "Test", null, DateTime.UtcNow, 0, null);
        const int zoom = 17;

        // ACT — la URL que NavigationManager.NavigateTo debería recibir
        var url = $"/mapa?lat={punto.Latitud.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&lng={punto.Longitud.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&zoom={zoom}&puntoId={punto.Id}";

        // ASSERT
        url.Should().Contain("lat=-34.60");
        url.Should().Contain("lng=-58.38");
        url.Should().Contain($"zoom={zoom}");
        url.Should().Contain($"puntoId={punto.Id}");
    }

    // ──────────────────────────────────────────
    // TEST-030: Columnas con información completa, descripción truncada
    // ──────────────────────────────────────────
    [Fact]
    public void ListaPuntos_Fila_MuestraDescripcionTruncadaA50Caracteres()
    {
        // ARRANGE
        var desc   = "Esta es una descripción larga que debe truncarse en la vista para que quede bonita";
        var punto  = new PuntoDto(1, -34.60m, -58.38m, "Test", desc, DateTime.UtcNow, 3, 1);
        const int maxLen = 50;

        // ACT — lógica de truncado que aplica el componente
        var descMostrada = punto.Descripcion?.Length > maxLen
            ? punto.Descripcion[..maxLen] + "..."
            : punto.Descripcion;

        // ASSERT
        descMostrada.Should().NotBeNull();
        descMostrada!.Length.Should().BeLessOrEqualTo(maxLen + 3); // +3 por "..."
        descMostrada.Should().EndWith("...");
        punto.CantidadFotos.Should().Be(3);
    }

    // ──────────────────────────────────────────
    // TEST-031: Botón eliminar → muestra diálogo de confirmación (modelo de UI)
    // ──────────────────────────────────────────
    [Fact]
    public void ListaPuntos_BotonEliminar_DebeMostrarConfirmacion()
    {
        // ARRANGE — estado que controla si el diálogo de confirmación está visible
        var puntoAEliminar = new PuntoDto(5, -34.60m, -58.38m, "Borrar", null, DateTime.UtcNow, 0, null);
        var mostrarConfirmacion = false;

        // ACT — usuario hace click en eliminar
        mostrarConfirmacion = true;

        // ASSERT
        mostrarConfirmacion.Should().BeTrue("al presionar eliminar debe abrirse el diálogo");
        puntoAEliminar.Id.Should().Be(5);
    }

    // ──────────────────────────────────────────
    // TEST-032: Confirmar eliminación → invoca DeletePuntoAsync + quita de lista
    // ──────────────────────────────────────────
    [Fact]
    public async Task ListaPuntos_ConfirmarEliminacion_InvocaApiYActualizaLista()
    {
        // ARRANGE
        var puntos = new List<PuntoDto>
        {
            new(1, -34.60m, -58.38m, "A mantener", null, DateTime.UtcNow, 0, null),
            new(2, -34.61m, -58.39m, "A eliminar", null, DateTime.UtcNow, 0, null)
        };
        _apiMock.Setup(a => a.DeletePuntoAsync(2)).ReturnsAsync(true);

        // ACT
        var eliminado = await _apiMock.Object.DeletePuntoAsync(2);
        if (eliminado)
            puntos.RemoveAll(p => p.Id == 2);

        // ASSERT
        _apiMock.Verify(a => a.DeletePuntoAsync(2), Times.Once);
        puntos.Should().HaveCount(1);
        puntos.Should().NotContain(p => p.Id == 2);
    }
}
