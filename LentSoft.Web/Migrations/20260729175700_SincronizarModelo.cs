using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarModelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Categoria",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Products");

            migrationBuilder.AddColumn<int>(
                name: "CategoriaId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosInventario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Producto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Responsable = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosInventario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Contacto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TipoProducto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "activo"),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Gafas" },
                    { 2, "Lentes" },
                    { 3, "Accesorios" }
                });

            migrationBuilder.InsertData(
                table: "MovimientosInventario",
                columns: new[] { "Id", "Cantidad", "Fecha", "Producto", "Responsable", "Tipo" },
                values: new object[,]
                {
                    { 1, 20, new DateTime(2026, 7, 25, 0, 0, 0, 0, DateTimeKind.Utc), "Lentes Ray-Ban Aviator", "Ana Martínez", "entrada" },
                    { 2, 5, new DateTime(2026, 7, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Lentes de Contacto Acuvue", "Juan Pérez", "salida" },
                    { 3, 10, new DateTime(2026, 7, 24, 0, 0, 0, 0, DateTimeKind.Utc), "Montura Oakley Sport", "Ana Martínez", "entrada" },
                    { 4, 15, new DateTime(2026, 7, 26, 0, 0, 0, 0, DateTimeKind.Utc), "Estuche Premium", "Juan Pérez", "salida" },
                    { 5, 50, new DateTime(2026, 7, 22, 0, 0, 0, 0, DateTimeKind.Utc), "Líquido Limpiador", "Ana Martínez", "entrada" }
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CategoriaId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CategoriaId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CategoriaId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CategoriaId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CategoriaId",
                value: 3);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CategoriaId",
                value: 3);

            migrationBuilder.InsertData(
                table: "Proveedores",
                columns: new[] { "Id", "Contacto", "Email", "Estado", "FechaRegistro", "LogoUrl", "Nombre", "Telefono", "TipoProducto" },
                values: new object[,]
                {
                    { 1, "Carlos Ruiz", "ventas@opticaglobal.com", "activo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Óptica Global S.A.", "555-1001", "Monturas" },
                    { 2, "Ana López", "contacto@lenstech.co", "activo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "LensTech Colombia", "555-1002", "Lentes de contacto" },
                    { 3, "Pedro Gómez", "info@distvisual.com", "activo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Distribuidora Visual", "555-1003", "Accesorios" },
                    { 4, "María Fernández", "dist@rayban.co", "activo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ray-Ban Distribuidor", "555-1004", "Lentes de sol" },
                    { 5, "José Martínez", "partner@oakley.co", "inactivo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Oakley Partner", "555-1005", "Monturas deportivas" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoriaId",
                table: "Products",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_Nombre",
                table: "Categorias",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Fecha",
                table: "MovimientosInventario",
                column: "Fecha",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosInventario_Tipo",
                table: "MovimientosInventario",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Nombre",
                table: "Proveedores",
                column: "Nombre");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categorias_CategoriaId",
                table: "Products",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categorias_CategoriaId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "MovimientosInventario");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoriaId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Products",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "Categoria",
                value: "lentes-sol");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "Categoria",
                value: "lentes-contacto");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "Categoria",
                value: "monturas");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "Categoria",
                value: "lentes-graduados");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "Categoria",
                value: "accesorios");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "Categoria",
                value: "accesorios");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Categoria",
                table: "Products",
                column: "Categoria");
        }
    }
}
