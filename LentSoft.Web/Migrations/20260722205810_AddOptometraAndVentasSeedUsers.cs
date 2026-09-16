using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LentSoft.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddOptometraAndVentasSeedUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "FechaRegistro", "Nombre", "PasswordHash", "Role", "Telefono", "UltimaCompra" },
                values: new object[,]
                {
                    { 3, "optometra@lentsoft.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dra. María García", "$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq", "optometra", null, null },
                    { 4, "ventas@lentsoft.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Juan Pérez (Ventas)", "$2a$11$MJPUqK7jAM6tEvUkExo1cO/3cmh4MpxnXNVPg./4kKzlsqAwPW/oq", "ventas", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
