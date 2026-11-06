using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFormulaOpticaToSalesOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormulaOpticaId",
                table: "SalesOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_FormulaOpticaId",
                table: "SalesOrders",
                column: "FormulaOpticaId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_FormulasOpticas_FormulaOpticaId",
                table: "SalesOrders",
                column: "FormulaOpticaId",
                principalTable: "FormulasOpticas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_FormulasOpticas_FormulaOpticaId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_FormulaOpticaId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "FormulaOpticaId",
                table: "SalesOrders");
        }
    }
}
