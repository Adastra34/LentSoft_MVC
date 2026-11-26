using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class SplitStockBodegaLocal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockBodega",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StockLocal",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Ubicacion",
                table: "InventoryMovements",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Bodega");

            // Backfill para productos existentes: StockBodega = Stock, StockLocal = 0
            migrationBuilder.Sql("UPDATE Products SET StockBodega = Stock, StockLocal = 0;");

            // Backfill para movimientos existentes: Ubicacion = 'Bodega'
            migrationBuilder.Sql("UPDATE InventoryMovements SET Ubicacion = 'Bodega' WHERE Ubicacion IS NULL OR Ubicacion = '';");

            migrationBuilder.UpdateData(
                table: "InventoryMovements",
                keyColumn: "Id",
                keyValue: 1,
                column: "Ubicacion",
                value: "Bodega");

            migrationBuilder.UpdateData(
                table: "InventoryMovements",
                keyColumn: "Id",
                keyValue: 2,
                column: "Ubicacion",
                value: "Bodega");

            migrationBuilder.UpdateData(
                table: "InventoryMovements",
                keyColumn: "Id",
                keyValue: 3,
                column: "Ubicacion",
                value: "Bodega");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "StockBodega", "StockLocal" },
                values: new object[] { 50, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "StockBodega", "StockLocal" },
                values: new object[] { 100, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "StockBodega", "StockLocal" },
                values: new object[] { 30, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "StockBodega", "StockLocal" },
                values: new object[] { 40, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "StockBodega", "StockLocal" },
                values: new object[] { 200, 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "StockBodega", "StockLocal" },
                values: new object[] { 150, 0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockBodega",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StockLocal",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Ubicacion",
                table: "InventoryMovements");
        }
    }
}
