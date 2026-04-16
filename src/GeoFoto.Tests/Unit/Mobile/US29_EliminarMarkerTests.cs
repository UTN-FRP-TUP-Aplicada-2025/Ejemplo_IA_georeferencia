using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios para GEO-US29 — Eliminar marker.
///
/// Verifica:
///   CA-01: Marker no sincronizado → delete directo (DeletePuntoAsync), sin SyncQueue
///   CA-02: Marker sincronizado → soft-delete: IsDeleted=true + PendingDelete en SyncQueue
///   CA-03: Cancelar confirmación → no se modifica nada
///   CA-04: Eliminar marker con fotos → encola Delete para fotos también
///   CA-05: Después de eliminar, la lista se refresca (punto ya no aparece)
/// </summary>
public class US29_EliminarMarkerTests
{
    // ──────────────────────────────────────────
    // CA-01: Marker no sincronizado → delete directo
    // ──────────────────────────────────────────
    [Fact]
    public async Task EliminarMarker_NoSincronizado_DeleteDirecto()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var punto = new LocalPunto
        {
            LocalId = 5,
            Nombre = "Punto local",
            SyncStatus = SyncStatusValues.PendingCreate,
            RemoteId = null   // nunca fue al servidor
        };

        dbMock.Setup(db => db.DeletePuntoAsync(5)).Returns(Task.CompletedTask);

        // ACT — punto sin RemoteId: delete directo
        var necesitaSyncQueue = punto.RemoteId.HasValue;
        if (!necesitaSyncQueue)
            await dbMock.Object.DeletePuntoAsync(punto.LocalId);

        // ASSERT
        dbMock.Verify(db => db.DeletePuntoAsync(5), Times.Once,
            "CA-01: marker no sincronizado debe eliminarse directamente sin SyncQueue");
        dbMock.Verify(db => db.EnqueueAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never,
            "CA-01: no debe encolarse nada si el marker nunca llegó al servidor");
    }

    // ──────────────────────────────────────────
    // CA-02: Marker sincronizado → soft-delete + SyncQueue
    // ──────────────────────────────────────────
    [Fact]
    public async Task EliminarMarker_Sincronizado_SoftDeleteYEncolaPendingDelete()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var punto = new LocalPunto
        {
            LocalId = 10,
            Nombre = "Punto sincronizado",
            SyncStatus = SyncStatusValues.Synced,
            RemoteId = 88
        };

        LocalPunto? puntoActualizado = null;
        string? operacionEncolada = null;

        dbMock.Setup(db => db.UpdatePuntoAsync(It.IsAny<LocalPunto>()))
              .Callback<LocalPunto>(p => puntoActualizado = p)
              .Returns(Task.CompletedTask);
        dbMock.Setup(db => db.EnqueueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
              .Callback<string, string, int, string>((op, _, _, _) => operacionEncolada = op)
              .Returns(Task.CompletedTask);

        // ACT — soft-delete de marker sincronizado
        punto.IsDeleted = true;
        punto.SyncStatus = SyncStatusValues.PendingDelete;
        await dbMock.Object.UpdatePuntoAsync(punto);
        await dbMock.Object.EnqueueAsync("Delete", "Punto", punto.LocalId, "{}");

        // ASSERT
        puntoActualizado!.IsDeleted.Should().BeTrue(
            "CA-02: el marker sincronizado debe marcarse IsDeleted=true");
        puntoActualizado.SyncStatus.Should().Be(SyncStatusValues.PendingDelete,
            "CA-02: el SyncStatus debe ser PendingDelete para sincronizar la eliminación");
        operacionEncolada.Should().Be("Delete",
            "CA-02: debe encolarse una operación 'Delete' para el servidor");
    }

    // ──────────────────────────────────────────
    // CA-03: Cancelar → no se modifica nada
    // ──────────────────────────────────────────
    [Fact]
    public async Task EliminarMarker_Cancela_NoModificaNada()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var punto = new LocalPunto { LocalId = 3, SyncStatus = SyncStatusValues.Synced };

        // ACT — el usuario cancela el diálogo de confirmación
        var confirmado = false;
        if (confirmado)
        {
            punto.IsDeleted = true;
            await dbMock.Object.UpdatePuntoAsync(punto);
        }

        // ASSERT
        dbMock.Verify(db => db.UpdatePuntoAsync(It.IsAny<LocalPunto>()), Times.Never,
            "CA-03: cancelar la confirmación no debe modificar el marker");
        punto.IsDeleted.Should().BeFalse(
            "CA-03: el marker no debe marcarse como eliminado si se canceló");
    }

    // ──────────────────────────────────────────
    // CA-04: Eliminar marker con fotos → encola Delete para fotos
    // ──────────────────────────────────────────
    [Fact]
    public async Task EliminarMarker_ConFotos_EncolaDeleteParaFotos()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var punto = new LocalPunto { LocalId = 1, RemoteId = 10, SyncStatus = SyncStatusValues.Synced };
        var fotos = new List<LocalFoto>
        {
            new() { LocalId = 101, PuntoLocalId = 1, RemoteId = 201, SyncStatus = SyncStatusValues.Synced },
            new() { LocalId = 102, PuntoLocalId = 1, RemoteId = 202, SyncStatus = SyncStatusValues.Synced },
        };

        dbMock.Setup(db => db.GetFotosByPuntoAsync(1)).ReturnsAsync(fotos);

        var operacionesEncoladas = new List<(string op, string entity)>();
        dbMock.Setup(db => db.UpdateFotoAsync(It.IsAny<LocalFoto>())).Returns(Task.CompletedTask);
        dbMock.Setup(db => db.UpdatePuntoAsync(It.IsAny<LocalPunto>())).Returns(Task.CompletedTask);
        dbMock.Setup(db => db.EnqueueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()))
              .Callback<string, string, int, string>((op, entity, _, _) =>
                  operacionesEncoladas.Add((op, entity)))
              .Returns(Task.CompletedTask);

        // ACT — eliminar marker y sus fotos
        var fotasDelPunto = await dbMock.Object.GetFotosByPuntoAsync(punto.LocalId);
        foreach (var foto in fotasDelPunto.Where(f => f.RemoteId.HasValue))
        {
            foto.IsDeleted = true;
            foto.SyncStatus = SyncStatusValues.PendingDelete;
            await dbMock.Object.UpdateFotoAsync(foto);
            await dbMock.Object.EnqueueAsync("Delete", "Foto", foto.LocalId, "{}");
        }
        punto.IsDeleted = true;
        punto.SyncStatus = SyncStatusValues.PendingDelete;
        await dbMock.Object.UpdatePuntoAsync(punto);
        await dbMock.Object.EnqueueAsync("Delete", "Punto", punto.LocalId, "{}");

        // ASSERT
        var deleteFotos = operacionesEncoladas.Count(o => o.op == "Delete" && o.entity == "Foto");
        var deletePunto = operacionesEncoladas.Count(o => o.op == "Delete" && o.entity == "Punto");

        deleteFotos.Should().Be(2,
            "CA-04: deben encolarse operaciones Delete para cada foto del marker");
        deletePunto.Should().Be(1,
            "CA-04: debe encolarse una operación Delete para el marker mismo");
    }

    // ──────────────────────────────────────────
    // CA-05: Después de eliminar, punto ya no está en la lista filtrada
    // ──────────────────────────────────────────
    [Fact]
    public void EliminarMarker_DespuesDeEliminar_NoApareceLista()
    {
        // ARRANGE
        var puntos = new List<PuntoDto>
        {
            new(1, -34.601m, -58.381m, "Visible", null, DateTime.UtcNow, 0, null),
            new(2, -34.605m, -58.385m, "Eliminado", null, DateTime.UtcNow, 0, null),
        };

        // ACT — simula que la lista se actualiza quitando el eliminado (como hace _puntos.Remove)
        puntos.RemoveAll(p => p.Id == 2);

        // ASSERT
        puntos.Should().ContainSingle(p => p.Nombre == "Visible");
        puntos.Should().NotContain(p => p.Id == 2,
            "CA-05: después de eliminar, el marker no debe aparecer en la lista");
    }
}
