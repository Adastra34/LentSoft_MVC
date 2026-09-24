using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAbonosTransaccionesAndDianFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoPago",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MontoPagado",
                table: "Orders",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CUFE",
                table: "Invoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RangoAutorizadoDesde",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RangoAutorizadoHasta",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResolucionDianNumero",
                table: "Invoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PagosVentas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    MetodoPago = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Responsable = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NumeroComprobante = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosVentas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PagosVentas_Orders_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransaccionesPagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CodigoAutorizacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UltimosDigitosTarjeta = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    MarcaTarjeta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MensajeRespuesta = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransaccionesPagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransaccionesPagos_Orders_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EstadoPago", "MontoPagado" },
                values: new object[] { "pendiente", 0m });

            migrationBuilder.UpdateData(
                table: "Orders",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EstadoPago", "MontoPagado" },
                values: new object[] { "pendiente", 0m });

            migrationBuilder.CreateIndex(
                name: "IX_PagosVentas_NumeroComprobante",
                table: "PagosVentas",
                column: "NumeroComprobante",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagosVentas_VentaId",
                table: "PagosVentas",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_TransaccionesPagos_Fecha",
                table: "TransaccionesPagos",
                column: "Fecha",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_TransaccionesPagos_VentaId",
                table: "TransaccionesPagos",
                column: "VentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagosVentas");

            migrationBuilder.DropTable(
                name: "TransaccionesPagos");

            migrationBuilder.DropColumn(
                name: "EstadoPago",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MontoPagado",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CUFE",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RangoAutorizadoDesde",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "RangoAutorizadoHasta",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ResolucionDianNumero",
                table: "Invoices");
        }
    }
}
