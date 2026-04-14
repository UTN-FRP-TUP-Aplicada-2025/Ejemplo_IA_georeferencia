using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoFoto.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Puntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Latitud = table.Column<decimal>(type: "decimal(10,7)", nullable: false),
                    Longitud = table.Column<decimal>(type: "decimal(10,7)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Puntos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PuntoId = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    RutaFisica = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FechaTomada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    LatitudExif = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    LongitudExif = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fotos_Puntos_PuntoId",
                        column: x => x.PuntoId,
                        principalTable: "Puntos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fotos_PuntoId",
                table: "Fotos",
                column: "PuntoId");

            migrationBuilder.CreateIndex(
                name: "IX_Fotos_UpdatedAt",
                table: "Fotos",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Puntos_Lat_Lng",
                table: "Puntos",
                columns: new[] { "Latitud", "Longitud" });

            migrationBuilder.CreateIndex(
                name: "IX_Puntos_UpdatedAt",
                table: "Puntos",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fotos");

            migrationBuilder.DropTable(
                name: "Puntos");
        }
    }
}
