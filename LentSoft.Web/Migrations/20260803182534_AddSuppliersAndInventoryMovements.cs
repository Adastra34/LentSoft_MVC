using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSuppliersAndInventoryMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Responsable = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoProductos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "InventoryMovements",
                columns: new[] { "Id", "Cantidad", "Fecha", "ProductId", "Responsable", "Tipo" },
                values: new object[,]
                {
                    { 1, 20, new DateTime(2026, 5, 1, 10, 0, 0, 0, DateTimeKind.Utc), 1, "Administrador", "Entrada" },
                    { 2, 5, new DateTime(2026, 5, 2, 14, 0, 0, 0, DateTimeKind.Utc), 2, "Administrador", "Salida" },
                    { 3, 10, new DateTime(2026, 5, 3, 11, 30, 0, 0, DateTimeKind.Utc), 3, "Administrador", "Entrada" }
                });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "Id", "Activo", "Correo", "FechaRegistro", "Nombre", "Telefono", "TipoProductos" },
                values: new object[,]
                {
                    { "PROV001", true, "ventas@opticaglobal.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Óptica Global S.A.", "555-1001", "Monturas" },
                    { "PROV002", true, "contacto@lenstech.co", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "LensTech Colombia", "555-1002", "Lentes de contacto" },
                    { "PROV003", true, "info@distvisual.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Distribuidora Visual", "555-1003", "Accesorios" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Fecha",
                table: "InventoryMovements",
                column: "Fecha",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_ProductId",
                table: "InventoryMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Activo",
                table: "Suppliers",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Nombre",
                table: "Suppliers",
                column: "Nombre");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryMovements");

            migrationBuilder.DropTable(
                name: "Suppliers");
        }
    }
}
