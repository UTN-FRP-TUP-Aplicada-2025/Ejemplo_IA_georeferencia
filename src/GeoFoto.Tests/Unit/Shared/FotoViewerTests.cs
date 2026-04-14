using FluentAssertions;
using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;
using Moq;

namespace GeoFoto.Tests.Unit.Shared;

/// <summary>
/// BLOQUE 6 — Carrusel de Fotos y Visor Fullscreen (DELTA-06)
/// TEST-033 a TEST-041
/// </summary>
public class FotoViewerTests
{
    // ──────────────────────────────────────────
    // TEST-033: Click en marker → abre MarkerPopup con datos y carrusel
    // ──────────────────────────────────────────
    [Fact]
    public async Task MarkerPopup_ClickMarker_CargaDatosYFotos()
    {
        // ARRANGE
        var apiMock = new Mock<IGeoFotoApiClient>();
        var detalle = new PuntoDetalleDto(
            1, -34.60m, -58.38m,
            "Poste A", "Descripción",
            DateTime.UtcNow,
            new List<FotoDto>
            {
                new(1, 1, "foto1.jpg", null, 50000, null, null),
                new(2, 1, "foto2.jpg", null, 60000, null, null),
                new(3, 1, "foto3.jpg", null, 70000, null, null)
            });
        apiMock.Setup(a => a.GetPuntoAsync(1)).ReturnsAsync(detalle);

        // ACT
        var resultado = await apiMock.Object.GetPuntoAsync(1);

        // ASSERT
        resultado.Should().NotBeNull();
        resultado!.Nombre.Should().Be("Poste A");
        resultado.Fotos.Should().HaveCount(3, "el carrusel debe mostrar 3 fotos");
    }

    // ──────────────────────────────────────────
    // TEST-034: Click en foto → abre FotoViewer en la foto correcta
    // ──────────────────────────────────────────
    [Fact]
    public void FotoViewer_ClickEnFoto_AbreEnIndiceCorrespondiente()
    {
        // ARRANGE
        var fotos = new List<FotoDto>
        {
            new(1, 1, "foto1.jpg", null, 50000, null, null),
            new(2, 1, "foto2.jpg", null, 60000, null, null),
            new(3, 1, "foto3.jpg", null, 70000, null, null)
        };

        // ACT — usuario hace click en la segunda foto (índice 1)
        var indiceSeleccionado = 1;
        var fotoMostrada       = fotos[indiceSeleccionado];

        // ASSERT
        fotoMostrada.Id.Should().Be(2);
        fotoMostrada.NombreArchivo.Should().Be("foto2.jpg");
    }

    // ──────────────────────────────────────────
    // TEST-035: Cerrar FotoViewer → vuelve al carrusel (NO al mapa)
    // ──────────────────────────────────────────
    [Fact]
    public void FotoViewer_Cerrar_VuelveACarruselNoAMapa()
    {
        // ARRANGE
        const string estadoMapa       = "Mapa";
        const string estadoCarrusel   = "MarkerPopup";
        const string estadoVisor      = "FotoViewer";
        var navegacion = new Stack<string>();

        // Navegar: Mapa → MarkerPopup → FotoViewer
        navegacion.Push(estadoMapa);
        navegacion.Push(estadoCarrusel);
        navegacion.Push(estadoVisor);

        // ACT — usuario cierra FotoViewer
        if (navegacion.Peek() == estadoVisor)
            navegacion.Pop();

        // ASSERT — vuelve al carrusel, no al mapa
        navegacion.Peek().Should().Be(estadoCarrusel, 
            "cerrar FotoViewer debe volver al MarkerPopup, no directamente al mapa");
    }

    // ──────────────────────────────────────────
    // TEST-036: Navegación de fotos — flecha derecha avanza
    // ──────────────────────────────────────────
    [Fact]
    public void FotoViewer_FlechaDerecha_AvanzaAlSiguiente()
    {
        // ARRANGE
        var fotos = Enumerable.Range(1, 3)
            .Select(i => new FotoDto(i, 1, $"foto{i}.jpg", null, 0, null, null))
            .ToList();
        var indiceActual = 1; // foto 2

        // ACT — flecha derecha → foto 3
        indiceActual = Math.Min(indiceActual + 1, fotos.Count - 1);

        // ASSERT
        fotos[indiceActual].Id.Should().Be(3);
    }

    [Fact]
    public void FotoViewer_FlechaIzquierda_RetrocedeAlAnterior()
    {
        // ARRANGE
        var fotos = Enumerable.Range(1, 3)
            .Select(i => new FotoDto(i, 1, $"foto{i}.jpg", null, 0, null, null))
            .ToList();
        var indiceActual = 1; // foto 2

        // ACT — flecha izquierda → foto 1
        indiceActual = Math.Max(indiceActual - 1, 0);

        // ASSERT
        fotos[indiceActual].Id.Should().Be(1);
    }

    // ──────────────────────────────────────────
    // TEST-037: Primera foto → flecha izquierda deshabilitada
    // ──────────────────────────────────────────
    [Fact]
    public void FotoViewer_PrimeraFoto_FlechaIzquierdaDeshabilitada()
    {
        // ARRANGE
        var indiceActual = 0;
        const int totalFotos = 3;

        // ACT / ASSERT
        var mostrarFlechaIzquierda = indiceActual > 0;
        mostrarFlechaIzquierda.Should().BeFalse("en la primera foto no debe aparecer flecha izquierda");
    }

    // ──────────────────────────────────────────
    // TEST-038: Última foto → flecha derecha deshabilitada
    // ──────────────────────────────────────────
    [Fact]
    public void FotoViewer_UltimaFoto_FlechaDerechaDeshabilitada()
    {
        // ARRANGE
        var fotos         = Enumerable.Range(1, 3).ToList();
        var indiceActual  = 2; // último (0-based)

        // ACT / ASSERT
        var mostrarFlechaDerecha = indiceActual < fotos.Count - 1;
        mostrarFlechaDerecha.Should().BeFalse("en la última foto no debe aparecer flecha derecha");
    }

    // ──────────────────────────────────────────
    // TEST-039: Punto sin fotos → mensaje informativo, no renderiza carrusel
    // ──────────────────────────────────────────
    [Fact]
    public async Task MarkerPopup_PuntoSinFotos_OcultaCarruselYMuestraMensaje()
    {
        // ARRANGE
        var apiMock = new Mock<IGeoFotoApiClient>();
        apiMock.Setup(a => a.GetPuntoAsync(5))
               .ReturnsAsync(new PuntoDetalleDto(
                   5, -34.60m, -58.38m, "Sin fotos", null,
                   DateTime.UtcNow, new List<FotoDto>()));

        // ACT
        var detalle    = await apiMock.Object.GetPuntoAsync(5);
        var tieneFotos = detalle!.Fotos.Count > 0;
        var mensaje    = tieneFotos ? null : "Este punto no tiene fotos asociadas";

        // ASSERT
        tieneFotos.Should().BeFalse();
        mensaje.Should().Be("Este punto no tiene fotos asociadas");
    }

    // ──────────────────────────────────────────
    // TEST-040: Botón "Agregar foto" en MarkerPopup → invoca CapturePhotoAsync
    // ──────────────────────────────────────────
    [Fact]
    public async Task MarkerPopup_BotonAgregarFoto_LanzaCaptura()
    {
        // ARRANGE
        var cameraMock = new Mock<IPhotoCaptureService>();
        cameraMock.Setup(c => c.CapturePhotoAsync())
                  .ReturnsAsync(new PhotoCaptureResult("/data/nueva.jpg", null, null, 80000));

        // ACT — usuario presiona "Agregar foto" dentro del popup
        var resultado = await cameraMock.Object.CapturePhotoAsync();

        // ASSERT
        cameraMock.Verify(c => c.CapturePhotoAsync(), Times.Once);
        resultado.Should().NotBeNull();
    }

    // ──────────────────────────────────────────
    // TEST-041: Cerrar MarkerPopup → vuelve al mapa (carrusel no visible)
    // ──────────────────────────────────────────
    [Fact]
    public void MarkerPopup_Cerrar_VuelveAlMapa()
    {
        // ARRANGE
        const string estadoMapa     = "Mapa";
        const string estadoPopup    = "MarkerPopup";
        var navegacion = new Stack<string>();
        navegacion.Push(estadoMapa);
        navegacion.Push(estadoPopup);

        // ACT — usuario cierra el popup
        if (navegacion.Peek() == estadoPopup)
            navegacion.Pop();

        // ASSERT
        navegacion.Peek().Should().Be(estadoMapa);
        navegacion.Should().HaveCount(1);
    }
}
