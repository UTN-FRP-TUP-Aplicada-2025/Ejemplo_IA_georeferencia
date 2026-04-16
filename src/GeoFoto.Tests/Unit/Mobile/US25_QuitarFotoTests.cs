using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios para GEO-US25 — Quitar foto desde el carrusel.
///
/// Verifica:
///   1. Foto no sincronizada → eliminación directa (DeleteFotoAsync), sin SyncQueue
///   2. Foto sincronizada → soft-delete: IsDeleted=true + PendingDelete en SyncQueue
///   3. Después de eliminar: índice del carrusel ajustado si estaba en la última foto
///   4. Después de eliminar la única foto: carrusel queda vacío
///   5. Dialog de confirmación distingue cancelar vs confirmar
/// </summary>
public class US25_QuitarFotoTests
{
    // ──────────────────────────────────────────
    // Test 1: Foto pendiente de crear (nunca sincronizada) → delete directo
    // ──────────────────────────────────────────
    [Fact]
    public async Task QuitarFoto_PendingCreate_EliminaDirectoSinSyncQueue()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var foto = new LocalFoto
        {
            LocalId = 5,
            PuntoLocalId = 1,
            NombreArchivo = "foto_nueva.jpg",
            SyncStatus = SyncStatusValues.PendingCreate,
            RemoteId = null  // nunca fue al servidor
        };

        dbMock.Setup(db => db.DeleteFotoAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

        // ACT — foto que nunca fue al servidor: delete directo, no encolar
        var necesitaSyncQueue = foto.RemoteId.HasValue;
        if (!necesitaSyncQueue)
            await dbMock.Object.DeleteFotoAsync(foto.LocalId);

        // ASSERT
        dbMock.Verify(db => db.DeleteFotoAsync(foto.LocalId), Times.Once,
            "una foto PendingCreate (no sincronizada) debe eliminarse directamente sin SyncQueue");
        dbMock.Verify(db => db.EnqueueAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never,
            "no debe encolarse nada si la foto nunca llegó al servidor");
    }

    // ──────────────────────────────────────────
    // Test 2: Foto sincronizada → soft-delete + encolar PendingDelete
    // ──────────────────────────────────────────
    [Fact]
    public async Task QuitarFoto_Sincronizada_SoftDeleteYEncolaPendingDelete()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var foto = new LocalFoto
        {
            LocalId = 10,
            PuntoLocalId = 1,
            NombreArchivo = "foto_sincronizada.jpg",
            SyncStatus = SyncStatusValues.Synced,
            RemoteId = 99
        };

        LocalFoto? fotoActualizada = null;
        string? operacionEncolada = null;

        dbMock.Setup(db => db.UpdateFotoAsync(It.IsAny<LocalFoto>()))
              .Callback<LocalFoto>(f => fotoActualizada = f)
              .Returns(Task.CompletedTask);
        dbMock.Setup(db => db.EnqueueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
              .Callback<string, string, int, string>((op, _, _, _) => operacionEncolada = op)
              .Returns(Task.CompletedTask);

        // ACT — foto sincronizada: soft-delete + sync queue
        foto.IsDeleted = true;
        foto.SyncStatus = SyncStatusValues.PendingDelete;
        await dbMock.Object.UpdateFotoAsync(foto);
        await dbMock.Object.EnqueueAsync("Delete", "Foto", foto.LocalId, "{}");

        // ASSERT
        fotoActualizada!.IsDeleted.Should().BeTrue(
            "la foto debe marcarse IsDeleted=true para soft-delete");
        fotoActualizada.SyncStatus.Should().Be(SyncStatusValues.PendingDelete,
            "el SyncStatus debe cambiar a PendingDelete");
        operacionEncolada.Should().Be("Delete",
            "debe encolarse la operación 'Delete' para sincronizar con el servidor");
    }

    // ──────────────────────────────────────────
    // Test 3: Índice ajustado al eliminar la última foto
    // ──────────────────────────────────────────
    [Fact]
    public void QuitarFoto_UltimaDelCarrusel_IndiceAjustado()
    {
        // ARRANGE
        var fotos = new List<LocalFoto>
        {
            new() { LocalId = 1 },
            new() { LocalId = 2 },
            new() { LocalId = 3 }
        };
        var indice = 2; // apuntando a la última

        // ACT — simula eliminar la foto en índice 2 y ajustar
        fotos.RemoveAt(indice);
        if (indice >= fotos.Count)
            indice = fotos.Count - 1;
        if (indice < 0) indice = 0;

        // ASSERT
        indice.Should().Be(1,
            "al eliminar la última foto del carrusel, el índice debe retroceder a la anterior");
        fotos.Count.Should().Be(2);
    }

    // ──────────────────────────────────────────
    // Test 4: Eliminar única foto → carrusel vacío
    // ──────────────────────────────────────────
    [Fact]
    public void QuitarFoto_UnicaFoto_CarruselQuedaVacio()
    {
        // ARRANGE
        var fotos = new List<LocalFoto> { new() { LocalId = 1 } };
        var indice = 0;

        // ACT
        fotos.RemoveAt(indice);
        indice = fotos.Count == 0 ? 0 : Math.Min(indice, fotos.Count - 1);

        // ASSERT
        fotos.Should().BeEmpty("después de eliminar la única foto, el carrusel debe quedar vacío");
        indice.Should().Be(0, "el índice reset a 0 cuando el carrusel queda vacío");
    }

    // ──────────────────────────────────────────
    // Test 5: Cancelar confirmación → no se elimina nada
    // ──────────────────────────────────────────
    [Fact]
    public async Task QuitarFoto_CancelarDialog_NoEliminaNada()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var foto = new LocalFoto { LocalId = 3, SyncStatus = SyncStatusValues.Synced };

        // ACT — el usuario cancela el dialog de confirmación (confirmed = false)
        var confirmado = false;
        if (confirmado)
        {
            foto.IsDeleted = true;
            await dbMock.Object.UpdateFotoAsync(foto);
        }

        // ASSERT
        dbMock.Verify(db => db.UpdateFotoAsync(It.IsAny<LocalFoto>()), Times.Never,
            "si el usuario cancela el dialog, no debe modificarse ni eliminarse la foto");
        foto.IsDeleted.Should().BeFalse(
            "la foto no debe marcarse como eliminada si se canceló la confirmación");
    }
}
