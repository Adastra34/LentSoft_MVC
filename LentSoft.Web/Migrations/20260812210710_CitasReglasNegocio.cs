using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class CitasReglasNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeIva",
                table: "Products",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "OptometraId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VecesReprogramada",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Appointments",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "OptometraId", "VecesReprogramada" },
                values: new object[] { 3, 0 });

            migrationBuilder.UpdateData(
                table: "Appointments",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "OptometraId", "VecesReprogramada" },
                values: new object[] { 3, 0 });

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1,
                column: "Salario",
                value: 2500000.00m);

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 2,
                column: "Salario",
                value: 1800000.00m);

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Email", "Nombre", "Salario" },
                values: new object[] { "carlos.mendoza@lentsoft.com", "Carlos Mendoza", 3500000.00m });

            migrationBuilder.UpdateData(
                table: "OrderItems",
                keyColumn: "Id",
                keyValue: 1,
                column: "PrecioUnitario",
                value: 2500000.00m);

            migrationBuilder.UpdateData(
                table: "OrderItems",
                keyColumn: "Id",
                keyValue: 2,
                column: "PrecioUnitario",
                value: 1800000.00m);

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 1,
                column: "Total",
                value: 2500000.00m);

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 2,
                column: "Total",
                value: 1800000.00m);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PorcentajeIva", "Precio" },
                values: new object[] { 19.00m, 2500000.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "PorcentajeIva", "Precio", "PrecioDescuento" },
                values: new object[] { 19.00m, 450000.00m, 399000.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "PorcentajeIva", "Precio" },
                values: new object[] { 19.00m, 1800000.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "PorcentajeIva", "Precio" },
                values: new object[] { 19.00m, 1200000.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "PorcentajeIva", "Precio", "PrecioDescuento" },
                values: new object[] { 19.00m, 150000.00m, 99000.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "PorcentajeIva", "Precio" },
                values: new object[] { 5.00m, 120000.00m });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_OptometraId",
                table: "Appointments",
                column: "OptometraId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Users_OptometraId",
                table: "Appointments",
                column: "OptometraId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Users_OptometraId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_OptometraId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PorcentajeIva",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OptometraId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "VecesReprogramada",
                table: "Appointments");

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1,
                column: "Salario",
                value: 25000.00m);

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 2,
                column: "Salario",
                value: 18000.00m);

            migrationBuilder.UpdateData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Email", "Nombre", "Salario" },
                values: new object[] { "ana.martinez@lentsoft.com", "Ana Martínez", 35000.00m });

            migrationBuilder.UpdateData(
                table: "OrderItems",
                keyColumn: "Id",
                keyValue: 1,
                column: "PrecioUnitario",
                value: 2500.00m);

            migrationBuilder.UpdateData(
                table: "OrderItems",
                keyColumn: "Id",
                keyValue: 2,
                column: "PrecioUnitario",
                value: 1800.00m);

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 1,
                column: "Total",
                value: 2500.00m);

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 2,
                column: "Total",
                value: 1800.00m);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "Precio",
                value: 2500.00m);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Precio", "PrecioDescuento" },
                values: new object[] { 450.00m, 399.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "Precio",
                value: 1800.00m);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "Precio",
                value: 1200.00m);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Precio", "PrecioDescuento" },
                values: new object[] { 150.00m, 99.00m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "Precio",
                value: 120.00m);
        }
    }
}
