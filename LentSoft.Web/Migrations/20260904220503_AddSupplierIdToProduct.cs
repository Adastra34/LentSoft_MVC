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
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'Contacto')
    ALTER TABLE [Suppliers] ADD [Contacto] nvarchar(100) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'Email')
    ALTER TABLE [Suppliers] ADD [Email] nvarchar(max) NOT NULL DEFAULT '';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'Estado')
    ALTER TABLE [Suppliers] ADD [Estado] nvarchar(max) NOT NULL DEFAULT '';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'LogoUrl')
    ALTER TABLE [Suppliers] ADD [LogoUrl] nvarchar(500) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Suppliers') AND name = 'TipoProducto')
    ALTER TABLE [Suppliers] ADD [TipoProducto] nvarchar(max) NOT NULL DEFAULT '';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SupplierId')
    ALTER TABLE [Products] ADD [SupplierId] nvarchar(20) NULL;
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('InventoryMovements') AND name = 'NombreProducto')
    ALTER TABLE [InventoryMovements] ADD [NombreProducto] nvarchar(200) NULL;
");

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

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_SupplierId' AND object_id = OBJECT_ID('Products'))
    CREATE INDEX [IX_Products_SupplierId] ON [Products] ([SupplierId]);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Products_Suppliers_SupplierId')
    ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]);
");
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
