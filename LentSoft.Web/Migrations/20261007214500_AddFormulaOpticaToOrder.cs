using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFormulaOpticaToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormulaOpticaId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_FormulaOpticaId",
                table: "Orders",
                column: "FormulaOpticaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_FormulasOpticas_FormulaOpticaId",
                table: "Orders",
                column: "FormulaOpticaId",
                principalTable: "FormulasOpticas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_FormulasOpticas_FormulaOpticaId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_FormulaOpticaId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FormulaOpticaId",
                table: "Orders");
        }
    }
}
