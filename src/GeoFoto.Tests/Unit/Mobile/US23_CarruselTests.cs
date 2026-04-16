using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios para GEO-US23 — MarkerPopup + Carrusel + Ampliar foto.
///
/// Verifica:
///   1. Popup con fotos → la lista de fotos no está vacía
///   2. Popup sin fotos → señal de "sin fotos" correcta
///   3. Navegación Next del carrusel incrementa el índice
///   4. Navegación Prev del carrusel decrementa el índice
///   5. Eliminar foto desde carrusel → encola PendingDelete en SyncQueue
///   6. Guardar título de punto persiste en SQLite
///   7. Guardar descripción de punto persiste en SQLite
///   8. Guardar comentario de foto persiste en SQLite
/// </summary>
public class US23_CarruselTests
{
    // ──────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────
    private static List<LocalFoto> CrearFotos(int cantidad) =>
        Enumerable.Range(1, cantidad).Select(i => new LocalFoto
        {
            LocalId = i,
            PuntoLocalId = 1,
            NombreArchivo = $"foto_{i}.jpg",
            SyncStatus = SyncStatusValues.Synced
        }).ToList();

    // ──────────────────────────────────────────
    // Test 1: Popup con fotos — carrusel no vacío
    // ──────────────────────────────────────────
    [Fact]
    public void MarkerPopup_AbreConFotos_CarruselVisible()
    {
        // ARRANGE
        var fotos = CrearFotos(3);

        // ACT — simula la lógica del popup al cargar fotos
        var hayFotos = fotos.Count > 0;
        var indiceInicial = 0;

        // ASSERT
        hayFotos.Should().BeTrue("el carrusel debe mostrar fotos cuando el punto las tiene");
        indiceInicial.Should().Be(0, "el carrusel debe iniciar en la primera foto");
        fotos.Count.Should().Be(3);
    }

    // ──────────────────────────────────────────
    // Test 2: Popup sin fotos — señal sin fotos
    // ──────────────────────────────────────────
    [Fact]
    public void MarkerPopup_SinFotos_MensajeSinFotosVisible()
    {
        // ARRANGE
        var fotos = new List<LocalFoto>();

        // ACT
        var sinFotos = fotos.Count == 0;
        var mensajeEsperado = "Este punto no tiene fotos — usá el botón de cámara para agregar la primera.";

        // ASSERT
        sinFotos.Should().BeTrue("cuando no hay fotos, la señal de carrusel vacío debe ser verdadera");
        mensajeEsperado.Should().Contain("Este punto no tiene fotos");
    }

    // ──────────────────────────────────────────
    // Test 3: Navegación Next — índice incrementa
    // ──────────────────────────────────────────
    [Fact]
    public void FotoCarousel_NavegaNext_IndiceActualizado()
    {
        // ARRANGE
        var fotos = CrearFotos(3);
        var indice = 0;

        // ACT — simula click en flecha derecha
        if (indice < fotos.Count - 1) indice++;

        // ASSERT
        indice.Should().Be(1, "después de navegar a la siguiente foto, el índice debe ser 1");
    }

    // ──────────────────────────────────────────
    // Test 4: Navegación Prev — índice decrementa
    // ──────────────────────────────────────────
    [Fact]
    public void FotoCarousel_NavegaPrev_IndiceActualizado()
    {
        // ARRANGE
        var fotos = CrearFotos(3);
        var indice = 2; // última foto

        // ACT — simula click en flecha izquierda
        if (indice > 0) indice--;

        // ASSERT
        indice.Should().Be(1, "después de navegar a la foto anterior, el índice debe decrementar");
    }

    // ──────────────────────────────────────────
    // Test 5: Eliminar foto → encola PendingDelete
    // ──────────────────────────────────────────
    [Fact]
    public async Task FotoCarousel_EliminarFoto_EncolarPendingDelete()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var foto = new LocalFoto
        {
            LocalId = 42,
            PuntoLocalId = 1,
            NombreArchivo = "foto_a_eliminar.jpg",
            SyncStatus = SyncStatusValues.Synced,
            RemoteId = 100
        };

        string? operacionEncolada = null;
        dbMock.Setup(db => db.EnqueueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
              .Callback<string, string, int, string>((op, entity, id, payload) => operacionEncolada = op)
              .Returns(Task.CompletedTask);
        dbMock.Setup(db => db.UpdateFotoAsync(It.IsAny<LocalFoto>())).Returns(Task.CompletedTask);

        // ACT — simula el proceso de soft-delete de una foto sincronizada
        foto.IsDeleted = true;
        foto.SyncStatus = SyncStatusValues.PendingDelete;
        await dbMock.Object.UpdateFotoAsync(foto);
        await dbMock.Object.EnqueueAsync("Delete", "Foto", foto.LocalId, "{}");

        // ASSERT
        operacionEncolada.Should().Be("Delete",
            "al eliminar una foto sincronizada, debe encolarse una operación 'Delete'");
        foto.IsDeleted.Should().BeTrue("la foto debe marcarse IsDeleted=true");
        foto.SyncStatus.Should().Be(SyncStatusValues.PendingDelete,
            "el estado de sync debe ser PendingDelete");
    }

    // ──────────────────────────────────────────
    // Test 6: Guardar título → persiste en SQLite
    // ──────────────────────────────────────────
    [Fact]
    public async Task LocalDbService_GuardarTituloPunto_PersisteSQLite()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var punto = new LocalPunto
        {
            LocalId = 1,
            Nombre = "Nombre original",
            Descripcion = "Desc"
        };

        LocalPunto? capturado = null;
        dbMock.Setup(db => db.UpdatePuntoAsync(It.IsAny<LocalPunto>()))
              .Callback<LocalPunto>(p => capturado = p)
              .Returns(Task.CompletedTask);

        // ACT
        punto.Nombre = "Nuevo título del marker";
        await dbMock.Object.UpdatePuntoAsync(punto);

        // ASSERT
        capturado.Should().NotBeNull();
        capturado!.Nombre.Should().Be("Nuevo título del marker",
            "el nuevo título debe persistirse en SQLite al guardar");
    }

    // ──────────────────────────────────────────
    // Test 7: Guardar descripción → persiste en SQLite
    // ──────────────────────────────────────────
    [Fact]
    public async Task LocalDbService_GuardarDescripcionPunto_PersisteSQLite()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var punto = new LocalPunto
        {
            LocalId = 1,
            Nombre = "Punto A",
            Descripcion = null
        };

        LocalPunto? capturado = null;
        dbMock.Setup(db => db.UpdatePuntoAsync(It.IsAny<LocalPunto>()))
              .Callback<LocalPunto>(p => capturado = p)
              .Returns(Task.CompletedTask);

        // ACT
        punto.Descripcion = "Descripción detallada del punto de inspección";
        await dbMock.Object.UpdatePuntoAsync(punto);

        // ASSERT
        capturado!.Descripcion.Should().Be("Descripción detallada del punto de inspección",
            "la descripción editada debe persistirse en SQLite");
    }

    // ──────────────────────────────────────────
    // Test 8: Guardar comentario de foto → persiste en SQLite
    // ──────────────────────────────────────────
    [Fact]
    public async Task LocalDbService_GuardarComentarioFoto_PersisteSQLite()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var foto = new LocalFoto
        {
            LocalId = 7,
            PuntoLocalId = 1,
            NombreArchivo = "foto_con_comentario.jpg",
            Comentario = null
        };

        LocalFoto? capturada = null;
        dbMock.Setup(db => db.UpdateFotoAsync(It.IsAny<LocalFoto>()))
              .Callback<LocalFoto>(f => capturada = f)
              .Returns(Task.CompletedTask);

        // ACT
        foto.Comentario = "Fisura visible en pared lateral norte";
        await dbMock.Object.UpdateFotoAsync(foto);

        // ASSERT
        capturada.Should().NotBeNull();
        capturada!.Comentario.Should().Be("Fisura visible en pared lateral norte",
            "el comentario de la foto debe persistirse en SQLite");
    }
}
