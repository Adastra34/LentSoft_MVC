using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddDatosRefraccionAHistorialClinico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CilindroOD",
                table: "HistorialesClinicos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CilindroOI",
                table: "HistorialesClinicos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EjeOD",
                table: "HistorialesClinicos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EjeOI",
                table: "HistorialesClinicos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EsferaOD",
                table: "HistorialesClinicos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EsferaOI",
                table: "HistorialesClinicos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CilindroOD",
                table: "HistorialesClinicos");

            migrationBuilder.DropColumn(
                name: "CilindroOI",
                table: "HistorialesClinicos");

            migrationBuilder.DropColumn(
                name: "EjeOD",
                table: "HistorialesClinicos");

            migrationBuilder.DropColumn(
                name: "EjeOI",
                table: "HistorialesClinicos");

            migrationBuilder.DropColumn(
                name: "EsferaOD",
                table: "HistorialesClinicos");

            migrationBuilder.DropColumn(
                name: "EsferaOI",
                table: "HistorialesClinicos");
        }
    }
}
