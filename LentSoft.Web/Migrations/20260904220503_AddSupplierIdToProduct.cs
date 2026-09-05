using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierIdToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contacto",
                table: "Suppliers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Suppliers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoProducto",
                table: "Suppliers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplierId",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreProducto",
                table: "InventoryMovements",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "InventoryMovements",
                keyColumn: "Id",
                keyValue: 1,
                column: "NombreProducto",
                value: "Lentes Ray-Ban Aviator");

            migrationBuilder.UpdateData(
                table: "InventoryMovements",
                keyColumn: "Id",
                keyValue: 2,
                column: "NombreProducto",
                value: "Lentes de Contacto Acuvue");

            migrationBuilder.UpdateData(
                table: "InventoryMovements",
                keyColumn: "Id",
                keyValue: 3,
                column: "NombreProducto",
                value: "Montura Oakley Sport");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "SupplierId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "SupplierId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "SupplierId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "SupplierId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "SupplierId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "SupplierId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: "PROV001",
                columns: new[] { "Contacto", "Email", "Estado", "LogoUrl", "TipoProducto" },
                values: new object[] { "Carlos Gómez", "ventas@opticaglobal.com", "Activo", "https://images.unsplash.com/photo-1560179707-f14e90ef3623", "Monturas" });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: "PROV002",
                columns: new[] { "Contacto", "Email", "Estado", "LogoUrl", "TipoProducto" },
                values: new object[] { "Ana Martínez", "contacto@lenstech.co", "Activo", "https://images.unsplash.com/photo-1572021335469-31706a17aaef", "Lentes de contacto" });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: "PROV003",
                columns: new[] { "Contacto", "Email", "Estado", "LogoUrl", "TipoProducto" },
                values: new object[] { "Roberto Díaz", "info@distvisual.com", "Activo", "https://images.unsplash.com/photo-1556761175-5973dc0f32e7", "Accesorios" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SupplierId",
                table: "Products",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Suppliers_SupplierId",
                table: "Products",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Suppliers_SupplierId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_SupplierId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Contacto",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "TipoProducto",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NombreProducto",
                table: "InventoryMovements");
        }
    }
}
