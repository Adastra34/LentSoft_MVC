using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOverlayAdjustmentFieldsToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EscalaOverlay",
                table: "Products",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 2.30m);

            migrationBuilder.AddColumn<decimal>(
                name: "OffsetXOverlay",
                table: "Products",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "OffsetYOverlay",
                table: "Products",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0.12m);

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EscalaOverlay", "OffsetXOverlay", "OffsetYOverlay" },
                values: new object[] { 2.30m, 0.00m, 0.12m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "EscalaOverlay", "OffsetXOverlay", "OffsetYOverlay" },
                values: new object[] { 2.30m, 0.00m, 0.12m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "EscalaOverlay", "OffsetXOverlay", "OffsetYOverlay" },
                values: new object[] { 2.30m, 0.00m, 0.12m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "EscalaOverlay", "OffsetXOverlay", "OffsetYOverlay" },
                values: new object[] { 2.30m, 0.00m, 0.12m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "EscalaOverlay", "OffsetXOverlay", "OffsetYOverlay" },
                values: new object[] { 2.30m, 0.00m, 0.12m });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "EscalaOverlay", "OffsetXOverlay", "OffsetYOverlay" },
                values: new object[] { 2.30m, 0.00m, 0.12m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EscalaOverlay",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OffsetXOverlay",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OffsetYOverlay",
                table: "Products");
        }
    }
}
