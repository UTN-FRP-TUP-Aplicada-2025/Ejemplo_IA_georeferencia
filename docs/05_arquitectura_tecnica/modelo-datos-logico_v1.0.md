# Modelo de Datos Lógico

**Proyecto:** GeoFoto — Registro Georeferenciado de Fotografías Offline-First  
**Documento:** modelo-datos-logico_v1.0.md  
**Versión:** 1.0  
**Estado:** Borrador  
**Fecha:** 2026-04-13  
**Autor:** Equipo Técnico  

---

## 1. Propósito

Este documento define el modelo de datos lógico de la solución GeoFoto. Se describen las estructuras de persistencia tanto en el servidor (SQL Server, proyecto `GeoFoto.Api`) como en el dispositivo móvil (SQLite, proyecto `GeoFoto.Mobile`), así como la estrategia de almacenamiento de archivos fotográficos, las convenciones de nomenclatura y las consideraciones de evolución futura.

El modelo responde a una arquitectura **offline-first** en la que el dispositivo móvil opera de forma autónoma y sincroniza cambios con el servidor cuando se dispone de conectividad.

---

## 2. Alcance

El modelo cubre:

- Tablas relacionales en SQL Server para la API de backend.
- Tablas locales en SQLite para la aplicación .NET MAUI.
- Cola de sincronización para gestión de operaciones pendientes.
- Estrategia de almacenamiento físico de archivos fotográficos.
- Convenciones de nomenclatura aplicables al proyecto.
- Consideraciones de evolución y migración.

No incluye:

- Modelo de autenticación o usuarios (previsto para versión futura).
- Esquemas de caché HTTP o caché de imágenes en memoria.
- Infraestructura de red o configuración de servidores.

---

## A. SQL Server — GeoFoto.Api

### A.1 Tabla: Puntos

Almacena los puntos geográficos de interés registrados por los usuarios.

| Campo | Tipo | Null | Default | Notas |
|-------|------|------|---------|-------|
| Id | `int` | NO | `IDENTITY` | Clave primaria. |
| Latitud | `decimal(10,7)` | NO | | Latitud en grados decimales. |
| Longitud | `decimal(10,7)` | NO | | Longitud en grados decimales. |
| Nombre | `nvarchar(200)` | SÍ | `NULL` | Nombre descriptivo opcional. |
| Descripcion | `nvarchar(1000)` | SÍ | `NULL` | Descripción extendida opcional. |
| FechaCreacion | `datetime2` | NO | `GETUTCDATE()` | Fecha de creación del registro. |
| UpdatedAt | `datetime2` | NO | `GETUTCDATE()` | Última modificación; árbitro para estrategia LWW (*Last-Writer-Wins*). |

#### Índices

| Nombre | Columna(s) | Propósito |
|--------|-----------|-----------|
| `PK_Puntos` | `Id` | Clave primaria clustered. |
| `IX_Puntos_UpdatedAt` | `UpdatedAt` | Consultas de sincronización delta (`WHERE UpdatedAt > @lastSync`). |

---

### A.2 Tabla: Fotos

Almacena los metadatos de cada fotografía asociada a un punto geográfico.

| Campo | Tipo | Null | Default | Notas |
|-------|------|------|---------|-------|
| Id | `int` | NO | `IDENTITY` | Clave primaria. |
| PuntoId | `int` | NO | | FK → `Puntos.Id` con `ON DELETE CASCADE`. |
| NombreArchivo | `nvarchar(260)` | NO | | Nombre original del archivo subido. |
| RutaFisica | `nvarchar(500)` | NO | | Ruta relativa en el servidor: `wwwroot/uploads/{year}/{month}/{guid}.{ext}`. |
| FechaTomada | `datetime2` | SÍ | `NULL` | Extraída del campo EXIF `DateTimeOriginal`, si está disponible. |
| TamanoBytes | `bigint` | NO | | Tamaño del archivo en bytes. |
| LatitudExif | `decimal(10,7)` | SÍ | `NULL` | Latitud extraída de los metadatos EXIF GPS. |
| LongitudExif | `decimal(10,7)` | SÍ | `NULL` | Longitud extraída de los metadatos EXIF GPS. |
| UpdatedAt | `datetime2` | NO | `GETUTCDATE()` | Última modificación; árbitro para estrategia LWW. |

#### Índices

| Nombre | Columna(s) | Propósito |
|--------|-----------|-----------|
| `PK_Fotos` | `Id` | Clave primaria clustered. |
| `IX_Fotos_PuntoId` | `PuntoId` | Búsquedas por clave foránea. |
| `IX_Fotos_UpdatedAt` | `UpdatedAt` | Consultas de sincronización delta. |

---

### A.3 Configuración EF Core

A continuación se presenta un ejemplo representativo de la configuración de entidades mediante Fluent API de Entity Framework Core:

```csharp
// Configuración de la entidad Punto
public class PuntoConfiguration : IEntityTypeConfiguration<Punto>
{
    public void Configure(EntityTypeBuilder<Punto> builder)
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
    }
}

// Configuración de la entidad Foto
public class FotoConfiguration : IEntityTypeConfiguration<Foto>
{
    public void Configure(EntityTypeBuilder<Foto> builder)
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

        builder.HasOne<Punto>()
               .WithMany()
               .HasForeignKey(f => f.PuntoId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.PuntoId)
               .HasDatabaseName("IX_Fotos_PuntoId");

        builder.HasIndex(f => f.UpdatedAt)
               .HasDatabaseName("IX_Fotos_UpdatedAt");
    }
}
```

---

## B. SQLite — GeoFoto.Mobile

### B.1 Tabla: Puntos_Local

Réplica local de los puntos geográficos con soporte para seguimiento de estado de sincronización.

| Campo | Tipo SQLite | Null | Default | Notas |
|-------|------------|------|---------|-------|
| LocalId | `INTEGER` | NO | `AUTOINCREMENT` | Clave primaria local. |
| RemoteId | `INTEGER` | SÍ | `NULL` | Correspondencia con `Puntos.Id` en el servidor; `NULL` si aún no se ha sincronizado. |
| Latitud | `REAL` | NO | | Latitud en grados decimales. |
| Longitud | `REAL` | NO | | Longitud en grados decimales. |
| Nombre | `TEXT` | SÍ | `NULL` | Nombre descriptivo opcional. |
| Descripcion | `TEXT` | SÍ | `NULL` | Descripción extendida opcional. |
| FechaCreacion | `TEXT` | NO | | Fecha ISO 8601 en UTC. |
| UpdatedAt | `TEXT` | NO | | Última modificación en formato ISO 8601 UTC. |
| SyncStatus | `TEXT` | NO | `'Pending'` | Estado de sincronización: `Pending`, `Synced`, `Modified`, `Deleted`. |

---

### B.2 Tabla: Fotos_Local

Réplica local de los metadatos de fotografías con seguimiento de sincronización.

| Campo | Tipo SQLite | Null | Default | Notas |
|-------|------------|------|---------|-------|
| LocalId | `INTEGER` | NO | `AUTOINCREMENT` | Clave primaria local. |
| RemoteId | `INTEGER` | SÍ | `NULL` | Correspondencia con `Fotos.Id` en el servidor. |
| PuntoLocalId | `INTEGER` | NO | | FK → `Puntos_Local.LocalId`. |
| NombreArchivo | `TEXT` | NO | | Nombre original del archivo. |
| RutaLocal | `TEXT` | NO | | Ruta en el sistema de archivos Android (directorio privado de la app). |
| FechaTomada | `TEXT` | SÍ | `NULL` | Fecha EXIF en formato ISO 8601, si está disponible. |
| TamanoBytes | `INTEGER` | NO | | Tamaño del archivo en bytes. |
| LatitudExif | `REAL` | SÍ | `NULL` | Latitud extraída de metadatos EXIF GPS. |
| LongitudExif | `REAL` | SÍ | `NULL` | Longitud extraída de metadatos EXIF GPS. |
| UpdatedAt | `TEXT` | NO | | Última modificación en formato ISO 8601 UTC. |
| SyncStatus | `TEXT` | NO | `'Pending'` | Estado de sincronización: `Pending`, `Synced`, `Modified`, `Deleted`. |

---

### B.3 Tabla: SyncQueue

Cola de operaciones pendientes de sincronización. Cada fila representa una operación que debe enviarse al servidor cuando se disponga de conectividad.

| Campo | Tipo SQLite | Null | Default | Notas |
|-------|------------|------|---------|-------|
| Id | `INTEGER` | NO | `AUTOINCREMENT` | Clave primaria. |
| OperationType | `TEXT` | NO | | Tipo de operación: `Create`, `Update`, `Delete`. |
| EntityType | `TEXT` | NO | | Entidad afectada: `Punto`, `Foto`. |
| LocalId | `INTEGER` | NO | | Referencia al `LocalId` de la entidad afectada. |
| Payload | `TEXT` | NO | | Serialización JSON del objeto a sincronizar. |
| Status | `TEXT` | NO | `'Pending'` | Estado del encolado: `Pending`, `InProgress`, `Done`, `Failed`. |
| Attempts | `INTEGER` | NO | `0` | Cantidad de intentos de envío realizados. |
| LastAttemptAt | `TEXT` | SÍ | `NULL` | Fecha ISO 8601 del último intento. |
| ErrorMessage | `TEXT` | SÍ | `NULL` | Mensaje de error del último intento fallido. |
| CreatedAt | `TEXT` | NO | | Fecha ISO 8601 de creación de la entrada en la cola. |

---

### B.4 Modelo sqlite-net-pcl

A continuación se presenta un ejemplo representativo de las clases de modelo para la capa de persistencia local con `sqlite-net-pcl`:

```csharp
using SQLite;

[Table("Puntos_Local")]
public class PuntoLocal
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }

    public int? RemoteId { get; set; }

    public double Latitud { get; set; }

    public double Longitud { get; set; }

    public string? Nombre { get; set; }

    public string? Descripcion { get; set; }

    public string FechaCreacion { get; set; } = DateTime.UtcNow.ToString("o");

    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    public string SyncStatus { get; set; } = "Pending";
}

[Table("Fotos_Local")]
public class FotoLocal
{
    [PrimaryKey, AutoIncrement]
    public int LocalId { get; set; }

    public int? RemoteId { get; set; }

    [Indexed]
    public int PuntoLocalId { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;

    public string RutaLocal { get; set; } = string.Empty;

    public string? FechaTomada { get; set; }

    public long TamanoBytes { get; set; }

    public double? LatitudExif { get; set; }

    public double? LongitudExif { get; set; }

    public string UpdatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    public string SyncStatus { get; set; } = "Pending";
}

[Table("SyncQueue")]
public class SyncQueueEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string OperationType { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public int LocalId { get; set; }

    public string Payload { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public int Attempts { get; set; } = 0;

    public string? LastAttemptAt { get; set; }

    public string? ErrorMessage { get; set; }

    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}
```

---

## C. Estrategia de Almacenamiento de Archivos

### C.1 Servidor (ASP.NET Core)

Las fotografías se almacenan en el sistema de archivos del servidor bajo la siguiente estructura:

```
wwwroot/uploads/{year}/{month}/{guid}.{ext}
```

| Componente | Descripción |
|-----------|-------------|
| `{year}` | Año de la marca de tiempo de subida (cuatro dígitos). |
| `{month}` | Mes de la marca de tiempo de subida (dos dígitos). |
| `{guid}` | Identificador único global generado al momento de la subida; garantiza unicidad. |
| `{ext}` | Extensión original del archivo (por ejemplo, `jpg`, `png`). |

Las imágenes se sirven a través del endpoint `GET /api/fotos/imagen/{id}`, que resuelve la ruta física a partir de la base de datos.

### C.2 Android (.NET MAUI)

Las fotografías se almacenan en el directorio privado de la aplicación:

```
FileSystem.AppDataDirectory/photos/{guid}.{ext}
```

| Aspecto | Detalle |
|---------|---------|
| Privacidad | El directorio es privado de la aplicación; no es accesible por otras aplicaciones. |
| Limpieza | Los archivos se eliminan cuando el punto asociado es borrado y la sincronización confirma la eliminación en el servidor. |
| Nombres | Se utiliza un GUID para evitar colisiones de nombres de archivo. |

---

## D. Convenciones de Nomenclatura

| Aspecto | Convención | Ejemplo |
|---------|-----------|---------|
| Tablas SQL Server | PascalCase singular. | `Puntos`, `Fotos` |
| Tablas SQLite locales | PascalCase + sufijo `_Local`. | `Puntos_Local`, `Fotos_Local` |
| Campos | PascalCase. | `FechaCreacion`, `TamanoBytes` |
| Claves foráneas (servidor) | `{TablaRelacionada}Id`. | `PuntoId` |
| Claves foráneas (local) | `{TablaRelacionada}LocalId`. | `PuntoLocalId` |
| Fechas en SQLite | Texto en formato ISO 8601 (`yyyy-MM-ddTHH:mm:ss.fffffffZ`). | `2026-04-13T14:30:00.0000000Z` |
| Zona horaria | UTC para todas las marcas de tiempo, tanto en servidor como en dispositivo. | — |

---

## E. Consideraciones de Evolución

| Área | Evolución prevista |
|------|-------------------|
| Almacenamiento de fotos | Migración futura a Azure Blob Storage para reemplazar el sistema de archivos local del servidor. |
| Autenticación | Incorporación de tablas de usuarios cuando se implemente la gestión de identidad. |
| Clasificación | Posible adición de una tabla `Tags` o `Categorias` para clasificación de puntos geográficos. |
| Migraciones SQL Server | Gestión mediante EF Core Migrations (`dotnet ef migrations add`, `dotnet ef database update`). |
| Migraciones SQLite | Gestión mediante `sqlite-net-pcl` con `CreateTableAsync<T>()` y verificaciones de esquema al inicio de la aplicación. |

---

## Control de Cambios

| Versión | Fecha | Autor | Cambios |
|---------|-------|-------|---------|
| 1.0 | 2026-04-13 | Equipo Técnico | Versión inicial del modelo de datos lógico para GeoFoto. |

---

**Fin del documento**