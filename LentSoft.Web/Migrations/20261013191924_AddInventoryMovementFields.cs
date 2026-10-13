using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryMovementFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Motivo",
                table: "InventoryMovements",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "Otro");

            migrationBuilder.AddColumn<string>(
                name: "Nota",
                table: "InventoryMovements",
                type: "TEXT",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Referencia",
                table: "InventoryMovements",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockResultante",
                table: "InventoryMovements",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "InventoryMovements",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Motivo", "Nota", "Referencia" },
                values: new object[] { "Otro", null, null });

            migrationBuilder.UpdateData(
                table: "InventoryMovements",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Motivo", "Nota", "Referencia" },
                values: new object[] { "Otro", null, null });

            migrationBuilder.UpdateData(
                table: "InventoryMovements",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Motivo", "Nota", "Referencia" },
                values: new object[] { "Otro", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_Tipo",
                table: "InventoryMovements",
                column: "Tipo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_Tipo",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "Motivo",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "Nota",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "Referencia",
                table: "InventoryMovements");

            migrationBuilder.DropColumn(
                name: "StockResultante",
                table: "InventoryMovements");
        }
    }
}
