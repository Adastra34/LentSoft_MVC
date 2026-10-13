using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplierAndOrderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Suppliers",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TiempoEntregaDias",
                table: "Suppliers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CantidadRecibida",
                table: "SupplierOrders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "SupplierOrders",
                keyColumn: "Id",
                keyValue: 1,
                column: "CantidadRecibida",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SupplierOrders",
                keyColumn: "Id",
                keyValue: 2,
                column: "CantidadRecibida",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: "PROV001",
                columns: new[] { "Direccion", "TiempoEntregaDias" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: "PROV002",
                columns: new[] { "Direccion", "TiempoEntregaDias" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: "PROV003",
                columns: new[] { "Direccion", "TiempoEntregaDias" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "TiempoEntregaDias",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CantidadRecibida",
                table: "SupplierOrders");
        }
    }
}
