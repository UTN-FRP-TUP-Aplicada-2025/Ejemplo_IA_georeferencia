using FluentAssertions;
using GeoFoto.Api.Data;
using GeoFoto.Api.Dtos;
using GeoFoto.Api.Models;
using GeoFoto.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GeoFoto.Tests.Integration.Api;

/// <summary>
/// Tests de integración — capa de servicio de la API sobre SQLite in-memory.
/// TEST-052 a TEST-059
/// Se testea directamente PuntosService (sin HTTP) para evitar el conflicto de
/// proveedores EF Core SQL Server + SQLite en el mismo proceso.
/// </summary>
public class PuntosEndpointTests : IAsyncLifetime, IDisposable
{
    private GeoFotoDbContext _db = null!;
    private NullFileStorageService _storage = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<GeoFotoDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new GeoFotoDbContext(options);
        await _db.Database.OpenConnectionAsync();
        await _db.Database.EnsureCreatedAsync();
        _storage = new NullFileStorageService();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }

    public void Dispose() { }

    private PuntosService CreateService() => new(_db, _storage);

    // ──────────────────────────────────────────
    // TEST-052: GetAll → retorna lista completa
    // ──────────────────────────────────────────
    [Fact]
    public async Task GetPuntos_Retorna200ConLista()
    {
        // ARRANGE
        _db.Puntos.AddRange(
            new Punto { Latitud = -34.60m, Longitud = -58.38m, Nombre = "P1", FechaCreacion = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Punto { Latitud = -34.61m, Longitud = -58.39m, Nombre = "P2", FechaCreacion = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Punto { Latitud = -34.62m, Longitud = -58.40m, Nombre = "P3", FechaCreacion = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        // ACT
        var svc    = CreateService();
        var puntos = await svc.GetAllAsync();

        // ASSERT
        puntos.Should().HaveCount(3);
        puntos.Should().Contain(p => p.Nombre == "P1");
    }

    // ──────────────────────────────────────────
    // TEST-053: GetById → retorna detalle del punto
    // ──────────────────────────────────────────
    [Fact]
    public async Task GetById_PuntoExistente_Retorna200ConDetalle()
    {
        // ARRANGE
        var punto = new Punto
        {
            Latitud = -34.70m, Longitud = -58.50m,
            Nombre = "TestDetalle", FechaCreacion = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Puntos.Add(punto);
        await _db.SaveChangesAsync();

        // ACT
        var svc    = CreateService();
        var result = await svc.GetByIdAsync(punto.Id);

        // ASSERT
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("TestDetalle");
        result.Fotos.Should().BeEmpty();
    }

    // ──────────────────────────────────────────
    // TEST-054: Delete → elimina punto y sus fotos en cascada
    // ──────────────────────────────────────────
    [Fact]
    public async Task DeletePunto_Existente_Retorna204YEliminaRegistro()
    {
        // ARRANGE
        var punto = new Punto
        {
            Latitud = -34.65m, Longitud = -58.45m,
            Nombre = "AEliminar", FechaCreacion = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Puntos.Add(punto);
        await _db.SaveChangesAsync();
        var puntoId = punto.Id;

        // ACT
        var svc    = CreateService();
        var ok     = await svc.DeleteAsync(puntoId);
        var result = await svc.GetByIdAsync(puntoId);

        // ASSERT
        ok.Should().BeTrue();
        result.Should().BeNull("el punto fue eliminado");
    }

    // ──────────────────────────────────────────
    // TEST-058: GetById para ID inexistente → null (equivalente a 404)
    // ──────────────────────────────────────────
    [Fact]
    public async Task GetById_PuntoInexistente_Retorna404()
    {
        // ACT
        var svc    = CreateService();
        var result = await svc.GetByIdAsync(99999);

        // ASSERT
        result.Should().BeNull();
    }

    // ──────────────────────────────────────────
    // TEST-059: Update → actualiza nombre y UpdatedAt
    // ──────────────────────────────────────────
    [Fact]
    public async Task UpdatePunto_Existente_Retorna204YActualiza()
    {
        // ARRANGE
        var punto = new Punto
        {
            Latitud = -34.63m, Longitud = -58.41m,
            Nombre = "Original", FechaCreacion = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Puntos.Add(punto);
        await _db.SaveChangesAsync();

        var req = new ActualizarPuntoRequest("Actualizado", "Nueva desc");

        // ACT
        var svc    = CreateService();
        var result = await svc.UpdateAsync(punto.Id, req);

        // ASSERT
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Actualizado");

        var stored = await _db.Puntos.FindAsync(punto.Id);
        stored!.Descripcion.Should().Be("Nueva desc");
        stored.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

/// <summary>Stub de IFileStorageService para tests de integración.</summary>
internal class NullFileStorageService : IFileStorageService
{
    public Task<string> SaveAsync(Stream stream, string fileName)
        => Task.FromResult(Path.Combine(Path.GetTempPath(), fileName));
    public void Delete(string rutaFisica) { }
}
