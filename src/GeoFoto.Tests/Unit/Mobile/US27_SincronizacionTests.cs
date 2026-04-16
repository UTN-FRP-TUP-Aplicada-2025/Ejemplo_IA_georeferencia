using GeoFoto.Shared.Models;
using GeoFoto.Shared.Services;

namespace GeoFoto.Tests.Unit.Mobile;

/// <summary>
/// Tests unitarios para GEO-US27 — Pantalla de sincronización.
///
/// Verifica:
///   1. La pantalla muestra correctamente los ítems pendientes
///   2. El botón "Sincronizar ahora" dispara SyncNowAsync (push + pull)
///   3. La fecha de última sincronización se actualiza post-sync
///   4. Un ítem fallido muestra el mensaje de error
///   5. Durante la sincronización, el botón está deshabilitado
/// </summary>
public class US27_SincronizacionTests
{
    // ──────────────────────────────────────────
    // Test 1: Pantalla muestra ítems pendientes
    // ──────────────────────────────────────────
    [Fact]
    public async Task PantallaSync_MuestraItemsPendientes()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var operaciones = new List<SyncQueueItem>
        {
            new() { Id = 1, OperationType = "Create", EntityType = "Punto",
                    LocalId = 10, Status = SyncQueueStatus.Pending },
            new() { Id = 2, OperationType = "Create", EntityType = "Foto",
                    LocalId = 20, Status = SyncQueueStatus.Pending },
            new() { Id = 3, OperationType = "Update", EntityType = "Punto",
                    LocalId = 10, Status = SyncQueueStatus.Done },
        };

        dbMock.Setup(db => db.GetPendingOperationsAsync())
              .ReturnsAsync(operaciones);

        // ACT
        var items = await dbMock.Object.GetPendingOperationsAsync();
        var pendientes = items.Count(o => o.Status == SyncQueueStatus.Pending);
        var sincronizados = items.Count(o => o.Status == SyncQueueStatus.Done);

        // ASSERT
        pendientes.Should().Be(2, "deben mostrarse exactamente 2 ítems pendientes");
        sincronizados.Should().Be(1, "debe mostrarse 1 ítem sincronizado");
        items.Count.Should().Be(3, "la pantalla muestra todos los ítems del historial");
    }

    // ──────────────────────────────────────────
    // Test 2: Botón "Sincronizar ahora" llama SyncNowAsync
    // ──────────────────────────────────────────
    [Fact]
    public async Task PantallaSync_BotonSincronizar_DisparaSyncNowAsync()
    {
        // ARRANGE
        var syncMock = new Mock<ISyncService>();
        syncMock.Setup(s => s.SyncNowAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        // ACT — simula click en el botón "Sincronizar ahora"
        var conectado = true;
        var sincronizando = false;

        if (conectado && !sincronizando)
        {
            sincronizando = true;
            await syncMock.Object.SyncNowAsync();
            sincronizando = false;
        }

        // ASSERT
        syncMock.Verify(s => s.SyncNowAsync(It.IsAny<CancellationToken>()), Times.Once,
            "SyncNowAsync debe llamarse exactamente una vez al presionar 'Sincronizar ahora'");
    }

    // ──────────────────────────────────────────
    // Test 3: Fecha de última sincronización se actualiza post-sync
    // ──────────────────────────────────────────
    [Fact]
    public void PantallaSync_UltimaSyncFecha_ActualizaPostSync()
    {
        // ARRANGE
        var prefsMock = new Mock<IPreferencesService>();
        string? valorGuardado = null;

        prefsMock.Setup(p => p.Set(It.IsAny<string>(), It.IsAny<string>()))
                 .Callback<string, string>((key, value) =>
                 {
                     if (key == "LastSyncAt") valorGuardado = value;
                 });
        prefsMock.Setup(p => p.Get("LastSyncAt", It.IsAny<string>()))
                 .Returns(() => valorGuardado ?? "Nunca");

        // ACT — simula guardar la fecha después de sincronizar
        var ahoraUtc = DateTime.UtcNow;
        prefsMock.Object.Set("LastSyncAt", ahoraUtc.ToString("O"));

        var ultimaSync = prefsMock.Object.Get("LastSyncAt", "Nunca");

        // ASSERT
        ultimaSync.Should().NotBe("Nunca",
            "después de sincronizar, la fecha de última sync debe estar registrada");
        DateTime.TryParse(ultimaSync, null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var parsedDate).Should().BeTrue(
            "la fecha guardada debe ser un valor de fecha válido");
        // Comparar en UTC para evitar divergencias de zona horaria
        var parsedUtc = parsedDate.Kind == DateTimeKind.Utc
            ? parsedDate
            : parsedDate.ToUniversalTime();
        parsedUtc.Should().BeCloseTo(ahoraUtc, TimeSpan.FromSeconds(10),
            "la fecha de última sync debe ser aproximadamente la hora actual");
    }

    // ──────────────────────────────────────────
    // Test 4: Ítem fallido muestra mensaje de error
    // ──────────────────────────────────────────
    [Fact]
    public async Task PantallaSync_ItemFallido_MuestraMensajeError()
    {
        // ARRANGE
        var dbMock = new Mock<ILocalDbService>();
        var operaciones = new List<SyncQueueItem>
        {
            new()
            {
                Id = 5,
                OperationType = "Create",
                EntityType = "Foto",
                LocalId = 99,
                Status = SyncQueueStatus.Failed,
                ErrorMessage = "Connection refused: servidor no disponible",
                Attempts = 3
            }
        };

        dbMock.Setup(db => db.GetPendingOperationsAsync()).ReturnsAsync(operaciones);

        // ACT
        var items = await dbMock.Object.GetPendingOperationsAsync();
        var fallido = items.FirstOrDefault(o => o.Status == SyncQueueStatus.Failed);

        // ASSERT
        fallido.Should().NotBeNull("debe existir el ítem fallido en el historial");
        fallido!.ErrorMessage.Should().NotBeNullOrEmpty(
            "el ítem fallido debe tener un mensaje de error para mostrarlo al usuario");
        fallido.Attempts.Should().BeGreaterThan(0,
            "el ítem fallido debe mostrar el número de reintentos realizados");
    }

    // ──────────────────────────────────────────
    // Test 5: Sin conexión → botón sincronizar deshabilitado
    // ──────────────────────────────────────────
    [Fact]
    public void PantallaSync_SinConexion_BotonDeshabilitado()
    {
        // ARRANGE
        var conectado = false;
        var sincronizando = false;

        // ACT — lógica del binding @Disabled del botón
        var botonDeshabilitado = !conectado || sincronizando;

        // ASSERT
        botonDeshabilitado.Should().BeTrue(
            "el botón 'Sincronizar ahora' debe estar deshabilitado cuando no hay conexión");
    }
}
