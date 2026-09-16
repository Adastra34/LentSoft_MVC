using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductOverlaysPng : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImagenOverlayUrl",
                value: "/img/overlays/rayban_aviator_final.png");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "ImagenOverlayUrl",
                value: "/img/overlays/lentes_graduados_classic_final.png");

            migrationBuilder.Sql("UPDATE Products SET ImagenOverlayUrl = '/img/overlays/obrien_deluxe_final.png' WHERE Nombre LIKE '%O''Brien%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImagenOverlayUrl",
                value: "/img/overlays/rayban_aviator.svg");

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "ImagenOverlayUrl",
                value: "/img/overlays/classic.svg");

            migrationBuilder.Sql("UPDATE Products SET ImagenOverlayUrl = NULL WHERE Nombre LIKE '%O''Brien%';");
        }
    }
}
