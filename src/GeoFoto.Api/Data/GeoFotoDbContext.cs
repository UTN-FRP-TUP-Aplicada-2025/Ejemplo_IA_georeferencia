using GeoFoto.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoFoto.Api.Data;

public class GeoFotoDbContext : DbContext
{
    public GeoFotoDbContext(DbContextOptions<GeoFotoDbContext> options) : base(options) { }

    public DbSet<Punto> Puntos => Set<Punto>();
    public DbSet<Foto> Fotos => Set<Foto>();
    public DbSet<DeletedEntity> DeletedEntities => Set<DeletedEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Punto configuration
        modelBuilder.Entity<Punto>(builder =>
        {
            builder.ToTable("Puntos");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Latitud)
                   .HasColumnType("decimal(10,7)")
                   .IsRequired();

            builder.Property(p => p.Longitud)
                   .HasColumnType("decimal(10,7)")
                   .IsRequired();

            builder.Property(p => p.Nombre)
                   .HasMaxLength(200);

            builder.Property(p => p.Descripcion)
                   .HasMaxLength(1000);

            builder.Property(p => p.FechaCreacion)
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(p => p.UpdatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(p => p.UpdatedAt)
                   .HasDatabaseName("IX_Puntos_UpdatedAt");

            builder.HasIndex(p => new { p.Latitud, p.Longitud })
                   .HasDatabaseName("IX_Puntos_Lat_Lng");
        });

        // Foto configuration
        modelBuilder.Entity<Foto>(builder =>
        {
            builder.ToTable("Fotos");
            builder.HasKey(f => f.Id);

            builder.Property(f => f.NombreArchivo)
                   .HasMaxLength(260)
                   .IsRequired();

            builder.Property(f => f.RutaFisica)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(f => f.TamanoBytes)
                   .IsRequired();

            builder.Property(f => f.LatitudExif)
                   .HasColumnType("decimal(10,7)");

            builder.Property(f => f.LongitudExif)
                   .HasColumnType("decimal(10,7)");

            builder.Property(f => f.UpdatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(f => f.Punto)
                   .WithMany(p => p.Fotos)
                   .HasForeignKey(f => f.PuntoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(f => f.PuntoId)
                   .HasDatabaseName("IX_Fotos_PuntoId");

            builder.HasIndex(f => f.UpdatedAt)
                   .HasDatabaseName("IX_Fotos_UpdatedAt");
        });

        modelBuilder.Entity<DeletedEntity>(builder =>
        {
            builder.ToTable("DeletedEntities");
            builder.HasKey(d => d.Id);
            builder.Property(d => d.EntityType).HasMaxLength(50).IsRequired();
            builder.HasIndex(d => d.DeletedAt).HasDatabaseName("IX_DeletedEntities_DeletedAt");
        });
    }
}
