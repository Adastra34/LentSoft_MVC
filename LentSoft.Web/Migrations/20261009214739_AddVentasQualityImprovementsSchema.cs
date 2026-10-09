using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddVentasQualityImprovementsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaveIdempotencia",
                table: "TransaccionesPagos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductoNombre",
                table: "SalesOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<int>(
                name: "ProductoId",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FormulaOpticaId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "AuditoriasVentas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoEntidad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntidadId = table.Column<int>(type: "int", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Detalles = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriasVentas", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 1,
                column: "FormulaOpticaId",
                value: null);

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 2,
                column: "FormulaOpticaId",
                value: null);

            migrationBuilder.UpdateData(
                table: "SalesOrders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ProductoId", "ProductoNombre" },
                values: new object[] { 1, "Lentes Ray-Ban Aviator" });

            migrationBuilder.UpdateData(
                table: "SalesOrders",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ProductoId", "ProductoNombre" },
                values: new object[] { 2, "Lentes de Contacto Acuvue" });

            migrationBuilder.CreateIndex(
                name: "IX_TransaccionesPagos_ClaveIdempotencia",
                table: "TransaccionesPagos",
                column: "ClaveIdempotencia");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_ProductoId",
                table: "SalesOrders",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_FormulaOpticaId",
                table: "Orders",
                column: "FormulaOpticaId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriasVentas_Fecha",
                table: "AuditoriasVentas",
                column: "Fecha",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriasVentas_TipoEntidad_EntidadId",
                table: "AuditoriasVentas",
                columns: new[] { "TipoEntidad", "EntidadId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_FormulasOpticas_FormulaOpticaId",
                table: "Orders",
                column: "FormulaOpticaId",
                principalTable: "FormulasOpticas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Products_ProductoId",
                table: "SalesOrders",
                column: "ProductoId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_FormulasOpticas_FormulaOpticaId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Products_ProductoId",
                table: "SalesOrders");

            migrationBuilder.DropTable(
                name: "AuditoriasVentas");

            migrationBuilder.DropIndex(
                name: "IX_TransaccionesPagos_ClaveIdempotencia",
                table: "TransaccionesPagos");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_ProductoId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_FormulaOpticaId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClaveIdempotencia",
                table: "TransaccionesPagos");

            migrationBuilder.DropColumn(
                name: "ProductoId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "FormulaOpticaId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "ProductoNombre",
                table: "SalesOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "SalesOrders",
                keyColumn: "Id",
                keyValue: 1,
                column: "ProductoNombre",
                value: "Gafas de Sol Polarizadas Especiales");

            migrationBuilder.UpdateData(
                table: "SalesOrders",
                keyColumn: "Id",
                keyValue: 2,
                column: "ProductoNombre",
                value: "Lentes de Contacto Toricos Custom");
        }
    }
}
