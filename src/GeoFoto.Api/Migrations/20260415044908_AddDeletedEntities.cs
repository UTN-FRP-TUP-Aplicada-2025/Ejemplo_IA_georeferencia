using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeoFoto.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeletedEntities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletedEntities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeletedEntities_DeletedAt",
                table: "DeletedEntities",
                column: "DeletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeletedEntities");
        }
    }
}
