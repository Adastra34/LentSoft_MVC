using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdentityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Apellido",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento",
                table: "Users",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "CC");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Apellido", "NumeroDocumento", "TipoDocumento" },
                values: new object[] { "Sistema", "1000000001", "CC" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Apellido", "Nombre", "NumeroDocumento", "TipoDocumento" },
                values: new object[] { "Demo", "Usuario", "1000000002", "CC" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Apellido", "Nombre", "NumeroDocumento", "TipoDocumento" },
                values: new object[] { "García", "María", "1000000003", "CC" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Apellido", "Nombre", "NumeroDocumento", "TipoDocumento" },
                values: new object[] { "Pérez", "Juan", "1000000004", "CC" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_NumeroDocumento",
                table: "Users",
                column: "NumeroDocumento",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_NumeroDocumento",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Apellido",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NumeroDocumento",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "Nombre",
                value: "Usuario Demo");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "Nombre",
                value: "Dra. María García");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4,
                column: "Nombre",
                value: "Juan Pérez (Ventas)");
        }
    }
}
